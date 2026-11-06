namespace PhcKnowledgeScanner.Abstractions.Collectors;

public sealed record CollectionIssue(
    CollectionIssueSeverity Severity,
    string Code,
    string Message,
    string? ObjectName = null,
    string? Detail = null,
    string? IssueId = null,
    string? OperationId = null);
