using System.Security.Cryptography;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;
using PhcKnowledgeScanner.Abstractions.Workspace;
using PhcKnowledgeScanner.Analysis.Knowledge;
using PhcKnowledgeScanner.Application.Services;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public class ScanKnowledgeSessionTests
{
    [Fact]
    public async Task Open_Valid_Workspace_Exposes_ScanId_Tables_And_Knowledge()
    {
        var root = await CreateWorkspaceWithTablesAsync(
            scanId: "scan-valid-001",
            label: "local/demo",
            tables: [("dbo", "bo"), ("dbo", "bi")]);

        await using var session = await ScanKnowledgeSession.OpenAsync(root);

        Assert.Equal(ScanKnowledgeSessionState.Open, session.State);
        Assert.Equal("scan-valid-001", session.ScanId);
        Assert.Equal("local/demo", session.InstallationLabel);
        Assert.Equal(2, session.Tables.Count);
        Assert.Contains(session.Tables, t => t.ObjectId == "table:dbo.bo");
        Assert.NotNull(session.Knowledge);
        Assert.IsAssignableFrom<IRelationalKnowledgeQuery>(session.Knowledge);
        Assert.Equal(1, session.ProjectionBuildCount);
    }

    [Fact]
    public async Task Projection_Is_Built_Exactly_Once_Per_Session()
    {
        var root = await CreateWorkspaceWithDeclaredAsync();
        var counter = new CountingProjector();

        await using var session = await ScanKnowledgeSession.OpenAsync(root, counter);

        Assert.Equal(1, counter.ProjectAsyncCalls);
        Assert.Equal(1, session.ProjectionBuildCount);
        _ = session.Knowledge.GetKnowledgeRelations();
        _ = session.Knowledge.FieldMappings;
        _ = session.Knowledge.GetEvidences();
        Assert.Equal(1, counter.ProjectAsyncCalls);
        Assert.Same(session.Knowledge, session.Knowledge);
    }

    [Fact]
    public async Task Two_Workspaces_Do_Not_Mix_Data()
    {
        var rootA = await CreateWorkspaceWithTablesAsync("scan-a", "inst-a", [("dbo", "aaa")]);
        var rootB = await CreateWorkspaceWithTablesAsync("scan-b", "inst-b", [("dbo", "bbb"), ("dbo", "ccc")]);

        await using var sessionA = await ScanKnowledgeSession.OpenAsync(rootA);
        await using var sessionB = await ScanKnowledgeSession.OpenAsync(rootB);

        Assert.Equal("scan-a", sessionA.ScanId);
        Assert.Equal("scan-b", sessionB.ScanId);
        Assert.Single(sessionA.Tables);
        Assert.Equal(2, sessionB.Tables.Count);
        Assert.Contains(sessionA.Tables, t => t.Name == "aaa");
        Assert.DoesNotContain(sessionA.Tables, t => t.Name == "bbb");
        Assert.DoesNotContain(sessionB.Tables, t => t.Name == "aaa");
        Assert.NotEqual(sessionA.WorkspaceRootPath, sessionB.WorkspaceRootPath);
    }

    [Fact]
    public async Task Dispose_Closes_Session_And_Blocks_Queries()
    {
        var root = await CreateWorkspaceWithTablesAsync("scan-close", "x", [("dbo", "t1")]);
        var session = await ScanKnowledgeSession.OpenAsync(root);

        await session.DisposeAsync();

        Assert.Equal(ScanKnowledgeSessionState.Closed, session.State);
        Assert.Throws<ObjectDisposedException>(() => _ = session.ScanId);
        Assert.Throws<ObjectDisposedException>(() => _ = session.Tables);
        Assert.Throws<ObjectDisposedException>(() => _ = session.Knowledge);
        Assert.Throws<ObjectDisposedException>(() => _ = session.Summary);
        Assert.Throws<ObjectDisposedException>(() => session.FindTables("t"));
    }

    [Fact]
    public async Task Old_Workspace_Without_Declared_Opens_With_Empty_Projection()
    {
        var root = await CreateWorkspaceWithTablesAsync("scan-old", "old", [("dbo", "st")]);

        await using var session = await ScanKnowledgeSession.OpenAsync(root);

        Assert.Equal("scan-old", session.ScanId);
        Assert.Single(session.Tables);
        Assert.Empty(session.Knowledge.GetEvidences(RelationEvidenceKind.PhcRel));
        Assert.Empty(session.Knowledge.KnowledgeRelations);
        Assert.Empty(session.Knowledge.FieldMappings);
    }

    [Fact]
    public async Task Missing_Directory_Throws()
    {
        var missing = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => ScanKnowledgeSession.OpenAsync(missing));
    }

    [Fact]
    public async Task Missing_Database_File_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await Assert.ThrowsAsync<FileNotFoundException>(() => ScanKnowledgeSession.OpenAsync(root));
    }

    [Fact]
    public async Task Open_And_Use_Does_Not_Modify_Sqlite_File()
    {
        var root = await CreateWorkspaceWithDeclaredAsync();
        var dbPath = Path.Combine(root, "workspace.db");

        var hashBefore = await Sha256FileAsync(dbPath);
        var lengthBefore = new FileInfo(dbPath).Length;
        var countsBefore = await CountRowsAsync(root);

        await using (var session = await ScanKnowledgeSession.OpenAsync(root))
        {
            Assert.NotEmpty(session.Tables);
            _ = session.Knowledge.KnowledgeRelations.Count;
            _ = session.Knowledge.FieldMappings.Count;
            _ = session.FindTables("bo");
        }

        var hashAfter = await Sha256FileAsync(dbPath);
        var lengthAfter = new FileInfo(dbPath).Length;
        var countsAfter = await CountRowsAsync(root);

        Assert.Equal(hashBefore, hashAfter);
        Assert.Equal(lengthBefore, lengthAfter);
        Assert.Equal(countsBefore, countsAfter);
    }

    [Fact]
    public async Task Real_Validation_Workspaces_Remain_Unchanged_When_Opened_ReadOnly()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhcKnowledgeScanner",
            "validation-full-inventory-20261103");
        var run2 = Path.Combine(baseDir, "run2");
        var run3 = Path.Combine(baseDir, "run3-pkfk");
        if (!File.Exists(Path.Combine(run2, "workspace.db")) || !File.Exists(Path.Combine(run3, "workspace.db")))
        {
            // Ambiente sem artefactos de validação — não falhar a suite CI.
            return;
        }

        async Task AssertUnchanged(string root)
        {
            var db = Path.Combine(root, "workspace.db");
            var before = await Sha256FileAsync(db);
            await using var session = await ScanKnowledgeSession.OpenAsync(root);
            Assert.False(string.IsNullOrWhiteSpace(session.ScanId));
            _ = session.Tables.Count;
            _ = session.Knowledge.KnowledgeRelations.Count;
            await session.DisposeAsync();
            var after = await Sha256FileAsync(db);
            Assert.Equal(before, after);
        }

        await AssertUnchanged(run2);
        await AssertUnchanged(run3);
    }

    [Fact]
    public async Task Knowledge_Query_Works_After_Sqlite_Connection_Closed()
    {
        var root = await CreateWorkspaceWithDeclaredAsync();
        await using var session = await ScanKnowledgeSession.OpenAsync(root);

        // Ligação já fechada no fim de OpenAsync — consultas só em memória.
        var krs = session.Knowledge.GetKnowledgeRelations();
        Assert.NotEmpty(krs);
        Assert.NotEmpty(session.Knowledge.FieldMappings);
        Assert.Contains(
            session.Knowledge.GetEvidences(RelationEvidenceKind.PhcRel),
            e => e.EvidenceId.StartsWith("phc.rel:", StringComparison.Ordinal));
    }

    private static async Task<string> CreateWorkspaceWithTablesAsync(
        string scanId,
        string label,
        IReadOnlyList<(string Schema, string Name)> tables)
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using var ws = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await ws.InitializeAsync(scanId, label, CancellationToken.None);
        var now = DateTimeOffset.UtcNow;
        var records = tables.Select(t => new TechnicalObjectRecord(
            TableInventoryClassification.BuildObjectId(t.Schema, t.Name),
            ObjectKind.Table,
            t.Name,
            t.Schema,
            null,
            "test",
            null,
            null,
            now)).ToList();
        await ws.UpsertObjectsAsync(records, CancellationToken.None);
        return root;
    }

    private static async Task<string> CreateWorkspaceWithDeclaredAsync()
    {
        var root = await CreateWorkspaceWithTablesAsync(
            "scan-decl",
            "local/decl",
            [("dbo", "bo"), ("dbo", "bi")]);

        await using var ws = await SqliteScanWorkspace.OpenExistingAsync(root, readOnly: false);
        const string group = "corr:exact:BO:BI:aabbccddeeff0011";
        var rel = new DeclaredRelationRecord(
            "phc.rel:s1",
            DeclaredRelationSourceTypes.PhcRel,
            "s1",
            "BO",
            null,
            "BI",
            null,
            "bo.bostamp=bi.bostamp",
            null, null, null, null, null, null,
            DateTimeOffset.UtcNow,
            "table:dbo.bo",
            "table:dbo.bi",
            TableReferenceResolution.Resolved,
            TableReferenceResolution.Resolved,
            DeclaredRelationCorrespondenceKind.Exact,
            group);
        var dtl = rel with
        {
            RelationId = "phc.dtl:s2",
            SourceType = DeclaredRelationSourceTypes.PhcDtl,
            SourceRecordId = "s2"
        };
        await ws.UpsertDeclaredRelationsAsync([rel, dtl], CancellationToken.None);

        var left = new FieldRefExprNode("bo.bostamp", "bo", "bostamp", TableReferenceResolution.Resolved, "table:dbo.bo");
        var right = new FieldRefExprNode("bi.bostamp", "bi", "bostamp", TableReferenceResolution.Resolved, "table:dbo.bi");
        var ast = new ComparisonExprNode(left, "=", right);
        await ws.UpsertDeclaredRelationInterpretationsAsync(
            [
                new DeclaredRelationInterpretationRecord(
                    "phc.rel:s1",
                    "declared-expr-v1.1",
                    ExpressionParseStatus.Parsed,
                    InterpretationConfidence.High,
                    ast,
                    null,
                    2, 2, 0, 0,
                    DateTimeOffset.UtcNow),
                new DeclaredRelationInterpretationRecord(
                    "phc.dtl:s2",
                    "declared-expr-v1.1",
                    ExpressionParseStatus.Parsed,
                    InterpretationConfidence.High,
                    ast,
                    null,
                    2, 2, 0, 0,
                    DateTimeOffset.UtcNow)
            ],
            CancellationToken.None);

        return root;
    }

    private static async Task<string> Sha256FileAsync(string path)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hash);
    }

    private static async Task<Dictionary<string, long>> CountRowsAsync(string workspaceRoot)
    {
        await using var ws = await SqliteScanWorkspace.OpenExistingAsync(workspaceRoot, readOnly: true);
        var summary = await ws.GetSummaryAsync();
        var declared = await ws.GetDeclaredRelationsAsync();
        var interps = await ws.GetDeclaredRelationInterpretationsAsync();
        return new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["objects_total"] = summary.ObjectCounts.Values.Sum(),
            ["declared"] = declared.Count,
            ["interpretations"] = interps.Count,
            ["relations"] = summary.RelationCount,
            ["issues"] = summary.IssueCount
        };
    }

    private sealed class CountingProjector : IRelationalKnowledgeProjector
    {
        private readonly RelationalKnowledgeProjector _inner = new();
        public int ProjectAsyncCalls { get; private set; }

        public async Task<RelationalKnowledgeSnapshot> ProjectAsync(
            IScanWorkspace workspace,
            CancellationToken cancellationToken = default)
        {
            ProjectAsyncCalls++;
            return await _inner.ProjectAsync(workspace, cancellationToken).ConfigureAwait(false);
        }

        public RelationalKnowledgeSnapshot Project(
            IReadOnlyList<TechnicalObjectRecord> foreignKeys,
            IReadOnlyList<DeclaredRelationRecord> declaredRelations,
            IReadOnlyList<DeclaredRelationInterpretationRecord> interpretations) =>
            _inner.Project(foreignKeys, declaredRelations, interpretations);
    }
}
