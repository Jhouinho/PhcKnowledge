using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Analysis.DeclaredRelations;

/// <summary>
/// Interpreta expressões de relações declaradas (Camada interpretação), sem alterar factos.
/// Pode ser reexecutada após mudança de <see cref="ParserVersion"/>.
/// </summary>
public sealed class DeclaredRelationInterpretationService
{
    public const string ParserVersion = "declared-expr-v1.1";

    public DeclaredRelationInterpretationRecord Interpret(
        DeclaredRelationRecord relation,
        IReadOnlyDictionary<string, List<string>> tableNameIndex,
        IReadOnlySet<string> knownTableObjectIds,
        DateTimeOffset interpretedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(relation);

        var parse = ExpressionParser.Parse(relation.OriginalExpression);
        var diagnostics = new List<Dictionary<string, object?>>();
        if (!string.IsNullOrWhiteSpace(parse.Diagnostic))
        {
            diagnostics.Add(new Dictionary<string, object?>
            {
                ["code"] = InterpretationDiagnosticCodes.ParseNote,
                ["detail"] = parse.Diagnostic
            });
        }

        ExprAstNode? resolvedAst = parse.Ast is null
            ? null
            : ResolveNode(parse.Ast, relation, tableNameIndex, knownTableObjectIds, diagnostics);

        var fieldRefs = new List<FieldRefExprNode>();
        if (resolvedAst is not null)
        {
            CollectFieldRefs(resolvedAst, fieldRefs);
        }

        var resolved = fieldRefs.Count(f =>
            !string.IsNullOrEmpty(f.Qualifier)
            && f.TableResolution == TableReferenceResolution.Resolved);
        var ambiguous = fieldRefs.Count(f =>
            !string.IsNullOrEmpty(f.Qualifier)
            && f.TableResolution == TableReferenceResolution.Ambiguous);
        var unresolved = fieldRefs.Count(f =>
            !string.IsNullOrEmpty(f.Qualifier)
            && f.TableResolution != TableReferenceResolution.Resolved
            && f.TableResolution != TableReferenceResolution.Ambiguous);

        // Unqualified: counted in FieldRefCount but not in resolved/amb/unresolved denominators.
        foreach (var f in fieldRefs.Where(f => string.IsNullOrEmpty(f.Qualifier)))
        {
            diagnostics.Add(new Dictionary<string, object?>
            {
                ["code"] = InterpretationDiagnosticCodes.UnqualifiedField,
                ["field"] = f.FieldName,
                ["originalText"] = f.OriginalText,
                ["detail"] = "Campo sem qualificador de tabela."
            });
        }

        var confidence = ComputeConfidence(parse.Status, ambiguous, unresolved);
        var diagnosticsJson = diagnostics.Count == 0
            ? null
            : JsonSerializer.Serialize(diagnostics, InterpretationJson.Options);

        return new DeclaredRelationInterpretationRecord(
            relation.RelationId,
            ParserVersion,
            parse.Status,
            confidence,
            resolvedAst,
            diagnosticsJson,
            fieldRefs.Count,
            resolved,
            ambiguous,
            unresolved,
            interpretedAtUtc);
    }

    public async Task<(IReadOnlyList<DeclaredRelationInterpretationRecord> Records, DeclaredRelationInterpretationMetrics Metrics)>
        InterpretWorkspaceAsync(IScanWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var relations = await workspace.GetDeclaredRelationsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var tables = await workspace.GetObjectsAsync(ObjectKind.Table, search: null, cancellationToken)
            .ConfigureAwait(false);
        var index = TableReferenceResolver.BuildNameIndex(tables);
        var known = new HashSet<string>(tables.Select(t => t.ObjectId), StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;

        var records = new List<DeclaredRelationInterpretationRecord>(relations.Count);
        foreach (var rel in relations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            records.Add(Interpret(rel, index, known, now));
        }

        return (records, BuildMetrics(records));
    }

    public static DeclaredRelationInterpretationMetrics BuildMetrics(
        IReadOnlyList<DeclaredRelationInterpretationRecord> records)
    {
        var incomplete = records
            .Where(r => r.ParseStatus is not ExpressionParseStatus.Parsed
                        || r.Confidence is InterpretationConfidence.Low or InterpretationConfidence.None
                        or InterpretationConfidence.Medium)
            .Select(r => r.RelationId)
            .Take(50)
            .ToList();

        var fieldTotal = records.Sum(r => r.FieldRefCount);
        var resolved = records.Sum(r => r.TableRefsResolved);
        var amb = records.Sum(r => r.TableRefsAmbiguous);
        var unres = records.Sum(r => r.TableRefsUnresolved);
        var qualified = resolved + amb + unres;
        var unqualified = fieldTotal - qualified;

        return new DeclaredRelationInterpretationMetrics(
            records.Count,
            records.Count(r => r.ParseStatus == ExpressionParseStatus.Parsed),
            records.Count(r => r.ParseStatus == ExpressionParseStatus.PartiallyParsed),
            records.Count(r => r.ParseStatus == ExpressionParseStatus.Unsupported),
            records.Count(r => r.ParseStatus == ExpressionParseStatus.Invalid),
            records.Count(r => r.ParseStatus == ExpressionParseStatus.Empty),
            fieldTotal,
            qualified,
            unqualified,
            resolved,
            amb,
            unres,
            incomplete);
    }

    public static string SerializeAst(ExprAstNode? ast) =>
        ast is null ? string.Empty : JsonSerializer.Serialize(ast, InterpretationJson.Options);

    public static ExprAstNode? DeserializeAst(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ExprAstNode>(json, InterpretationJson.Options);
    }

    private static ExprAstNode ResolveNode(
        ExprAstNode node,
        DeclaredRelationRecord relation,
        IReadOnlyDictionary<string, List<string>> index,
        IReadOnlySet<string> knownIds,
        List<Dictionary<string, object?>> diagnostics) =>
        node switch
        {
            LogicalExprNode logic => new LogicalExprNode(
                logic.Operator,
                logic.Children.Select(c => ResolveNode(c, relation, index, knownIds, diagnostics)).ToList()),
            GroupExprNode group => new GroupExprNode(
                ResolveNode(group.Inner, relation, index, knownIds, diagnostics)),
            ComparisonExprNode cmp => new ComparisonExprNode(
                ResolveNode(cmp.Left, relation, index, knownIds, diagnostics),
                cmp.Operator,
                ResolveNode(cmp.Right, relation, index, knownIds, diagnostics)),
            BinaryExprNode bin => new BinaryExprNode(
                bin.Operator,
                ResolveNode(bin.Left, relation, index, knownIds, diagnostics),
                ResolveNode(bin.Right, relation, index, knownIds, diagnostics)),
            FunctionCallExprNode fn => new FunctionCallExprNode(
                fn.FunctionName,
                fn.Arguments.Select(a => ResolveNode(a, relation, index, knownIds, diagnostics)).ToList(),
                fn.OriginalText),
            FieldRefExprNode field => ResolveField(field, relation, index, knownIds, diagnostics),
            _ => node
        };

    private static FieldRefExprNode ResolveField(
        FieldRefExprNode field,
        DeclaredRelationRecord relation,
        IReadOnlyDictionary<string, List<string>> index,
        IReadOnlySet<string> knownIds,
        List<Dictionary<string, object?>> diagnostics)
    {
        if (string.IsNullOrEmpty(field.Qualifier))
        {
            return field with
            {
                TableResolution = TableReferenceResolution.Unresolved,
                TableObjectId = null
            };
        }

        if (string.Equals(field.Qualifier, relation.FromTable, StringComparison.OrdinalIgnoreCase)
            && relation.FromTableResolution == TableReferenceResolution.Resolved
            && !string.IsNullOrEmpty(relation.FromObjectId))
        {
            return field with
            {
                TableResolution = TableReferenceResolution.Resolved,
                TableObjectId = relation.FromObjectId
            };
        }

        if (string.Equals(field.Qualifier, relation.ToTable, StringComparison.OrdinalIgnoreCase)
            && relation.ToTableResolution == TableReferenceResolution.Resolved
            && !string.IsNullOrEmpty(relation.ToObjectId))
        {
            return field with
            {
                TableResolution = TableReferenceResolution.Resolved,
                TableObjectId = relation.ToObjectId
            };
        }

        var resolved = TableReferenceResolver.Resolve(field.Qualifier, index, knownIds);
        if (resolved.Resolution == TableReferenceResolution.Resolved)
        {
            return field with
            {
                TableResolution = TableReferenceResolution.Resolved,
                TableObjectId = resolved.ObjectId
            };
        }

        if (resolved.Resolution == TableReferenceResolution.Ambiguous)
        {
            diagnostics.Add(new Dictionary<string, object?>
            {
                ["code"] = InterpretationDiagnosticCodes.Ambiguous,
                ["qualifier"] = field.Qualifier,
                ["originalText"] = field.OriginalText,
                ["detail"] = "Várias tabelas com o mesmo nome em schemas diferentes."
            });
            return field with
            {
                TableResolution = TableReferenceResolution.Ambiguous,
                TableObjectId = null
            };
        }

        // Unresolved: distinguir MissingTable vs NameMismatch (sem fuzzy matching).
        var qualifierExists = index.ContainsKey(field.Qualifier);
        var matchesDeclaredName =
            string.Equals(field.Qualifier, relation.FromTable, StringComparison.OrdinalIgnoreCase)
            || string.Equals(field.Qualifier, relation.ToTable, StringComparison.OrdinalIgnoreCase);

        var declaredEndpointExists =
            (relation.FromTableResolution == TableReferenceResolution.Resolved
             && !string.IsNullOrEmpty(relation.FromObjectId))
            || (relation.ToTableResolution == TableReferenceResolution.Resolved
                && !string.IsNullOrEmpty(relation.ToObjectId));

        if (!qualifierExists && !matchesDeclaredName && declaredEndpointExists)
        {
            diagnostics.Add(new Dictionary<string, object?>
            {
                ["code"] = InterpretationDiagnosticCodes.NameMismatch,
                ["qualifier"] = field.Qualifier,
                ["originalText"] = field.OriginalText,
                ["declaredFrom"] = relation.FromTable,
                ["declaredTo"] = relation.ToTable,
                ["fromObjectId"] = relation.FromObjectId,
                ["toObjectId"] = relation.ToObjectId,
                ["detail"] =
                    "Qualificador na expressão difere dos nomes declarados da relação e não existe no inventário; " +
                    "sem associação automática."
            });
        }
        else
        {
            diagnostics.Add(new Dictionary<string, object?>
            {
                ["code"] = InterpretationDiagnosticCodes.MissingTable,
                ["qualifier"] = field.Qualifier,
                ["originalText"] = field.OriginalText,
                ["declaredFrom"] = relation.FromTable,
                ["declaredTo"] = relation.ToTable,
                ["detail"] = "Tabela não encontrada no inventário físico."
            });
        }

        return field with
        {
            TableResolution = TableReferenceResolution.Unresolved,
            TableObjectId = null
        };
    }

    private static void CollectFieldRefs(ExprAstNode node, List<FieldRefExprNode> sink)
    {
        switch (node)
        {
            case FieldRefExprNode f:
                sink.Add(f);
                break;
            case LogicalExprNode l:
                foreach (var c in l.Children)
                {
                    CollectFieldRefs(c, sink);
                }

                break;
            case GroupExprNode g:
                CollectFieldRefs(g.Inner, sink);
                break;
            case ComparisonExprNode c:
                CollectFieldRefs(c.Left, sink);
                CollectFieldRefs(c.Right, sink);
                break;
            case BinaryExprNode b:
                CollectFieldRefs(b.Left, sink);
                CollectFieldRefs(b.Right, sink);
                break;
            case FunctionCallExprNode fn:
                foreach (var a in fn.Arguments)
                {
                    CollectFieldRefs(a, sink);
                }

                break;
        }
    }

    private static InterpretationConfidence ComputeConfidence(
        ExpressionParseStatus status,
        int ambiguous,
        int unresolvedQualified)
    {
        if (status is ExpressionParseStatus.Empty or ExpressionParseStatus.Invalid
            or ExpressionParseStatus.Unsupported)
        {
            return InterpretationConfidence.None;
        }

        if (status == ExpressionParseStatus.PartiallyParsed)
        {
            return InterpretationConfidence.Low;
        }

        if (ambiguous > 0 || unresolvedQualified > 0)
        {
            return InterpretationConfidence.Medium;
        }

        return InterpretationConfidence.High;
    }
}
