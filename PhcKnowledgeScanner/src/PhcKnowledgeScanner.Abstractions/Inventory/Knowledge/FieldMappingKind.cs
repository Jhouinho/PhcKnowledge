namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>Classificação de um predicado extraído da AST (sem inferência de chaves).</summary>
public enum FieldMappingKind
{
    /// <summary>Comparação <c>=</c> entre dois <see cref="Expression.FieldRefExprNode"/>.</summary>
    DirectFieldEquality = 0,

    /// <summary>
    /// Comparação <c>=</c> envolvendo função/expressão binária (não igualdade directa campo=campo).
    /// </summary>
    ExpressionComparison = 1,

    /// <summary>
    /// Predicado que não é correspondência directa entre dois campos
    /// (literal, operadores ≠ <c>=</c>, etc.).
    /// </summary>
    NonFieldPredicate = 2
}
