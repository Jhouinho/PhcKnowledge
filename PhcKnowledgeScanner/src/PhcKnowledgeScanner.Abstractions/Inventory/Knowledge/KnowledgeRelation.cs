namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>
/// Relação consolidada (projeção). Evidências de interpretação ficam em
/// <see cref="InterpretationEvidenceIds"/> — nunca originam KR independentes.
/// </summary>
public sealed record KnowledgeRelation(
    string KnowledgeRelationId,
    RelationEndpoint From,
    RelationEndpoint To,
    EndpointResolutionState EndpointResolutionState,
    KnowledgeConsolidationBasis ConsolidationBasis,
    IReadOnlyList<string> LinkEvidenceIds,
    IReadOnlyList<string> InterpretationEvidenceIds,
    string? ExactCorrespondenceGroupId,
    string? ProbableCorrespondenceGroupId,
    IReadOnlyList<EvidenceDiagnostic> Diagnostics);
