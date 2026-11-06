using System.Security.Cryptography;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Application.Services;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public class WorkspaceSessionHostTests
{
    [Fact]
    public async Task Open_Exposes_Tables_And_Supports_Case_Insensitive_Search()
    {
        var root = await CreateWorkspaceAsync(
            "scan-host-1",
            "demo",
            [("dbo", "bo"), ("dbo", "cl"), ("dbo", "u_Custom")]);

        await using var host = new WorkspaceSessionHost();
        var result = await host.OpenAsync(root);

        Assert.True(result.Success);
        Assert.NotNull(result.Session);
        Assert.True(host.HasSession);
        Assert.Equal("scan-host-1", host.ActiveSession!.ScanId);
        Assert.Equal(3, host.FindTables(null).Count);

        // "BO" também casa com o schema "dbo" (substring); usar termo inequívoco.
        var custom = host.FindTables("U_CUSTOM");
        Assert.Single(custom);
        Assert.Equal("u_Custom", custom[0].Name);
        Assert.Contains(host.FindTables("bo"), t => t.Name == "bo");
        Assert.Contains(host.FindTables("CL"), t => t.Name == "cl");

        var selected = host.GetTable("table:dbo.bo");
        Assert.NotNull(selected);
        Assert.Equal("bo", selected!.Name);
        Assert.Equal(ObjectKind.Table, selected.Kind);
    }

    [Fact]
    public async Task Open_Accepts_Workspace_Db_File_Path()
    {
        var root = await CreateWorkspaceAsync("scan-db-path", "x", [("dbo", "t1")]);
        var db = Path.Combine(root, "workspace.db");

        await using var host = new WorkspaceSessionHost();
        var result = await host.OpenAsync(db);

        Assert.True(result.Success);
        Assert.Equal(root, host.ActiveSession!.WorkspaceRootPath);
        Assert.Equal(db, host.ActiveSession.DatabasePath);
    }

    [Fact]
    public async Task Switch_Workspace_Replaces_Session_And_Closes_Previous()
    {
        var rootA = await CreateWorkspaceAsync("scan-a", "a", [("dbo", "aaa")]);
        var rootB = await CreateWorkspaceAsync("scan-b", "b", [("dbo", "bbb"), ("dbo", "ccc")]);

        await using var host = new WorkspaceSessionHost();
        Assert.True((await host.OpenAsync(rootA)).Success);
        var previous = host.ActiveSession!;
        Assert.Equal("scan-a", previous.ScanId);

        Assert.True((await host.OpenAsync(rootB)).Success);
        Assert.Equal(ScanKnowledgeSessionState.Closed, previous.State);
        Assert.Equal("scan-b", host.ActiveSession!.ScanId);
        Assert.Equal(2, host.FindTables(null).Count);
        Assert.Contains(host.FindTables(null), t => t.Name == "bbb");
        Assert.DoesNotContain(host.FindTables(null), t => t.Name == "aaa");
    }

    [Fact]
    public async Task Failed_Open_Preserves_Previous_Session()
    {
        var root = await CreateWorkspaceAsync("scan-keep", "keep", [("dbo", "keep_me")]);
        var missing = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));

        await using var host = new WorkspaceSessionHost();
        Assert.True((await host.OpenAsync(root)).Success);
        var kept = host.ActiveSession!;

        var failed = await host.OpenAsync(missing);
        Assert.False(failed.Success);
        Assert.True(host.HasSession);
        Assert.Same(kept, host.ActiveSession);
        Assert.Equal(ScanKnowledgeSessionState.Open, kept.State);
        Assert.Equal("scan-keep", host.ActiveSession!.ScanId);
        Assert.Single(host.FindTables("keep"));
    }

    [Fact]
    public async Task Concurrent_Open_Is_Rejected()
    {
        var rootA = await CreateWorkspaceAsync("scan-c1", "c1", [("dbo", "a")]);
        var rootB = await CreateWorkspaceAsync("scan-c2", "c2", [("dbo", "b")]);

        await using var host = new WorkspaceSessionHost();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.OpeningDelayAsync = async _ =>
        {
            entered.TrySetResult();
            await release.Task;
        };

        var first = host.OpenAsync(rootA);
        await entered.Task;
        Assert.True(host.IsOpening);

        var second = await host.OpenAsync(rootB);
        Assert.False(second.Success);
        Assert.Contains("abertura", second.ErrorMessage!, StringComparison.OrdinalIgnoreCase);

        release.TrySetResult();
        var firstResult = await first;
        Assert.True(firstResult.Success);
        Assert.Equal("scan-c1", host.ActiveSession!.ScanId);
    }

    [Fact]
    public async Task CloseSession_Releases_Active_Session_And_Allows_Reopen()
    {
        var rootA = await CreateWorkspaceAsync("scan-close-a", "a", [("dbo", "a1")]);
        var rootB = await CreateWorkspaceAsync("scan-close-b", "b", [("dbo", "b1")]);

        await using var host = new WorkspaceSessionHost();
        Assert.True((await host.OpenAsync(rootA)).Success);
        var closed = host.ActiveSession!;

        await host.CloseSessionAsync();
        Assert.False(host.HasSession);
        Assert.Null(host.ActiveSession);
        Assert.Equal(ScanKnowledgeSessionState.Closed, closed.State);
        Assert.ThrowsAny<Exception>(() => host.FindTables(null));

        Assert.True((await host.OpenAsync(rootB)).Success);
        Assert.Equal("scan-close-b", host.ActiveSession!.ScanId);
    }

    [Fact]
    public async Task Dispose_Host_Closes_Session_And_Blocks_Further_Use()
    {
        var root = await CreateWorkspaceAsync("scan-dispose", "d", [("dbo", "t")]);
        var host = new WorkspaceSessionHost();
        Assert.True((await host.OpenAsync(root)).Success);
        var session = host.ActiveSession!;

        await host.DisposeAsync();

        Assert.False(host.HasSession);
        Assert.Equal(ScanKnowledgeSessionState.Closed, session.State);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => host.OpenAsync(root));
        Assert.Throws<ObjectDisposedException>(() => host.FindTables(null));
    }

    [Fact]
    public async Task Open_Does_Not_Modify_Copied_Validation_Workspace()
    {
        var source = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhcKnowledgeScanner",
            "validation-full-inventory-20261103",
            "run2",
            "workspace.db");
        if (!File.Exists(source))
        {
            return;
        }

        var copyRoot = Path.Combine(
            Path.GetTempPath(),
            "PhcKnowledgeScanner-tests",
            "run2-copy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(copyRoot);
        var copyDb = Path.Combine(copyRoot, "workspace.db");
        File.Copy(source, copyDb);

        var before = await Sha256FileAsync(copyDb);

        await using var host = new WorkspaceSessionHost();
        var opened = await host.OpenAsync(copyRoot);
        Assert.True(opened.Success, opened.ErrorMessage);
        Assert.False(string.IsNullOrWhiteSpace(host.ActiveSession!.ScanId));
        Assert.True(host.FindTables(null).Count >= 8000);
        Assert.Contains(host.FindTables("bo"), t => t.Name.Equals("bo", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(host.FindTables("cl"), t => t.Name.Equals("cl", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(host.FindTables("u_"));

        await host.CloseSessionAsync();
        var after = await Sha256FileAsync(copyDb);
        Assert.Equal(before, after);
    }

    private static async Task<string> CreateWorkspaceAsync(
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
}
