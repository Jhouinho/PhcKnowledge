using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;

namespace PhcKnowledgeScanner.Tests;

public sealed class CoverageSemanticsTests
{
    [Fact]
    public void Unknown_Completeness_Is_Partial_Even_When_No_Rows_Are_Visible()
    {
        var capability = new DeclaredCapability(
            "sql.server.structure",
            "sql.server.structure.triggers",
            "2.0",
            "sql.server.structure",
            true);
        var issue = new CollectionIssue(
            CollectionIssueSeverity.Warning,
            "sql.trigger.metadata_visibility_unverified",
            "Metadata visibility could not be verified.",
            IssueId: "issue:triggers:1",
            OperationId: capability.OperationId);
        var scope = new Dictionary<string, string>
        {
            ["completeness"] = "unknown",
            ["metadataVisibility"] = "not_verified"
        };

        var result = CoverageSemantics.FromIssues(
            capability,
            requested: true,
            observedCount: 0,
            [issue],
            scope: scope);

        Assert.Equal(OperationExecutionResult.Partial, result.Result);
        Assert.Equal(ContentAvailability.Partial, result.ContentAvailability);
        Assert.Equal(0, result.ObservedCount);
        Assert.Contains(issue.IssueId!, result.IssueIds);
    }

    [Fact]
    public void Verified_Completeness_With_Warning_Remains_Succeeded_With_Warnings()
    {
        var capability = new DeclaredCapability(
            "sql.server.structure",
            "sql.server.structure.triggers",
            "2.0",
            "sql.server.structure",
            true);
        var issue = new CollectionIssue(
            CollectionIssueSeverity.Warning,
            "sql.trigger.definition_missing",
            "A trigger definition was unavailable.");
        var scope = new Dictionary<string, string> { ["completeness"] = "verified" };

        var result = CoverageSemantics.FromIssues(
            capability,
            requested: true,
            observedCount: 2,
            [issue],
            scope: scope);

        Assert.Equal(OperationExecutionResult.SucceededWithWarnings, result.Result);
        Assert.Equal(ContentAvailability.Available, result.ContentAvailability);
    }
}
