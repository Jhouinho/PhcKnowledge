namespace PhcKnowledgeScanner.Abstractions.Collectors;

public sealed record CollectorResult(
    string CollectorId,
    CollectorStatus Status,
    IReadOnlyList<CollectedArtifact> Artifacts,
    IReadOnlyList<CollectionIssue> Issues,
    TimeSpan Duration);
