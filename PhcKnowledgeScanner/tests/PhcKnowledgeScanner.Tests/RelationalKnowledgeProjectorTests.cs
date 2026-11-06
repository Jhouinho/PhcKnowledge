using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;
using PhcKnowledgeScanner.Analysis.Knowledge;
using PhcKnowledgeScanner.Collectors.Sql;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public class RelationalKnowledgeProjectorTests
{
    private readonly RelationalKnowledgeProjector _projector = new();

    [Fact]
    public void Isolated_Fk_Produces_Singleton_KnowledgeRelation()
    {
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo", "bi", "FK_bi_bo", "dbo", "bo",
            [("bostamp", "bostamp", 1)], DateTimeOffset.UtcNow);

        var snap = _projector.Project([fk], [], []);

        Assert.Single(snap.GetEvidences(RelationEvidenceKind.SqlForeignKey));
        Assert.Single(snap.KnowledgeRelations);
        var kr = snap.KnowledgeRelations[0];
        Assert.Equal(KnowledgeConsolidationBasis.Singleton, kr.ConsolidationBasis);
        Assert.Equal(KnowledgeRelationIds.ForeignKeyKnowledgeRelationId(fk.ObjectId), kr.KnowledgeRelationId);
        Assert.Equal("table:dbo.bi", kr.From.ObjectId);
        Assert.Equal("table:dbo.bo", kr.To.ObjectId);
        Assert.Equal(EndpointResolutionState.BothResolved, kr.EndpointResolutionState);
    }

    [Fact]
    public void Exact_Rel_And_Dtl_Produce_Single_KnowledgeRelation()
    {
        const string group = "corr:exact:BO:BI:aabbccdd11223344";
        var rel = Declared(
            "phc.rel:r1", DeclaredRelationSourceTypes.PhcRel, "BO", "BI", "bo.bostamp=bi.bostamp",
            "table:dbo.bo", "table:dbo.bi", DeclaredRelationCorrespondenceKind.Exact, group);
        var dtl = Declared(
            "phc.dtl:d1", DeclaredRelationSourceTypes.PhcDtl, "BO", "BI", "bo.bostamp=bi.bostamp",
            "table:dbo.bo", "table:dbo.bi", DeclaredRelationCorrespondenceKind.Exact, group);

        var snap = _projector.Project([], [rel, dtl], []);

        Assert.Equal(2, snap.GetEvidences().Count(e => e.Kind is RelationEvidenceKind.PhcRel or RelationEvidenceKind.PhcDtl));
        Assert.Single(snap.KnowledgeRelations);
        var kr = Assert.Single(snap.KnowledgeRelations);
        Assert.Equal(KnowledgeConsolidationBasis.DeclaredExactCorrespondence, kr.ConsolidationBasis);
        Assert.Equal(KnowledgeRelationIds.ExactKnowledgeRelationId(group), kr.KnowledgeRelationId);
        Assert.Equal(2, kr.LinkEvidenceIds.Count);
        Assert.Contains("phc.rel:r1", kr.LinkEvidenceIds);
        Assert.Contains("phc.dtl:d1", kr.LinkEvidenceIds);
    }

    [Fact]
    public void Exact_Group_With_Four_Members_Produces_Single_KnowledgeRelation()
    {
        // Fixture realista: interh FD→PE (2 REL + 2 DTL, mesma expressão).
        const string group = "corr:exact:FD:PE:bee6de5d5942ed36";
        var members = new[]
        {
            Declared("phc.rel:ADM25022740221,644000001", DeclaredRelationSourceTypes.PhcRel, "FD", "PE", "fd.no=pe.no",
                "table:dbo.fd", "table:dbo.pe", DeclaredRelationCorrespondenceKind.Exact, group),
            Declared("phc.rel:ADM25022740221,644000002", DeclaredRelationSourceTypes.PhcRel, "FD", "PE", "fd.no=pe.no",
                "table:dbo.fd", "table:dbo.pe", DeclaredRelationCorrespondenceKind.Exact, group),
            Declared("phc.dtl:ADM25022740221,635000099", DeclaredRelationSourceTypes.PhcDtl, "FD", "PE", "fd.no=pe.no",
                "table:dbo.fd", "table:dbo.pe", DeclaredRelationCorrespondenceKind.Exact, group),
            Declared("phc.dtl:ADM25022740221,635000100", DeclaredRelationSourceTypes.PhcDtl, "FD", "PE", "fd.no=pe.no",
                "table:dbo.fd", "table:dbo.pe", DeclaredRelationCorrespondenceKind.Exact, group),
        };

        // Ordem de leitura deliberadamente não canónica
        var shuffled = members.Reverse().ToList();
        var snap = _projector.Project([], shuffled, []);

        Assert.Single(snap.KnowledgeRelations);
        var kr = snap.KnowledgeRelations[0];
        Assert.Equal(4, kr.LinkEvidenceIds.Count);
        Assert.Equal(
            KnowledgeRelationIds.CanonicalizeEvidenceIds(members.Select(m => m.RelationId)),
            kr.LinkEvidenceIds);
    }

    [Fact]
    public void Rel_Without_Correspondence_Is_Independent()
    {
        var rel = Declared(
            "phc.rel:alone", DeclaredRelationSourceTypes.PhcRel, "GTXT", "TNT", "1=1",
            "table:dbo.gtxt", null, DeclaredRelationCorrespondenceKind.None, null,
            toRes: TableReferenceResolution.Unresolved);

        var snap = _projector.Project([], [rel], []);
        var kr = Assert.Single(snap.KnowledgeRelations);
        Assert.Equal(KnowledgeConsolidationBasis.Singleton, kr.ConsolidationBasis);
        Assert.Equal(KnowledgeRelationIds.SingletonDeclaredKnowledgeRelationId(rel.RelationId), kr.KnowledgeRelationId);
        Assert.Equal(EndpointResolutionState.Partial, kr.EndpointResolutionState);
    }

    [Fact]
    public void Dtl_Without_Correspondence_Is_Independent()
    {
        var dtl = Declared(
            "phc.dtl:alone", DeclaredRelationSourceTypes.PhcDtl, "X", "Y", "x.a=y.a",
            null, null, DeclaredRelationCorrespondenceKind.None, null,
            fromRes: TableReferenceResolution.Unresolved,
            toRes: TableReferenceResolution.Unresolved);

        var snap = _projector.Project([], [dtl], []);
        Assert.Single(snap.KnowledgeRelations);
        Assert.Equal(EndpointResolutionState.Unresolved, snap.KnowledgeRelations[0].EndpointResolutionState);
    }

    [Fact]
    public void Probable_Group_Is_Not_Consolidated()
    {
        const string probable = "corr:probable:BO:ST:stamp1";
        var rel = Declared(
            "phc.rel:p1", DeclaredRelationSourceTypes.PhcRel, "BO", "ST", "bo.x=st.x",
            "table:dbo.bo", "table:dbo.st", DeclaredRelationCorrespondenceKind.Probable, probable);
        var dtl = Declared(
            "phc.dtl:p2", DeclaredRelationSourceTypes.PhcDtl, "BO", "ST", "bo.y=st.y",
            "table:dbo.bo", "table:dbo.st", DeclaredRelationCorrespondenceKind.Probable, probable);

        var snap = _projector.Project([], [rel, dtl], []);

        Assert.Equal(2, snap.KnowledgeRelations.Count);
        Assert.All(snap.KnowledgeRelations, kr =>
        {
            Assert.Equal(KnowledgeConsolidationBasis.Singleton, kr.ConsolidationBasis);
            Assert.Equal(probable, kr.ProbableCorrespondenceGroupId);
            Assert.Null(kr.ExactCorrespondenceGroupId);
        });
    }

    [Fact]
    public void Fk_And_Rel_With_Same_Endpoints_Remain_Independent()
    {
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo", "bi", "FK_bi_bo", "dbo", "bo",
            [("bostamp", "bostamp", 1)], DateTimeOffset.UtcNow);
        var rel = Declared(
            "phc.rel:same", DeclaredRelationSourceTypes.PhcRel, "BI", "BO", "bi.bostamp=bo.bostamp",
            "table:dbo.bi", "table:dbo.bo", DeclaredRelationCorrespondenceKind.None, null);

        var snap = _projector.Project([fk], [rel], []);

        Assert.Equal(2, snap.KnowledgeRelations.Count);
        Assert.Contains(snap.KnowledgeRelations, r => r.KnowledgeRelationId.StartsWith("kr:fk:", StringComparison.Ordinal));
        Assert.Contains(snap.KnowledgeRelations, r => r.KnowledgeRelationId.StartsWith("kr:declared:", StringComparison.Ordinal));
    }

    [Fact]
    public void Unresolved_Endpoints_Are_Preserved()
    {
        var rel = Declared(
            "phc.rel:unres", DeclaredRelationSourceTypes.PhcRel, "GTXT", "TAR", "gtxt.x=tar.x",
            "table:dbo.gtxt", null, DeclaredRelationCorrespondenceKind.None, null,
            toRes: TableReferenceResolution.Unresolved);

        var snap = _projector.Project([], [rel], []);
        var kr = Assert.Single(snap.KnowledgeRelations);
        Assert.Equal("GTXT", kr.From.RawLabel);
        Assert.Equal("TAR", kr.To.RawLabel);
        Assert.Null(kr.To.ObjectId);
        Assert.Equal(EndpointResolutionState.Partial, kr.EndpointResolutionState);
    }

    [Fact]
    public void Interpretations_Attach_To_Correct_Evidences_Not_Separate_Kr()
    {
        const string group = "corr:exact:A:B:deadbeef00000001";
        var rel = Declared(
            "phc.rel:i1", DeclaredRelationSourceTypes.PhcRel, "A", "B", "a.x=b.x",
            "table:dbo.a", "table:dbo.b", DeclaredRelationCorrespondenceKind.Exact, group);
        var dtl = Declared(
            "phc.dtl:i2", DeclaredRelationSourceTypes.PhcDtl, "A", "B", "a.x=b.x",
            "table:dbo.a", "table:dbo.b", DeclaredRelationCorrespondenceKind.Exact, group);
        var interps = new[]
        {
            Interp("phc.rel:i1", "declared-expr-v1.1", InterpretationConfidence.High, """[{"code":"UnqualifiedField"}]"""),
            Interp("phc.dtl:i2", "declared-expr-v1.1", InterpretationConfidence.Medium, """[{"code":"MissingTable"}]""")
        };

        var snap = _projector.Project([], [rel, dtl], interps);

        Assert.Equal(2, snap.GetEvidences(RelationEvidenceKind.ExpressionInterpretation).Count);
        Assert.Single(snap.KnowledgeRelations);
        var kr = snap.KnowledgeRelations[0];
        Assert.Equal(2, kr.InterpretationEvidenceIds.Count);
        Assert.DoesNotContain(snap.KnowledgeRelations, r => r.LinkEvidenceIds.Any(id => id.StartsWith("interp:", StringComparison.Ordinal)));

        var prov = snap.GetProvenance(kr.KnowledgeRelationId)!;
        Assert.Equal(2, prov.LinkEvidences.Count);
        Assert.Equal(2, prov.InterpretationEvidences.Count);
        Assert.All(prov.InterpretationEvidences, e => Assert.NotNull(e.LinkedDeclaredRelationId));
    }

    [Fact]
    public void All_Diagnostics_Are_Preserved_Across_Group()
    {
        const string group = "corr:exact:A:B:cafebabecafebabe";
        var rel = Declared(
            "phc.rel:d1", DeclaredRelationSourceTypes.PhcRel, "A", "B", "a=b",
            "table:dbo.a", "table:dbo.b", DeclaredRelationCorrespondenceKind.Exact, group);
        var dtl = Declared(
            "phc.dtl:d2", DeclaredRelationSourceTypes.PhcDtl, "A", "B", "a=b",
            "table:dbo.a", "table:dbo.b", DeclaredRelationCorrespondenceKind.Exact, group);
        var interps = new[]
        {
            Interp("phc.rel:d1", "declared-expr-v1.1", InterpretationConfidence.High, """[{"code":"NameMismatch"}]"""),
            Interp("phc.dtl:d2", "declared-expr-v1.1", InterpretationConfidence.High, null)
        };

        var snap = _projector.Project([], [rel, dtl], interps);
        var kr = Assert.Single(snap.KnowledgeRelations);

        Assert.Contains(kr.Diagnostics, d => d.DiagnosticsJson != null && d.DiagnosticsJson.Contains("NameMismatch", StringComparison.Ordinal));
        // União sem perda: entrada da evidência sem JSON continua presente
        Assert.Contains(kr.Diagnostics, d => d.EvidenceId.Contains("phc.dtl:d2", StringComparison.Ordinal) ||
                                             d.EvidenceId.StartsWith("interp:phc.dtl:d2", StringComparison.Ordinal));
        Assert.True(kr.Diagnostics.Count >= 4); // 2 link + 2 interp
    }

    [Fact]
    public void Ids_Are_Stable_Under_Different_Input_Orders()
    {
        const string group = "corr:exact:CL:FT:1122334455667788";
        var declared = new[]
        {
            Declared("phc.rel:z", DeclaredRelationSourceTypes.PhcRel, "CL", "FT", "cl.no=ft.no",
                "table:dbo.cl", "table:dbo.ft", DeclaredRelationCorrespondenceKind.Exact, group),
            Declared("phc.dtl:a", DeclaredRelationSourceTypes.PhcDtl, "CL", "FT", "cl.no=ft.no",
                "table:dbo.cl", "table:dbo.ft", DeclaredRelationCorrespondenceKind.Exact, group),
            Declared("phc.rel:solo", DeclaredRelationSourceTypes.PhcRel, "X", "Y", "1=1",
                null, null, DeclaredRelationCorrespondenceKind.None, null,
                fromRes: TableReferenceResolution.Unresolved, toRes: TableReferenceResolution.Unresolved)
        };
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo", "child", "FK_c_p", "dbo", "parent", [("Id", "Id", 1)], DateTimeOffset.UtcNow);

        var snap1 = _projector.Project([fk], declared, []);
        var snap2 = _projector.Project(
            [fk],
            declared.Reverse().ToList(),
            []);

        Assert.Equal(
            snap1.KnowledgeRelations.Select(r => r.KnowledgeRelationId),
            snap2.KnowledgeRelations.Select(r => r.KnowledgeRelationId));
        Assert.Equal(
            snap1.Evidences.Select(e => e.EvidenceId),
            snap2.Evidences.Select(e => e.EvidenceId));
        Assert.Equal(
            snap1.KnowledgeRelations.Select(r => string.Join(',', r.LinkEvidenceIds)),
            snap2.KnowledgeRelations.Select(r => string.Join(',', r.LinkEvidenceIds)));
    }

    [Fact]
    public void No_Duplicate_Ids()
    {
        const string group = "corr:exact:A:B:0011223344556677";
        var declared = new[]
        {
            Declared("phc.rel:1", DeclaredRelationSourceTypes.PhcRel, "A", "B", "a=b",
                "table:dbo.a", "table:dbo.b", DeclaredRelationCorrespondenceKind.Exact, group),
            Declared("phc.dtl:2", DeclaredRelationSourceTypes.PhcDtl, "A", "B", "a=b",
                "table:dbo.a", "table:dbo.b", DeclaredRelationCorrespondenceKind.Exact, group),
            Declared("phc.rel:3", DeclaredRelationSourceTypes.PhcRel, "C", "D", "c=d",
                "table:dbo.c", "table:dbo.d", DeclaredRelationCorrespondenceKind.None, null)
        };
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo", "t1", "FK_t1_t2", "dbo", "t2", [("a", "a", 1)], DateTimeOffset.UtcNow);
        var interps = declared.Select(d => Interp(d.RelationId, "declared-expr-v1.1", InterpretationConfidence.High, null)).ToList();

        var snap = _projector.Project([fk], declared, interps);

        Assert.Equal(snap.Evidences.Count, snap.Evidences.Select(e => e.EvidenceId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            snap.KnowledgeRelations.Count,
            snap.KnowledgeRelations.Select(r => r.KnowledgeRelationId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Reproject_Same_Workspace_Is_Equivalent()
    {
        await using var workspace = await CreateWorkspaceAsync();
        const string group = "corr:exact:BO:BI:9988776655443322";
        var rel = Declared(
            "phc.rel:w1", DeclaredRelationSourceTypes.PhcRel, "BO", "BI", "bo=bi",
            "table:dbo.bo", "table:dbo.bi", DeclaredRelationCorrespondenceKind.Exact, group);
        var dtl = Declared(
            "phc.dtl:w2", DeclaredRelationSourceTypes.PhcDtl, "BO", "BI", "bo=bi",
            "table:dbo.bo", "table:dbo.bi", DeclaredRelationCorrespondenceKind.Exact, group);
        await workspace.UpsertDeclaredRelationsAsync([rel, dtl], CancellationToken.None);
        await workspace.UpsertDeclaredRelationInterpretationsAsync(
            [Interp("phc.rel:w1", "declared-expr-v1.1", InterpretationConfidence.High, null)],
            CancellationToken.None);
        await workspace.UpsertObjectAsync(
            SqlKeyInventory.CreateForeignKey("dbo", "bi", "FK_bi_bo", "dbo", "bo",
                [("bostamp", "bostamp", 1)], DateTimeOffset.UtcNow),
            CancellationToken.None);

        var a = await _projector.ProjectAsync(workspace, CancellationToken.None);
        var b = await _projector.ProjectAsync(workspace, CancellationToken.None);

        Assert.Equal(a.Evidences.Select(e => e.EvidenceId), b.Evidences.Select(e => e.EvidenceId));
        Assert.Equal(a.KnowledgeRelations.Select(r => r.KnowledgeRelationId), b.KnowledgeRelations.Select(r => r.KnowledgeRelationId));
        Assert.Equal(
            a.KnowledgeRelations.Select(r => (r.LinkEvidenceIds.Count, r.InterpretationEvidenceIds.Count, r.ConsolidationBasis)),
            b.KnowledgeRelations.Select(r => (r.LinkEvidenceIds.Count, r.InterpretationEvidenceIds.Count, r.ConsolidationBasis)));
    }

    [Fact]
    public async Task Legacy_Workspace_Without_Knowledge_Tables_Can_Be_Read()
    {
        await using var workspace = await CreateWorkspaceAsync();
        // Workspace “antigo”: só objectos + declared, sem materialização de conhecimento
        await workspace.UpsertDeclaredRelationsAsync(
            [
                Declared("phc.rel:legacy", DeclaredRelationSourceTypes.PhcRel, "BO", "ST", "bo.stref=st.ref",
                    "table:dbo.bo", "table:dbo.st", DeclaredRelationCorrespondenceKind.None, null)
            ],
            CancellationToken.None);

        var snap = await _projector.ProjectAsync(workspace, CancellationToken.None);
        Assert.Single(snap.GetEvidences(RelationEvidenceKind.PhcRel));
        Assert.Single(snap.KnowledgeRelations);
    }

    [Fact]
    public void Query_Filters_By_Kind_And_Endpoints()
    {
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo", "bi", "FK_bi_bo", "dbo", "bo", [("bostamp", "bostamp", 1)], DateTimeOffset.UtcNow);
        var rel = Declared(
            "phc.rel:q", DeclaredRelationSourceTypes.PhcRel, "CL", "FT", "cl.no=ft.no",
            "table:dbo.cl", "table:dbo.ft", DeclaredRelationCorrespondenceKind.None, null);

        var snap = _projector.Project([fk], [rel], []);

        Assert.Single(snap.GetEvidences(RelationEvidenceKind.SqlForeignKey));
        Assert.Single(snap.GetEvidences(fromObjectId: "table:dbo.cl"));
        Assert.Single(snap.GetKnowledgeRelations(fromObjectId: "table:dbo.bi", toObjectId: "table:dbo.bo"));
        Assert.NotNull(snap.GetKnowledgeRelation(KnowledgeRelationIds.ForeignKeyKnowledgeRelationId(fk.ObjectId)));
    }

    private static DeclaredRelationRecord Declared(
        string id,
        string sourceType,
        string from,
        string to,
        string expr,
        string? fromObj,
        string? toObj,
        DeclaredRelationCorrespondenceKind kind,
        string? groupId,
        TableReferenceResolution? fromRes = null,
        TableReferenceResolution? toRes = null) =>
        new(
            id,
            sourceType,
            id.Contains(':', StringComparison.Ordinal) ? id[(id.IndexOf(':') + 1)..] : id,
            from,
            null,
            to,
            null,
            expr,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            fromObj,
            toObj,
            fromRes ?? (fromObj is null ? TableReferenceResolution.Unresolved : TableReferenceResolution.Resolved),
            toRes ?? (toObj is null ? TableReferenceResolution.Unresolved : TableReferenceResolution.Resolved),
            kind,
            groupId);

    private static DeclaredRelationInterpretationRecord Interp(
        string relationId,
        string parserVersion,
        InterpretationConfidence confidence,
        string? diagnosticsJson) =>
        new(
            relationId,
            parserVersion,
            ExpressionParseStatus.Parsed,
            confidence,
            Ast: null,
            diagnosticsJson,
            FieldRefCount: 0,
            TableRefsResolved: 0,
            TableRefsAmbiguous: 0,
            TableRefsUnresolved: 0,
            DateTimeOffset.UtcNow);

    private static async Task<SqliteScanWorkspace> CreateWorkspaceAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-kr", "local/kr", CancellationToken.None);
        return workspace;
    }
}
