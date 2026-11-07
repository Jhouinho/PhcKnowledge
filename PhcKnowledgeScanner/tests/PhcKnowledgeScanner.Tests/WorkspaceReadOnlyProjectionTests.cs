using System.Security.Cryptography;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public sealed class WorkspaceReadOnlyProjectionTests
{
    [Fact]
    public async Task ReadOnly_Workspace_Exposes_Capture_Metadata_Artifact_Bytes_And_Integrity()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF, 0x41, 0x0D, 0x0A, 0x42, 0x00, 0xFF };
        string artifactId;

        await using (var writer = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None))
        {
            await writer.InitializeAsync("scan-readonly", "test/install", CancellationToken.None);
            artifactId = await writer.SaveArtifactAsync(
                "collector.test",
                "definitions/raw.bin",
                bytes,
                "application/octet-stream",
                new Dictionary<string, string> { ["origin"] = "test" },
                CancellationToken.None);

            await writer.SaveCaptureCoverageAsync(
                "capture-123",
                [
                    new DeclaredCapability("test", "test.operation", "1", "collector.test", true, "test capability")
                ],
                [
                    new OperationCoverage(
                        "test.operation",
                        "1",
                        true,
                        OperationExecutionResult.Succeeded,
                        SourceAvailability.Present,
                        ContentAvailability.Available,
                        1,
                        ["issue-1"],
                        new Dictionary<string, string> { ["database"] = "PHC_TEST" })
                ],
                CancellationToken.None);
        }

        await using var reader = await SqliteScanWorkspace.OpenExistingAsync(root, readOnly: true);
        var metadata = await reader.GetCaptureMetadataAsync();
        var artifacts = await reader.GetArtifactsAsync();
        var actualBytes = await reader.ReadArtifactBytesAsync(artifactId);
        var integrity = await reader.CheckIntegrityAsync();

        Assert.Equal("capture-123", metadata.CaptureId);
        Assert.Equal("initialized", metadata.ScanStatus);
        Assert.True(metadata.HasCompleteCoverageMetadata);
        Assert.Single(metadata.Capabilities!);
        Assert.Single(metadata.Coverage!);
        Assert.Equal("test.operation", metadata.Coverage![0].OperationId);

        var artifact = Assert.Single(artifacts);
        Assert.Equal(artifactId, artifact.ArtifactId);
        Assert.Equal(bytes.LongLength, artifact.ByteLength);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), artifact.Sha256);
        Assert.Equal("test", artifact.Metadata!["origin"]);
        Assert.Equal(bytes, actualBytes);

        Assert.True(integrity.IsValid);
        Assert.Equal(new[] { "ok" }, integrity.Messages);
    }

    [Fact]
    public async Task ReadOnly_Workspace_Distinguishes_Missing_Artifact_Row_From_Missing_File()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using (var writer = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None))
        {
            await writer.InitializeAsync("scan-missing", "test/install", CancellationToken.None);
        }

        await using var reader = await SqliteScanWorkspace.OpenExistingAsync(root, readOnly: true);

        Assert.Null(await reader.ReadArtifactBytesAsync("does-not-exist"));
    }

    [Fact]
    public async Task ReadOnly_Workspace_Reports_Legacy_Coverage_Metadata_As_Unavailable()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        await using (var writer = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None))
        {
            await writer.InitializeAsync("scan-legacy", "test/install", CancellationToken.None);
        }

        await using var reader = await SqliteScanWorkspace.OpenExistingAsync(root, readOnly: true);
        var metadata = await reader.GetCaptureMetadataAsync();

        Assert.True(metadata.HasCaptureIdColumn);
        Assert.True(metadata.HasCapabilitiesColumn);
        Assert.True(metadata.HasCoverageColumn);
        Assert.Null(metadata.Capabilities);
        Assert.Null(metadata.Coverage);
        Assert.False(metadata.HasCompleteCoverageMetadata);
    }
}
