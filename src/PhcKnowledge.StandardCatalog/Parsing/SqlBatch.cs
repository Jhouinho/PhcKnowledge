namespace PhcKnowledge.StandardCatalog.Parsing;

/// <summary>
/// A SQL batch delimited by a standalone GO line. Offsets are zero-based UTF-16
/// character offsets into the original decoded source text.
/// </summary>
public sealed record SqlBatch(
    int Index,
    int SourceStartOffset,
    int SourceEndOffset,
    int SourceStartLine,
    string Text);
