namespace PhcKnowledgeScanner.Abstractions.Workspace;

/// <summary>Result of SQLite PRAGMA integrity_check.</summary>
public sealed record WorkspaceIntegrityResult(
    bool IsValid,
    IReadOnlyList<string> Messages);
