namespace PhcKnowledgeScanner.Abstractions.Coverage;

/// <summary>Cobertura de uma operação lógica numa captura (IKP coverage.operations[]).</summary>
public sealed record OperationCoverage(
    string OperationId,
    string CapabilityVersion,
    bool Requested,
    OperationExecutionResult Result,
    SourceAvailability SourceAvailability,
    ContentAvailability ContentAvailability,
    int ObservedCount,
    IReadOnlyList<string> IssueIds,
    IReadOnlyDictionary<string, string>? Scope = null);
