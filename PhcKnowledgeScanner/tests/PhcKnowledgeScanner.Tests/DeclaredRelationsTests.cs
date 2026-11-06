using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public class DeclaredRelationsTests
{
    [Fact]
    public async Task DeclaredRelations_Persist_And_Are_Idempotent()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-decl", "local/demo", CancellationToken.None);

        var record = SampleRel("ADM1", "bo", "cl", "bo.no=cl.no");
        await workspace.UpsertDeclaredRelationsAsync([record], CancellationToken.None);
        await workspace.UpsertDeclaredRelationsAsync(
            [record with { FromTableName = "Dossiers Internos" }],
            CancellationToken.None);

        var all = await workspace.GetDeclaredRelationsAsync();
        Assert.Single(all);
        Assert.Equal("Dossiers Internos", all[0].FromTableName);
        Assert.Equal(DeclaredRelationSourceTypes.PhcRel, all[0].SourceType);

        var derived = await workspace.GetRelationsAsync();
        Assert.Empty(derived);
    }

    [Fact]
    public void RelationIds_Are_Stable_From_Source_Stamp()
    {
        var a = SampleRel("ADM25022739915,410000001", "abs", "absfp", "abs.absstamp=absfp.absstamp");
        var b = SampleRel("ADM25022739915,410000001", "abs", "absfp", "abs.absstamp=absfp.absstamp");
        Assert.Equal(a.RelationId, b.RelationId);
        Assert.Equal("phc.rel:ADM25022739915,410000001", a.RelationId);
    }

    [Fact]
    public void Correspondence_Exact_And_Probable_Keep_Both_Origins()
    {
        var relExact = SampleRel("R1", "bo", "cl", "bo.no=cl.no and bo.estab=cl.estab");
        var dtlExact = SampleDtl("D1", "bo", "cl", "bo.no=cl.no and bo.estab=cl.estab");
        var relProb = SampleRel("R2", "bi", "st", "bi.ref=st.ref");
        var dtlProb = SampleDtl("D2", "bi", "st", "bi.ref = st.ref AND 1=1");
        var orphan = SampleDtl("D3", "xx", "yy", "xx.a=yy.a");

        var result = DeclaredRelationCorrespondence.Apply([relExact, dtlExact, relProb, dtlProb, orphan]);

        Assert.Equal(5, result.Count);
        var exactGroup = result.Where(r => r.CorrespondenceKind == DeclaredRelationCorrespondenceKind.Exact).ToList();
        Assert.Equal(2, exactGroup.Count);
        Assert.Single(exactGroup.Select(r => r.CorrespondenceGroupId).Distinct());

        var probable = result.Where(r => r.CorrespondenceKind == DeclaredRelationCorrespondenceKind.Probable).ToList();
        Assert.Equal(2, probable.Count);

        Assert.Equal(DeclaredRelationCorrespondenceKind.None, result.Single(r => r.SourceRecordId == "D3").CorrespondenceKind);
    }

    [Fact]
    public async Task Filter_By_Source_And_Tables()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-f", "local/demo", CancellationToken.None);

        await workspace.UpsertDeclaredRelationsAsync(
            [
                SampleRel("A", "bo", "cl", "bo.no=cl.no"),
                SampleDtl("B", "bo", "cl", "bo.no=cl.no"),
                SampleRel("C", "ft", "st", "ft.ref=st.ref")
            ],
            CancellationToken.None);

        var relOnly = await workspace.GetDeclaredRelationsAsync(sourceType: DeclaredRelationSourceTypes.PhcRel);
        Assert.Equal(2, relOnly.Count);

        var boCl = await workspace.GetDeclaredRelationsAsync(fromTable: "bo", toTable: "cl");
        Assert.Equal(2, boCl.Count);
    }

    [Fact]
    public async Task Missing_Declared_Sources_Do_Not_Break_Workspace_Init()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-empty", "local/demo", CancellationToken.None);
        var list = await workspace.GetDeclaredRelationsAsync();
        Assert.Empty(list);
    }

    [Fact]
    public async Task Existing_Declared_Relations_RoundTrip_Resolution_States()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-res", "local/demo", CancellationToken.None);

        var record = SampleRel("R-AMB", "bo", "cl", "bo.no=cl.no") with
        {
            FromObjectId = "table:dbo.bo",
            ToObjectId = null,
            FromTableResolution = TableReferenceResolution.Resolved,
            ToTableResolution = TableReferenceResolution.Ambiguous
        };

        await workspace.UpsertDeclaredRelationsAsync([record], CancellationToken.None);
        var loaded = Assert.Single(await workspace.GetDeclaredRelationsAsync());
        Assert.Equal(TableReferenceResolution.Resolved, loaded.FromTableResolution);
        Assert.Equal(TableReferenceResolution.Ambiguous, loaded.ToTableResolution);
        Assert.True(loaded.FromTableResolved);
        Assert.False(loaded.ToTableResolved);
        Assert.Equal("bo.no=cl.no", loaded.OriginalExpression);
    }

    internal static DeclaredRelationRecord SampleRel(string stamp, string from, string to, string expr) =>
        new(
            $"{DeclaredRelationSourceTypes.PhcRel}:{stamp}",
            DeclaredRelationSourceTypes.PhcRel,
            stamp,
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
            null,
            null,
            TableReferenceResolution.Unresolved,
            TableReferenceResolution.Unresolved,
            DeclaredRelationCorrespondenceKind.None,
            null);

    internal static DeclaredRelationRecord SampleDtl(string stamp, string from, string to, string expr) =>
        new(
            $"{DeclaredRelationSourceTypes.PhcDtl}:{stamp}",
            DeclaredRelationSourceTypes.PhcDtl,
            stamp,
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
            null,
            null,
            TableReferenceResolution.Unresolved,
            TableReferenceResolution.Unresolved,
            DeclaredRelationCorrespondenceKind.None,
            null);
}
