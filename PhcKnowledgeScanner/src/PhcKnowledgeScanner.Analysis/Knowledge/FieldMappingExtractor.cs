using System.Text;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

namespace PhcKnowledgeScanner.Analysis.Knowledge;

/// <summary>
/// Extrai <see cref="FieldMapping"/> a partir de ASTs já interpretadas.
/// Não re-parseia; não infere chaves compostas; não executa expressões.
/// </summary>
public static class FieldMappingExtractor
{
    public static IReadOnlyList<FieldMapping> Extract(
        string declaredEvidenceId,
        string? knowledgeRelationId,
        string interpretationEvidenceId,
        string parserVersion,
        ExprAstNode? ast,
        string? diagnosticsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(declaredEvidenceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(interpretationEvidenceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(parserVersion);

        if (ast is null)
        {
            return Array.Empty<FieldMapping>();
        }

        var results = new List<FieldMapping>();
        var ordinal = 0;
        Walk(
            ast,
            path: "root",
            logicalPath: "root",
            underOr: false,
            underAnd: false,
            declaredEvidenceId,
            knowledgeRelationId,
            interpretationEvidenceId,
            parserVersion,
            diagnosticsJson,
            results,
            ref ordinal);

        return results
            .OrderBy(m => m.PredicateOrdinal)
            .ThenBy(m => m.FieldMappingId, StringComparer.Ordinal)
            .ToList();
    }

    private static void Walk(
        ExprAstNode node,
        string path,
        string logicalPath,
        bool underOr,
        bool underAnd,
        string evidenceId,
        string? knowledgeRelationId,
        string interpretationEvidenceId,
        string parserVersion,
        string? diagnosticsJson,
        List<FieldMapping> sink,
        ref int ordinal)
    {
        switch (node)
        {
            case GroupExprNode group:
                Walk(
                    group.Inner,
                    path + "/group",
                    logicalPath + "/group",
                    underOr,
                    underAnd,
                    evidenceId,
                    knowledgeRelationId,
                    interpretationEvidenceId,
                    parserVersion,
                    diagnosticsJson,
                    sink,
                    ref ordinal);
                break;

            case LogicalExprNode logic:
            {
                var op = NormalizeLogical(logic.Operator);
                var isOr = op == "OR";
                var isAnd = op == "AND";
                for (var i = 0; i < logic.Children.Count; i++)
                {
                    Walk(
                        logic.Children[i],
                        $"{path}/logical:{op}/{i}",
                        $"{logicalPath}/{op}[{i}]",
                        underOr || isOr,
                        underAnd || isAnd,
                        evidenceId,
                        knowledgeRelationId,
                        interpretationEvidenceId,
                        parserVersion,
                        diagnosticsJson,
                        sink,
                        ref ordinal);
                }

                break;
            }

            case ComparisonExprNode cmp:
            {
                var predicateOrdinal = ordinal++;
                var astPath = $"{path}/comparison#{predicateOrdinal}";
                var left = BuildOperand(cmp.Left);
                var right = BuildOperand(cmp.Right);
                var kind = Classify(cmp.Operator, left, right);
                var context = ResolveContext(underOr, underAnd);
                var predicateText = $"{left.OriginalText} {cmp.Operator} {right.OriginalText}";

                sink.Add(new FieldMapping(
                    FieldMappingIds.Build(evidenceId, parserVersion, astPath),
                    evidenceId,
                    knowledgeRelationId,
                    interpretationEvidenceId,
                    parserVersion,
                    kind,
                    cmp.Operator,
                    predicateText,
                    astPath,
                    predicateOrdinal,
                    left,
                    right,
                    context,
                    logicalPath,
                    diagnosticsJson));
                break;
            }

            // Binary/Function/Field/Literal/Unsupported na raiz sem comparison: sem mapping.
            default:
                break;
        }
    }

    internal static FieldMappingKind Classify(
        string op,
        FieldMappingOperand left,
        FieldMappingOperand right)
    {
        if (!string.Equals(op, "=", StringComparison.Ordinal))
        {
            // <>, !=, <, >, <=, >= — nunca DirectFieldEquality.
            return FieldMappingKind.NonFieldPredicate;
        }

        if (left.Kind == FieldMappingOperand.KindField && right.Kind == FieldMappingOperand.KindField)
        {
            return FieldMappingKind.DirectFieldEquality;
        }

        var leftIsExpr = left.Kind == FieldMappingOperand.KindExpression;
        var rightIsExpr = right.Kind == FieldMappingOperand.KindExpression;
        if (leftIsExpr || rightIsExpr)
        {
            return FieldMappingKind.ExpressionComparison;
        }

        // campo=literal, literal=literal, unsupported, etc.
        return FieldMappingKind.NonFieldPredicate;
    }

    internal static FieldMappingLogicalContext ResolveContext(bool underOr, bool underAnd)
    {
        if (underOr)
        {
            return FieldMappingLogicalContext.AlternativeUnderOr;
        }

        if (underAnd)
        {
            return FieldMappingLogicalContext.RequiredUnderAnd;
        }

        // Predicado único / raiz sem AND|OR: condição necessária da expressão.
        return FieldMappingLogicalContext.RequiredUnderAnd;
    }

    private static FieldMappingOperand BuildOperand(ExprAstNode node) =>
        node switch
        {
            FieldRefExprNode f => new FieldMappingOperand(
                FieldMappingOperand.KindField,
                f.OriginalText,
                ToMapped(f),
                [ToMapped(f)]),
            LiteralExprNode lit => new FieldMappingOperand(
                FieldMappingOperand.KindLiteral,
                lit.OriginalText,
                Field: null,
                ContainedFields: Array.Empty<MappedFieldRef>()),
            BinaryExprNode or FunctionCallExprNode => new FieldMappingOperand(
                FieldMappingOperand.KindExpression,
                Render(node),
                Field: null,
                ContainedFields: CollectFields(node)),
            UnsupportedExprNode u => new FieldMappingOperand(
                FieldMappingOperand.KindUnsupported,
                u.OriginalText,
                Field: null,
                ContainedFields: Array.Empty<MappedFieldRef>()),
            GroupExprNode g => BuildOperand(g.Inner),
            _ => new FieldMappingOperand(
                FieldMappingOperand.KindExpression,
                Render(node),
                Field: null,
                ContainedFields: CollectFields(node))
        };

    private static MappedFieldRef ToMapped(FieldRefExprNode f) =>
        new(f.OriginalText, f.Qualifier, f.FieldName, f.TableResolution, f.TableObjectId);

    private static IReadOnlyList<MappedFieldRef> CollectFields(ExprAstNode node)
    {
        var list = new List<MappedFieldRef>();
        CollectFields(node, list);
        return list;
    }

    private static void CollectFields(ExprAstNode node, List<MappedFieldRef> sink)
    {
        switch (node)
        {
            case FieldRefExprNode f:
                sink.Add(ToMapped(f));
                break;
            case GroupExprNode g:
                CollectFields(g.Inner, sink);
                break;
            case ComparisonExprNode c:
                CollectFields(c.Left, sink);
                CollectFields(c.Right, sink);
                break;
            case BinaryExprNode b:
                CollectFields(b.Left, sink);
                CollectFields(b.Right, sink);
                break;
            case FunctionCallExprNode fn:
                foreach (var a in fn.Arguments)
                {
                    CollectFields(a, sink);
                }

                break;
            case LogicalExprNode logic:
                foreach (var c in logic.Children)
                {
                    CollectFields(c, sink);
                }

                break;
        }
    }

    private static string Render(ExprAstNode node) =>
        node switch
        {
            FieldRefExprNode f => f.OriginalText,
            LiteralExprNode lit => lit.OriginalText,
            UnsupportedExprNode u => u.OriginalText,
            BinaryExprNode b => $"{Render(b.Left)}{b.Operator}{Render(b.Right)}",
            FunctionCallExprNode fn =>
                string.IsNullOrWhiteSpace(fn.OriginalText)
                    ? $"{fn.FunctionName}({string.Join(",", fn.Arguments.Select(Render))})"
                    : fn.OriginalText,
            GroupExprNode g => $"({Render(g.Inner)})",
            ComparisonExprNode c => $"{Render(c.Left)} {c.Operator} {Render(c.Right)}",
            LogicalExprNode logic => string.Join(
                $" {NormalizeLogical(logic.Operator)} ",
                logic.Children.Select(Render)),
            _ => node.GetType().Name
        };

    private static string NormalizeLogical(string op) =>
        op.Trim().ToUpperInvariant() switch
        {
            "AND" => "AND",
            "OR" => "OR",
            var other => other
        };
}
