namespace PhcKnowledgeScanner.Abstractions.Coverage;

/// <summary>Resultado da execução de uma operação lógica (IKP coverage.result).</summary>
public enum OperationExecutionResult
{
    Succeeded = 0,
    SucceededWithWarnings = 1,
    Partial = 2,
    Failed = 3,
    Skipped = 4,
    NotAttempted = 5
}
