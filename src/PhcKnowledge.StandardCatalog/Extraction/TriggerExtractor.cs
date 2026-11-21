using System.Security.Cryptography;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using PhcKnowledge.StandardCatalog.Parsing;

namespace PhcKnowledge.StandardCatalog.Extraction;

/// <summary>
/// Extracts CREATE TRIGGER statements from SQL batches without executing or
/// regenerating SQL. The definition is sliced from the original source text.
/// </summary>
public sealed class TriggerExtractor
{
    private readonly SqlBatchParser _batchParser;

    public TriggerExtractor(SqlBatchParser? batchParser = null)
    {
        _batchParser = batchParser ?? new SqlBatchParser();
    }

    public IReadOnlyList<TriggerDefinition> Extract(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var batches = _batchParser.Parse(source);
        var triggers = new List<TriggerDefinition>();
        bool? ansiNulls = null;
        bool? quotedIdentifier = null;

        foreach (var batch in batches)
        {
            var parser = new TSql160Parser(initialQuotedIdentifiers: quotedIdentifier ?? true);
            var tokens = parser.GetTokenStream(new StringReader(batch.Text), out var tokenErrors);
            if (tokenErrors.Count > 0)
            {
                throw SqlBatchParser.CreateException(
                    $"Batch {batch.Index} could not be tokenized.",
                    tokenErrors,
                    source,
                    batch.SourceStartOffset);
            }

            var significantTokens = tokens
                .Where(IsSignificant)
                .ToArray();

            if (ContainsCreateTrigger(significantTokens))
            {
                var fragment = parser.Parse(tokens, out var parseErrors);
                if (parseErrors.Count > 0)
                {
                    throw SqlBatchParser.CreateException(
                        $"Trigger batch {batch.Index} contains invalid T-SQL.",
                        parseErrors,
                        source,
                        batch.SourceStartOffset);
                }

                if (fragment is not TSqlScript script)
                    throw new InvalidOperationException($"Batch {batch.Index} did not produce a T-SQL script.");

                var statements = script.Batches
                    .SelectMany(x => x.Statements)
                    .OfType<CreateTriggerStatement>()
                    .ToArray();

                if (statements.Length == 0)
                {
                    throw new SqlBatchParseException(
                        $"Batch {batch.Index} contains CREATE TRIGGER tokens but no CREATE TRIGGER statement was parsed.",
                        Array.Empty<SqlParseDiagnostic>());
                }

                foreach (var statement in statements)
                {
                    triggers.Add(CreateDefinition(
                        source,
                        batch,
                        statement,
                        significantTokens,
                        ansiNulls,
                        quotedIdentifier));
                }

                continue;
            }

            ApplySetOptions(significantTokens, ref ansiNulls, ref quotedIdentifier);
        }

        return triggers;
    }

    private static TriggerDefinition CreateDefinition(
        string source,
        SqlBatch batch,
        CreateTriggerStatement statement,
        IReadOnlyList<TSqlParserToken> tokens,
        bool? ansiNulls,
        bool? quotedIdentifier)
    {
        var start = statement.StartOffset;
        var length = statement.FragmentLength;

        if (start < 0 || length <= 0 || start > batch.Text.Length || length > batch.Text.Length - start)
        {
            throw new SqlBatchParseException(
                $"The parser returned an invalid source range for trigger at batch {batch.Index}.",
                Array.Empty<SqlParseDiagnostic>());
        }

        var definition = batch.Text.Substring(start, length);
        var absoluteStart = batch.SourceStartOffset + start;
        var absoluteEnd = absoluteStart + length;
        var sourcePosition = SqlBatchParser.GetLineAndColumn(source, absoluteStart);

        var triggerNameParts = statement.Name.Identifiers;
        var targetNameParts = statement.TriggerObject.Name.Identifiers;
        if (triggerNameParts.Count == 0 || targetNameParts.Count == 0)
        {
            throw new SqlBatchParseException(
                $"A trigger in batch {batch.Index} has no usable object name.",
                Array.Empty<SqlParseDiagnostic>());
        }

        var bodyStart = statement.StatementList?.StartOffset ?? (start + length);
        var headerTokens = tokens.Where(token =>
            token.Offset >= start && token.Offset < bodyStart).ToArray();

        var isEncrypted = ContainsAdjacentTokens(headerTokens, "WITH", "ENCRYPTION");
        var fingerprint = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(definition)))
            .ToLowerInvariant();

        return new TriggerDefinition(
            triggerNameParts[^1].Value,
            GetSchema(triggerNameParts),
            targetNameParts[^1].Value,
            GetSchema(targetNameParts),
            statement.TriggerActions
                .Select(action => action.TriggerActionType.ToString().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            statement.TriggerType.ToString().ToUpperInvariant(),
            isEncrypted,
            definition,
            fingerprint,
            absoluteStart,
            absoluteEnd,
            sourcePosition.Line,
            sourcePosition.Column,
            ansiNulls,
            quotedIdentifier);
    }

    private static string? GetSchema(IList<Identifier> identifiers) =>
        identifiers.Count >= 2 ? identifiers[^2].Value : null;

    private static bool ContainsCreateTrigger(IReadOnlyList<TSqlParserToken> tokens)
    {
        for (var i = 0; i + 1 < tokens.Count; i++)
        {
            if (EqualsToken(tokens[i], "CREATE") && EqualsToken(tokens[i + 1], "TRIGGER"))
                return true;

            if (i + 2 < tokens.Count
                && EqualsToken(tokens[i], "CREATE")
                && EqualsToken(tokens[i + 1], "OR")
                && EqualsToken(tokens[i + 2], "ALTER"))
            {
                // CREATE OR ALTER TRIGGER is deliberately not treated as an
                // importable CREATE TRIGGER source in this first importer phase.
                continue;
            }
        }

        return false;
    }

    private static bool ContainsAdjacentTokens(
        IReadOnlyList<TSqlParserToken> tokens,
        string first,
        string second)
    {
        for (var i = 0; i + 1 < tokens.Count; i++)
        {
            if (EqualsToken(tokens[i], first) && EqualsToken(tokens[i + 1], second))
                return true;
        }

        return false;
    }

    private static bool IsSignificant(TSqlParserToken token) =>
        token.TokenType is not (
            TSqlTokenType.SingleLineComment
            or TSqlTokenType.MultilineComment
            or TSqlTokenType.EndOfFile);

    private static bool EqualsToken(TSqlParserToken token, string value) =>
        string.Equals(token.Text, value, StringComparison.OrdinalIgnoreCase);

    private static void ApplySetOptions(
        IReadOnlyList<TSqlParserToken> tokens,
        ref bool? ansiNulls,
        ref bool? quotedIdentifier)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!EqualsToken(tokens[i], "SET"))
                continue;

            var end = i + 1;
            while (end < tokens.Count
                && !EqualsToken(tokens[end], "SET")
                && tokens[end].Text != ";")
            {
                end++;
            }

            var segment = tokens.Skip(i + 1).Take(end - i - 1).ToArray();
            var settings = segment
                .Where(token => EqualsToken(token, "ANSI_NULLS") || EqualsToken(token, "QUOTED_IDENTIFIER"))
                .Select(token => token.Text)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var valueToken = segment.LastOrDefault(token =>
                EqualsToken(token, "ON") || EqualsToken(token, "OFF"));

            if (valueToken is not null)
            {
                var value = EqualsToken(valueToken, "ON");
                foreach (var setting in settings)
                {
                    if (string.Equals(setting, "ANSI_NULLS", StringComparison.OrdinalIgnoreCase))
                        ansiNulls = value;
                    else if (string.Equals(setting, "QUOTED_IDENTIFIER", StringComparison.OrdinalIgnoreCase))
                        quotedIdentifier = value;
                }
            }

            i = end - 1;
        }
    }
}
