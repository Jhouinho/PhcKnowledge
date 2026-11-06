namespace PhcKnowledgeScanner.Analysis.DeclaredRelations;

internal enum ExprTokenKind
{
    End,
    Identifier,
    Number,
    String,
    Dot,
    Comma,
    Plus,
    LParen,
    RParen,
    And,
    Or,
    Eq,
    NotEq,
    Lt,
    Gt,
    LtEq,
    GtEq,
    Unknown
}

internal readonly record struct ExprToken(ExprTokenKind Kind, string Text, int Position);

/// <summary>Tokenizer determinístico (sem regex) para expressões tliga/expliga V1.1.</summary>
internal sealed class ExpressionTokenizer
{
    /// <summary>Acento agudo tipográfico PHC (U+00B4), delimitador de literais — não é backtick SQL.</summary>
    public const char PhcTypographicQuote = '\u00B4';

    private readonly string _text;
    private int _i;

    public ExpressionTokenizer(string text)
    {
        _text = text ?? string.Empty;
    }

    public IReadOnlyList<ExprToken> Tokenize()
    {
        var list = new List<ExprToken>();
        while (true)
        {
            SkipWs();
            if (_i >= _text.Length)
            {
                list.Add(new ExprToken(ExprTokenKind.End, string.Empty, _i));
                break;
            }

            var start = _i;
            var c = _text[_i];

            if (c == '.')
            {
                _i++;
                list.Add(new ExprToken(ExprTokenKind.Dot, ".", start));
                continue;
            }

            if (c == ',')
            {
                _i++;
                list.Add(new ExprToken(ExprTokenKind.Comma, ",", start));
                continue;
            }

            if (c == '+')
            {
                _i++;
                list.Add(new ExprToken(ExprTokenKind.Plus, "+", start));
                continue;
            }

            if (c == '(')
            {
                _i++;
                list.Add(new ExprToken(ExprTokenKind.LParen, "(", start));
                continue;
            }

            if (c == ')')
            {
                _i++;
                list.Add(new ExprToken(ExprTokenKind.RParen, ")", start));
                continue;
            }

            if (c == '\'')
            {
                list.Add(ReadAsciiString(start));
                continue;
            }

            if (c == PhcTypographicQuote)
            {
                list.Add(ReadTypographicString(start));
                continue;
            }

            if (c == '[')
            {
                list.Add(ReadBracketedIdent(start));
                continue;
            }

            if (char.IsDigit(c) || (c == '-' && PeekIsDigit()))
            {
                list.Add(ReadNumber(start));
                continue;
            }

            if (TryReadOperator(out var op))
            {
                list.Add(op);
                continue;
            }

            if (IsIdentStart(c))
            {
                list.Add(ReadIdentOrKeyword(start));
                continue;
            }

            _i++;
            list.Add(new ExprToken(ExprTokenKind.Unknown, _text[start.._i], start));
        }

        return list;
    }

    private bool PeekIsDigit() => _i + 1 < _text.Length && char.IsDigit(_text[_i + 1]);

    private void SkipWs()
    {
        while (_i < _text.Length && char.IsWhiteSpace(_text[_i]))
        {
            _i++;
        }
    }

    private ExprToken ReadAsciiString(int start)
    {
        _i++; // '
        while (_i < _text.Length)
        {
            var c = _text[_i++];
            if (c == '\'')
            {
                if (_i < _text.Length && _text[_i] == '\'')
                {
                    _i++;
                    continue;
                }

                return new ExprToken(ExprTokenKind.String, _text[start.._i], start);
            }
        }

        return new ExprToken(ExprTokenKind.Unknown, _text[start..], start);
    }

    private ExprToken ReadTypographicString(int start)
    {
        _i++; // opening ´
        while (_i < _text.Length)
        {
            if (_text[_i] == PhcTypographicQuote)
            {
                _i++;
                return new ExprToken(ExprTokenKind.String, _text[start.._i], start);
            }

            _i++;
        }

        return new ExprToken(ExprTokenKind.Unknown, _text[start..], start);
    }

    private ExprToken ReadBracketedIdent(int start)
    {
        _i++;
        while (_i < _text.Length && _text[_i] != ']')
        {
            _i++;
        }

        if (_i >= _text.Length)
        {
            return new ExprToken(ExprTokenKind.Unknown, _text[start..], start);
        }

        _i++;
        return new ExprToken(ExprTokenKind.Identifier, _text[start.._i], start);
    }

    private ExprToken ReadNumber(int start)
    {
        if (_text[_i] == '-')
        {
            _i++;
        }

        while (_i < _text.Length && char.IsDigit(_text[_i]))
        {
            _i++;
        }

        if (_i < _text.Length && _text[_i] == '.')
        {
            var j = _i + 1;
            if (j < _text.Length && char.IsDigit(_text[j]))
            {
                _i++;
                while (_i < _text.Length && char.IsDigit(_text[_i]))
                {
                    _i++;
                }
            }
        }

        return new ExprToken(ExprTokenKind.Number, _text[start.._i], start);
    }

    private bool TryReadOperator(out ExprToken token)
    {
        token = default;
        if (_i >= _text.Length)
        {
            return false;
        }

        if (_i + 1 < _text.Length)
        {
            var two = _text.Substring(_i, 2);
            ExprTokenKind? kind = two switch
            {
                "<>" => ExprTokenKind.NotEq,
                "!=" => ExprTokenKind.NotEq,
                "<=" => ExprTokenKind.LtEq,
                ">=" => ExprTokenKind.GtEq,
                _ => null
            };
            if (kind is not null)
            {
                token = new ExprToken(kind.Value, two, _i);
                _i += 2;
                return true;
            }
        }

        var c = _text[_i];
        ExprTokenKind? one = c switch
        {
            '=' => ExprTokenKind.Eq,
            '<' => ExprTokenKind.Lt,
            '>' => ExprTokenKind.Gt,
            _ => null
        };
        if (one is not null)
        {
            token = new ExprToken(one.Value, c.ToString(), _i);
            _i++;
            return true;
        }

        return false;
    }

    private ExprToken ReadIdentOrKeyword(int start)
    {
        while (_i < _text.Length && IsIdentPart(_text[_i]))
        {
            _i++;
        }

        var text = _text[start.._i];
        if (text.Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            return new ExprToken(ExprTokenKind.And, text, start);
        }

        if (text.Equals("OR", StringComparison.OrdinalIgnoreCase))
        {
            return new ExprToken(ExprTokenKind.Or, text, start);
        }

        return new ExprToken(ExprTokenKind.Identifier, text, start);
    }

    private static bool IsIdentStart(char c) =>
        char.IsLetter(c) || c == '_' || c == '@' || c == '#';

    private static bool IsIdentPart(char c) =>
        char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';

    internal static string UnwrapIdentifier(string tokenText)
    {
        if (tokenText.Length >= 2 && tokenText[0] == '[' && tokenText[^1] == ']')
        {
            return tokenText[1..^1];
        }

        return tokenText;
    }

    internal static string UnwrapStringLiteral(string tokenText)
    {
        if (tokenText.Length >= 2)
        {
            var a = tokenText[0];
            var b = tokenText[^1];
            if (a == '\'' && b == '\'')
            {
                return tokenText[1..^1].Replace("''", "'", StringComparison.Ordinal);
            }

            if (a == PhcTypographicQuote && b == PhcTypographicQuote)
            {
                return tokenText[1..^1];
            }
        }

        return tokenText;
    }
}
