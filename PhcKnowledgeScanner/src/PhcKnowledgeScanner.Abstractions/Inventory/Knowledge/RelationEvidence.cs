using PhcKnowledgeScanner.Abstractions.Inventory.Expression;

namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>
/// Projeção uniforme de uma evidência relacional. Não substitui factos SQLite;
/// <see cref="SourceId"/> preserva a identidade da origem.
/// </summary>
public sealed record RelationEvidence(
    string EvidenceId,
    RelationEvidenceKind Kind,
    string SourceId,
    RelationEndpoint From,
    RelationEndpoint To,
    string? OriginalExpression,
    string? SummaryJson,
    string? LinkedDeclaredRelationId,
    string? ParserVersion,
    ExpressionParseStatus? ParseStatus,
    InterpretationConfidence? Confidence,
    string? DiagnosticsJson,
    DeclaredRelationCorrespondenceKind? CorrespondenceKind,
    string? CorrespondenceGroupId,
    DateTimeOffset? ObservedAtUtc);
