using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;

namespace PhcKnowledgeScanner.Analysis.DeclaredRelations;

/// <summary>
/// Parser recursivo determinístico V1.1.
/// <code>
/// expr       := or_expr
/// or_expr    := and_expr (OR and_expr)*
/// and_expr   := comparison (AND comparison)*
/// comparison := sum (cmp_op sum)?
/// sum        := operand (('+' | unknown_op) operand)*
/// operand    := '(' expr ')' | number | string | function_call | field_ref | unsupported
/// function   := ident '(' [sum (',' sum)*] ')'
/// </code>
/// </summary>
internal sealed class ExpressionParser
{
    private readonly IReadOnlyList<ExprToken> _tokens;
    private int _pos;
    private bool _sawUnsupported;
    private string? _diagnostic;

    private ExpressionParser(IReadOnlyList<ExprToken> tokens)
    {
        _tokens = tokens;
    }

    public static ParseResult Parse(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return new ParseResult(
                ExpressionParseStatus.Empty,
                null,
                "Expressão vazia.",
                HadUnsupported: false,
                HadInvalid: false);
        }

        var tokens = new ExpressionTokenizer(expression).Tokenize();
        var parser = new ExpressionParser(tokens);
        try
        {
            var ast = parser.ParseExpression();

            if (parser.Current.Kind != ExprTokenKind.End)
            {
                // Tokens restantes: se já há AST utilizável → recuperação parcial; senão Invalid.
                parser._sawUnsupported = true;
                var leftover = parser.Current.Text;
                parser._diagnostic = "Tokens restantes após análise parcial: " + leftover;
                while (parser.Current.Kind != ExprTokenKind.End)
                {
                    parser.Advance();
                }

                return new ParseResult(
                    ExpressionParseStatus.PartiallyParsed,
                    ast,
                    parser._diagnostic,
                    HadUnsupported: true,
                    HadInvalid: false);
            }

            var status = parser._sawUnsupported
                ? ExpressionParseStatus.PartiallyParsed
                : ExpressionParseStatus.Parsed;

            if (ast is UnsupportedExprNode && status != ExpressionParseStatus.PartiallyParsed)
            {
                status = ExpressionParseStatus.Unsupported;
            }

            return new ParseResult(status, ast, parser._diagnostic, parser._sawUnsupported, HadInvalid: false);
        }
        catch (ParseException ex)
        {
            return new ParseResult(
                ExpressionParseStatus.Invalid,
                null,
                ex.Message,
                HadUnsupported: false,
                HadInvalid: true);
        }
    }

    private ExprAstNode ParseExpression() => ParseOr();

    private ExprAstNode ParseOr()
    {
        var left = ParseAnd();
        if (Current.Kind != ExprTokenKind.Or)
        {
            return left;
        }

        var children = new List<ExprAstNode> { left };
        while (Current.Kind == ExprTokenKind.Or)
        {
            Advance();
            children.Add(ParseAnd());
        }

        return new LogicalExprNode("OR", children);
    }

    private ExprAstNode ParseAnd()
    {
        var left = ParseComparison();
        if (Current.Kind != ExprTokenKind.And)
        {
            return left;
        }

        var children = new List<ExprAstNode> { left };
        while (Current.Kind == ExprTokenKind.And)
        {
            Advance();
            children.Add(ParseComparison());
        }

        return new LogicalExprNode("AND", children);
    }

    private ExprAstNode ParseComparison()
    {
        var left = ParseSum();
        if (!IsComparisonOp(Current.Kind))
        {
            return left;
        }

        var op = OperatorText(Current);
        Advance();
        var right = ParseSum();
        return new ComparisonExprNode(left, op, right);
    }

    private ExprAstNode ParseSum()
    {
        var left = ParseOperand();
        while (true)
        {
            if (Current.Kind == ExprTokenKind.Plus)
            {
                Advance();
                var right = ParseOperand();
                left = new BinaryExprNode("+", left, right);
                continue;
            }

            // Operador desconhecido entre operandos → partial recovery.
            if (Current.Kind == ExprTokenKind.Unknown
                && PeekLooksLikeOperandStart())
            {
                var unk = Advance();
                _sawUnsupported = true;
                _diagnostic = string.IsNullOrEmpty(_diagnostic)
                    ? "Operador não suportado: " + unk.Text
                    : _diagnostic;
                var right = ParseOperand();
                left = new BinaryExprNode(unk.Text, left, right);
                continue;
            }

            break;
        }

        return left;
    }

    private bool PeekLooksLikeOperandStart()
    {
        var k = Peek.Kind;
        return k is ExprTokenKind.Identifier or ExprTokenKind.Number or ExprTokenKind.String
            or ExprTokenKind.LParen;
    }

    private ExprAstNode ParseOperand()
    {
        if (Current.Kind == ExprTokenKind.Number)
        {
            var t = Advance();
            return new LiteralExprNode("number", t.Text, t.Text);
        }

        if (Current.Kind == ExprTokenKind.String)
        {
            var t = Advance();
            var value = ExpressionTokenizer.UnwrapStringLiteral(t.Text);
            var kind = t.Text.Length > 0 && t.Text[0] == ExpressionTokenizer.PhcTypographicQuote
                ? "string_phc"
                : "string";
            return new LiteralExprNode(kind, value, t.Text);
        }

        if (Current.Kind == ExprTokenKind.Identifier)
        {
            if (Peek.Kind == ExprTokenKind.LParen)
            {
                return ParseFunctionCall();
            }

            return ParseFieldRef();
        }

        if (Current.Kind == ExprTokenKind.LParen)
        {
            Advance();
            // Agrupamento de expressão completa (permite comparação/AND/OR no interior).
            var inner = ParseExpression();
            Expect(ExprTokenKind.RParen, ")");
            return new GroupExprNode(inner);
        }

        if (Current.Kind == ExprTokenKind.Unknown)
        {
            var t = Advance();
            _sawUnsupported = true;
            return new UnsupportedExprNode(t.Text, "Token não suportado: " + t.Text);
        }

        throw new ParseException($"Operando inválido na posição {Current.Position}: '{Current.Text}'.");
    }

    private FunctionCallExprNode ParseFunctionCall()
    {
        var nameTok = Advance();
        var name = Unwrap(nameTok.Text);
        Expect(ExprTokenKind.LParen, "(");
        var args = new List<ExprAstNode>();
        if (Current.Kind != ExprTokenKind.RParen)
        {
            args.Add(ParseSum());
            while (Current.Kind == ExprTokenKind.Comma)
            {
                Advance();
                args.Add(ParseSum());
            }
        }

        Expect(ExprTokenKind.RParen, ")");
        var original = name + "(" + args.Count + " args)";
        return new FunctionCallExprNode(name, args, original);
    }

    private FieldRefExprNode ParseFieldRef()
    {
        var firstTok = Advance();
        var first = Unwrap(firstTok.Text);
        if (Current.Kind == ExprTokenKind.Dot)
        {
            Advance();
            if (Current.Kind != ExprTokenKind.Identifier)
            {
                throw new ParseException($"Esperado identificador de campo após '.' (posição {Current.Position}).");
            }

            var secondTok = Advance();
            var second = Unwrap(secondTok.Text);
            return new FieldRefExprNode(
                firstTok.Text + "." + secondTok.Text,
                first,
                second,
                TableReferenceResolution.Unresolved,
                null);
        }

        return new FieldRefExprNode(
            firstTok.Text,
            null,
            first,
            TableReferenceResolution.Unresolved,
            null);
    }

    private ExprToken Current => _pos < _tokens.Count ? _tokens[_pos] : _tokens[^1];

    private ExprToken Peek => _pos + 1 < _tokens.Count ? _tokens[_pos + 1] : _tokens[^1];

    private ExprToken Advance()
    {
        var t = Current;
        if (_pos < _tokens.Count - 1)
        {
            _pos++;
        }

        return t;
    }

    private void Expect(ExprTokenKind kind, string label)
    {
        if (Current.Kind != kind)
        {
            throw new ParseException($"Esperado '{label}' na posição {Current.Position}, obtido '{Current.Text}'.");
        }

        Advance();
    }

    private static bool IsComparisonOp(ExprTokenKind kind) =>
        kind is ExprTokenKind.Eq or ExprTokenKind.NotEq or ExprTokenKind.Lt or ExprTokenKind.Gt
            or ExprTokenKind.LtEq or ExprTokenKind.GtEq;

    private static string OperatorText(ExprToken token) => token.Kind switch
    {
        ExprTokenKind.Eq => "=",
        ExprTokenKind.NotEq => token.Text,
        ExprTokenKind.Lt => "<",
        ExprTokenKind.Gt => ">",
        ExprTokenKind.LtEq => "<=",
        ExprTokenKind.GtEq => ">=",
        _ => token.Text
    };

    private static string Unwrap(string text) => ExpressionTokenizer.UnwrapIdentifier(text);

    internal sealed record ParseResult(
        ExpressionParseStatus Status,
        ExprAstNode? Ast,
        string? Diagnostic,
        bool HadUnsupported,
        bool HadInvalid);

    private sealed class ParseException : Exception
    {
        public ParseException(string message) : base(message)
        {
        }
    }
}
