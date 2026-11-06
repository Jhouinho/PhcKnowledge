namespace PhcKnowledgeScanner.Abstractions.Inventory.Expression;

/// <summary>
/// Resultado da interpretação de uma expressão declarada.
/// Independente do facto <see cref="DeclaredRelationRecord"/>; referencia o <c>relation_id</c> estável.
/// </summary>
public sealed record DeclaredRelationInterpretationRecord(
    string RelationId,
    string ParserVersion,
    ExpressionParseStatus ParseStatus,
    InterpretationConfidence Confidence,
    ExprAstNode? Ast,
    string? DiagnosticsJson,
    int FieldRefCount,
    int TableRefsResolved,
    int TableRefsAmbiguous,
    int TableRefsUnresolved,
    DateTimeOffset InterpretedAtUtc)
{
    /// <summary>Referências com qualificador (= resolved + ambiguous + unresolved).</summary>
    public int QualifiedFieldRefCount =>
        TableRefsResolved + TableRefsAmbiguous + TableRefsUnresolved;

    /// <summary>Referências sem qualificador (não entram no denominador de resolução de tabelas).</summary>
    public int UnqualifiedFieldRefCount =>
        Math.Max(0, FieldRefCount - QualifiedFieldRefCount);
}

/// <summary>Agregados de uma passagem de interpretação.</summary>
public sealed record DeclaredRelationInterpretationMetrics(
    int Analyzed,
    int Parsed,
    int PartiallyParsed,
    int Unsupported,
    int Invalid,
    int Empty,
    int FieldRefsIdentified,
    int QualifiedFieldRefs,
    int UnqualifiedFieldRefs,
    int TableRefsResolved,
    int TableRefsAmbiguous,
    int TableRefsUnresolved,
    IReadOnlyList<string> IncompleteRelationIds);
