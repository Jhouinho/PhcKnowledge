using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Analysis;
using PhcKnowledgeScanner.Collectors.Sql;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public class SqlKeyInventoryTests
{
    [Fact]
    public void PrimaryKeysSql_And_ForeignKeysSql_Do_Not_Filter_U_Prefix()
    {
        Assert.DoesNotContain("u[_]", SqlKeyInventory.PrimaryKeysSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("u[_]", SqlKeyInventory.ForeignKeysSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LIKE N'u", SqlKeyInventory.PrimaryKeysSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LIKE N'u", SqlKeyInventory.ForeignKeysSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrimaryKey_Ids_Are_Deterministic_For_Standard_And_Custom_Tables()
    {
        var now = DateTimeOffset.UtcNow;
        var standard = SqlKeyInventory.CreatePrimaryKey(
            "dbo", "bo", "PK_bo", 1, [("bostamp", 1)], now);
        var custom = SqlKeyInventory.CreatePrimaryKey(
            "dbo", "u_extra", "PK_u_extra", 1, [("ustamp", 1)], now);

        Assert.Equal("pk:dbo.bo.PK_bo", standard.ObjectId);
        Assert.Equal("pk:dbo.u_extra.PK_u_extra", custom.ObjectId);
        Assert.Equal("table:dbo.bo", standard.ParentObjectId);
        Assert.Equal("table:dbo.u_extra", custom.ParentObjectId);
        Assert.Equal(ObjectKind.PrimaryKey, standard.Kind);
        Assert.Equal("dbo", standard.SchemaName);
    }

    [Fact]
    public void PrimaryKey_Preserves_Composite_Column_Order()
    {
        var pk = SqlKeyInventory.CreatePrimaryKey(
            "dbo",
            "bo",
            "PK_bo_comp",
            2,
            [("ano", 2), ("bostamp", 1), ("ndoc", 3)],
            DateTimeOffset.UtcNow);

        using var doc = JsonDocument.Parse(pk.SummaryJson!);
        var cols = doc.RootElement.GetProperty("columns").EnumerateArray().ToList();
        // Materializer serializa a lista recebida; o collector ordena antes de chamar.
        Assert.Equal(3, cols.Count);
        Assert.Equal("ano", cols[0].GetProperty("name").GetString());
        Assert.Equal(2, cols[0].GetProperty("ordinal").GetInt32());
    }

    [Fact]
    public void ForeignKey_Between_Standard_Tables_Preserves_Schemas_And_Ordinals()
    {
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo",
            "bi",
            "FK_bi_bo",
            "dbo",
            "bo",
            [("bostamp", "bostamp", 1), ("ndoc", "ndoc", 2)],
            DateTimeOffset.UtcNow,
            deleteReferentialAction: "NO_ACTION",
            updateReferentialAction: "CASCADE");

        Assert.Equal("fk:dbo.bi.FK_bi_bo", fk.ObjectId);
        Assert.Equal("table:dbo.bi", fk.ParentObjectId);
        Assert.Equal("dbo", fk.SchemaName);

        using var doc = JsonDocument.Parse(fk.SummaryJson!);
        Assert.Equal("dbo", doc.RootElement.GetProperty("referencedSchema").GetString());
        Assert.Equal("bo", doc.RootElement.GetProperty("referencedTable").GetString());
        Assert.Equal("NO_ACTION", doc.RootElement.GetProperty("deleteReferentialAction").GetString());
        Assert.Equal("CASCADE", doc.RootElement.GetProperty("updateReferentialAction").GetString());

        var cols = doc.RootElement.GetProperty("columns").EnumerateArray().ToList();
        Assert.Equal(2, cols.Count);
        Assert.Equal("bostamp", cols[0].GetProperty("parent").GetString());
        Assert.Equal(1, cols[0].GetProperty("ordinal").GetInt32());
        Assert.Equal("ndoc", cols[1].GetProperty("parent").GetString());
        Assert.Equal(2, cols[1].GetProperty("ordinal").GetInt32());
    }

    [Fact]
    public void ForeignKey_Involving_U_Table_Uses_Stable_Id()
    {
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo",
            "u_lines",
            "FK_u_lines_bo",
            "dbo",
            "bo",
            [("bostamp", "bostamp", 1)],
            DateTimeOffset.UtcNow);

        Assert.Equal("fk:dbo.u_lines.FK_u_lines_bo", fk.ObjectId);
        Assert.Equal("table:dbo.u_lines", fk.ParentObjectId);
    }

    [Fact]
    public async Task Upsert_Pk_And_Fk_Is_Idempotent_Without_Duplicates()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var now = DateTimeOffset.UtcNow;
        var pk = SqlKeyInventory.CreatePrimaryKey("dbo", "bo", "PK_bo", 1, [("bostamp", 1)], now);
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo", "bi", "FK_bi_bo", "dbo", "bo", [("bostamp", "bostamp", 1)], now);

        await workspace.UpsertObjectsAsync([pk, fk], CancellationToken.None);
        await workspace.UpsertObjectsAsync(
            [
                pk with { CollectedAtUtc = now.AddMinutes(1) },
                fk with { CollectedAtUtc = now.AddMinutes(1) }
            ],
            CancellationToken.None);

        var pks = await workspace.GetObjectsAsync(ObjectKind.PrimaryKey);
        var fks = await workspace.GetObjectsAsync(ObjectKind.ForeignKey);
        Assert.Single(pks);
        Assert.Single(fks);
        Assert.Equal(pk.ObjectId, pks[0].ObjectId);
        Assert.Equal(fk.ObjectId, fks[0].ObjectId);
    }

    [Fact]
    public async Task Existing_Workspace_Schema_Opens_And_Reads_Legacy_And_New_Summaries()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var now = DateTimeOffset.UtcNow;

        // Forma antiga (só uniqueIndexId) — workspaces pré-1a.
        await workspace.UpsertObjectAsync(new TechnicalObjectRecord(
            "pk:dbo.u_old.PK_u_old",
            ObjectKind.PrimaryKey,
            "PK_u_old",
            "dbo",
            "table:dbo.u_old",
            SqlServerStructureCollector.CollectorId,
            null,
            """{"uniqueIndexId":1}""",
            now), CancellationToken.None);

        var enriched = SqlKeyInventory.CreatePrimaryKey(
            "dbo", "bo", "PK_bo", 1, [("bostamp", 1)], now);
        await workspace.UpsertObjectAsync(enriched, CancellationToken.None);

        var pks = await workspace.GetObjectsAsync(ObjectKind.PrimaryKey);
        Assert.Equal(2, pks.Count);
        Assert.Contains(pks, p => p.ObjectId == "pk:dbo.u_old.PK_u_old");
        Assert.Contains(pks, p => p.ObjectId == "pk:dbo.bo.PK_bo");
    }

    [Fact]
    public async Task ForeignKeyAnalyzer_Handles_Zero_Fks_Without_Error()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var analyzer = new ForeignKeyRelationAnalyzer();
        var count = await analyzer.AnalyzeAsync(workspace, CancellationToken.None);
        Assert.Equal(0, count);
        Assert.Empty(await workspace.GetRelationsAsync());
    }

    [Fact]
    public async Task ForeignKeyAnalyzer_Creates_Derived_Relation_With_Stable_Id()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo",
            "bi",
            "FK_bi_bo",
            "dbo",
            "bo",
            [("bostamp", "bostamp", 1), ("obrig", "obrig", 2)],
            DateTimeOffset.UtcNow,
            "NO_ACTION",
            "NO_ACTION");
        await workspace.UpsertObjectAsync(fk, CancellationToken.None);

        var analyzer = new ForeignKeyRelationAnalyzer();
        Assert.Equal(1, await analyzer.AnalyzeAsync(workspace, CancellationToken.None));

        var relations = await workspace.GetRelationsAsync();
        Assert.Single(relations);
        Assert.Equal(SqlKeyInventory.BuildDerivedForeignKeyRelationId(fk.ObjectId), relations[0].RelationId);
        Assert.Equal("rel:fk:fk:dbo.bi.FK_bi_bo", relations[0].RelationId);
        Assert.Equal("table:dbo.bi", relations[0].FromObjectId);
        Assert.Equal("table:dbo.bo", relations[0].ToObjectId);
        Assert.Equal(KnowledgeLayer.Relation, relations[0].Layer);
        Assert.Equal("sql.foreign_key", relations[0].RelationType);
    }

    [Fact]
    public async Task ForeignKeyAnalyzer_Does_Not_Duplicate_On_Rerun()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo", "child", "FK_child_parent", "dbo", "parent",
            [("ParentId", "Id", 1)], DateTimeOffset.UtcNow);
        await workspace.UpsertObjectAsync(fk, CancellationToken.None);

        var analyzer = new ForeignKeyRelationAnalyzer();
        await analyzer.AnalyzeAsync(workspace, CancellationToken.None);
        await analyzer.AnalyzeAsync(workspace, CancellationToken.None);

        Assert.Single(await workspace.GetRelationsAsync());
    }

    private static async Task<SqliteScanWorkspace> CreateWorkspaceAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-keys", "local/keys", CancellationToken.None);
        return workspace;
    }
}
