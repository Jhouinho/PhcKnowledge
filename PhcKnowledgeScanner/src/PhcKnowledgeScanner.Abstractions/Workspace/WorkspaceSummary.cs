namespace PhcKnowledgeScanner.Abstractions.Workspace;

public sealed record WorkspaceSummary(
    string ScanId,
    string InstallationLabel,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyDictionary<string, int> ObjectCounts,
    int IssueCount,
    int WarningCount,
    int ErrorCount,
    int RelationCount);
