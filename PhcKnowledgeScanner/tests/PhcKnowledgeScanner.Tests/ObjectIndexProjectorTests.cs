using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Workspace;
using PhcKnowledgeScanner.Export.ObjectIndex;

namespace PhcKnowledgeScanner.Tests;

public sealed class ObjectIndexProjectorTests
{
    [Fact]
    public void Project_Emits_Deterministic_IKP_Object_Index()
    {
        var bytes = Encoding.UTF8.GetBytes("FUNCTION BODY\r\n");
        var hash = Hash(bytes);
        var record = new TechnicalObjectRecord(
            "phc.brule:S1",
            ObjectKind.PhcUserRule,
            "Regra de teste",
            "dbo",
            null,
            "phc.customizations",
            "artifact-1",
            """{"source":"brule","stamp":"S1","tabela":"FT","inactivo":"0"}""",
            DateTimeOffset.UnixEpoch);
        var capture = new WorkspaceCaptureMetadata(
            "b1b2c3d4-1111-4222-8333-123456789abc",
            "completed",
            null,
            null,
            true,
            false,
            false);
        var input = ObjectIndexProjectionInput.Success(
            capture,
            [record],
            Array.Empty<CollectionIssue>(),
            [new WorkspaceArtifactMetadata(
                "artifact-1", "phc.customizations", "brule/S1.prg", "text/plain",
                hash, bytes.Length, null)],
            [new DefinitionArtifactSnapshot("artifact-1", bytes, hash, bytes.Length)],
            Array.Empty<ObjectIndexProjectionDiagnostic>());

        var projector = new ObjectIndexProjector();
        var first = projector.Project(input);
        var second = projector.Project(input);

        Assert.True(first.IsSuccess);
        Assert.NotNull(first.JsonBytes);
        Assert.Equal(first.JsonBytes, second.JsonBytes);

        using var document = JsonDocument.Parse(first.JsonBytes!);
        var root = document.RootElement;
        Assert.Equal("1", root.GetProperty("schemaVersion").GetString());
        Assert.Equal(capture.CaptureId, root.GetProperty("captureId").GetString());
        var item = Assert.Single(root.GetProperty("objects").EnumerateArray());
        Assert.Equal(record.ObjectId, item.GetProperty("sourceObjectId").GetString());
        Assert.Equal(ObjectKind.PhcUserRule, item.GetProperty("objectKind").GetString());
        Assert.Equal(KnownOperations.PhcBrule, item.GetProperty("operationId").GetString());
        Assert.Equal("phc.stamp", item.GetProperty("sourceIdentity").GetProperty("scheme").GetString());
        Assert.Equal("S1", item.GetProperty("sourceIdentity").GetProperty("value").GetString());
        Assert.Equal("Available", item.GetProperty("definition").GetProperty("status").GetString());
        Assert.Equal(hash, item.GetProperty("definition").GetProperty("definitionFingerprint").GetProperty("value").GetString());
        Assert.True(item.TryGetProperty("metadataFingerprint", out _));
    }

    [Fact]
    public void Project_Fails_When_OperationId_Cannot_Be_Resolved()
    {
        var record = new TechnicalObjectRecord(
            "unknown-1", "unknown.kind", "Unknown", null, null,
            "unknown.collector", null, null, DateTimeOffset.UnixEpoch);
        var input = CreateInput([record]);

        var result = new ObjectIndexProjector().Project(input);

        Assert.False(result.IsSuccess);
        Assert.Null(result.JsonBytes);
        Assert.Contains(result.Diagnostics, x => x.Code == "object_kind_unsupported");
    }

    [Fact]
    public void Project_Fails_When_SourceObjectId_Is_Duplicated()
    {
        var record = new TechnicalObjectRecord(
            "duplicate", ObjectKind.Database, "DB", null, null,
            "sql.server.structure", null, """{"versionProperty":"16"}""", DateTimeOffset.UnixEpoch);
        var input = CreateInput([record, record]);

        var result = new ObjectIndexProjector().Project(input);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, x => x.Code == "object_index_source_object_id_not_unique");
    }

    private static ObjectIndexProjectionInput CreateInput(IReadOnlyList<TechnicalObjectRecord> objects) =>
        ObjectIndexProjectionInput.Success(
            new WorkspaceCaptureMetadata(
                "b1b2c3d4-1111-4222-8333-123456789abc",
                "completed", null, null, true, false, false),
            objects,
            Array.Empty<CollectionIssue>(),
            Array.Empty<WorkspaceArtifactMetadata>(),
            Array.Empty<DefinitionArtifactSnapshot>(),
            Array.Empty<ObjectIndexProjectionDiagnostic>());

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
