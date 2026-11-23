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
        Assert.Equal("2", root.GetProperty("schemaVersion").GetString());
        Assert.Equal(capture.CaptureId, root.GetProperty("captureId").GetString());
        var item = Assert.Single(root.GetProperty("objects").EnumerateArray());
        Assert.Equal(record.ObjectId, item.GetProperty("sourceObjectId").GetString());
        Assert.Equal(ObjectKind.PhcUserRule, item.GetProperty("objectKind").GetString());
        Assert.Equal(KnownOperations.PhcBrule, item.GetProperty("operationId").GetString());
        Assert.Equal("phc.stamp", item.GetProperty("sourceIdentity").GetProperty("scheme").GetString());
        Assert.Equal("S1", item.GetProperty("sourceIdentity").GetProperty("value").GetString());
        Assert.Equal("Available", item.GetProperty("definition").GetProperty("status").GetString());
        var definitionFingerprint = item.GetProperty("definition").GetProperty("definitionFingerprint");
        Assert.Equal("Unavailable", definitionFingerprint.GetProperty("status").GetString());
        Assert.Equal("NoValidatedPolicy", definitionFingerprint.GetProperty("reasonCode").GetString());
        Assert.False(definitionFingerprint.TryGetProperty("value", out _));
        Assert.True(item.TryGetProperty("metadataFingerprint", out _));
    }

    [Fact]
    public void Project_Is_Deterministic_When_Mixed_Synthetic_Capture_Order_Changes()
    {
        var database = new TechnicalObjectRecord(
            "sql.database:demo",
            ObjectKind.Database,
            "PHC_DEMO",
            null,
            null,
            "sql.server.structure",
            null,
            """{"versionProperty":"16"}""",
            DateTimeOffset.UnixEpoch);
        var table = new TechnicalObjectRecord(
            "sql.table:123",
            ObjectKind.Table,
            "FT",
            "dbo",
            null,
            "sql.server.structure",
            null,
            """{"objectId":123,"typeDesc":"USER_TABLE","isMsShipped":false}""",
            DateTimeOffset.UnixEpoch);
        var userField = new TechnicalObjectRecord(
            "phc.user_field:456",
            ObjectKind.PhcUserField,
            "u_test",
            "dbo",
            table.ObjectId,
            "phc.customizations",
            null,
            """{"source":"sys.columns","columnId":456,"parentObjectId":"123","type":"varchar"}""",
            DateTimeOffset.UnixEpoch);
        var rule = new TechnicalObjectRecord(
            "phc.brule:SYNTH-1",
            ObjectKind.PhcUserRule,
            "Regra sintética",
            "dbo",
            null,
            "phc.customizations",
            "artifact-rule",
            """{"source":"brule","stamp":"SYNTH-1","tabela":"FT","inactivo":"0"}""",
            DateTimeOffset.UnixEpoch);

        TechnicalObjectRecord Phc(string objectId, string kind, string source) =>
            new(
                objectId,
                kind,
                $"Synthetic {kind}",
                "dbo",
                null,
                "phc.customizations",
                null,
                $$"""{"source":"{{source}}","stamp":"{{objectId}}","inactivo":"0","tabela":"FT","ecran":"FT","campo":"u_test","qevento":"1","codigo":"TEST","teclas":"F7","nome":"Synthetic","tag":"TAG","tvalobj":"VALUE","tipo":"JS","defeito":"0","classif":"U"}""",
                DateTimeOffset.UnixEpoch);

        var primaryKey = new TechnicalObjectRecord(
            "sql.pk:pk-ft",
            ObjectKind.PrimaryKey,
            "PK_FT",
            "dbo",
            table.ObjectId,
            "sql.server.structure",
            null,
            """{"columns":[{"name":"FTSTAMP","ordinal":1}]}""",
            DateTimeOffset.UnixEpoch);
        var foreignKey = new TechnicalObjectRecord(
            "sql.fk:fk-ft",
            ObjectKind.ForeignKey,
            "FK_FT",
            "dbo",
            table.ObjectId,
            "sql.server.structure",
            null,
            """{"referencedSchema":"dbo","referencedTable":"CL","deleteReferentialAction":"NO_ACTION","updateReferentialAction":"NO_ACTION","columns":[{"parent":"CLSTAMP","referenced":"CLSTAMP","ordinal":1}]}""",
            DateTimeOffset.UnixEpoch);
        var view = new TechnicalObjectRecord(
            "sql.view:v-demo",
            ObjectKind.View,
            "vw_Demo",
            "dbo",
            null,
            "sql.server.structure",
            null,
            null,
            DateTimeOffset.UnixEpoch);
        var procedure = new TechnicalObjectRecord(
            "sql.procedure:p-demo",
            ObjectKind.StoredProcedure,
            "sp_Demo",
            "dbo",
            null,
            "sql.server.structure",
            null,
            null,
            DateTimeOffset.UnixEpoch);
        var trigger = new TechnicalObjectRecord(
            "sql.trigger:tr-demo",
            ObjectKind.Trigger,
            "tr_Demo",
            "dbo",
            table.ObjectId,
            "sql.server.structure",
            null,
            """{"isDisabled":false}""",
            DateTimeOffset.UnixEpoch);

        var completeObjects = new[]
        {
            database, table, userField, rule,
            primaryKey, foreignKey, view, procedure, trigger,
            Phc("phc.ebrule:SYNTH-2", ObjectKind.PhcWebUserRule, "ebrule"),
            Phc("phc.ti:SYNTH-3", ObjectKind.PhcHotkey, "ti"),
            Phc("phc.udefs:SYNTH-4", ObjectKind.PhcDefaultValue, "udefs"),
            Phc("phc.eudefs:SYNTH-5", ObjectKind.PhcWebDefaultValue, "eudefs"),
            Phc("phc.eventos:SYNTH-6", ObjectKind.PhcEvent, "eventos"),
            Phc("phc.eeventos:SYNTH-7", ObjectKind.PhcWebEvent, "eeventos"),
            Phc("phc.escr:SYNTH-8", ObjectKind.PhcWebScript, "escr"),
            Phc("phc.jsu:SYNTH-9", ObjectKind.PhcUserJavascript, "jsu"),
            Phc("phc.cssjs:SYNTH-10", ObjectKind.PhcWebCssJs, "cssjs"),
            Phc("phc.formc:SYNTH-11", ObjectKind.PhcHiddenGrid, "formc"),
            Phc("phc.formb:SYNTH-12", ObjectKind.PhcCustomScreen, "formb"),
            Phc("phc.ftu:SYNTH-13", ObjectKind.PhcUserTsqlFunction, "ftu")
        };

        var definitionBytes = Encoding.UTF8.GetBytes("FUNCTION BODY\\r\\n");
        var definitionHash = Hash(definitionBytes);
        ObjectIndexProjectionInput CreateMixedInput(IReadOnlyList<TechnicalObjectRecord> sourceObjects) =>
            ObjectIndexProjectionInput.Success(
                new WorkspaceCaptureMetadata(
                    "b1b2c3d4-1111-4222-8333-123456789abc",
                    "completed", null, null, true, false, false),
                sourceObjects,
                Array.Empty<CollectionIssue>(),
                [new WorkspaceArtifactMetadata(
                    "artifact-rule", "phc.customizations", "brule/SYNTH-1.prg",
                    "text/plain", definitionHash, definitionBytes.Length, null)],
                [new DefinitionArtifactSnapshot(
                    "artifact-rule", definitionBytes, definitionHash, definitionBytes.Length)],
                Array.Empty<ObjectIndexProjectionDiagnostic>());

        var first = new ObjectIndexProjector().Project(
            CreateMixedInput(completeObjects));
        var second = new ObjectIndexProjector().Project(
            CreateMixedInput(completeObjects.Reverse().ToArray()));

        Assert.True(first.IsSuccess, string.Join("; ", first.Diagnostics.Select(x => x.Code)));
        Assert.True(second.IsSuccess, string.Join("; ", second.Diagnostics.Select(x => x.Code)));
        Assert.Equal(first.JsonBytes, second.JsonBytes);

        // CI validates this complete mixed SQL/PHC projection against the
        // authoritative Draft 2020-12 schema.
        var validationOutput = Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_IKP_OUTPUT");
        if (!string.IsNullOrWhiteSpace(validationOutput))
        {
            var outputDirectory = Path.GetDirectoryName(validationOutput);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            File.WriteAllBytes(validationOutput, first.JsonBytes!);
        }

        using var document = JsonDocument.Parse(first.JsonBytes!);
        var objects = document.RootElement.GetProperty("objects").EnumerateArray().ToArray();
        Assert.Equal(21, objects.Length);

        var projectedTable = objects.Single(x => x.GetProperty("sourceObjectId").GetString() == table.ObjectId);
        Assert.Equal("sqlserver.object_id", projectedTable.GetProperty("sourceIdentity").GetProperty("scheme").GetString());
        Assert.Equal("123", projectedTable.GetProperty("sourceIdentity").GetProperty("value").GetString());

        var projectedField = objects.Single(x => x.GetProperty("sourceObjectId").GetString() == userField.ObjectId);
        Assert.Equal(table.ObjectId, projectedField.GetProperty("parentSourceObjectId").GetString());
        Assert.Equal("sqlserver.column_id", projectedField.GetProperty("sourceIdentity").GetProperty("scheme").GetString());
        Assert.Equal("456", projectedField.GetProperty("sourceIdentity").GetProperty("value").GetString());
        Assert.Equal(
            "database:PHC_DEMO/object:123",
            projectedField.GetProperty("sourceIdentity").GetProperty("scope").GetString());

        var projectedRule = objects.Single(x => x.GetProperty("sourceObjectId").GetString() == rule.ObjectId);
        Assert.Equal("phc.stamp", projectedRule.GetProperty("sourceIdentity").GetProperty("scheme").GetString());
        Assert.Equal("SYNTH-1", projectedRule.GetProperty("sourceIdentity").GetProperty("value").GetString());
        Assert.Equal("Available", projectedRule.GetProperty("definition").GetProperty("status").GetString());
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
    public void Contract_Validator_Accepts_Available_Fingerprint_With_Versioned_Policy()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"phc.brule","metadata":{},"definition":{"status":"Available","artifactIds":["a"],"definitionFingerprint":{"status":"Available","algorithm":"sha256","value":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","policy":{"id":"test.policy","version":"1"}}},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Contract_Validator_Accepts_Unknown_Fingerprint_Reason_Code()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"phc.brule","metadata":{},"definition":{"status":"Available","artifactIds":["a"],"definitionFingerprint":{"status":"Unavailable","reasonCode":"FutureReasonCode"}},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Contract_Validator_Rejects_Available_Definition_Without_Artifact_And_Fingerprint()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"definition":{"status":"Available","artifactIds":[]},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Contains(diagnostics, x => x.Code == "object_index_definition_available_invalid");
    }

    [Fact]
    public void Contract_Validator_Rejects_NonCanonical_Uuid_And_Invalid_Fingerprint()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","captureId":"{b1b2c3d4-1111-4222-8333-123456789abc}","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"metadataFingerprint":{"algorithm":"sha256","value":"NOT_A_HASH"},"definition":{"status":"NotCollected","artifactIds":[]},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Contains(diagnostics, x => x.Code == "object_index_capture_id_invalid");
        Assert.Contains(diagnostics, x => x.Code == "object_index_fingerprint_invalid");
    }

    [Fact]
    public void Contract_Validator_Rejects_Invalid_Source_Identity()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","sourceIdentity":{"scheme":"sqlserver.object_id","value":"","scope":"database:demo"},"metadata":{},"definition":{"status":"NotCollected","artifactIds":[]},"operationId":"op"}]}""");

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
        var summary = source is null ? null : $$"""{"source":"{{source}}","stamp":"SYNTH-1"}""";
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
    public void Project_Preserves_Parent_Reference_When_Parent_Is_In_Same_Capture()
    {
        var parent = new TechnicalObjectRecord(
            "sql.table:123",
            ObjectKind.Table,
            "FT",
            "dbo",
            null,
            "sql.server.structure",
            null,
            """{"typeDesc":"USER_TABLE","isMsShipped":false}""",
            DateTimeOffset.UnixEpoch);
        var child = new TechnicalObjectRecord(
            "phc.user_field:456",
            ObjectKind.PhcUserField,
            "u_test",
            "dbo",
            parent.ObjectId,
            "phc.customizations",
            null,
            """{"source":"sys.columns","columnId":456,"parentObjectId":"123"}""",
            DateTimeOffset.UnixEpoch);

        var result = new ObjectIndexProjector().Project(CreateInput([child, parent]));

        Assert.True(result.IsSuccess, string.Join("; ", result.Diagnostics.Select(x => x.Code)));
        using var document = JsonDocument.Parse(result.JsonBytes!);
        var projectedChild = document.RootElement.GetProperty("objects")
            .EnumerateArray()
            .Single(x => x.GetProperty("sourceObjectId").GetString() == child.ObjectId);
        Assert.Equal(parent.ObjectId, projectedChild.GetProperty("parentSourceObjectId").GetString());
    }

    [Fact]
    public void Project_Does_Not_Emit_Unresolved_Parent_Reference()
    {
        var child = new TechnicalObjectRecord(
            "phc.user_field:456",
            ObjectKind.PhcUserField,
            "u_test",
            "dbo",
            "missing-parent",
            "phc.customizations",
            null,
            """{"source":"sys.columns","columnId":456,"parentObjectId":"123"}""",
            DateTimeOffset.UnixEpoch);

        var result = new ObjectIndexProjector().Project(CreateInput([child]));

        Assert.True(result.IsSuccess);
        using var document = JsonDocument.Parse(result.JsonBytes!);
        var item = Assert.Single(document.RootElement.GetProperty("objects").EnumerateArray());
        Assert.False(item.TryGetProperty("parentSourceObjectId", out _));
        Assert.Contains(result.Diagnostics, x => x.Code == "object_index_parent_unresolved");
    }

    [Fact]
    public void Contract_Validator_Accepts_Minimal_Valid_Object_Index()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"definition":{"status":"NotCollected","artifactIds":[]},"operationId":"sql.tables"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Contract_Validator_Rejects_Invalid_Definition_Status_And_Duplicate_Artifacts()
    {
        var invalidStatus = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"definition":{"status":"Unknown","artifactIds":[]},"operationId":"op"}]}""");
        var duplicateArtifacts = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"definition":{"status":"NotCollected","artifactIds":["a","a"]},"operationId":"op"}]}""");

        Assert.Contains(ObjectIndexContractValidator.Validate(invalidStatus), x => x.Code == "object_index_definition_status_invalid");
        Assert.Contains(ObjectIndexContractValidator.Validate(duplicateArtifacts), x => x.Code == "object_index_definition_artifacts_invalid");
    }

    [Fact]
    public void Contract_Validator_Rejects_Fingerprint_With_Additional_Properties()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","metadata":{},"metadataFingerprint":{"algorithm":"sha256","value":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","extra":true},"definition":{"status":"NotCollected","artifactIds":[]},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Contains(diagnostics, x => x.Code == "object_index_fingerprint_invalid");
    }

    [Fact]
    public void Contract_Validator_Rejects_Missing_Required_Properties()
    {
        var json = Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","captureId":"b1b2c3d4-1111-4222-8333-123456789abc","objects":[{"sourceObjectId":"x","objectKind":"table","definition":{"status":"NotCollected","artifactIds":[]},"operationId":"op"}]}""");

        var diagnostics = ObjectIndexContractValidator.Validate(json);

        Assert.Contains(diagnostics, x => x.Code == "object_index_metadata_invalid");
    }

    [Fact]
    public void Contract_Validator_Rejects_Invalid_Json()
    {
        var diagnostics = ObjectIndexContractValidator.Validate(Encoding.UTF8.GetBytes("{"));

        Assert.Contains(diagnostics, x => x.Code == "object_index_json_invalid");
    }

    [Theory]
    [InlineData(ObjectKind.View, "sql.view:dbo.vw_Demo", "vw_Demo", KnownOperations.SqlViews)]
    [InlineData(ObjectKind.StoredProcedure, "sql.procedure:dbo.usp_Demo", "usp_Demo", KnownOperations.SqlProcedures)]
    public void Project_Does_Not_Create_Unmaterialized_Schema_Parent_For_Sql_Modules(
        string kind,
        string objectId,
        string name,
        string operationId)
    {
        var record = new TechnicalObjectRecord(
            objectId,
            kind,
            name,
            "dbo",
            null,
            "sql.server.structure",
            null,
            null,
            DateTimeOffset.UnixEpoch);

        var result = new ObjectIndexProjector().Project(CreateInput([record]));

        Assert.True(result.IsSuccess, string.Join("; ", result.Diagnostics.Select(x => x.Code)));
        using var document = JsonDocument.Parse(result.JsonBytes!);
        var item = Assert.Single(document.RootElement.GetProperty("objects").EnumerateArray());
        Assert.Equal("dbo", item.GetProperty("schemaName").GetString());
        Assert.False(item.TryGetProperty("parentSourceObjectId", out _));
        Assert.DoesNotContain(result.Diagnostics, x => x.Code == "object_index_parent_unresolved");
        Assert.Equal(operationId, item.GetProperty("operationId").GetString());
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
