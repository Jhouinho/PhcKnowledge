namespace PhcKnowledgeScanner.Abstractions.Connection;

public sealed record ConnectionTestResult(
    bool Succeeded,
    string Message,
    string? ServerVersion = null,
    string? DatabaseName = null,
    string? CurrentUser = null);
