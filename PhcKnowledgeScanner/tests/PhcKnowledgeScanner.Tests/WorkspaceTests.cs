using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public class WorkspaceTests
{
    [Fact]
    public async Task UpsertAndQuery_RoundTripsFactsAndArtifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan1", "local/demo", CancellationToken.None);

        var bytes = "CREATE VIEW dbo.v AS SELECT 1 AS x;"u8.ToArray();
        var artifactId = await workspace.SaveArtifactAsync(
            "sql.server.structure",
            "views/dbo.v.sql",
            bytes,
            "text/sql",
            null,
            CancellationToken.None);

        await workspace.UpsertObjectAsync(new TechnicalObjectRecord(
            "view:dbo.v",
            ObjectKind.View,
            "v",
            "dbo",
            "schema:dbo",
            "sql.server.structure",
            artifactId,
            null,
            DateTimeOffset.UtcNow), CancellationToken.None);

        await workspace.AddIssueAsync("sql.server.structure", new CollectionIssue(
            CollectionIssueSeverity.Warning,
            "test",
            "aviso"), CancellationToken.None);

        var objects = await workspace.GetObjectsAsync(ObjectKind.View);
        Assert.Single(objects);
        var text = await workspace.ReadArtifactTextAsync(artifactId);
        Assert.Contains("CREATE VIEW", text);
        var summary = await workspace.GetSummaryAsync();
        Assert.Equal(1, summary.ObjectCounts[ObjectKind.View]);
        Assert.Equal(1, summary.WarningCount);
    }
}
