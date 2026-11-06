using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;
using PhcKnowledgeScanner.Analysis.DeclaredRelations;
using PhcKnowledgeScanner.Analysis.Knowledge;
using PhcKnowledgeScanner.Application.Services;

namespace PhcKnowledgeScanner.Tests;

public class InventoryFieldMappingExplorerTests
{
    private readonly DeclaredRelationInterpretationService _interp = new();
    private readonly RelationalKnowledgeProjector _projector = new();

    [Fact]
    public void Obtains_Mappings_For_Relation()
    {
        var snap = ProjectRel("phc.rel:m1", "bo.bostamp=bi.bostamp", "bo", "bi");
        var kr = Assert.Single(snap.KnowledgeRelations);
        var maps = InventoryRelationExplorer.BuildFieldMappings(snap, kr.KnowledgeRelationId);
        Assert.Single(maps);
        Assert.Equal(FieldMappingKind.DirectFieldEquality, maps[0].Kind);
        Assert.Equal(kr.KnowledgeRelationId, snap.GetFieldMapping(maps[0].FieldMappingId)!.KnowledgeRelationId);
    }

    [Fact]
    public void Relation_Without_Mappings()
    {
        // Sem interpretação → sem FieldMappings.
        var declared = Declared("phc.rel:empty", DeclaredRelationSourceTypes.PhcRel, "bo", "bi",
            "bo.bostamp=bi.bostamp", "table:dbo.bo", "table:dbo.bi");
        var snap = _projector.Project([], [declared], []);
        var kr = Assert.Single(snap.KnowledgeRelations);
        var detail = InventoryRelationExplorer.BuildDetail(snap, kr.KnowledgeRelationId)!;
        Assert.Empty(detail.FieldMappings);
    }

    [Fact]
    public void DirectFieldEquality_Both_Resolved()
    {
        var snap = ProjectRel("phc.rel:dfe", "bo.bostamp=bi.bostamp", "bo", "bi");
        var maps = InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId);
        var m = Assert.Single(maps);
        Assert.Equal(FieldMappingKind.DirectFieldEquality, m.Kind);
        Assert.Equal("Igualdade directa de campos", m.KindLabel);
        Assert.NotNull(m.Left.PrimaryField);
        Assert.NotNull(m.Right.PrimaryField);
        Assert.Equal(TableReferenceResolution.Resolved, m.Left.PrimaryField!.TableResolution);
        Assert.Equal(TableReferenceResolution.Resolved, m.Right.PrimaryField!.TableResolution);
        Assert.False(m.HasIncompleteResolution);
        Assert.Contains("Resolvido", m.Left.PrimaryField.ResolutionLabel);
    }

    [Fact]
    public void ExpressionComparison_Is_Distinguished()
    {
        var snap = ProjectRel("phc.rel:expr", "LEFT(ra.rastamp,24)=fo.fostamp", "ra", "fo");
        var m = Assert.Single(InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId));
        Assert.Equal(FieldMappingKind.ExpressionComparison, m.Kind);
        Assert.Equal("Comparação com expressão", m.KindLabel);
        Assert.Equal(FieldMappingOperand.KindExpression, m.Left.Kind);
        Assert.NotEmpty(m.Left.ContainedFields);
        Assert.False(string.IsNullOrWhiteSpace(m.Left.OriginalText));
        Assert.Contains("ExpressionComparison", m.CompactDisplay);
    }

    [Fact]
    public void NonFieldPredicate_Is_Distinguished()
    {
        var snap = ProjectRel("phc.rel:nfp", "tabela='tnt'", "gtxt", "tnt");
        var m = Assert.Single(InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId));
        Assert.Equal(FieldMappingKind.NonFieldPredicate, m.Kind);
        Assert.Equal("Predicado não-campo", m.KindLabel);
        Assert.Equal(FieldMappingOperand.KindLiteral, m.Right.Kind);
        Assert.Contains("NonFieldPredicate", m.CompactDisplay);
    }

    [Fact]
    public void RequiredUnderAnd_Context()
    {
        var snap = ProjectRel("phc.rel:and", "pa.no=cl.no and pa.estab=cl.estab", "pa", "cl");
        var maps = InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId);
        Assert.Equal(2, maps.Count);
        Assert.All(maps, m =>
        {
            Assert.Equal(FieldMappingLogicalContext.RequiredUnderAnd, m.LogicalContext);
            Assert.Contains("AND", m.LogicalContextLabel);
            Assert.Contains("não implica chave composta", m.LogicalContextLabel);
        });
    }

    [Fact]
    public void AlternativeUnderOr_Context()
    {
        var snap = ProjectRel("phc.rel:or", "a.x=b.x or a.y=b.y", "a", "b");
        var maps = InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId);
        Assert.Equal(2, maps.Count);
        Assert.All(maps, m =>
        {
            Assert.Equal(FieldMappingLogicalContext.AlternativeUnderOr, m.LogicalContext);
            Assert.Contains("OR", m.LogicalContextLabel);
            Assert.Contains("alternativa", m.LogicalContextLabel);
        });
    }

    [Fact]
    public void Unqualified_Field_Is_Explicit()
    {
        var snap = ProjectRel("phc.rel:uq", "tabela='tnt'", "gtxt", "tnt");
        var m = Assert.Single(InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId));
        Assert.NotNull(m.Left.PrimaryField);
        Assert.Null(m.Left.PrimaryField!.Qualifier);
        Assert.Equal("Não qualificado", m.Left.PrimaryField.QualifierLabel);
    }

    [Fact]
    public void Unresolved_Field_Is_Marked()
    {
        // Qualificador "ghost" sem tabela no índice → Unresolved.
        var snap = ProjectCustom(
            "phc.rel:unres",
            DeclaredRelationSourceTypes.PhcRel,
            "ghost.x=bo.bostamp",
            from: "ghost",
            to: "bo",
            indexTables: ["bo"]);
        var m = Assert.Single(InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId));
        Assert.True(m.HasIncompleteResolution);
        Assert.Equal(TableReferenceResolution.Unresolved, m.Left.PrimaryField!.TableResolution);
        Assert.Equal("Não resolvido", m.Left.PrimaryField.ResolutionLabel);
    }

    [Fact]
    public void Ambiguous_Field_Is_Marked()
    {
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["x"] = ["table:dbo.x1", "table:dbo.x2"],
            ["bo"] = ["table:dbo.bo"]
        };
        var known = new HashSet<string>(["table:dbo.x1", "table:dbo.x2", "table:dbo.bo"], StringComparer.OrdinalIgnoreCase);
        var declared = Declared("phc.rel:amb", DeclaredRelationSourceTypes.PhcRel, "x", "bo",
            "x.f=bo.bostamp", fromObjectId: null, toObjectId: "table:dbo.bo",
            fromRes: TableReferenceResolution.Ambiguous, toRes: TableReferenceResolution.Resolved);
        var interp = _interp.Interpret(declared, index, known, DateTimeOffset.UtcNow);
        var snap = _projector.Project([], [declared], [interp]);
        var maps = InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId);
        Assert.NotEmpty(maps);
        Assert.Contains(maps, m =>
            m.Left.PrimaryField?.TableResolution == TableReferenceResolution.Ambiguous
            || m.Right.PrimaryField?.TableResolution == TableReferenceResolution.Ambiguous
            || m.Left.ContainedFields.Any(f => f.TableResolution == TableReferenceResolution.Ambiguous)
            || m.Right.ContainedFields.Any(f => f.TableResolution == TableReferenceResolution.Ambiguous)
            || m.HasIncompleteResolution);
        var amb = maps.First(m => m.HasIncompleteResolution ||
                                  m.Left.PrimaryField?.TableResolution == TableReferenceResolution.Ambiguous);
        if (amb.Left.PrimaryField?.TableResolution == TableReferenceResolution.Ambiguous)
        {
            Assert.Equal("Ambíguo", amb.Left.PrimaryField.ResolutionLabel);
        }
    }

    [Fact]
    public void Only_One_Side_Resolved()
    {
        var snap = ProjectCustom(
            "phc.rel:partial",
            DeclaredRelationSourceTypes.PhcRel,
            "ghost.x=bo.bostamp",
            from: "ghost",
            to: "bo",
            indexTables: ["bo"]);
        var m = Assert.Single(InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId));
        Assert.Equal(TableReferenceResolution.Unresolved, m.Left.PrimaryField!.TableResolution);
        Assert.Equal(TableReferenceResolution.Resolved, m.Right.PrimaryField!.TableResolution);
        Assert.True(m.HasIncompleteResolution);
    }

    [Fact]
    public void Mapping_With_Diagnostics_Preserved()
    {
        var left = new FieldRefExprNode("a.x", "a", "x", TableReferenceResolution.Resolved, "table:dbo.a");
        var right = new FieldRefExprNode("b.x", "b", "x", TableReferenceResolution.Resolved, "table:dbo.b");
        var cmp = new ComparisonExprNode(left, "=", right);
        var maps = FieldMappingExtractor.Extract(
            "phc.rel:diag",
            "kr:declared:phc.rel:diag",
            "interp:phc.rel:diag:v",
            "declared-expr-v1.1",
            cmp,
            """[{"code":"NameMismatch"}]""");
        var fm = Assert.Single(maps);
        // Projector path: inject via interpretation DiagnosticsJson on a real project.
        var declared = Declared("phc.rel:diag2", DeclaredRelationSourceTypes.PhcRel, "a", "b",
            "a.x=b.x", "table:dbo.a", "table:dbo.b");
        var interp = new DeclaredRelationInterpretationRecord(
            declared.RelationId,
            "declared-expr-v1.1",
            ExpressionParseStatus.Parsed,
            InterpretationConfidence.High,
            cmp,
            """[{"code":"NameMismatch"}]""",
            2, 2, 0, 0,
            DateTimeOffset.UtcNow);
        var snap = _projector.Project([], [declared], [interp]);
        var view = Assert.Single(InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId));
        Assert.Contains("NameMismatch", view.DiagnosticsJson);
        Assert.Equal(fm.Kind, view.Kind);
    }

    [Fact]
    public void Provenance_Rel_Evidence()
    {
        var snap = ProjectRel("phc.rel:prov", "bo.bostamp=bi.bostamp", "bo", "bi");
        var m = Assert.Single(InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId));
        Assert.Equal(RelationEvidenceKind.PhcRel, m.EvidenceKind);
        Assert.Equal("PHC REL", m.EvidenceKindLabel);
        Assert.StartsWith("phc.rel:", m.EvidenceId, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(m.InterpretationEvidenceId));
        Assert.Equal("declared-expr-v1.1", m.ParserVersion);
        Assert.False(string.IsNullOrWhiteSpace(m.AstPath));
        Assert.False(string.IsNullOrWhiteSpace(m.LogicalPath));
        Assert.False(string.IsNullOrWhiteSpace(m.PredicateText));
    }

    [Fact]
    public void Provenance_Dtl_Evidence()
    {
        var snap = ProjectCustom(
            "phc.dtl:prov",
            DeclaredRelationSourceTypes.PhcDtl,
            "fd.no=pe.no",
            "fd",
            "pe",
            ["fd", "pe"]);
        var m = Assert.Single(InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId));
        Assert.Equal(RelationEvidenceKind.PhcDtl, m.EvidenceKind);
        Assert.Equal("PHC DTL", m.EvidenceKindLabel);
        Assert.StartsWith("phc.dtl:", m.EvidenceId, StringComparison.Ordinal);
    }

    [Fact]
    public void Deterministic_Order_By_FieldMappingId()
    {
        var snap = ProjectRel("phc.rel:ord", "pa.no=cl.no and pa.estab=cl.estab", "pa", "cl");
        var maps = InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId);
        Assert.Equal(2, maps.Count);
        Assert.True(string.CompareOrdinal(maps[0].FieldMappingId, maps[1].FieldMappingId) < 0);
        var again = InventoryRelationExplorer.BuildFieldMappings(snap, snap.KnowledgeRelations[0].KnowledgeRelationId);
        Assert.Equal(maps.Select(m => m.FieldMappingId), again.Select(m => m.FieldMappingId));
    }

    [Fact]
    public void Switching_Relation_Does_Not_Rebuild_Projection()
    {
        const string group = "corr:exact:FD:PE:aabbccddeeff0011";
        var rel = Declared("phc.rel:fd", DeclaredRelationSourceTypes.PhcRel, "fd", "pe",
            "fd.no=pe.no", "table:dbo.fd", "table:dbo.pe",
            DeclaredRelationCorrespondenceKind.Exact, group);
        var dtl = Declared("phc.dtl:fd", DeclaredRelationSourceTypes.PhcDtl, "fd", "pe",
            "fd.no=pe.no", "table:dbo.fd", "table:dbo.pe",
            DeclaredRelationCorrespondenceKind.Exact, group);
        var other = Declared("phc.rel:other", DeclaredRelationSourceTypes.PhcRel, "bo", "bi",
            "bo.bostamp=bi.bostamp", "table:dbo.bo", "table:dbo.bi");
        var interps = new[]
        {
            Interpret("phc.rel:fd", "fd.no=pe.no", "fd", "pe"),
            Interpret("phc.dtl:fd", "fd.no=pe.no", "fd", "pe"),
            Interpret("phc.rel:other", "bo.bostamp=bi.bostamp", "bo", "bi")
        };
        var snap = _projector.Project([], [rel, dtl, other], interps);
        Assert.Equal(2, snap.KnowledgeRelations.Count);

        var fd = snap.KnowledgeRelations.Single(r => r.ConsolidationBasis == KnowledgeConsolidationBasis.DeclaredExactCorrespondence);
        var bo = snap.KnowledgeRelations.Single(r => r.ConsolidationBasis == KnowledgeConsolidationBasis.Singleton);

        var detailFd = InventoryRelationExplorer.BuildDetail(snap, fd.KnowledgeRelationId)!;
        var detailBo = InventoryRelationExplorer.BuildDetail(snap, bo.KnowledgeRelationId)!;
        Assert.NotEmpty(detailFd.FieldMappings);
        Assert.NotEmpty(detailBo.FieldMappings);
        Assert.Same(snap.FieldMappings, snap.FieldMappings);
        Assert.Equal(snap.FieldMappings.Count,
            detailFd.FieldMappings.Count + detailBo.FieldMappings.Count
            - detailFd.FieldMappings.Count(m => detailBo.FieldMappings.Any(x => x.FieldMappingId == m.FieldMappingId)));
        // Contagens estáveis: soma das views por KR cobre todos os mappings com KnowledgeRelationId.
        Assert.Equal(
            snap.FieldMappings.Count(m => m.KnowledgeRelationId is not null),
            InventoryRelationExplorer.BuildFieldMappings(snap, fd.KnowledgeRelationId).Count
            + InventoryRelationExplorer.BuildFieldMappings(snap, bo.KnowledgeRelationId).Count);
    }

    private RelationalKnowledgeSnapshot ProjectRel(string relationId, string expr, params string[] tables)
    {
        var from = tables[0];
        var to = tables[1];
        var declared = Declared(relationId, DeclaredRelationSourceTypes.PhcRel, from, to, expr,
            $"table:dbo.{from.ToLowerInvariant()}", $"table:dbo.{to.ToLowerInvariant()}");
        var interp = Interpret(relationId, expr, tables);
        return _projector.Project([], [declared], [interp]);
    }

    private RelationalKnowledgeSnapshot ProjectCustom(
        string relationId,
        string sourceType,
        string expr,
        string from,
        string to,
        IReadOnlyList<string> indexTables)
    {
        var fromRes = indexTables.Any(t => t.Equals(from, StringComparison.OrdinalIgnoreCase))
            ? TableReferenceResolution.Resolved
            : TableReferenceResolution.Unresolved;
        var toRes = indexTables.Any(t => t.Equals(to, StringComparison.OrdinalIgnoreCase))
            ? TableReferenceResolution.Resolved
            : TableReferenceResolution.Unresolved;
        string? fromId = fromRes == TableReferenceResolution.Resolved
            ? $"table:dbo.{from.ToLowerInvariant()}"
            : null;
        string? toId = toRes == TableReferenceResolution.Resolved
            ? $"table:dbo.{to.ToLowerInvariant()}"
            : null;
        var declared = Declared(relationId, sourceType, from, to, expr, fromId, toId, fromRes: fromRes, toRes: toRes);
        var interp = Interpret(relationId, expr, indexTables.ToArray());
        return _projector.Project([], [declared], [interp]);
    }

    private DeclaredRelationInterpretationRecord Interpret(string relationId, string expr, params string[] tables)
    {
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tables.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var id = $"table:dbo.{t.ToLowerInvariant()}";
            index[t] = [id];
            index[t.ToLowerInvariant()] = [id];
        }

        var known = new HashSet<string>(index.Values.SelectMany(v => v), StringComparer.OrdinalIgnoreCase);
        var from = tables.ElementAtOrDefault(0) ?? "a";
        var to = tables.ElementAtOrDefault(1) ?? "b";
        var sourceType = relationId.StartsWith("phc.dtl", StringComparison.Ordinal)
            ? DeclaredRelationSourceTypes.PhcDtl
            : DeclaredRelationSourceTypes.PhcRel;
        var declared = Declared(relationId, sourceType, from, to, expr,
            index.ContainsKey(from) ? $"table:dbo.{from.ToLowerInvariant()}" : null,
            index.ContainsKey(to) ? $"table:dbo.{to.ToLowerInvariant()}" : null,
            fromRes: index.ContainsKey(from) ? TableReferenceResolution.Resolved : TableReferenceResolution.Unresolved,
            toRes: index.ContainsKey(to) ? TableReferenceResolution.Resolved : TableReferenceResolution.Unresolved);
        return _interp.Interpret(declared, index, known, DateTimeOffset.UtcNow);
    }

    private static DeclaredRelationRecord Declared(
        string relationId,
        string sourceType,
        string from,
        string to,
        string expr,
        string? fromObjectId,
        string? toObjectId,
        DeclaredRelationCorrespondenceKind correspondence = DeclaredRelationCorrespondenceKind.None,
        string? group = null,
        TableReferenceResolution? fromRes = null,
        TableReferenceResolution? toRes = null) =>
        new(
            relationId,
            sourceType,
            relationId,
            from,
            null,
            to,
            null,
            expr,
            null, null, null, null, null, null,
            DateTimeOffset.UtcNow,
            fromObjectId,
            toObjectId,
            fromRes ?? (fromObjectId is null ? TableReferenceResolution.Unresolved : TableReferenceResolution.Resolved),
            toRes ?? (toObjectId is null ? TableReferenceResolution.Unresolved : TableReferenceResolution.Resolved),
            correspondence,
            group);
}
