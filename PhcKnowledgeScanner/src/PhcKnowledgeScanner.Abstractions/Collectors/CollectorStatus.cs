namespace PhcKnowledgeScanner.Abstractions.Collectors;

public enum CollectorStatus
{
    Succeeded = 0,
    SucceededWithWarnings = 1,
    Skipped = 2,
    Failed = 3,
    Partial = 4
}
