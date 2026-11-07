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

    [Fact]
    public void Contract_Validator_Rejects_Available_Definition_Without_Artifact_And_Fingerprint()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"definition":{"status":"Available","artifactIds":[]},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Contains(diagnostics, x => x.Code == "object_index_definition_available_invalid");
    }

    [Fact]
    public void Contract_Validator_Rejects_NonCanonical_Uuid_And_Invalid_Fingerprint()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","captureId":"{b1b2c3d4-1111-4222-8333-123456789abc}","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"metadataFingerprint":{"algorithm":"sha256","value":"NOT_A_HASH"},"definition":{"status":"NotCollected","artifactIds":[]},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Contains(diagnostics, x => x.Code == "object_index_capture_id_invalid");
        Assert.Contains(diagnostics, x => x.Code == "object_index_fingerprint_invalid");
    }

    [Fact]
    public void Contract_Validator_Rejects_Invalid_Source_Identity()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","sourceIdentity":{"scheme":"sqlserver.object_id","value":"","scope":"database:demo"},"metadata":{},"definition":{"status":"NotCollected","artifactIds":[]},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Contains(diagnostics, x => x.Code == "object_index_source_identity_invalid");
    }

    [Fact]
    public void Project_Fails_When_Operation_Is_Not_Declared_In_Capture_Capabilities()
    {
        var record = new TechnicalObjectRecord(
            "database:PHC_DEMO",
            ObjectKind.Database,
            "PHC_DEMO",
            null,
            null,
            "sql.server.structure",
            null,
            """{"versionProperty":"16"}""",
            DateTimeOffset.UnixEpoch);
        var capture = new WorkspaceCaptureMetadata(
            "b1b2c3d4-1111-4222-8333-123456789abc",
            "completed",
            Array.Empty<DeclaredCapability>(),
            Array.Empty<OperationCoverage>(),
            true,
            true,
            true);
        var input = ObjectIndexProjectionInput.Success(
            capture,
            [record],
            Array.Empty<CollectionIssue>(),
            Array.Empty<WorkspaceArtifactMetadata>(),
            Array.Empty<DefinitionArtifactSnapshot>(),
            Array.Empty<ObjectIndexProjectionDiagnostic>());

        var result = new ObjectIndexProjector().Project(input);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, x => x.Code == "object_index_operation_not_declared_supported");
    }

    [Fact]
    public void Project_Fails_When_Objects_Are_Attributed_To_A_Failed_Operation()
    {
        var record = new TechnicalObjectRecord(
            "phc.brule:S1",
            ObjectKind.PhcUserRule,
            "Synthetic rule",
            "dbo",
            null,
            "phc.customizations",
            null,
            """{"source":"brule","stamp":"S1"}""",
            DateTimeOffset.UnixEpoch);
        var capture = new WorkspaceCaptureMetadata(
            "b1b2c3d4-1111-4222-8333-123456789abc",
            "completed",
            [new DeclaredCapability(
                "phc.customizations",
                KnownOperations.PhcBrule,
                KnownOperations.Version,
                "phc.customizations",
                true)],
            [new OperationCoverage(
                KnownOperations.PhcBrule,
                KnownOperations.Version,
                true,
                OperationExecutionResult.Failed,
                SourceAvailability.Present,
                ContentAvailability.Partial,
                1,
                Array.Empty<string>())],
            true,
            true,
            true);
        var input = ObjectIndexProjectionInput.Success(
            capture,
            [record],
            Array.Empty<CollectionIssue>(),
            Array.Empty<WorkspaceArtifactMetadata>(),
            Array.Empty<DefinitionArtifactSnapshot>(),
            Array.Empty<ObjectIndexProjectionDiagnostic>());

        var result = new ObjectIndexProjector().Project(input);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, x => x.Code == "object_index_operation_coverage_inconsistent");
    }

    [Fact]
    public async Task Workspace_Projector_Connects_Read_Only_Reader_To_Pure_Projector()
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-tests", Guid.NewGuid().ToString("N"));
        var captureId = "b1b2c3d4-1111-4222-8333-123456789abc";
        var bytes = Encoding.UTF8.GetBytes("FUNCTION BODY");
        await using var workspace = await PhcKnowledgeScanner.Workspace.SqliteScanWorkspace.CreateAsync(
            root, CancellationToken.None);

        await workspace.InitializeAsync("scan-projector", "demo", CancellationToken.None);
        await workspace.SaveCaptureCoverageAsync(
            captureId,
            [new DeclaredCapability(
                "phc.customizations",
                KnownOperations.PhcBrule,
                KnownOperations.Version,
                "phc.customizations",
                true)],
            [new OperationCoverage(
                KnownOperations.PhcBrule,
                KnownOperations.Version,
                true,
                OperationExecutionResult.Succeeded,
                SourceAvailability.Present,
                ContentAvailability.Available,
                1,
                Array.Empty<string>())],
            CancellationToken.None);
        var artifactId = await workspace.SaveArtifactAsync(
            "phc.customizations",
            "brule/S1.prg",
            bytes,
            "text/plain",
            null,
            CancellationToken.None);
        await workspace.UpsertObjectAsync(
            new TechnicalObjectRecord(
                "phc.brule:S1",
                ObjectKind.PhcUserRule,
                "Regra de teste",
                "dbo",
                null,
                "phc.customizations",
                artifactId,
                """{"source":"brule","stamp":"S1","tabela":"FT","inactivo":"0"}""",
                DateTimeOffset.UnixEpoch),
            CancellationToken.None);

        var result = await new ObjectIndexWorkspaceProjector().ProjectAsync(workspace);

        Assert.True(result.IsSuccess);
        using var document = JsonDocument.Parse(result.JsonBytes!);
        var item = Assert.Single(document.RootElement.GetProperty("objects").EnumerateArray());
        Assert.Equal(artifactId, item.GetProperty("definition").GetProperty("artifactIds")[0].GetString());
        Assert.Equal("Available", item.GetProperty("definition").GetProperty("status").GetString());
    }

    [Theory]
    [InlineData(ObjectKind.Database, "sql.server.structure", null, KnownOperations.SqlDatabase)]
    [InlineData(ObjectKind.Table, "sql.server.structure", null, KnownOperations.SqlTablesInventory)]
    [InlineData(ObjectKind.PrimaryKey, "sql.server.structure", null, KnownOperations.SqlPrimaryKeys)]
    [InlineData(ObjectKind.ForeignKey, "sql.server.structure", null, KnownOperations.SqlForeignKeys)]
    [InlineData(ObjectKind.View, "sql.server.structure", null, KnownOperations.SqlViews)]
    [InlineData(ObjectKind.StoredProcedure, "sql.server.structure", null, KnownOperations.SqlProcedures)]
    [InlineData(ObjectKind.Trigger, "sql.server.structure", null, KnownOperations.SqlTriggers)]
    [InlineData(ObjectKind.PhcUserRule, "phc.customizations", "brule", KnownOperations.PhcBrule)]
    [InlineData(ObjectKind.PhcWebUserRule, "phc.customizations", "ebrule", KnownOperations.PhcEbrule)]
    [InlineData(ObjectKind.PhcHotkey, "phc.customizations", "ti", KnownOperations.PhcTeclas)]
    [InlineData(ObjectKind.PhcDefaultValue, "phc.customizations", "udefs", KnownOperations.PhcUdefs)]
    [InlineData(ObjectKind.PhcWebDefaultValue, "phc.customizations", "eudefs", KnownOperations.PhcEudefs)]
    [InlineData(ObjectKind.PhcEvent, "phc.customizations", "eventos", KnownOperations.PhcEventos)]
    [InlineData(ObjectKind.PhcWebEvent, "phc.customizations", "eeventos", KnownOperations.PhcEeventos)]
    [InlineData(ObjectKind.PhcUserField, "phc.customizations", "sys.columns", KnownOperations.PhcUserFields)]
    [InlineData(ObjectKind.PhcWebScript, "phc.customizations", "escr", KnownOperations.PhcEscr)]
    [InlineData(ObjectKind.PhcUserJavascript, "phc.customizations", "jsu", KnownOperations.PhcJsu)]
    [InlineData(ObjectKind.PhcWebCssJs, "phc.customizations", "cssjs", KnownOperations.PhcCssjs)]
    [InlineData(ObjectKind.PhcHiddenGrid, "phc.customizations", "formc", KnownOperations.PhcFormc)]
    [InlineData(ObjectKind.PhcCustomScreen, "phc.customizations", "formb", KnownOperations.PhcFormb)]
    [InlineData(ObjectKind.PhcUserTsqlFunction, "phc.customizations", "ftu", KnownOperations.PhcFtu)]
    [InlineData(ObjectKind.PhcUserTsqlFunction, "phc.customizations", "sys.objects", KnownOperations.PhcFtu)]
    public void Project_Emits_Valid_IKP_For_Every_Emitted_Object_Kind(
        string kind,
        string collectorId,
        string? source,
        string expectedOperation)
    {
        var summary = source is null ? null : $"""{"source":"{{source}}","stamp":"SYNTH-1"}""";
        var record = new TechnicalObjectRecord(
            $"synthetic:{kind}:{source ?? "sql"}",
            kind,
            "Synthetic object",
            "dbo",
            null,
            collectorId,
            null,
            summary,
            DateTimeOffset.UnixEpoch);

        var result = new ObjectIndexProjector().Project(CreateInput([record]));

        Assert.True(result.IsSuccess, string.Join("; ", result.Diagnostics.Select(x => x.Code + ": " + x.Message)));
        using var document = JsonDocument.Parse(result.JsonBytes!);
        var item = Assert.Single(document.RootElement.GetProperty("objects").EnumerateArray());
        Assert.Equal(kind, item.GetProperty("objectKind").GetString());
        Assert.Equal(expectedOperation, item.GetProperty("operationId").GetString());
    }

    [Fact]
    public void Contract_Validator_Accepts_Minimal_Valid_Object_Index()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"definition":{"status":"NotCollected","artifactIds":[]},"operationId":"sql.tables"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Contract_Validator_Rejects_Invalid_Definition_Status_And_Duplicate_Artifacts()
    {
        var invalidStatus = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"definition":{"status":"Unknown","artifactIds":[]},"operationId":"op"}]}""");
        var duplicateArtifacts = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"definition":{"status":"NotCollected","artifactIds":["a","a"]},"operationId":"op"}]}""");

        Assert.Contains(ObjectIndexContractValidator.Validate(invalidStatus), x => x.Code == "object_index_definition_status_invalid");
        Assert.Contains(ObjectIndexContractValidator.Validate(duplicateArtifacts), x => x.Code == "object_index_definition_artifacts_invalid");
    }

    [Fact]
    public void Contract_Validator_Rejects_Fingerprint_With_Additional_Properties()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"metadataFingerprint":{"algorithm":"sha256","value":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","extra":true},"definition":{"status":"NotCollected","artifactIds":[]},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Contains(diagnostics, x => x.Code == "object_index_fingerprint_invalid");
    }

    [Fact]
    public void Contract_Validator_Rejects_Missing_Required_Properties()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"1","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","definition":{"status":"NotCollected","artifactIds":[]},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Contains(diagnostics, x => x.Code == "object_index_metadata_invalid");
    }

    [Fact]
    public void Contract_Validator_Rejects_Invalid_Json()
    {
        var diagnostics = ObjectIndexContractValidator.Validate(Encoding.UTF8.GetBytes("{"));

        Assert.Contains(diagnostics, x => x.Code == "object_index_json_invalid");
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
