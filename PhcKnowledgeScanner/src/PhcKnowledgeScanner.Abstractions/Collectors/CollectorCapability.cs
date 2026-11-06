namespace PhcKnowledgeScanner.Abstractions.Collectors;

public sealed record CollectorCapability(
    bool IsAvailable,
    string? ReasonUnavailable = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
