using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Analysis;

/// <summary>
/// Inferências de ownership de schema — desativadas (ruído de schemas de sistema SQL).
/// Mantido para não partir DI; a análise útil é a de FK em tabelas custom.
/// </summary>
public sealed class SchemaOwnershipInferenceAnalyzer
{
    public Task<int> AnalyzeAsync(IScanWorkspace workspace, CancellationToken cancellationToken)
    {
        _ = workspace;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(0);
    }
}
