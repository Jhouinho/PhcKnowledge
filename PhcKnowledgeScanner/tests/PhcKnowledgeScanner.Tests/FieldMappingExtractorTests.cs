using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;
using PhcKnowledgeScanner.Analysis.DeclaredRelations;
using PhcKnowledgeScanner.Analysis.Knowledge;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public class FieldMappingExtractorTests
{
    private readonly DeclaredRelationInterpretationService _interp = new();
    private readonly RelationalKnowledgeProjector _projector = new();
    private const char Q = '\u00B4';

    [Fact]
    public void Direct_Equality_Between_Two_FieldRefs()
    {
        var m = Assert.Single(Extract("bi.bistamp=bi2.bi2stamp", "bi", "bi2"));
        Assert.Equal(FieldMappingKind.DirectFieldEquality, m.Kind);
        Assert.Equal("=", m.Operator);
        Assert.Equal(FieldMappingOperand.KindField, m.Left.Kind);
        Assert.Equal(FieldMappingOperand.KindField, m.Right.Kind);
        Assert.Equal("bi.bistamp", m.Left.Field!.OriginalText);
        Assert.Equal("bi2.bi2stamp", m.Right.Field!.OriginalText);
        Assert.Equal(FieldMappingLogicalContext.RequiredUnderAnd, m.LogicalContext);
        Assert.Equal("phc.rel:test", m.EvidenceId);
    }

    [Fact]
    public void Comparison_With_Function_One_Side()
    {
        var m = Assert.Single(Extract("LEFT(ra.rastamp,24)=fo.fostamp", "ra", "fo"));
        Assert.Equal(FieldMappingKind.ExpressionComparison, m.Kind);
        Assert.Equal(FieldMappingOperand.KindExpression, m.Left.Kind);
        Assert.Equal(FieldMappingOperand.KindField, m.Right.Kind);
        Assert.Contains(m.Left.ContainedFields, f => f.FieldName.Equals("rastamp", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("fostamp", m.Right.Field!.FieldName);
    }

    [Fact]
    public void Comparison_With_Expression_Both_Sides()
    {
        var m = Assert.Single(Extract($"{Q}C{Q}+LEFT(ra.rastamp,24)=fo.fostamp", "ra", "fo"));
        Assert.Equal(FieldMappingKind.ExpressionComparison, m.Kind);
        Assert.Equal(FieldMappingOperand.KindExpression, m.Left.Kind);
        Assert.Equal(FieldMappingOperand.KindField, m.Right.Kind);
        Assert.Contains(m.Left.ContainedFields, f => f.FieldName.Equals("rastamp", StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrWhiteSpace(m.Left.OriginalText));
    }

    [Fact]
    public void Field_Equals_Literal_Is_NonFieldPredicate()
    {
        var m = Assert.Single(Extract("tabela='tnt'", "gtxt", "tnt"));
        Assert.Equal(FieldMappingKind.NonFieldPredicate, m.Kind);
        Assert.Equal(FieldMappingOperand.KindField, m.Left.Kind);
        Assert.Equal(FieldMappingOperand.KindLiteral, m.Right.Kind);
        Assert.Equal("tabela", m.Left.Field!.FieldName);
        Assert.Null(m.Left.Field.Qualifier);
    }

    [Fact]
    public void Non_Equality_Operators_Are_Never_DirectFieldEquality()
    {
        foreach (var op in new[] { "<>", "!=", "<", ">", "<=", ">=" })
        {
            // AST via nós manuais (tokenizer pode não cobrir todos)
            var left = new FieldRefExprNode("a.x", "a", "x", TableReferenceResolution.Resolved, "table:dbo.a");
            var right = new FieldRefExprNode("b.x", "b", "x", TableReferenceResolution.Resolved, "table:dbo.b");
            var cmp = new ComparisonExprNode(left, op, right);
            var maps = FieldMappingExtractor.Extract(
                "phc.rel:op", null, "interp:phc.rel:op:v", "declared-expr-v1.1", cmp, null);
            var m = Assert.Single(maps);
            Assert.Equal(FieldMappingKind.NonFieldPredicate, m.Kind);
            Assert.NotEqual(FieldMappingKind.DirectFieldEquality, m.Kind);
        }
    }

    [Fact]
    public void Simple_And_Does_Not_Infer_Composite_Key()
    {
        var maps = Extract("pa.no=cl.no and pa.estab=cl.estab", "pa", "cl");
        Assert.Equal(2, maps.Count);
        Assert.All(maps, m =>
        {
            Assert.Equal(FieldMappingKind.DirectFieldEquality, m.Kind);
            Assert.Equal(FieldMappingLogicalContext.RequiredUnderAnd, m.LogicalContext);
        });
        // Dois mappings independentes — sem entidade "composite key"
        Assert.Equal(2, maps.Select(m => m.FieldMappingId).Distinct().Count());
        Assert.Contains(maps, m => m.Left.Field!.FieldName == "no");
        Assert.Contains(maps, m => m.Left.Field!.FieldName == "estab");
    }

    [Fact]
    public void Simple_Or_Is_Alternative_Not_Required()
    {
        var maps = Extract("a.x=b.x or a.y=b.y", "a", "b");
        Assert.Equal(2, maps.Count);
        Assert.All(maps, m => Assert.Equal(FieldMappingLogicalContext.AlternativeUnderOr, m.LogicalContext));
    }

    [Fact]
    public void And_Inside_Or_Preserves_Alternative_Context()
    {
        var maps = Extract("(a.x=b.x and a.y=b.y) or a.z=c.z", "a", "b", "c");
        Assert.Equal(3, maps.Count);
        Assert.All(maps, m => Assert.Equal(FieldMappingLogicalContext.AlternativeUnderOr, m.LogicalContext));
        Assert.Contains(maps, m => m.LogicalPath.Contains("OR[0]", StringComparison.Ordinal));
        Assert.Contains(maps, m => m.LogicalPath.Contains("OR[1]", StringComparison.Ordinal));
        // AND sob OR não cria chave composta
        Assert.Equal(3, maps.Count(m => m.Kind == FieldMappingKind.DirectFieldEquality));
    }

    [Fact]
    public void Or_Inside_And_Marks_Or_Branch_As_Alternative()
    {
        var maps = Extract("a.w=b.w and (a.x=b.x or a.y=b.y)", "a", "b");
        Assert.Equal(3, maps.Count);
        var required = maps.Where(m => m.LogicalContext == FieldMappingLogicalContext.RequiredUnderAnd).ToList();
        var alt = maps.Where(m => m.LogicalContext == FieldMappingLogicalContext.AlternativeUnderOr).ToList();
        Assert.Single(required);
        Assert.Equal(2, alt.Count);
        Assert.Equal("w", required[0].Left.Field!.FieldName);
    }

    [Fact]
    public void Nested_Groups_Preserve_Logical_Path()
    {
        var maps = Extract("((a.x=b.x))", "a", "b");
        var m = Assert.Single(maps);
        Assert.Contains("/group", m.AstPath, StringComparison.Ordinal);
        Assert.Contains("/group", m.LogicalPath, StringComparison.Ordinal);
    }

    [Fact]
    public void Qualified_Resolved_Field_Preserved()
    {
        var m = Assert.Single(Extract("bo.bostamp=bi.bostamp", "bo", "bi"));
        Assert.Equal(TableReferenceResolution.Resolved, m.Left.Field!.TableResolution);
        Assert.Equal("table:dbo.bo", m.Left.Field.TableObjectId);
        Assert.Equal(TableReferenceResolution.Resolved, m.Right.Field!.TableResolution);
    }

    [Fact]
    public void Unqualified_Field_Preserved_Without_Guessing()
    {
        var m = Assert.Single(Extract("tabela='tnt'", "gtxt", "tnt"));
        Assert.Null(m.Left.Field!.Qualifier);
        Assert.Equal(TableReferenceResolution.Unresolved, m.Left.Field.TableResolution);
    }

    [Fact]
    public void Unresolved_Table_Qualifier_Still_Emits_Mapping()
    {
        // Qualifier "zz" não existe no inventário do InterpretExpr helper
        var m = Assert.Single(Extract("zz.col=bo.bostamp", "bo", "bi"));
        Assert.Equal(FieldMappingKind.DirectFieldEquality, m.Kind);
        Assert.True(
            m.Left.Field!.TableResolution is TableReferenceResolution.Unresolved
                or TableReferenceResolution.Ambiguous
                or TableReferenceResolution.Resolved);
        // Mesmo incompleto, mapping existe
        Assert.NotNull(m.FieldMappingId);
    }

    [Fact]
    public void Incomplete_Resolution_Does_Not_Drop_Mapping()
    {
        var left = new FieldRefExprNode("mystery.x", "mystery", "x", TableReferenceResolution.Unresolved, null);
        var right = new FieldRefExprNode("y", null, "y", TableReferenceResolution.Unresolved, null);
        var maps = FieldMappingExtractor.Extract(
            "phc.dtl:inc", "kr:declared:phc.dtl:inc", "interp:phc.dtl:inc:v", "declared-expr-v1.1",
            new ComparisonExprNode(left, "=", right),
            """[{"code":"MissingTable"}]""");
        var m = Assert.Single(maps);
        Assert.Equal(FieldMappingKind.DirectFieldEquality, m.Kind);
        Assert.Contains("MissingTable", m.DiagnosticsJson);
    }

    [Fact]
    public void Mapping_Binds_To_Declared_EvidenceId_Not_Interpretation()
    {
        var maps = Extract("a.x=b.x", "a", "b");
        Assert.Equal("phc.rel:test", Assert.Single(maps).EvidenceId);
        Assert.StartsWith("interp:", Assert.Single(maps).InterpretationEvidenceId);
    }

    [Fact]
    public void KnowledgeRelation_With_Multiple_Evidences_Keeps_Independent_Mappings()
    {
        const string group = "corr:exact:A:B:abcdabcdabcdabcd";
        var rel = Declared("phc.rel:m1", "A", "B", "a.x=b.x", group);
        var dtl = Declared("phc.dtl:m2", "A", "B", "a.y=b.y", group);
        var interps = new[]
        {
            InterpretRecord("phc.rel:m1", "a.x=b.x", "A", "B"),
            InterpretRecord("phc.dtl:m2", "a.y=b.y", "A", "B")
        };

        var snap = _projector.Project([], [rel, dtl], interps);
        Assert.Single(snap.KnowledgeRelations);
        var krId = snap.KnowledgeRelations[0].KnowledgeRelationId;
        var maps = snap.GetFieldMappings(knowledgeRelationId: krId);
        Assert.Equal(2, maps.Count);
        Assert.Contains(maps, m => m.EvidenceId == "phc.rel:m1" && m.Left.Field!.FieldName == "x");
        Assert.Contains(maps, m => m.EvidenceId == "phc.dtl:m2" && m.Left.Field!.FieldName == "y");
        // Não deduplicar por nomes coincidentes entre evidências
        Assert.Equal(2, maps.Select(m => m.FieldMappingId).Distinct().Count());
    }

    [Fact]
    public void Ids_Are_Deterministic_And_Unique()
    {
        var a = Extract("a.x=b.x and a.y=b.y", "a", "b");
        var b = Extract("a.x=b.x and a.y=b.y", "a", "b");
        Assert.Equal(a.Select(m => m.FieldMappingId), b.Select(m => m.FieldMappingId));
        Assert.Equal(a.Count, a.Select(m => m.FieldMappingId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Predicate_Text_And_Provenance_Preserved()
    {
        var m = Assert.Single(Extract("bo.bostamp=bi.bostamp", "bo", "bi"));
        Assert.Contains("bostamp", m.PredicateText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("=", m.Operator);
        Assert.False(string.IsNullOrWhiteSpace(m.AstPath));
        Assert.Equal("declared-expr-v1.1", m.ParserVersion);
    }

    [Fact]
    public async Task Reproject_Yields_Equivalent_FieldMappings()
    {
        await using var workspace = await CreateWorkspaceAsync();
        var rel = Declared("phc.rel:rp", "BO", "BI", "bo.bostamp=bi.bostamp", null);
        await workspace.UpsertDeclaredRelationsAsync([rel], CancellationToken.None);
        await workspace.UpsertDeclaredRelationInterpretationsAsync(
            [InterpretRecord("phc.rel:rp", "bo.bostamp=bi.bostamp", "BO", "BI")],
            CancellationToken.None);

        var s1 = await _projector.ProjectAsync(workspace);
        var s2 = await _projector.ProjectAsync(workspace);
        Assert.Equal(
            s1.FieldMappings.Select(m => (m.FieldMappingId, m.Kind, m.EvidenceId, m.LogicalContext)),
            s2.FieldMappings.Select(m => (m.FieldMappingId, m.Kind, m.EvidenceId, m.LogicalContext)));
    }

    [Fact]
    public void Input_Order_Does_Not_Change_Mapping_Ids()
    {
        const string group = "corr:exact:P:Q:1122334455667788";
        var rel = Declared("phc.rel:o1", "P", "Q", "p.a=q.a", group);
        var dtl = Declared("phc.dtl:o2", "P", "Q", "p.a=q.a", group);
        var interps = new[]
        {
            InterpretRecord("phc.rel:o1", "p.a=q.a", "P", "Q"),
            InterpretRecord("phc.dtl:o2", "p.a=q.a", "P", "Q")
        };

        var s1 = _projector.Project([], [rel, dtl], interps);
        var s2 = _projector.Project([], [dtl, rel], interps.Reverse().ToList());
        Assert.Equal(
            s1.FieldMappings.Select(m => m.FieldMappingId).OrderBy(x => x, StringComparer.Ordinal),
            s2.FieldMappings.Select(m => m.FieldMappingId).OrderBy(x => x, StringComparer.Ordinal));
    }

    private IReadOnlyList<FieldMapping> Extract(string expr, params string[] tables)
    {
        var record = InterpretRecord("phc.rel:test", expr, tables);
        return FieldMappingExtractor.Extract(
            "phc.rel:test",
            "kr:declared:phc.rel:test",
            KnowledgeRelationIds.InterpretationEvidenceId("phc.rel:test", record.ParserVersion),
            record.ParserVersion,
            record.Ast,
            record.DiagnosticsJson);
    }

    private DeclaredRelationInterpretationRecord InterpretRecord(
        string relationId,
        string expr,
        params string[] tables)
    {
        var from = tables.ElementAtOrDefault(0) ?? "a";
        var to = tables.ElementAtOrDefault(1) ?? "b";
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tables.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var id = $"table:dbo.{t.ToLowerInvariant()}";
            index[t] = [id];
            index[t.ToLowerInvariant()] = [id];
        }

        var known = new HashSet<string>(index.Values.SelectMany(v => v), StringComparer.OrdinalIgnoreCase);
        var declared = new DeclaredRelationRecord(
            relationId,
            DeclaredRelationSourceTypes.PhcRel,
            "stamp",
            from,
            null,
            to,
            null,
            expr,
            null, null, null, null, null, null,
            DateTimeOffset.UtcNow,
            $"table:dbo.{from.ToLowerInvariant()}",
            $"table:dbo.{to.ToLowerInvariant()}",
            TableReferenceResolution.Resolved,
            TableReferenceResolution.Resolved,
            DeclaredRelationCorrespondenceKind.None,
            null);

        return _interp.Interpret(declared, index, known, DateTimeOffset.UtcNow);
    }

    private static DeclaredRelationRecord Declared(
        string id,
        string from,
        string to,
        string expr,
        string? exactGroup) =>
        new(
            id,
            id.StartsWith("phc.dtl", StringComparison.Ordinal)
                ? DeclaredRelationSourceTypes.PhcDtl
                : DeclaredRelationSourceTypes.PhcRel,
            id,
            from,
            null,
            to,
            null,
            expr,
            null, null, null, null, null, null,
            DateTimeOffset.UtcNow,
            $"table:dbo.{from.ToLowerInvariant()}",
            $"table:dbo.{to.ToLowerInvariant()}",
            TableReferenceResolution.Resolved,
            TableReferenceResolution.Resolved,
            exactGroup is null
                ? DeclaredRelationCorrespondenceKind.None
                : DeclaredRelationCorrespondenceKind.Exact,
            exactGroup);

    private static async Task<SqliteScanWorkspace> CreateWorkspaceAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        var ws = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await ws.InitializeAsync("fm", "local/fm", CancellationToken.None);
        return ws;
    }
}
