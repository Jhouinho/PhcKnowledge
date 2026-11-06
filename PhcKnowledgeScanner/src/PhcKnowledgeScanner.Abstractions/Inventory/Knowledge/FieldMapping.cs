namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>
/// Correspondência/predicado extraído da AST. Origem obrigatória: <see cref="EvidenceId"/> (REL/DTL).
/// <see cref="KnowledgeRelationId"/> é navegação opcional — não substitui a evidência.
/// </summary>
public sealed record FieldMapping(
    string FieldMappingId,
    string EvidenceId,
    string? KnowledgeRelationId,
    string? InterpretationEvidenceId,
    string? ParserVersion,
    FieldMappingKind Kind,
    string Operator,
    string PredicateText,
    string AstPath,
    int PredicateOrdinal,
    FieldMappingOperand Left,
    FieldMappingOperand Right,
    FieldMappingLogicalContext LogicalContext,
    string LogicalPath,
    string? DiagnosticsJson);
