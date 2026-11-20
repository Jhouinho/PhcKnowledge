using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace PhcKnowledge.StandardCatalog.Parsing;

/// <summary>
/// Splits T-SQL source on lexer-recognised GO batch separators. It deliberately
/// does not use string splitting, so GO inside strings and comments is ignored.
/// </summary>
public sealed class SqlBatchParser
{
    public IReadOnlyList<SqlBatch> Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var parser = new TSql160Parser(initialQuotedIdentifiers: true);
        var tokens = parser.GetTokenStream(new StringReader(source), out var errors);
        if (errors.Count > 0)
        {
            throw CreateException("The SQL source could not be tokenized.", errors, source, 0);
        }

        var batches = new List<SqlBatch>();
        var segmentStart = 0;

        foreach (var token in tokens.Where(x => x.TokenType == TSqlTokenType.Go))
        {
            var separatorLineStart = FindLineStart(source, token.Offset);
            AddBatch(batches, source, segmentStart, separatorLineStart);

            var lineEnd = FindLineEnd(source, token.Offset);
            segmentStart = SkipLineBreak(source, lineEnd);
        }

        AddBatch(batches, source, segmentStart, source.Length);
        return batches;
    }

    private static void AddBatch(
        ICollection<SqlBatch> batches,
        string source,
        int start,
        int end)
    {
        if (end <= start)
            return;

        var text = source[start..end];
        if (string.IsNullOrWhiteSpace(text))
            return;

        batches.Add(new SqlBatch(
            batches.Count,
            start,
            end,
            GetLineAndColumn(source, start).Line,
            text));
    }

    private static int FindLineStart(string source, int offset)
    {
        var index = Math.Clamp(offset, 0, source.Length);
        while (index > 0 && source[index - 1] is not ('\r' or '\n'))
            index--;
        return index;
    }

    private static int FindLineEnd(string source, int offset)
    {
        var index = Math.Clamp(offset, 0, source.Length);
        while (index < source.Length && source[index] is not ('\r' or '\n'))
            index++;
        return index;
    }

    private static int SkipLineBreak(string source, int index)
    {
        if (index < source.Length && source[index] == '\r')
            index++;
        if (index < source.Length && source[index] == '\n')
            index++;
        return index;
    }

    internal static (int Line, int Column) GetLineAndColumn(string source, int offset)
    {
        var line = 1;
        var column = 1;
        var limit = Math.Clamp(offset, 0, source.Length);

        for (var i = 0; i < limit; i++)
        {
            if (source[i] == '\r')
            {
                line++;
                column = 1;
                if (i + 1 < limit && source[i + 1] == '\n')
                    i++;
            }
            else if (source[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }

    internal static SqlBatchParseException CreateException(
        string message,
        IList<ParseError> errors,
        string source,
        int sourceOffset)
    {
        var diagnostics = errors
            .Select(error =>
            {
                var localOffset = Math.Clamp(error.Offset, 0, Math.Max(0, source.Length - sourceOffset));
                var position = GetLineAndColumn(source, sourceOffset + localOffset);
                return new SqlParseDiagnostic(
                    error.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    error.Message,
                    position.Line,
                    position.Column);
            })
            .ToArray();

        return new SqlBatchParseException(message, diagnostics);
    }
}
