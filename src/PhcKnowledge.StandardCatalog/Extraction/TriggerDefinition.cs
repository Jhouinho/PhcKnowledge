namespace PhcKnowledge.StandardCatalog.Extraction;

/// <summary>
/// One parsed trigger definition. Source offsets are zero-based UTF-16 character
/// offsets into the original decoded SQL source.
/// </summary>
public sealed record TriggerDefinition(
    string Name,
    string? Schema,
    string TargetTable,
    string? TargetSchema,
    IReadOnlyList<string> Events,
    string Timing,
    bool IsEncrypted,
    string DefinitionSql,
    string DefinitionFingerprint,
    int SourceStartOffset,
    int SourceEndOffsetExclusive,
    int SourceStartLine,
    int SourceStartColumn,
    bool? AnsiNulls,
    bool? QuotedIdentifier);
