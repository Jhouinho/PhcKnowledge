namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>Lado de um predicado: campo, literal ou expressão (texto fiel + FieldRefs contidos).</summary>
public sealed record FieldMappingOperand(
    string Kind,
    string OriginalText,
    MappedFieldRef? Field,
    IReadOnlyList<MappedFieldRef> ContainedFields)
{
    public const string KindField = "field";
    public const string KindLiteral = "literal";
    public const string KindExpression = "expression";
    public const string KindUnsupported = "unsupported";
}
