using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Export.ObjectIndex;

namespace PhcKnowledgeScanner.Tests;

public sealed class ObjectOperationResolverTests
{
    [Theory]
    [InlineData(ObjectKind.Database, KnownOperations.SqlDatabase)]
    [InlineData(ObjectKind.Table, KnownOperations.SqlTablesInventory)]
    [InlineData(ObjectKind.PrimaryKey, KnownOperations.SqlPrimaryKeys)]
    [InlineData(ObjectKind.ForeignKey, KnownOperations.SqlForeignKeys)]
    [InlineData(ObjectKind.View, KnownOperations.SqlViews)]
    [InlineData(ObjectKind.StoredProcedure, KnownOperations.SqlProcedures)]
    [InlineData(ObjectKind.Trigger, KnownOperations.SqlTriggers)]
    public void Resolves_Sql_Object_Kinds(string kind, string expectedOperation)
    {
        var result = ObjectOperationResolver.Resolve(Create(kind, "sql.server.structure"));

        Assert.True(result.IsResolved);
        Assert.Equal(expectedOperation, result.OperationId);
        Assert.Null(result.DiagnosticCode);
    }

    [Theory]
    [InlineData(ObjectKind.PhcUserRule, "brule", KnownOperations.PhcBrule)]
    [InlineData(ObjectKind.PhcWebUserRule, "ebrule", KnownOperations.PhcEbrule)]
    [InlineData(ObjectKind.PhcHotkey, "ti", KnownOperations.PhcTeclas)]
    [InlineData(ObjectKind.PhcDefaultValue, "udefs", KnownOperations.PhcUdefs)]
    [InlineData(ObjectKind.PhcWebDefaultValue, "eudefs", KnownOperations.PhcEudefs)]
    [InlineData(ObjectKind.PhcEvent, "eventos", KnownOperations.PhcEventos)]
    [InlineData(ObjectKind.PhcWebEvent, "eeventos", KnownOperations.PhcEeventos)]
    [InlineData(ObjectKind.PhcUserField, "sys.columns", KnownOperations.PhcUserFields)]
    [InlineData(ObjectKind.PhcWebScript, "escr", KnownOperations.PhcEscr)]
    [InlineData(ObjectKind.PhcUserJavascript, "jsu", KnownOperations.PhcJsu)]
    [InlineData(ObjectKind.PhcWebCssJs, "cssjs", KnownOperations.PhcCssjs)]
    [InlineData(ObjectKind.PhcHiddenGrid, "formc", KnownOperations.PhcFormc)]
    [InlineData(ObjectKind.PhcCustomScreen, "formb", KnownOperations.PhcFormb)]
    [InlineData(ObjectKind.PhcUserTsqlFunction, "ftu", KnownOperations.PhcFtu)]
    [InlineData(ObjectKind.PhcUserTsqlFunction, "sys.objects", KnownOperations.PhcFtu)]
    public void Resolves_Phc_Object_Kind_And_Source_Variant(
        string kind,
        string source,
        string expectedOperation)
    {
        var result = ObjectOperationResolver.Resolve(Create(kind, "phc.customizations", $$"""{"source":"{{source}}"}"""));

        Assert.True(result.IsResolved);
        Assert.Equal(expectedOperation, result.OperationId);
        Assert.Null(result.DiagnosticCode);
    }

    [Fact]
    public void Rejects_Unknown_Object_Kind()
    {
        var result = ObjectOperationResolver.Resolve(Create("phc.campo_definition", "phc.customizations", """{"source":"campos"}"""));

        Assert.False(result.IsResolved);
        Assert.Equal("object_kind_unsupported", result.DiagnosticCode);
    }

    [Fact]
    public void Rejects_Collector_Kind_Mismatch()
    {
        var result = ObjectOperationResolver.Resolve(Create(ObjectKind.Table, "phc.customizations"));

        Assert.False(result.IsResolved);
        Assert.Equal("object_collector_mismatch", result.DiagnosticCode);
    }

    [Fact]
    public void Rejects_Missing_Phc_Summary()
    {
        var result = ObjectOperationResolver.Resolve(Create(ObjectKind.PhcUserRule, "phc.customizations"));

        Assert.False(result.IsResolved);
        Assert.Equal("object_summary_missing", result.DiagnosticCode);
    }

    [Fact]
    public void Rejects_Invalid_Phc_Summary()
    {
        var result = ObjectOperationResolver.Resolve(Create(ObjectKind.PhcUserRule, "phc.customizations", "{"));

        Assert.False(result.IsResolved);
        Assert.Equal("object_summary_invalid", result.DiagnosticCode);
    }

    [Fact]
    public void Rejects_Phc_Summary_Without_Source()
    {
        var result = ObjectOperationResolver.Resolve(Create(ObjectKind.PhcUserRule, "phc.customizations", """{"stamp":"abc"}"""));

        Assert.False(result.IsResolved);
        Assert.Equal("object_source_missing", result.DiagnosticCode);
    }

    [Fact]
    public void Rejects_Source_Variant_That_Does_Not_Belong_To_Kind()
    {
        var result = ObjectOperationResolver.Resolve(Create(ObjectKind.PhcUserRule, "phc.customizations", """{"source":"sys.objects"}"""));

        Assert.False(result.IsResolved);
        Assert.Equal("object_source_variant_unsupported", result.DiagnosticCode);
    }

    [Fact]
    public void Does_Not_Resolve_Phc_Function_Without_Explicit_Variant()
    {
        var result = ObjectOperationResolver.Resolve(Create(ObjectKind.PhcUserTsqlFunction, "phc.customizations", """{"stamp":"abc"}"""));

        Assert.False(result.IsResolved);
        Assert.Equal("object_source_missing", result.DiagnosticCode);
    }

    private static TechnicalObjectRecord Create(
        string kind,
        string collectorId,
        string? summaryJson = null) =>
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
