namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>
/// Diagnóstico preservado por evidência (união sem perda).
/// <see cref="DiagnosticsJson"/> é o payload original da interpretação quando aplicável.
/// </summary>
public sealed record EvidenceDiagnostic(
    string EvidenceId,
    RelationEvidenceKind Kind,
    string? DiagnosticsJson);
