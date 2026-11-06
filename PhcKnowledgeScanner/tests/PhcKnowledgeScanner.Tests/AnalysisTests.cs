using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Analysis;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public class AnalysisTests
{
    [Fact]
    public async Task ForeignKeyAnalyzer_CreatesRelationLayerNotInference()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan2", "local/demo", CancellationToken.None);

        var summary = JsonSerializer.Serialize(new
        {
            referencedSchema = "dbo",
            referencedTable = "Parent",
            columns = new[] { new { parent = "ParentId", referenced = "Id" } }
        });

        await workspace.UpsertObjectAsync(new TechnicalObjectRecord(
            "fk:dbo.Child.FK_Child_Parent",
            ObjectKind.ForeignKey,
            "FK_Child_Parent",
            "dbo",
            "table:dbo.Child",
            "sql.server.structure",
            null,
            summary,
            DateTimeOffset.UtcNow), CancellationToken.None);

        var analyzer = new ForeignKeyRelationAnalyzer();
        var count = await analyzer.AnalyzeAsync(workspace, CancellationToken.None);
        Assert.Equal(1, count);

        var relations = await workspace.GetRelationsAsync();
        Assert.Single(relations);
        Assert.Equal(KnowledgeLayer.Relation, relations[0].Layer);
        Assert.Equal("table:dbo.Child", relations[0].FromObjectId);
        Assert.Equal("table:dbo.Parent", relations[0].ToObjectId);
    }
}
