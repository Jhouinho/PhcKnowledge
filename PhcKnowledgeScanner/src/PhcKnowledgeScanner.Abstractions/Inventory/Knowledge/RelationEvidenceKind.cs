namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>Origem semântica de uma evidência relacional (não misturar tipos).</summary>
public enum RelationEvidenceKind
{
    SqlForeignKey = 0,
    PhcRel = 1,
    PhcDtl = 2,
    ExpressionInterpretation = 3
}
