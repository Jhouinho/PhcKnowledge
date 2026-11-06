using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Analysis.DeclaredRelations;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public class DeclaredRelationExpressionTests
{
    private readonly DeclaredRelationInterpretationService _svc = new();
    private const char Q = '\u00B4'; // ´

    [Fact]
    public void Simple_Equality_Bi_Bistamp()
    {
        var result = InterpretExpr("bi.bistamp=bi2.bi2stamp", "bi", "bi2");
        Assert.Equal(ExpressionParseStatus.Parsed, result.ParseStatus);
        Assert.Equal(InterpretationConfidence.High, result.Confidence);
        var cmp = Assert.IsType<ComparisonExprNode>(result.Ast);
        Assert.Equal("=", cmp.Operator);
        var left = Assert.IsType<FieldRefExprNode>(cmp.Left);
        Assert.Equal("bi", left.Qualifier);
        Assert.Equal(TableReferenceResolution.Resolved, left.TableResolution);
    }

    [Fact]
    public void Compound_And_Equality()
    {
        var result = InterpretExpr("pa.no=cl.no and pa.estab=cl.estab", "pa", "cl");
        Assert.Equal(ExpressionParseStatus.Parsed, result.ParseStatus);
        var logic = Assert.IsType<LogicalExprNode>(result.Ast);
        Assert.Equal("AND", logic.Operator);
        Assert.Equal(4, result.FieldRefCount);
        Assert.Equal(4, result.TableRefsResolved);
    }

    [Fact]
    public void Parentheses_Are_Preserved()
    {
        var result = InterpretExpr("(pa.no=cl.no) and (pa.estab=cl.estab)", "pa", "cl");
        var logic = Assert.IsType<LogicalExprNode>(result.Ast);
        Assert.All(logic.Children, c => Assert.IsType<GroupExprNode>(c));
    }

    [Fact]
    public void Or_Is_Not_Flattened_To_And()
    {
        var expr = "(sn.no=cl.no and sn.estab=cl.estab and sn.agente=0) or (sn.agente=1 and sn.clno=cl.no and sn.clestab=cl.estab)";
        var result = InterpretExpr(expr, "sn", "cl");
        Assert.Equal(ExpressionParseStatus.Parsed, result.ParseStatus);
        var logic = Assert.IsType<LogicalExprNode>(result.Ast);
        Assert.Equal("OR", logic.Operator);
        Assert.Equal(2, logic.Children.Count);
    }

    [Fact]
    public void Field_Equals_Literal_Is_Not_Table_Reference()
    {
        var result = InterpretExpr("tabela='tnt'", "gtxt", "tnt");
        var cmp = Assert.IsType<ComparisonExprNode>(result.Ast);
        var left = Assert.IsType<FieldRefExprNode>(cmp.Left);
        Assert.Null(left.Qualifier);
        Assert.Equal(1, result.UnqualifiedFieldRefCount);
        Assert.Contains(InterpretationDiagnosticCodes.UnqualifiedField, result.DiagnosticsJson);
    }

    [Fact]
    public void Typographic_Literal_C()
    {
        var result = InterpretExpr($"{Q}C{Q}=fo.x", "ra", "fo");
        Assert.Equal(ExpressionParseStatus.Parsed, result.ParseStatus);
        var cmp = Assert.IsType<ComparisonExprNode>(result.Ast);
        var lit = Assert.IsType<LiteralExprNode>(cmp.Left);
        Assert.Equal("C", lit.Value);
        Assert.Equal("string_phc", lit.Kind);
    }

    [Fact]
    public void Typographic_Literal_D()
    {
        var result = InterpretExpr($"{Q}D{Q}=fo.x", "ra", "fo");
        var lit = Assert.IsType<LiteralExprNode>(Assert.IsType<ComparisonExprNode>(result.Ast).Left);
        Assert.Equal("D", lit.Value);
    }

    [Fact]
    public void Binary_Plus_Is_Not_Evaluated()
    {
        var result = InterpretExpr($"{Q}C{Q}+ra.rastamp=fo.fostamp", "ra", "fo");
        Assert.Equal(ExpressionParseStatus.Parsed, result.ParseStatus);
        var cmp = Assert.IsType<ComparisonExprNode>(result.Ast);
        var bin = Assert.IsType<BinaryExprNode>(cmp.Left);
        Assert.Equal("+", bin.Operator);
        Assert.IsType<LiteralExprNode>(bin.Left);
        Assert.IsType<FieldRefExprNode>(bin.Right);
    }

    [Fact]
    public void Left_Function_With_Two_Arguments()
    {
        var result = InterpretExpr("LEFT(ra.rastamp, 24)=fo.fostamp", "ra", "fo");
        Assert.Equal(ExpressionParseStatus.Parsed, result.ParseStatus);
        var cmp = Assert.IsType<ComparisonExprNode>(result.Ast);
        var fn = Assert.IsType<FunctionCallExprNode>(cmp.Left);
        Assert.Equal("LEFT", fn.FunctionName);
        Assert.Equal(2, fn.Arguments.Count);
        Assert.IsType<FieldRefExprNode>(fn.Arguments[0]);
        var lit = Assert.IsType<LiteralExprNode>(fn.Arguments[1]);
        Assert.Equal("24", lit.Value);
    }

    [Fact]
    public void Ra_Fo_Full_Expression_With_Or()
    {
        var expr =
            $"({Q}C{Q} + LEFT(ra.rastamp, 24) = fo.fostamp OR {Q}D{Q} + LEFT(ra.rastamp, 24) = fo.fostamp)";
        var result = InterpretExpr(expr, "ra", "fo");
        Assert.Equal(ExpressionParseStatus.Parsed, result.ParseStatus);
        Assert.Equal(InterpretationConfidence.High, result.Confidence);
        Assert.NotEqual(ExpressionParseStatus.Invalid, result.ParseStatus);
        var group = Assert.IsType<GroupExprNode>(result.Ast);
        var logic = Assert.IsType<LogicalExprNode>(group.Inner);
        Assert.Equal("OR", logic.Operator);
        Assert.Equal(2, logic.Children.Count);
        foreach (var child in logic.Children)
        {
            var cmp = Assert.IsType<ComparisonExprNode>(child);
            var bin = Assert.IsType<BinaryExprNode>(cmp.Left);
            Assert.Equal("+", bin.Operator);
            Assert.IsType<FunctionCallExprNode>(bin.Right);
        }
    }

    [Fact]
    public void Ra_Ft_Full_Expression()
    {
        var expr = $"{Q}F{Q} + LEFT(ra.rastamp, 24) = ft.ftstamp";
        var result = InterpretExpr(expr, "ra", "ft");
        Assert.Equal(ExpressionParseStatus.Parsed, result.ParseStatus);
        Assert.NotEqual(ExpressionParseStatus.Invalid, result.ParseStatus);
        var cmp = Assert.IsType<ComparisonExprNode>(result.Ast);
        var bin = Assert.IsType<BinaryExprNode>(cmp.Left);
        Assert.IsType<LiteralExprNode>(bin.Left);
        Assert.IsType<FunctionCallExprNode>(bin.Right);
    }

    [Fact]
    public void Parentheses_Around_Binary()
    {
        var result = InterpretExpr($"({Q}C{Q}+ra.x)=fo.y", "ra", "fo");
        Assert.Equal(ExpressionParseStatus.Parsed, result.ParseStatus);
        var cmp = Assert.IsType<ComparisonExprNode>(result.Ast);
        var group = Assert.IsType<GroupExprNode>(cmp.Left);
        Assert.IsType<BinaryExprNode>(group.Inner);
    }

    [Fact]
    public void Partial_Recovery_Unknown_Operator()
    {
        var result = InterpretExpr("a.x * b.y", "a", "b");
        Assert.Equal(ExpressionParseStatus.PartiallyParsed, result.ParseStatus);
        Assert.Equal(InterpretationConfidence.Low, result.Confidence);
        Assert.NotNull(result.Ast);
        var bin = Assert.IsType<BinaryExprNode>(result.Ast);
        Assert.Equal("*", bin.Operator);
    }

    [Fact]
    public void Partial_Recovery_Unknown_Token_In_Operand_Context()
    {
        // Token residual após estrutura parcial
        var result = InterpretExpr("a.x=b.y ???", "a", "b");
        Assert.Equal(ExpressionParseStatus.PartiallyParsed, result.ParseStatus);
        Assert.NotNull(result.Ast);
        Assert.IsType<ComparisonExprNode>(result.Ast);
    }

    [Fact]
    public void Malformed_Still_Invalid()
    {
        var result = InterpretExpr("bi.bistamp=", "bi", "bi2");
        Assert.Equal(ExpressionParseStatus.Invalid, result.ParseStatus);
        Assert.Equal(InterpretationConfidence.None, result.Confidence);
    }

    [Fact]
    public void Empty_Expression()
    {
        var result = InterpretExpr("   ", "a", "b");
        Assert.Equal(ExpressionParseStatus.Empty, result.ParseStatus);
    }

    [Fact]
    public void MissingTable_Tnt_And_Tar()
    {
        var tnt = InterpretWithInventory(
            "tnt.tntstamp=gtxt.stamp and tabela = 'tnt'",
            "gtxt",
            "tnt",
            fromResolved: true,
            toResolved: false,
            tables: [("dbo", "gtxt")]);
        Assert.Equal(ExpressionParseStatus.Parsed, tnt.ParseStatus);
        Assert.Equal(InterpretationConfidence.Medium, tnt.Confidence);
        Assert.Contains(InterpretationDiagnosticCodes.MissingTable, tnt.DiagnosticsJson);
        Assert.DoesNotContain(InterpretationDiagnosticCodes.NameMismatch, tnt.DiagnosticsJson!);

        var tar = InterpretWithInventory(
            "tar.tarstamp=gtxt.stamp and tabela = 'tar'",
            "gtxt",
            "tar",
            fromResolved: true,
            toResolved: false,
            tables: [("dbo", "gtxt")]);
        Assert.Contains(InterpretationDiagnosticCodes.MissingTable, tar.DiagnosticsJson);
    }

    [Fact]
    public void NameMismatch_Epagc_And_Shrpt()
    {
        var ep = InterpretWithInventory(
            "epacgc.epacgcstamp = epacgctab.epacgcstamp",
            "EPAGC",
            "EPAGCTAB",
            fromResolved: true,
            toResolved: true,
            tables: [("dbo", "epagc"), ("dbo", "epagctab")]);
        Assert.Contains(InterpretationDiagnosticCodes.NameMismatch, ep.DiagnosticsJson);
        Assert.Equal(2, ep.TableRefsUnresolved);

        var sh = InterpretWithInventory(
            "sharpt2.shrptstamp = shrpt.shrptstamp",
            "SHRPT2",
            "SHRPT",
            fromResolved: true,
            toResolved: true,
            tables: [("dbo", "shrpt2"), ("dbo", "shrpt")]);
        Assert.Contains(InterpretationDiagnosticCodes.NameMismatch, sh.DiagnosticsJson);
        Assert.Equal(1, sh.TableRefsUnresolved);
        Assert.Equal(1, sh.TableRefsResolved);
    }

    [Fact]
    public void Metrics_Are_Mathematically_Coherent()
    {
        var records = new[]
        {
            InterpretExpr("a.x=b.y", "a", "b"),
            InterpretWithInventory(
                "tnt.x=gtxt.y and tabela='tnt'",
                "gtxt",
                "tnt",
                true,
                false,
                [("dbo", "gtxt")])
        };
        var m = DeclaredRelationInterpretationService.BuildMetrics(records);
        Assert.Equal(m.FieldRefsIdentified, m.QualifiedFieldRefs + m.UnqualifiedFieldRefs);
        Assert.Equal(
            m.QualifiedFieldRefs,
            m.TableRefsResolved + m.TableRefsAmbiguous + m.TableRefsUnresolved);
    }

    [Fact]
    public async Task Ast_V11_Persists_And_RoundTrips()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-v11", "local/demo", CancellationToken.None);

        var expr = $"{Q}C{Q} + LEFT(ra.rastamp, 24) = fo.fostamp";
        var rel = SampleRel("P1", "ra", "fo", expr);
        await workspace.UpsertDeclaredRelationsAsync([rel], CancellationToken.None);
        await workspace.UpsertObjectsAsync([Table("dbo", "ra"), Table("dbo", "fo")], CancellationToken.None);

        var (records, _) = await _svc.InterpretWorkspaceAsync(workspace, CancellationToken.None);
        await workspace.UpsertDeclaredRelationInterpretationsAsync(records, CancellationToken.None);

        var loaded = Assert.Single(await workspace.GetDeclaredRelationInterpretationsAsync());
        Assert.Equal(DeclaredRelationInterpretationService.ParserVersion, loaded.ParserVersion);
        Assert.Equal(ExpressionParseStatus.Parsed, loaded.ParseStatus);
        var cmp = Assert.IsType<ComparisonExprNode>(loaded.Ast);
        Assert.IsType<BinaryExprNode>(cmp.Left);

        // Enum textual
        var json = DeclaredRelationInterpretationService.SerializeAst(loaded.Ast);
        Assert.Contains("Resolved", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"tableResolution\":1", json, StringComparison.Ordinal);

        var fact = Assert.Single(await workspace.GetDeclaredRelationsAsync());
        Assert.Equal(expr, fact.OriginalExpression);
    }

    [Fact]
    public async Task Compatibility_Reads_V1_Numeric_Enum_Ast()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-v1compat", "local/demo", CancellationToken.None);

        // AST V1 com enum numérico
        const string v1Ast =
            """{"$type":"comparison","left":{"$type":"field","originalText":"bi.x","qualifier":"bi","fieldName":"x","tableResolution":1,"tableObjectId":"table:dbo.bi"},"operator":"=","right":{"$type":"field","originalText":"cl.y","qualifier":"cl","fieldName":"y","tableResolution":1,"tableObjectId":"table:dbo.cl"}}""";
        var old = new DeclaredRelationInterpretationRecord(
            "phc.rel:OLD",
            "declared-expr-v1",
            ExpressionParseStatus.Parsed,
            InterpretationConfidence.High,
            JsonSerializer.Deserialize<ExprAstNode>(v1Ast, InterpretationJson.Options),
            null,
            2, 2, 0, 0,
            DateTimeOffset.UtcNow);
        Assert.NotNull(old.Ast);
        await workspace.UpsertDeclaredRelationInterpretationsAsync([old], CancellationToken.None);
        var loaded = Assert.Single(await workspace.GetDeclaredRelationInterpretationsAsync());
        var cmp = Assert.IsType<ComparisonExprNode>(loaded.Ast);
        Assert.Equal(TableReferenceResolution.Resolved, Assert.IsType<FieldRefExprNode>(cmp.Left).TableResolution);
    }

    [Fact]
    public async Task Reinterpretation_Updates_Parser_Version_Idempotently()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-re", "local/demo", CancellationToken.None);

        var expr = $"{Q}F{Q} + LEFT(ra.rastamp, 24) = ft.ftstamp";
        var rel = SampleRel("R1", "ra", "ft", expr);
        await workspace.UpsertDeclaredRelationsAsync([rel], CancellationToken.None);
        await workspace.UpsertObjectsAsync([Table("dbo", "ra"), Table("dbo", "ft")], CancellationToken.None);

        var old = new DeclaredRelationInterpretationRecord(
            rel.RelationId,
            "declared-expr-v1",
            ExpressionParseStatus.Invalid,
            InterpretationConfidence.None,
            null,
            null,
            0, 0, 0, 0,
            DateTimeOffset.UtcNow.AddHours(-1));
        await workspace.UpsertDeclaredRelationInterpretationsAsync([old], CancellationToken.None);

        var (first, _) = await _svc.InterpretWorkspaceAsync(workspace, CancellationToken.None);
        await workspace.UpsertDeclaredRelationInterpretationsAsync(first, CancellationToken.None);
        var (second, metrics) = await _svc.InterpretWorkspaceAsync(workspace, CancellationToken.None);
        await workspace.UpsertDeclaredRelationInterpretationsAsync(second, CancellationToken.None);

        var all = await workspace.GetDeclaredRelationInterpretationsAsync();
        Assert.Single(all);
        Assert.Equal(DeclaredRelationInterpretationService.ParserVersion, all[0].ParserVersion);
        Assert.Equal(ExpressionParseStatus.Parsed, all[0].ParseStatus);
        Assert.Equal(1, metrics.Analyzed);
        Assert.Equal(expr, (await workspace.GetDeclaredRelationsAsync())[0].OriginalExpression);
    }

    [Fact]
    public void Original_Expression_On_Fact_Is_Unchanged()
    {
        const string expr = "bi.bistamp=bi2.bi2stamp";
        var rel = SampleRel("ORIG", "bi", "bi2", expr);
        _ = InterpretExpr(expr, "bi", "bi2");
        Assert.Equal(expr, rel.OriginalExpression);
    }

    [Fact]
    public void Ambiguous_Table_Qualifier()
    {
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["orders"] = ["table:dbo.orders", "table:sales.orders"],
            ["bo"] = ["table:dbo.bo"],
            ["cl"] = ["table:dbo.cl"]
        };
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "table:dbo.orders", "table:sales.orders", "table:dbo.bo", "table:dbo.cl"
        };
        var rel = SampleRel("AMB", "bo", "cl", "orders.id=cl.no");
        var result = _svc.Interpret(rel, index, known, DateTimeOffset.UtcNow);
        Assert.True(result.TableRefsAmbiguous >= 1);
        Assert.Contains(InterpretationDiagnosticCodes.Ambiguous, result.DiagnosticsJson);
    }

    private DeclaredRelationInterpretationRecord InterpretExpr(string expr, string from, string to) =>
        InterpretWithInventory(expr, from, to, true, true, [("dbo", from), ("dbo", to)]);

    private DeclaredRelationInterpretationRecord InterpretWithInventory(
        string expr,
        string from,
        string to,
        bool fromResolved,
        bool toResolved,
        (string schema, string name)[] tables)
    {
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (schema, name) in tables)
        {
            var id = TableInventoryClassification.BuildObjectId(schema, name);
            if (!index.TryGetValue(name, out var list))
            {
                list = [];
                index[name] = list;
            }

            list.Add(id);
        }

        var known = new HashSet<string>(index.Values.SelectMany(v => v), StringComparer.OrdinalIgnoreCase);
        var rel = SampleRel("T", from, to, expr) with
        {
            FromObjectId = fromResolved ? $"table:dbo.{from}" : null,
            ToObjectId = toResolved ? $"table:dbo.{to}" : null,
            FromTableResolution = fromResolved
                ? TableReferenceResolution.Resolved
                : TableReferenceResolution.Unresolved,
            ToTableResolution = toResolved
                ? TableReferenceResolution.Resolved
                : TableReferenceResolution.Unresolved
        };
        // Corrigir object ids para nomes reais do inventário quando diferem do from/to textual
        if (fromResolved && index.TryGetValue(from, out var fl) && fl.Count == 1)
        {
            rel = rel with { FromObjectId = fl[0] };
        }
        else if (fromResolved)
        {
            // EPAGC → table:dbo.epagc
            var match = tables.FirstOrDefault(t =>
                string.Equals(t.name, from, StringComparison.OrdinalIgnoreCase));
            if (match.name is not null)
            {
                rel = rel with
                {
                    FromObjectId = TableInventoryClassification.BuildObjectId(match.schema, match.name)
                };
            }
        }

        if (toResolved && index.TryGetValue(to, out var tl) && tl.Count == 1)
        {
            rel = rel with { ToObjectId = tl[0] };
        }
        else if (toResolved)
        {
            var match = tables.FirstOrDefault(t =>
                string.Equals(t.name, to, StringComparison.OrdinalIgnoreCase));
            if (match.name is not null)
            {
                rel = rel with
                {
                    ToObjectId = TableInventoryClassification.BuildObjectId(match.schema, match.name)
                };
            }
        }

        return _svc.Interpret(rel, index, known, DateTimeOffset.UtcNow);
    }

    private static DeclaredRelationRecord SampleRel(string stamp, string from, string to, string expr) =>
        new(
            $"{DeclaredRelationSourceTypes.PhcRel}:{stamp}",
            DeclaredRelationSourceTypes.PhcRel,
            stamp,
            from,
            null,
            to,
            null,
            expr,
            null, null, null, null, null, null,
            DateTimeOffset.UtcNow,
            $"table:dbo.{from}",
            $"table:dbo.{to}",
            TableReferenceResolution.Resolved,
            TableReferenceResolution.Resolved,
            DeclaredRelationCorrespondenceKind.None,
            null);

    private static TechnicalObjectRecord Table(string schema, string name) =>
        new(
            TableInventoryClassification.BuildObjectId(schema, name),
            ObjectKind.Table,
            name,
            schema,
            null,
            "sql.server.structure",
            null,
            """{"classification":"application_or_unknown"}""",
            DateTimeOffset.UtcNow);
}
