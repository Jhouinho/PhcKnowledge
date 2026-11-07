using System.Text;
using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Export.ObjectIndex;

namespace PhcKnowledgeScanner.Tests;

public sealed class MetadataFingerprintCalculatorTests
{
    [Fact]
    public void Canonical_Json_Sorts_Object_Properties_And_Preserves_Array_Order()
    {
        using var document = JsonDocument.Parse(
            """{"z":1,"a":{"d":true,"b":"x"},"items":[3,2,1]}""");

        var canonical = Encoding.UTF8.GetString(CanonicalJsonSerializer.Serialize(document.RootElement));

        Assert.Equal("""{"a":{"b":"x","d":true},"items":[3,2,1],"z":1}""", canonical);
    }

    [Fact]
    public void Canonical_Json_Uses_Required_String_Escapes_And_Leaves_Unicode_Unescaped()
    {
        using var document = JsonDocument.Parse("""{"text":"quote:\" slash:\\ line:\n control:\u0001 café"}""");

        var canonical = Encoding.UTF8.GetString(CanonicalJsonSerializer.Serialize(document.RootElement));

        Assert.Equal("""{"text":"quote:\" slash:\\ line:\n control:\u0001 café"}""", canonical);
    }

    [Fact]
    public void Canonical_Json_Rejects_Non_Integer_Numbers_Outside_Current_Projection_Contract()
    {
        using var document = JsonDocument.Parse("""{"value":1.5}""");

        Assert.Throws<InvalidOperationException>(
            () => CanonicalJsonSerializer.Serialize(document.RootElement));
    }

    [Fact]
    public void Table_Fingerprint_Ignores_Internal_Ids_Dates_And_Derived_Classification()
    {
        var first = MetadataFingerprintCalculator.Calculate(MetadataProjectionResolver.Resolve(
            Create(
                ObjectKind.Table,
                "sql.server.structure",
                """{"objectId":10,"typeDesc":"USER_TABLE","isMsShipped":false,"classification":"custom","namePrefixU":true,"createDate":"2020-01-01T00:00:00Z","modifyDate":"2021-01-01T00:00:00Z"}""")));

        var second = MetadataFingerprintCalculator.Calculate(MetadataProjectionResolver.Resolve(
            Create(
                ObjectKind.Table,
                "sql.server.structure",
                """{"objectId":99,"typeDesc":"USER_TABLE","isMsShipped":false,"classification":"application","namePrefixU":false,"createDate":"2024-01-01T00:00:00Z","modifyDate":"2026-01-01T00:00:00Z"}""")));

        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    [Fact]
    public void Semantic_Table_Metadata_Change_Changes_Fingerprint()
    {
        var first = Fingerprint(Create(
            ObjectKind.Table,
            "sql.server.structure",
            """{"objectId":10,"typeDesc":"USER_TABLE","isMsShipped":false}"""));
        var second = Fingerprint(Create(
            ObjectKind.Table,
            "sql.server.structure",
            """{"objectId":10,"typeDesc":"VIEW","isMsShipped":false}"""));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Phc_Activity_Change_Changes_Fingerprint()
    {
        var active = Fingerprint(Create(
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"0"}"""));
        var inactive = Fingerprint(Create(
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"1"}"""));

        Assert.NotEqual(active, inactive);
    }

    [Fact]
    public void Primary_Key_Fingerprint_Ignores_Unique_Index_Id()
    {
        var first = Fingerprint(Create(
            ObjectKind.PrimaryKey,
            "sql.server.structure",
            """{"uniqueIndexId":1,"columns":[{"name":"fistamp","ordinal":1}]}"""));
        var second = Fingerprint(Create(
            ObjectKind.PrimaryKey,
            "sql.server.structure",
            """{"uniqueIndexId":42,"columns":[{"name":"fistamp","ordinal":1}]}"""));

        Assert.Equal(first, second);
    }

    [Fact]
    public void No_Fingerprint_Is_Produced_When_Projection_Is_Intentionally_Omitted()
    {
        var projection = MetadataProjectionResolver.Resolve(Create(
            ObjectKind.Database,
            "sql.server.structure",
            """{"name":"PHCDB","databaseId":7,"versionProperty":"SQL Server"}"""));

        var result = MetadataFingerprintCalculator.Calculate(projection);

        Assert.False(result.HasFingerprint);
        Assert.Null(result.DiagnosticCode);
    }

    [Fact]
    public void Unknown_Activity_Propagates_Diagnostic_And_No_Fingerprint()
    {
        var projection = MetadataProjectionResolver.Resolve(Create(
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"unknown"}"""));

        var result = MetadataFingerprintCalculator.Calculate(projection);

        Assert.False(result.HasFingerprint);
        Assert.Equal("metadata_activity_unrecognized", result.DiagnosticCode);
    }

    [Fact]
    public void Fingerprint_Is_Lowercase_Sha256_Hex()
    {
        var result = Fingerprint(Create(
            ObjectKind.Trigger,
            "sql.server.structure",
            """{"isDisabled":false}"""));

        Assert.Equal("sha256", result.Algorithm);
        Assert.Equal(64, result.Value.Length);
        Assert.Equal(result.Value.ToLowerInvariant(), result.Value);
    }

    private static MetadataFingerprintValue Fingerprint(TechnicalObjectRecord record)
    {
        var result = MetadataFingerprintCalculator.Calculate(MetadataProjectionResolver.Resolve(record));
        Assert.True(result.HasFingerprint);
        return result.Fingerprint!;
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
