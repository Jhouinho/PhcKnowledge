namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>Referência a campo preservada a partir da AST (sem inferir PK/FK).</summary>
public sealed record MappedFieldRef(
    string OriginalText,
    string? Qualifier,
    string FieldName,
    TableReferenceResolution TableResolution,
    string? TableObjectId);
