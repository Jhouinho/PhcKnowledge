using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public sealed class WorkspaceReadApiTests
{
    [Fact]
    public async Task Reads_Capture_Metadata_Artifact_Metadata_Exact_Bytes_And_Integrity()
    {
        var root = NewRoot();
        string artifactId;
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF, (byte)'A', (byte)'\r', (byte)'\n', (byte)'B' };

        await using (var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None))
        {
            await workspace.InitializeAsync("scan-read", "demo", CancellationToken.None);
            await workspace.SaveCaptureCoverageAsync(
                "capture-read",
                Array.Empty<PhcKnowledgeScanner.Abstractions.Coverage.DeclaredCapability>(),
                Array.Empty<PhcKnowledgeScanner.Abstractions.Coverage.OperationCoverage>(),
                CancellationToken.None);

            artifactId = await workspace.SaveArtifactAsync(
                "sql.server.structure",
                "views/dbo.v.sql",
                bytes,
                "text/sql",
                new Dictionary<string, string> { ["source"] = "unit-test" },
                CancellationToken.None);

            var capture = await workspace.GetCaptureMetadataAsync();
            Assert.True(capture.HasMetadataRow);
            Assert.Equal("capture-read", capture.CaptureId);
            Assert.True(capture.HasCaptureIdColumn);
            Assert.True(capture.HasCompleteCoverageMetadata);
            Assert.Empty(capture.Capabilities!);
            Assert.Empty(capture.Coverage!);
            Assert.Empty(capture.Diagnostics!);

            var artifacts = await workspace.GetArtifactsAsync();
            var artifact = Assert.Single(artifacts);
            Assert.Equal(artifactId, artifact.ArtifactId);
            Assert.Equal("sql.server.structure", artifact.CollectorId);
            Assert.Equal("views/dbo.v.sql", artifact.RelativePath);
            Assert.Equal(bytes.LongLength, artifact.ByteLength);
            Assert.Equal("unit-test", artifact.Metadata!["source"]);
            Assert.NotNull(artifact.MetadataJson);

            var byId = await workspace.GetArtifactMetadataAsync(artifactId);
            Assert.Equal(artifact.ArtifactId, byId?.ArtifactId);
            Assert.Equal(artifact.Sha256, byId?.Sha256);

            var readBytes = await workspace.ReadArtifactBytesAsync(artifactId);
            Assert.NotNull(readBytes);
            Assert.Equal(bytes, readBytes!);

            var integrity = await workspace.CheckIntegrityAsync();
            Assert.True(integrity.IsValid);
            Assert.Equal(new[] { "ok" }, integrity.Messages);
        }
    }

    [Fact]
    public async Task Read_Only_Api_Does_Not_Modify_Workspace_Database()
    {
        var root = NewRoot();
        var bytes = "definition\r\n"u8.ToArray();

        await using (var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None))
        {
            await workspace.InitializeAsync("scan-readonly", "demo", CancellationToken.None);
            await workspace.SaveCaptureCoverageAsync(
                "capture-readonly",
                Array.Empty<PhcKnowledgeScanner.Abstractions.Coverage.DeclaredCapability>(),
                Array.Empty<PhcKnowledgeScanner.Abstractions.Coverage.OperationCoverage>(),
                CancellationToken.None);
            await workspace.SaveArtifactAsync(
                "phc.customizations",
                "brule/rule.txt",
                bytes,
                "text/plain",
                null,
                CancellationToken.None);
        }

        var dbPath = Path.Combine(root, "workspace.db");
        var before = SHA256.HashData(await ReadFileSharedAsync(dbPath));

        await using (var readOnly = await SqliteScanWorkspace.OpenExistingAsync(root, readOnly: true))
        {
            var capture = await readOnly.GetCaptureMetadataAsync();
            Assert.Equal("capture-readonly", capture.CaptureId);

            var artifacts = await readOnly.GetArtifactsAsync();
            var artifact = Assert.Single(artifacts);
            var readBytes = await readOnly.ReadArtifactBytesAsync(artifact.ArtifactId);
            Assert.NotNull(readBytes);
            Assert.Equal(bytes, readBytes!);

            var integrity = await readOnly.CheckIntegrityAsync();
            Assert.True(integrity.IsValid);
        }

        var after = SHA256.HashData(await ReadFileSharedAsync(dbPath));
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Old_Workspace_Without_Capture_Json_Columns_Reports_Metadata_Unavailable()
    {
        var root = NewRoot();
        await using (var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None))
        {
            await workspace.InitializeAsync("scan-old", "legacy", CancellationToken.None);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(root, "workspace.db")
        }.ToString();
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                ALTER TABLE scan_meta DROP COLUMN capture_id;
                ALTER TABLE scan_meta DROP COLUMN capabilities_json;
                ALTER TABLE scan_meta DROP COLUMN coverage_json;
                """;
            await command.ExecuteNonQueryAsync();
        }

        await using var readOnly = await SqliteScanWorkspace.OpenExistingAsync(root, readOnly: true);
        var capture = await readOnly.GetCaptureMetadataAsync();

        Assert.True(capture.HasMetadataRow);
        Assert.False(capture.HasCaptureIdColumn);
        Assert.False(capture.HasCapabilitiesColumn);
        Assert.False(capture.HasCoverageColumn);
        Assert.False(capture.HasCompleteCoverageMetadata);
        Assert.Null(capture.Capabilities);
        Assert.Null(capture.Coverage);
        Assert.NotEmpty(capture.Diagnostics!);
    }

    [Fact]
    public async Task Missing_Artifact_File_Is_Distinct_From_Missing_Artifact_Row()
    {
        var root = NewRoot();
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-missing", "demo", CancellationToken.None);

        var artifactId = await workspace.SaveArtifactAsync(
            "sql.server.structure",
            "views/missing.sql",
            "content"u8.ToArray(),
            "text/sql",
            null,
            CancellationToken.None);

        var artifact = await workspace.GetArtifactMetadataAsync(artifactId);
        Assert.NotNull(artifact);

        var fullPath = Path.Combine(root, "artifacts", artifact!.CollectorId, artifact.RelativePath);
        File.Delete(fullPath);

        Assert.Null(await workspace.ReadArtifactBytesAsync(artifactId));
        Assert.Null(await workspace.GetArtifactMetadataAsync("not-present"));
    }

    [Fact]
    public async Task Artifact_Path_Traversal_Is_Rejected()
    {
        var root = NewRoot();
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync("scan-path", "demo", CancellationToken.None);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(root, "workspace.db")
        }.ToString();
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO artifacts(artifact_id, collector_id, relative_path, content_type, sha256, byte_length, metadata_json)
                VALUES ('sql.server.structure/../outside.txt', 'sql.server.structure', '../outside.txt', 'text/plain', '00', 1, NULL);
                """;
            await command.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => workspace.ReadArtifactBytesAsync("sql.server.structure/../outside.txt"));
    }

    private static async Task<byte[]> ReadFileSharedAsync(string path)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            useAsync: true);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private static string NewRoot() =>
        Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
}
