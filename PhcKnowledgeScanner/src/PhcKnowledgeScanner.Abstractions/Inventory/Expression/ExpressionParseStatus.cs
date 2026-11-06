namespace PhcKnowledgeScanner.Abstractions.Inventory.Expression;

/// <summary>Estado sintáctico do parsing — não implica validação semântica da relação.</summary>
public enum ExpressionParseStatus
{
    Parsed = 0,
    PartiallyParsed = 1,
    Unsupported = 2,
    Invalid = 3,
    Empty = 4
}
