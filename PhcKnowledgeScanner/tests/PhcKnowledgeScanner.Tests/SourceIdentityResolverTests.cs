using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Export.ObjectIndex;

namespace PhcKnowledgeScanner.Tests;

public sealed class SourceIdentityResolverTests
{
    [Fact]
    public void Projects_Phc_Stamp_With_Source_Table_Scope()
    {
        var record = Create(
            "phc.brule:stamp-1",
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"stamp-1","tabela":"FT"}""");

        var result = SourceIdentityResolver.ResolveAll([record])[record.ObjectId];

        Assert.True(result.HasIdentity);
        Assert.Equal(new SourceIdentityValue("phc.stamp", "stamp-1", "phc.table:dbo.brule"), result.Identity);
        Assert.Null(result.DiagnosticCode);
    }

    [Fact]
    public void Hotkey_Uses_Ti_Table_Scope()
    {
        var record = Create(
            "phc.tecla:stamp-2",
            ObjectKind.PhcHotkey,
            "phc.customizations",
            """{"source":"ti","stamp":"stamp-2","ecran":"FT"}""");

        var result = SourceIdentityResolver.ResolveAll([record])[record.ObjectId];

        Assert.Equal("phc.table:dbo.ti", result.Identity?.Scope);
    }

    [Fact]
    public void Ftu_Function_Uses_Phc_Stamp_But_SysObjects_Function_Has_No_Native_Identity()
    {
        var ftu = Create(
            "phc.user_tsql_function:ftu-1",
            ObjectKind.PhcUserTsqlFunction,
            "phc.customizations",
            """{"source":"ftu","stamp":"ftu-1","classif":"U"}""");
        var installed = Create(
            "phc.user_tsql_function:installed:dbo.Fx_Test",
            ObjectKind.PhcUserTsqlFunction,
            "phc.customizations",
            """{"source":"sys.objects","schema":"dbo","nome":"Fx_Test","inactivo":false,"estado":"Activo"}""");

        var results = SourceIdentityResolver.ResolveAll([ftu, installed]);

        Assert.Equal("phc.stamp", results[ftu.ObjectId].Identity?.Scheme);
        Assert.Equal("ftu-1", results[ftu.ObjectId].Identity?.Value);
        Assert.False(results[installed.ObjectId].HasIdentity);
        Assert.Null(results[installed.ObjectId].DiagnosticCode);
    }

    [Fact]
    public void Sql_Table_Identity_Uses_Database_Name_As_Scope()
    {
        var database = Create("db:PHCDB", ObjectKind.Database, "sql.server.structure", null, name: "PHCDB");
        var table = Create(
            "table:dbo.ft",
            ObjectKind.Table,
            "sql.server.structure",
            """{"objectId":123,"typeDesc":"USER_TABLE"}""");

        var results = SourceIdentityResolver.ResolveAll([database, table]);
        var identity = results[table.ObjectId].Identity;

        Assert.Equal(new SourceIdentityValue("sqlserver.object_id", "123", "database:PHCDB"), identity);
    }

    [Fact]
    public void User_Field_Identity_Uses_Resolved_Parent_Table_Object_Id()
    {
        var database = Create("db:PHCDB", ObjectKind.Database, "sql.server.structure", null, name: "PHCDB");
        var table = Create(
            "table:dbo.fo",
            ObjectKind.Table,
            "sql.server.structure",
            """{"objectId":42,"typeDesc":"USER_TABLE"}""");
        var field = Create(
            "phc.user_field:dbo.fo.u_codigo",
            ObjectKind.PhcUserField,
            "phc.customizations",
            """{"source":"sys.columns","columnId":7,"schema":"dbo","table":"fo","column":"u_codigo"}""",
            parentObjectId: table.ObjectId);

        var results = SourceIdentityResolver.ResolveAll([database, table, field]);
        var identity = results[field.ObjectId].Identity;

        Assert.Equal(
            new SourceIdentityValue("sqlserver.column_id", "7", "database:PHCDB/object:42"),
            identity);
    }

    [Fact]
    public void User_Field_Without_Resolved_Parent_Does_Not_Get_Identity()
    {
        var database = Create("db:PHCDB", ObjectKind.Database, "sql.server.structure", null, name: "PHCDB");
        var field = Create(
            "phc.user_field:dbo.missing.u_codigo",
            ObjectKind.PhcUserField,
            "phc.customizations",
            """{"source":"sys.columns","columnId":7}""",
            parentObjectId: "table:dbo.missing");

        var result = SourceIdentityResolver.ResolveAll([database, field])[field.ObjectId];

        Assert.False(result.HasIdentity);
        Assert.Equal("source_identity_parent_table_unresolved", result.DiagnosticCode);
    }

    [Fact]
    public void Sql_Table_Without_Unique_Database_Scope_Does_Not_Get_Identity()
    {
        var table = Create(
            "table:dbo.ft",
            ObjectKind.Table,
            "sql.server.structure",
            """{"objectId":123}""");

        var result = SourceIdentityResolver.ResolveAll([table])[table.ObjectId];

        Assert.False(result.HasIdentity);
        Assert.Equal("source_identity_database_missing", result.DiagnosticCode);
    }

    [Fact]
    public void Multiple_Database_Objects_Make_Sql_Scope_Ambiguous()
    {
        var database1 = Create("db:PHC1", ObjectKind.Database, "sql.server.structure", null, name: "PHC1");
        var database2 = Create("db:PHC2", ObjectKind.Database, "sql.server.structure", null, name: "PHC2");
        var table = Create(
            "table:dbo.ft",
            ObjectKind.Table,
            "sql.server.structure",
            """{"objectId":123}""");

        var result = SourceIdentityResolver.ResolveAll([database1, database2, table])[table.ObjectId];

        Assert.False(result.HasIdentity);
        Assert.Equal("source_identity_database_ambiguous", result.DiagnosticCode);
    }

    [Fact]
    public void Primary_Key_Does_Not_Receive_Invented_Native_Identity()
    {
        var record = Create(
            "pk:dbo.ft:pk_ft",
            ObjectKind.PrimaryKey,
            "sql.server.structure",
            """{"uniqueIndexId":1,"columns":[{"name":"fistamp","ordinal":1}]}""");

        var result = SourceIdentityResolver.ResolveAll([record])[record.ObjectId];

        Assert.False(result.HasIdentity);
        Assert.Null(result.DiagnosticCode);
    }

    [Fact]
    public void Missing_Phc_Stamp_Is_Reported_Not_Replaced_With_Object_Name()
    {
        var record = Create(
            "phc.brule:synthetic-id",
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","tabela":"FT"}""");

        var result = SourceIdentityResolver.ResolveAll([record])[record.ObjectId];

        Assert.False(result.HasIdentity);
        Assert.Equal("source_identity_stamp_missing", result.DiagnosticCode);
    }

    private static TechnicalObjectRecord Create(
        string objectId,
        string kind,
        string collectorId,
        string? summaryJson,
        string? parentObjectId = null,
        string name = "Test object") =>
        new(
            objectId,
            kind,
            name,
            null,
            parentObjectId,
            collectorId,
            null,
            summaryJson,
            DateTimeOffset.UnixEpoch);
}
