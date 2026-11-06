using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public class TableInventoryTests
{
    [Fact]
    public void Classification_Distinguishes_System_Custom_And_Unknown()
    {
        Assert.Equal(
            TableInventoryClassification.SqlSystem,
            TableInventoryClassification.Classify(isMsShipped: true, "anything"));
        Assert.Equal(
            TableInventoryClassification.CandidateCustom,
            TableInventoryClassification.Classify(isMsShipped: false, "u_meuscampos"));
        Assert.Equal(
            TableInventoryClassification.ApplicationOrUnknown,
            TableInventoryClassification.Classify(isMsShipped: false, "bo"));
        Assert.Equal(
            TableInventoryClassification.ApplicationOrUnknown,
            TableInventoryClassification.Classify(isMsShipped: false, "custom_no_u_prefix"));
    }

    [Fact]
    public async Task Collects_U_Prefix_And_Standard_Tables_Into_Workspace()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var now = DateTimeOffset.UtcNow;

        await workspace.UpsertObjectsAsync(
            [
                BuildTable("dbo", "u_extra", candidateCustom: true, now),
                BuildTable("dbo", "bo", candidateCustom: false, now),
                BuildTable("dbo", "cl", candidateCustom: false, now)
            ],
            CancellationToken.None);

        var tables = await workspace.GetObjectsAsync(ObjectKind.Table);
        Assert.Equal(3, tables.Count);
        Assert.Contains(tables, t => t.ObjectId == "table:dbo.u_extra");
        Assert.Contains(tables, t => t.ObjectId == "table:dbo.bo");

        var bo = tables.Single(t => t.Name == "bo");
        using var doc = JsonDocument.Parse(bo.SummaryJson!);
        Assert.Equal(
            TableInventoryClassification.ApplicationOrUnknown,
            doc.RootElement.GetProperty("classification").GetString());
    }

    [Fact]
    public async Task Same_Name_Different_Schemas_Have_Distinct_Stable_Ids()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var now = DateTimeOffset.UtcNow;

        await workspace.UpsertObjectsAsync(
            [
                BuildTable("dbo", "orders", false, now),
                BuildTable("sales", "orders", false, now)
            ],
            CancellationToken.None);

        var tables = await workspace.GetObjectsAsync(ObjectKind.Table);
        Assert.Equal(2, tables.Count);
        Assert.Contains(tables, t => t.ObjectId == "table:dbo.orders");
        Assert.Contains(tables, t => t.ObjectId == "table:sales.orders");
    }

    [Fact]
    public async Task Object_Ids_Are_Stable_Across_Two_Collections_Without_Duplication()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var t1 = DateTimeOffset.UtcNow;
        var t2 = t1.AddMinutes(5);

        var first = BuildTable("dbo", "st", false, t1);
        var second = BuildTable("dbo", "st", false, t2) with
        {
            SummaryJson = """{"objectId":99,"classification":"application_or_unknown","isMsShipped":false}"""
        };

        await workspace.UpsertObjectsAsync([first], CancellationToken.None);
        await workspace.UpsertObjectsAsync([second], CancellationToken.None);

        var tables = await workspace.GetObjectsAsync(ObjectKind.Table);
        Assert.Single(tables);
        Assert.Equal("table:dbo.st", tables[0].ObjectId);
        Assert.Equal(t2, tables[0].CollectedAtUtc);
        Assert.Contains("objectId\":99", tables[0].SummaryJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolver_Unambiguous_Bare_And_Qualified_Names()
    {
        var tables = new[]
        {
            BuildTable("dbo", "bo", false, DateTimeOffset.UtcNow),
            BuildTable("dbo", "cl", false, DateTimeOffset.UtcNow)
        };
        var index = TableReferenceResolver.BuildNameIndex(tables);
        var ids = tables.Select(t => t.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var bare = TableReferenceResolver.Resolve("bo", index, ids);
        Assert.Equal(TableReferenceResolution.Resolved, bare.Resolution);
        Assert.Equal("table:dbo.bo", bare.ObjectId);

        var qualified = TableReferenceResolver.Resolve("dbo.cl", index, ids);
        Assert.Equal(TableReferenceResolution.Resolved, qualified.Resolution);
        Assert.Equal("table:dbo.cl", qualified.ObjectId);
    }

    [Fact]
    public void Resolver_Ambiguous_When_Same_Name_In_Multiple_Schemas()
    {
        var tables = new[]
        {
            BuildTable("dbo", "orders", false, DateTimeOffset.UtcNow),
            BuildTable("sales", "orders", false, DateTimeOffset.UtcNow)
        };
        var index = TableReferenceResolver.BuildNameIndex(tables);

        var result = TableReferenceResolver.Resolve("orders", index);
        Assert.Equal(TableReferenceResolution.Ambiguous, result.Resolution);
        Assert.Null(result.ObjectId);

        var qualified = TableReferenceResolver.Resolve("sales.orders", index);
        Assert.Equal(TableReferenceResolution.Resolved, qualified.Resolution);
        Assert.Equal("table:sales.orders", qualified.ObjectId);
    }

    [Fact]
    public void Resolver_Unresolved_When_Missing()
    {
        var index = TableReferenceResolver.BuildNameIndex(
            [BuildTable("dbo", "bo", false, DateTimeOffset.UtcNow)]);

        var result = TableReferenceResolver.Resolve("noexiste", index);
        Assert.Equal(TableReferenceResolution.Unresolved, result.Resolution);
        Assert.Null(result.ObjectId);

        var missingQualified = TableReferenceResolver.Resolve("dbo.ghost", index);
        Assert.Equal(TableReferenceResolution.Unresolved, missingQualified.Resolution);
    }

    [Fact]
    public async Task Declared_Relations_Resolve_Against_Full_Inventory()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var now = DateTimeOffset.UtcNow;
        await workspace.UpsertObjectsAsync(
            [
                BuildTable("dbo", "bo", false, now),
                BuildTable("dbo", "cl", false, now),
                BuildTable("dbo", "u_link", true, now)
            ],
            CancellationToken.None);

        var tables = await workspace.GetObjectsAsync(ObjectKind.Table);
        var index = TableReferenceResolver.BuildNameIndex(tables);
        var ids = tables.Select(t => t.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var from = TableReferenceResolver.Resolve("bo", index, ids);
        var to = TableReferenceResolver.Resolve("cl", index, ids);
        var custom = TableReferenceResolver.Resolve("u_link", index, ids);

        var record = DeclaredRelationsTests.SampleRel("X1", "bo", "cl", "bo.no=cl.no") with
        {
            FromObjectId = from.ObjectId,
            ToObjectId = to.ObjectId,
            FromTableResolution = from.Resolution,
            ToTableResolution = to.Resolution
        };

        await workspace.UpsertDeclaredRelationsAsync([record], CancellationToken.None);
        var loaded = Assert.Single(await workspace.GetDeclaredRelationsAsync());
        Assert.True(loaded.FromTableResolved);
        Assert.True(loaded.ToTableResolved);
        Assert.Equal("table:dbo.bo", loaded.FromObjectId);
        Assert.Equal("table:dbo.cl", loaded.ToObjectId);
        Assert.Equal(TableReferenceResolution.Resolved, custom.Resolution);
    }

    [Fact]
    public async Task Declared_Relations_Preserve_Original_When_Ambiguous_Or_Unresolved()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var now = DateTimeOffset.UtcNow;
        await workspace.UpsertObjectsAsync(
            [
                BuildTable("dbo", "orders", false, now),
                BuildTable("app", "orders", false, now)
            ],
            CancellationToken.None);

        var tables = await workspace.GetObjectsAsync(ObjectKind.Table);
        var index = TableReferenceResolver.BuildNameIndex(tables);

        var from = TableReferenceResolver.Resolve("orders", index);
        var to = TableReferenceResolver.Resolve("missing", index);
        Assert.Equal(TableReferenceResolution.Ambiguous, from.Resolution);
        Assert.Equal(TableReferenceResolution.Unresolved, to.Resolution);

        var expr = "orders.id=missing.id";
        var record = DeclaredRelationsTests.SampleRel("AMB1", "orders", "missing", expr) with
        {
            FromObjectId = from.ObjectId,
            ToObjectId = to.ObjectId,
            FromTableResolution = from.Resolution,
            ToTableResolution = to.Resolution
        };

        await workspace.UpsertDeclaredRelationsAsync([record], CancellationToken.None);
        var loaded = Assert.Single(await workspace.GetDeclaredRelationsAsync());
        Assert.Equal(expr, loaded.OriginalExpression);
        Assert.Equal("orders", loaded.FromTable);
        Assert.Equal("missing", loaded.ToTable);
        Assert.Null(loaded.FromObjectId);
        Assert.Null(loaded.ToObjectId);
        Assert.Equal(TableReferenceResolution.Ambiguous, loaded.FromTableResolution);
        Assert.Equal(TableReferenceResolution.Unresolved, loaded.ToTableResolution);
    }

    [Fact]
    public void Catalog_Failure_Issue_Is_Error_Not_Empty_Success()
    {
        // Espelha o código/severidade usados em CollectAllTablesAsync perante falha de catálogo.
        var issue = new CollectionIssue(
            CollectionIssueSeverity.Error,
            "sql.tables.catalog",
            "Não foi possível consultar o inventário de tabelas (sys.tables/sys.schemas). Possível falta de permissões.",
            Detail: "UnauthorizedAccessException: denied");

        Assert.Equal(CollectionIssueSeverity.Error, issue.Severity);
        Assert.Equal("sql.tables.catalog", issue.Code);
        Assert.Contains("permissões", issue.Message, StringComparison.OrdinalIgnoreCase);

        var empty = new CollectionIssue(
            CollectionIssueSeverity.Warning,
            "sql.tables.catalog_empty",
            "A consulta a sys.tables/sys.schemas devolveu 0 tabelas. Confirmar permissões VIEW DEFINITION / acesso ao catálogo.");
        Assert.Equal(CollectionIssueSeverity.Warning, empty.Severity);
        Assert.NotEqual(issue.Code, empty.Code);
    }

    [Fact]
    public async Task GetObjects_By_Kind_Without_Search_Returns_All_Tables()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var now = DateTimeOffset.UtcNow;
        var batch = Enumerable.Range(0, 120)
            .Select(i => BuildTable("dbo", $"t{i}", false, now))
            .ToList();
        await workspace.UpsertObjectsAsync(batch, CancellationToken.None);

        var all = await workspace.GetObjectsAsync(ObjectKind.Table);
        Assert.Equal(120, all.Count);
    }

    private static async Task<SqliteScanWorkspace> CreateWorkspaceAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-tables", "local/demo", CancellationToken.None);
        return workspace;
    }

    private static TechnicalObjectRecord BuildTable(
        string schema,
        string name,
        bool candidateCustom,
        DateTimeOffset collectedAt)
    {
        var classification = TableInventoryClassification.Classify(false, name);
        var summary = JsonSerializer.Serialize(new
        {
            objectId = 1,
            typeDesc = "USER_TABLE",
            isMsShipped = false,
            classification,
            namePrefixU = candidateCustom || TableInventoryClassification.IsCandidateCustomName(name),
            createDate = (string?)null,
            modifyDate = (string?)null
        });

        return new TechnicalObjectRecord(
            TableInventoryClassification.BuildObjectId(schema, name),
            ObjectKind.Table,
            name,
            schema,
            null,
            "sql.server.structure",
            null,
            summary,
            collectedAt);
    }
}
