using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;
using PhcKnowledgeScanner.Analysis.Knowledge;
using PhcKnowledgeScanner.Application.Services;
using PhcKnowledgeScanner.Collectors.Sql;

namespace PhcKnowledgeScanner.Tests;

public class InventoryRelationExplorerTests
{
    [Fact]
    public void Outgoing_And_Incoming_Are_Separated()
    {
        var snap = Project(
            fks: [SqlKeyInventory.CreateForeignKey(
                "dbo", "bo", "FK_bo_bi", "dbo", "bi", [("bostamp", "bostamp", 1)], DateTimeOffset.UtcNow)],
            declared:
            [
                Declared("phc.rel:r1", DeclaredRelationSourceTypes.PhcRel, "cl", "bo",
                    "table:dbo.cl", "table:dbo.bo", DeclaredRelationCorrespondenceKind.None, null)
            ]);

        var board = InventoryRelationExplorer.BuildBoard(snap, "table:dbo.bo");

        Assert.Single(board.Outgoing);
        Assert.Equal("dbo.bi", board.Outgoing[0].RelatedDisplay);
        Assert.Contains("FK SQL", board.Outgoing[0].EvidenceKindsLabel);

        Assert.Single(board.Incoming);
        Assert.Contains("cl", board.Incoming[0].RelatedDisplay, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PHC REL", board.Incoming[0].EvidenceKindsLabel);
    }

    [Fact]
    public void Distinguishes_Fk_Rel_And_Dtl()
    {
        var snap = Project(
            fks: [SqlKeyInventory.CreateForeignKey(
                "dbo", "bo", "FK_bo_bi", "dbo", "bi", [("bostamp", "bostamp", 1)], DateTimeOffset.UtcNow)],
            declared:
            [
                Declared("phc.rel:1", DeclaredRelationSourceTypes.PhcRel, "bo", "st",
                    "table:dbo.bo", "table:dbo.st", DeclaredRelationCorrespondenceKind.None, null),
                Declared("phc.dtl:1", DeclaredRelationSourceTypes.PhcDtl, "bo", "ft",
                    "table:dbo.bo", "table:dbo.ft", DeclaredRelationCorrespondenceKind.None, null)
            ]);

        var board = InventoryRelationExplorer.BuildBoard(snap, "table:dbo.bo");
        Assert.Equal(3, board.Outgoing.Count);
        Assert.Contains(board.Outgoing, i => i.EvidenceKindsLabel == "FK SQL");
        Assert.Contains(board.Outgoing, i => i.EvidenceKindsLabel == "PHC REL");
        Assert.Contains(board.Outgoing, i => i.EvidenceKindsLabel == "PHC DTL");
        Assert.DoesNotContain(board.Outgoing, i =>
            i.EvidenceKindsLabel.Contains("FK", StringComparison.Ordinal)
            && i.EvidenceKindsLabel.Contains("REL", StringComparison.Ordinal));
    }

    [Fact]
    public void Correspondence_Exact_Probable_And_None()
    {
        const string exactGroup = "corr:exact:FD:PE:abc";
        const string probableGroup = "corr:probable:BO:ST:def";
        var snap = Project(
            declared:
            [
                Declared("phc.rel:e1", DeclaredRelationSourceTypes.PhcRel, "fd", "pe",
                    "table:dbo.fd", "table:dbo.pe", DeclaredRelationCorrespondenceKind.Exact, exactGroup),
                Declared("phc.dtl:e1", DeclaredRelationSourceTypes.PhcDtl, "fd", "pe",
                    "table:dbo.fd", "table:dbo.pe", DeclaredRelationCorrespondenceKind.Exact, exactGroup),
                Declared("phc.rel:p1", DeclaredRelationSourceTypes.PhcRel, "bo", "st",
                    "table:dbo.bo", "table:dbo.st", DeclaredRelationCorrespondenceKind.Probable, probableGroup),
                Declared("phc.rel:n1", DeclaredRelationSourceTypes.PhcRel, "bo", "cl",
                    "table:dbo.bo", "table:dbo.cl", DeclaredRelationCorrespondenceKind.None, null)
            ]);

        var fd = InventoryRelationExplorer.BuildBoard(snap, "table:dbo.fd");
        Assert.Single(fd.Outgoing);
        Assert.Equal("Exact", fd.Outgoing[0].CorrespondenceLabel);
        Assert.Equal(2, fd.Outgoing[0].LinkEvidenceCount);
        Assert.Contains("PHC DTL", fd.Outgoing[0].EvidenceKindsLabel);
        Assert.Contains("PHC REL", fd.Outgoing[0].EvidenceKindsLabel);

        var bo = InventoryRelationExplorer.BuildBoard(snap, "table:dbo.bo");
        Assert.Contains(bo.Outgoing, i => i.CorrespondenceLabel == "Probable");
        Assert.Contains(bo.Outgoing, i => i.CorrespondenceLabel == "None");
    }

    [Fact]
    public void Detail_Shows_Evidence_Without_Interpretation()
    {
        var snap = Project(
            declared:
            [
                Declared("phc.rel:x", DeclaredRelationSourceTypes.PhcRel, "bo", "bi",
                    "table:dbo.bo", "table:dbo.bi", DeclaredRelationCorrespondenceKind.None, null,
                    expression: "bo.bostamp=bi.bostamp")
            ]);

        var board = InventoryRelationExplorer.BuildBoard(snap, "table:dbo.bo");
        var detail = InventoryRelationExplorer.BuildDetail(snap, board.Outgoing[0].KnowledgeRelationId);
        Assert.NotNull(detail);
        Assert.Single(detail!.LinkEvidences);
        Assert.Empty(detail.InterpretationEvidences);
        Assert.Equal("bo.bostamp=bi.bostamp", detail.LinkEvidences[0].OriginalExpression);
        Assert.Equal(RelationEvidenceKind.PhcRel, detail.LinkEvidences[0].Kind);
    }

    [Fact]
    public void Detail_Shows_Interpretation_Diagnostics()
    {
        var declared = Declared("phc.rel:y", DeclaredRelationSourceTypes.PhcRel, "bo", "bi",
            "table:dbo.bo", "table:dbo.bi", DeclaredRelationCorrespondenceKind.None, null,
            expression: "bo.bostamp=bi.bostamp");
        var interp = new DeclaredRelationInterpretationRecord(
            declared.RelationId,
            "declared-expr-v1.1",
            ExpressionParseStatus.PartiallyParsed,
            InterpretationConfidence.Low,
            Ast: null,
            DiagnosticsJson: "{\"warn\":\"token\"}",
            FieldRefCount: 1,
            TableRefsResolved: 0,
            TableRefsAmbiguous: 0,
            TableRefsUnresolved: 1,
            InterpretedAtUtc: DateTimeOffset.UtcNow);

        var snap = new RelationalKnowledgeProjector().Project(
            Array.Empty<TechnicalObjectRecord>(),
            [declared],
            [interp]);

        var board = InventoryRelationExplorer.BuildBoard(snap, "table:dbo.bo");
        var detail = InventoryRelationExplorer.BuildDetail(snap, board.Outgoing[0].KnowledgeRelationId)!;
        Assert.NotEmpty(detail.InterpretationEvidences);
        Assert.Equal(ExpressionParseStatus.PartiallyParsed, detail.InterpretationEvidences[0].ParseStatus);
        Assert.Contains("token", detail.InterpretationEvidences[0].DiagnosticsJson);
        Assert.NotEmpty(detail.DiagnosticsLines);
    }

    [Fact]
    public void Unresolved_Endpoints_Marked_And_Not_Navigable()
    {
        var declared = Declared("phc.rel:u", DeclaredRelationSourceTypes.PhcRel, "ghost", "bo",
            fromObjectId: null, toObjectId: "table:dbo.bo",
            DeclaredRelationCorrespondenceKind.None, null,
            fromRes: TableReferenceResolution.Unresolved,
            toRes: TableReferenceResolution.Resolved);

        var snap = Project(declared: [declared]);
        var board = InventoryRelationExplorer.BuildBoard(snap, "table:dbo.bo");
        Assert.Single(board.Incoming);
        Assert.True(board.Incoming[0].HasResolutionIssue);
        Assert.False(board.Incoming[0].CanNavigateToRelated);

        var detail = InventoryRelationExplorer.BuildDetail(snap, board.Incoming[0].KnowledgeRelationId)!;
        Assert.Equal(EndpointResolutionState.Partial, detail.EndpointResolutionState);
        Assert.False(detail.CanNavigateFrom);
        Assert.True(detail.CanNavigateTo);
    }

    [Fact]
    public void Navigation_Only_When_Resolved()
    {
        var epResolved = new RelationEndpoint("bo", "dbo", "table:dbo.bo", TableReferenceResolution.Resolved);
        var epUnresolved = new RelationEndpoint("ghost", null, null, TableReferenceResolution.Unresolved);
        Assert.True(InventoryRelationExplorer.CanNavigate(epResolved));
        Assert.False(InventoryRelationExplorer.CanNavigate(epUnresolved));
    }

    [Fact]
    public void Switching_Table_Does_Not_Require_New_Projection_Instance()
    {
        var snap = Project(
            declared:
            [
                Declared("phc.rel:1", DeclaredRelationSourceTypes.PhcRel, "bo", "bi",
                    "table:dbo.bo", "table:dbo.bi", DeclaredRelationCorrespondenceKind.None, null),
                Declared("phc.rel:2", DeclaredRelationSourceTypes.PhcRel, "cl", "bo",
                    "table:dbo.cl", "table:dbo.bo", DeclaredRelationCorrespondenceKind.None, null)
            ]);

        var a = InventoryRelationExplorer.BuildBoard(snap, "table:dbo.bo");
        var b = InventoryRelationExplorer.BuildBoard(snap, "table:dbo.cl");
        Assert.NotEmpty(a.Outgoing);
        Assert.NotEmpty(b.Outgoing);
        // A mesma instância de snapshot alimenta ambas as consultas (sem reprojectar).
        Assert.Same(snap.KnowledgeRelations, snap.KnowledgeRelations);
        Assert.Equal(2, snap.KnowledgeRelations.Count);
        Assert.Contains(a.Outgoing, i => i.RelatedDisplay.Contains("bi", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(b.Outgoing, i => i.RelatedDisplay.Contains("bo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Session_Navigation_Keeps_ProjectionBuildCount()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using (var ws = await PhcKnowledgeScanner.Workspace.SqliteScanWorkspace.CreateAsync(root, CancellationToken.None))
        {
            await ws.InitializeAsync("scan-nav", "nav", CancellationToken.None);
            var now = DateTimeOffset.UtcNow;
            await ws.UpsertObjectsAsync(
                [
                    new TechnicalObjectRecord("table:dbo.bo", ObjectKind.Table, "bo", "dbo", null, "t", null, null, now),
                    new TechnicalObjectRecord("table:dbo.bi", ObjectKind.Table, "bi", "dbo", null, "t", null, null, now)
                ],
                CancellationToken.None);
            await ws.UpsertDeclaredRelationsAsync(
                [
                    Declared("phc.rel:nav", DeclaredRelationSourceTypes.PhcRel, "bo", "bi",
                        "table:dbo.bo", "table:dbo.bi", DeclaredRelationCorrespondenceKind.None, null)
                ],
                CancellationToken.None);
        }

        await using var session = await ScanKnowledgeSession.OpenAsync(root);
        Assert.Equal(1, session.ProjectionBuildCount);

        var boardBo = InventoryRelationExplorer.BuildBoard(session.Knowledge, "table:dbo.bo");
        Assert.NotEmpty(boardBo.Outgoing);
        var relatedId = boardBo.Outgoing[0].RelatedObjectId;
        Assert.True(InventoryRelationExplorer.CanNavigate(
            new RelationEndpoint("bi", "dbo", relatedId, TableReferenceResolution.Resolved)));

        var boardBi = InventoryRelationExplorer.BuildBoard(session.Knowledge, relatedId!);
        Assert.NotEmpty(boardBi.Incoming);
        Assert.Equal(1, session.ProjectionBuildCount);

        var ranked = session.FindTables("bo");
        Assert.Equal("bo", ranked[0].Name);
    }

    [Fact]
    public void Fk_Detail_Exposes_Columns_When_Present()
    {
        var fk = SqlKeyInventory.CreateForeignKey(
            "dbo", "bo", "FK_bo_bi", "dbo", "bi",
            [("bostamp", "bostamp", 1)],
            DateTimeOffset.UtcNow,
            deleteReferentialAction: "Cascade",
            updateReferentialAction: "NoAction");
        var snap = Project(fks: [fk]);
        var board = InventoryRelationExplorer.BuildBoard(snap, "table:dbo.bo");
        var detail = InventoryRelationExplorer.BuildDetail(snap, board.Outgoing[0].KnowledgeRelationId)!;
        Assert.Equal(RelationEvidenceKind.SqlForeignKey, detail.LinkEvidences[0].Kind);
        Assert.Contains("bostamp", detail.LinkEvidences[0].FkColumnsSummary);
        Assert.Equal("Cascade", detail.LinkEvidences[0].FkActions.DeleteAction);
    }

    private static RelationalKnowledgeSnapshot Project(
        IReadOnlyList<TechnicalObjectRecord>? fks = null,
        IReadOnlyList<DeclaredRelationRecord>? declared = null,
        IReadOnlyList<DeclaredRelationInterpretationRecord>? interps = null) =>
        new RelationalKnowledgeProjector().Project(
            fks ?? Array.Empty<TechnicalObjectRecord>(),
            declared ?? Array.Empty<DeclaredRelationRecord>(),
            interps ?? Array.Empty<DeclaredRelationInterpretationRecord>());

    private static DeclaredRelationRecord Declared(
        string relationId,
        string sourceType,
        string fromTable,
        string toTable,
        string? fromObjectId,
        string? toObjectId,
        DeclaredRelationCorrespondenceKind correspondence,
        string? group,
        string expression = "a=b",
        TableReferenceResolution? fromRes = null,
        TableReferenceResolution? toRes = null) =>
        new(
            relationId,
            sourceType,
            relationId.Split(':').Last(),
            fromTable,
            null,
            toTable,
            null,
            expression,
            null, null, null, null, null, null,
            DateTimeOffset.UtcNow,
            fromObjectId,
            toObjectId,
            fromRes ?? (fromObjectId is null ? TableReferenceResolution.Unresolved : TableReferenceResolution.Resolved),
            toRes ?? (toObjectId is null ? TableReferenceResolution.Unresolved : TableReferenceResolution.Resolved),
            correspondence,
            group);
}
