using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Export.ObjectIndex;

namespace PhcKnowledgeScanner.Tests;

public sealed class MetadataProjectionResolverTests
{
    [Fact]
    public void Table_Descriptive_Metadata_And_Fingerprint_Use_Different_Field_Sets()
    {
        var record = Create(
            ObjectKind.Table,
            "sql.server.structure",
            """{"objectId":123,"typeDesc":"USER_TABLE","isMsShipped":false,"classification":"custom","namePrefixU":true,"createDate":"2020-01-01T00:00:00Z","modifyDate":"2026-01-01T00:00:00Z"}""");

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Null(result.DiagnosticCode);
        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        using var fingerprint = JsonDocument.Parse(result.FingerprintProjection!.Value.GetRawText());

        Assert.Equal("USER_TABLE", metadata.RootElement.GetProperty("typeDesc").GetString());
        Assert.Equal("custom", metadata.RootElement.GetProperty("classification").GetString());
        Assert.True(metadata.RootElement.GetProperty("namePrefixU").GetBoolean());
        Assert.Equal(2, fingerprint.RootElement.EnumerateObject().Count());
        Assert.Equal("USER_TABLE", fingerprint.RootElement.GetProperty("typeDesc").GetString());
        Assert.False(fingerprint.RootElement.GetProperty("isMsShipped").GetBoolean());
        Assert.False(fingerprint.RootElement.TryGetProperty("createDate", out _));
    }

    [Fact]
    public void Database_Projects_Version_Without_Metadata_Fingerprint()
    {
        var record = Create(
            ObjectKind.Database,
            "sql.server.structure",
            """{"name":"PHCDB","databaseId":7,"versionProperty":"SQL Server 2022"}""");

        var result = MetadataProjectionResolver.Resolve(record);

        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        Assert.Equal("SQL Server 2022", metadata.RootElement.GetProperty("versionProperty").GetString());
        Assert.False(result.HasFingerprintProjection);
    }

    [Fact]
    public void Primary_Key_Preserves_Column_Order_In_Metadata_And_Fingerprint()
    {
        var record = Create(
            ObjectKind.PrimaryKey,
            "sql.server.structure",
            """{"uniqueIndexId":4,"columns":[{"name":"col_b","ordinal":1},{"name":"col_a","ordinal":2}]}""");

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Null(result.DiagnosticCode);
        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        var names = metadata.RootElement.GetProperty("columns").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .ToArray();
        Assert.Equal(new[] { "col_b", "col_a" }, names);
        Assert.False(metadata.RootElement.TryGetProperty("uniqueIndexId", out _));
    }

    [Fact]
    public void Foreign_Key_Includes_Actions_And_Ordered_Columns()
    {
        var record = Create(
            ObjectKind.ForeignKey,
            "sql.server.structure",
            """{"referencedSchema":"dbo","referencedTable":"bo","deleteReferentialAction":"CASCADE","updateReferentialAction":"NO_ACTION","columns":[{"parent":"bostamp","referenced":"bostamp","ordinal":1}]}""");

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Null(result.DiagnosticCode);
        using var fingerprint = JsonDocument.Parse(result.FingerprintProjection!.Value.GetRawText());
        Assert.Equal("CASCADE", fingerprint.RootElement.GetProperty("deleteReferentialAction").GetString());
        Assert.Equal("bostamp", fingerprint.RootElement.GetProperty("columns")[0].GetProperty("parent").GetString());
    }

    [Fact]
    public void Trigger_Fingerprints_IsDisabled()
    {
        var record = new TechnicalObjectRecord(
            "trigger:test",
            ObjectKind.Trigger,
            "tr_test",
            "dbo",
            "table:dbo.FT",
            "sql.server.structure",
            null,
            """{"isDisabled":true}""",
            DateTimeOffset.UnixEpoch);

        var result = MetadataProjectionResolver.Resolve(record);

        using var fingerprint = JsonDocument.Parse(result.FingerprintProjection!.Value.GetRawText());
        Assert.True(fingerprint.RootElement.GetProperty("isDisabled").GetBoolean());
    }

    [Fact]
    public void Trigger_Preserves_Matching_Tuple_Events_And_InsteadOf_Metadata()
    {
        var record = new TechnicalObjectRecord(
            "trigger:tuple",
            ObjectKind.Trigger,
            "tr_test",
            "dbo",
            "table:dbo.FT",
            "sql.server.structure",
            null,
            """{"triggerSchema":"dbo","triggerName":"tr_test","parentSchema":"dbo","parentName":"FT","events":["INSERT","UPDATE"],"isDisabled":false,"isInsteadOf":true}""",
            DateTimeOffset.UnixEpoch);

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Null(result.DiagnosticCode);
        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        using var fingerprint = JsonDocument.Parse(result.FingerprintProjection!.Value.GetRawText());
        Assert.Equal("dbo", metadata.RootElement.GetProperty("triggerSchema").GetString());
        Assert.Equal("tr_test", metadata.RootElement.GetProperty("triggerName").GetString());
        Assert.Equal("dbo", metadata.RootElement.GetProperty("parentSchema").GetString());
        Assert.Equal("FT", metadata.RootElement.GetProperty("parentName").GetString());
        Assert.True(metadata.RootElement.GetProperty("isInsteadOf").GetBoolean());
        Assert.Equal(new[] { "INSERT", "UPDATE" },
            metadata.RootElement.GetProperty("events").EnumerateArray().Select(x => x.GetString()).ToArray());
        Assert.True(fingerprint.RootElement.GetProperty("isInsteadOf").GetBoolean());
        Assert.Equal(new[] { "INSERT", "UPDATE" },
            fingerprint.RootElement.GetProperty("events").EnumerateArray().Select(x => x.GetString()).ToArray());
    }

    [Fact]
    public void Trigger_V2_Uses_Explicit_Tuple_When_Identifiers_Contain_Dots_Or_Unicode()
    {
        var record = new TechnicalObjectRecord(
            "trigger:tuple-with-special-identifiers",
            ObjectKind.Trigger,
            "tr.ação",
            "sch.ema",
            "table:sch.ema.tab.le",
            "sql.server.structure",
            null,
            """{"triggerSchema":"sch.ema","triggerName":"tr.ação","parentSchema":"sch.ema","parentName":"tab.le","events":["INSERT"],"isDisabled":false,"isInsteadOf":false}""",
            DateTimeOffset.UnixEpoch);

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Null(result.DiagnosticCode);
        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        Assert.Equal("sch.ema", metadata.RootElement.GetProperty("parentSchema").GetString());
        Assert.Equal("tab.le", metadata.RootElement.GetProperty("parentName").GetString());
    }

    [Fact]
    public void Trigger_V2_With_Only_Half_Of_Parent_Tuple_Does_Not_Fall_Back_To_Dotted_Record_Id()
    {
        var record = new TechnicalObjectRecord(
            "trigger:partial-tuple",
            ObjectKind.Trigger,
            "tr_test",
            "dbo",
            "table:dbo.FT",
            "sql.server.structure",
            null,
            """{"triggerSchema":"dbo","triggerName":"tr_test","parentSchema":"dbo","events":["INSERT"],"isDisabled":false,"isInsteadOf":false}""",
            DateTimeOffset.UnixEpoch);

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Equal("metadata_fingerprint_incomplete", result.DiagnosticCode);
        Assert.False(result.HasFingerprintProjection);
    }

    [Fact]
    public void Legacy_Trigger_With_Dotted_Parent_Name_Is_Not_Guessed()
    {
        var record = new TechnicalObjectRecord(
            "trigger:legacy-dotted",
            ObjectKind.Trigger,
            "tr_legacy",
            "dbo",
            "table:dbo.FT.HIST",
            "sql.server.structure",
            null,
            """{"isDisabled":false}""",
            DateTimeOffset.UnixEpoch);

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Equal("metadata_fingerprint_incomplete", result.DiagnosticCode);
        Assert.False(result.HasFingerprintProjection);
    }

    [Fact]
    public void Legacy_Trigger_Summary_Still_Projects_Identity_From_Capture_Record()
    {
        var record = new TechnicalObjectRecord(
            "trigger:legacy",
            ObjectKind.Trigger,
            "tr_legacy",
            "dbo",
            "table:dbo.FT",
            "sql.server.structure",
            null,
            """{"isDisabled":false}""",
            DateTimeOffset.UnixEpoch);

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Null(result.DiagnosticCode);
        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        Assert.Equal("dbo", metadata.RootElement.GetProperty("triggerSchema").GetString());
        Assert.Equal("tr_legacy", metadata.RootElement.GetProperty("triggerName").GetString());
        Assert.Equal("dbo", metadata.RootElement.GetProperty("parentSchema").GetString());
        Assert.Equal("FT", metadata.RootElement.GetProperty("parentName").GetString());
        Assert.False(metadata.RootElement.TryGetProperty("events", out _));
        Assert.False(metadata.RootElement.TryGetProperty("isInsteadOf", out _));
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("1", true)]
    [InlineData(".T.", true)]
    [InlineData("não", false)]
    public void Phc_Activity_Is_Preserved_Raw_And_Normalized_Only_When_Recognized(
        string raw,
        bool expectedInactive)
    {
        var record = Create(
            ObjectKind.PhcUserRule,
            "phc.customizations",
            $$"""{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"{{raw}}","estado":"Activo"}""");

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Null(result.DiagnosticCode);
        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        using var fingerprint = JsonDocument.Parse(result.FingerprintProjection!.Value.GetRawText());
        Assert.Equal(raw, metadata.RootElement.GetProperty("inactivoRaw").GetString());
        Assert.Equal(expectedInactive, fingerprint.RootElement.GetProperty("inactivo").GetBoolean());
        Assert.False(metadata.RootElement.TryGetProperty("estado", out _));
    }

    [Fact]
    public void Unknown_Phc_Activity_Is_Not_Silently_Treated_As_Active()
    {
        var record = Create(
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"unknown","estado":"Activo"}""");

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Equal("metadata_activity_unrecognized", result.DiagnosticCode);
        Assert.False(result.HasFingerprintProjection);
        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        Assert.Equal("unknown", metadata.RootElement.GetProperty("inactivoRaw").GetString());
    }

    [Fact]
    public void Javascript_Uses_Inativo_Source_Field_But_Exports_Generic_Raw_Name()
    {
        var record = Create(
            ObjectKind.PhcUserJavascript,
            "phc.customizations",
            """{"source":"jsu","stamp":"s2","ecran":"FT","inativo":"1","estado":"Inactivo"}""");

        var result = MetadataProjectionResolver.Resolve(record);

        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        using var fingerprint = JsonDocument.Parse(result.FingerprintProjection!.Value.GetRawText());
        Assert.Equal("1", metadata.RootElement.GetProperty("inactivoRaw").GetString());
        Assert.True(fingerprint.RootElement.GetProperty("inactivo").GetBoolean());
    }

    [Fact]
    public void User_Field_Excludes_ColumnId_And_Prefix_From_Fingerprint()
    {
        var record = Create(
            ObjectKind.PhcUserField,
            "phc.customizations",
            """{"source":"sys.columns","prefix":"u_","schema":"dbo","table":"fo","column":"u_codigo","type":"varchar","maxLength":20,"precision":0,"scale":0,"isNullable":false,"columnId":8}""");

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Null(result.DiagnosticCode);
        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        using var fingerprint = JsonDocument.Parse(result.FingerprintProjection!.Value.GetRawText());
        Assert.Equal("u_", metadata.RootElement.GetProperty("prefix").GetString());
        Assert.False(fingerprint.RootElement.TryGetProperty("prefix", out _));
        Assert.False(fingerprint.RootElement.TryGetProperty("columnId", out _));
        Assert.Equal("u_codigo", fingerprint.RootElement.GetProperty("column").GetString());
    }

    [Fact]
    public void Installed_SysObjects_Function_Does_Not_Project_Synthetic_Activity_Or_Duplicate_Name()
    {
        var record = Create(
            ObjectKind.PhcUserTsqlFunction,
            "phc.customizations",
            """{"source":"sys.objects","schema":"dbo","nome":"Fx_Test","inactivo":false,"estado":"Activo"}""");

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Null(result.DiagnosticCode);
        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        using var fingerprint = JsonDocument.Parse(result.FingerprintProjection!.Value.GetRawText());
        Assert.Equal("sys.objects", metadata.RootElement.GetProperty("sourceVariant").GetString());
        Assert.Equal("dbo", metadata.RootElement.GetProperty("schema").GetString());
        Assert.False(metadata.RootElement.TryGetProperty("nome", out _));
        Assert.False(metadata.RootElement.TryGetProperty("inactivoRaw", out _));
        Assert.Single(fingerprint.RootElement.EnumerateObject().ToArray());
        Assert.Equal("dbo", fingerprint.RootElement.GetProperty("schema").GetString());
    }

    [Fact]
    public void View_Without_Summary_Produces_Empty_Metadata_And_No_Fingerprint()
    {
        var result = MetadataProjectionResolver.Resolve(Create(ObjectKind.View, "sql.server.structure", null));

        Assert.Null(result.DiagnosticCode);
        Assert.False(result.HasFingerprintProjection);
        Assert.Equal(JsonValueKind.Object, result.Metadata.ValueKind);
        Assert.Empty(result.Metadata.EnumerateObject().ToArray());
    }

    [Fact]
    public void Missing_Semantic_Field_Preserves_Metadata_But_Omits_Fingerprint()
    {
        var record = Create(
            ObjectKind.Table,
            "sql.server.structure",
            """{"typeDesc":"USER_TABLE","classification":"custom"}""");

        var result = MetadataProjectionResolver.Resolve(record);

        Assert.Equal("metadata_fingerprint_incomplete", result.DiagnosticCode);
        Assert.False(result.HasFingerprintProjection);
        using var metadata = JsonDocument.Parse(result.Metadata.GetRawText());
        Assert.Equal("custom", metadata.RootElement.GetProperty("classification").GetString());
    }

    private static TechnicalObjectRecord Create(
        string kind,
        string collectorId,
        string? summaryJson) =>
        new(
            "test-object",
            kind,
            "Test object",
            null,
            null,
            collectorId,
            null,
            summaryJson,
            DateTimeOffset.UnixEpoch);
}
