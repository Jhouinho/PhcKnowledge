using PhcKnowledgeScanner.Abstractions.Coverage;

namespace PhcKnowledgeScanner.Abstractions.Collectors;

public sealed record CollectorResult(
    string CollectorId,
    CollectorStatus Status,
    IReadOnlyList<CollectedArtifact> Artifacts,
    IReadOnlyList<CollectionIssue> Issues,
    TimeSpan Duration,
    IReadOnlyList<OperationCoverage>? OperationCoverages = null)
{
    public IReadOnlyList<OperationCoverage> Coverages =>
        OperationCoverages ?? Array.Empty<OperationCoverage>();
}
