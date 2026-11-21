namespace PhcKnowledge.StandardCatalog.Parsing;

public sealed record SqlParseDiagnostic(
    string Code,
    string Message,
    int Line,
    int Column);

public sealed class SqlBatchParseException : Exception
{
    public SqlBatchParseException(
        string message,
        IReadOnlyList<SqlParseDiagnostic> diagnostics)
        : base(message)
    {
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<SqlParseDiagnostic> Diagnostics { get; }
}
