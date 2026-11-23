using System.Security.Cryptography;
using System.Text;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Export.ObjectIndex;

namespace PhcKnowledgeScanner.Tests;

public sealed class DefinitionProjectionResolverTests
{
    [Fact]
    public void User_Field_Is_NotCollected_Even_When_Descriptive_Artifact_Exists()
    {
        var record = Create(
            "field-1",
            ObjectKind.PhcUserField,
            "phc.customizations",
            """{"source":"sys.columns","columnId":4}""",
            definitionArtifactId: "description-1");
        var artifact = Artifact("description-1", "generated column description");

        var result = Resolve([record], [artifact])[record.ObjectId];

        Assert.Equal(DefinitionStatus.NotCollected, result.Status);
        Assert.False(result.HasFingerprint);
        Assert.Empty(result.ArtifactIds);
        Assert.Equal("definition_descriptive_artifact_ignored", result.DiagnosticCode);
    }

    [Theory]
    [InlineData(ObjectKind.Database)]
    [InlineData(ObjectKind.Table)]
    [InlineData(ObjectKind.PrimaryKey)]
    [InlineData(ObjectKind.ForeignKey)]
    public void Structural_Objects_Are_NotCollected(string kind)
    {
        var record = Create("object-1", kind, "sql.server.structure", null);

        var result = Resolve([record], [])[record.ObjectId];

        Assert.Equal(DefinitionStatus.NotCollected, result.Status);
        Assert.False(result.HasFingerprint);
    }

    [Fact]
    public void Phc_Definition_Is_Available_Without_Validated_Semantic_Policy()
    {
        var content = Encoding.UTF8.GetBytes("FUNCTION BODY\r\n");
        var record = Create(
            "rule-1",
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"0"}""",
            definitionArtifactId: "artifact-1");

        var result = Resolve([record], [Artifact("artifact-1", content)])[record.ObjectId];

        Assert.Equal(DefinitionStatus.Available, result.Status);
        Assert.Equal(new[] { "artifact-1" }, result.ArtifactIds);
        Assert.False(result.HasFingerprint);
        Assert.Equal("NoValidatedPolicy", result.FingerprintUnavailableReasonCode);
    }

    [Fact]
    public void Sql_View_With_Definition_Artifact_Is_Available()
    {
        var content = Encoding.UTF8.GetBytes("CREATE VIEW dbo.vw_test AS SELECT 1;");
        var record = Create(
            "view:dbo.vw_test",
            ObjectKind.View,
            "sql.server.structure",
            null,
            definitionArtifactId: "view-artifact",
            name: "vw_test",
            schemaName: "dbo");

        var result = Resolve([record], [Artifact("view-artifact", content)])[record.ObjectId];

        Assert.Equal(DefinitionStatus.Available, result.Status);
        Assert.False(result.HasFingerprint);
        Assert.Equal("NoValidatedPolicy", result.FingerprintUnavailableReasonCode);
    }

    [Fact]
    public void Sql_View_Matches_Only_Issue_For_Same_Operation_And_Object_Name()
    {
        var record = Create(
            "view:dbo.vw_test",
            ObjectKind.View,
            "sql.server.structure",
            null,
            name: "vw_test",
            schemaName: "dbo");
        var issue = new CollectionIssue(
            CollectionIssueSeverity.Warning,
            "sql.view.definition_missing",
            "Definition unavailable",
            "dbo.vw_test",
            OperationId: KnownOperations.SqlViews);

        var result = Resolve([record], [], [issue])[record.ObjectId];

        Assert.Equal(DefinitionStatus.Unavailable, result.Status);
        Assert.Equal("definition_unavailable_issue", result.DiagnosticCode);
    }

    [Fact]
    public void Generic_Operation_Issue_Does_Not_Become_Object_Specific_Evidence()
    {
        var record = Create(
            "view:dbo.vw_test",
            ObjectKind.View,
            "sql.server.structure",
            null,
            name: "vw_test",
            schemaName: "dbo");
        var issue = new CollectionIssue(
            CollectionIssueSeverity.Error,
            "sql.views",
            "Whole operation failed",
            OperationId: KnownOperations.SqlViews);

        var result = Resolve([record], [], [issue])[record.ObjectId];

        Assert.Equal(DefinitionStatus.Unavailable, result.Status);
        Assert.Equal("definition_artifact_missing", result.DiagnosticCode);
    }

    [Fact]
    public void Trigger_Issue_Name_Uses_Parent_Table_And_Trigger_Name()
    {
        var record = Create(
            "trigger:dbo.ft.tr_test",
            ObjectKind.Trigger,
            "sql.server.structure",
            null,
            parentObjectId: "table:dbo.ft",
            name: "tr_test",
            schemaName: "dbo");
        var issue = new CollectionIssue(
            CollectionIssueSeverity.Warning,
            "sql.trigger.definition_missing",
            "Definition unavailable",
            "dbo.ft.tr_test",
            OperationId: KnownOperations.SqlTriggers);

        var result = Resolve([record], [], [issue])[record.ObjectId];

        Assert.Equal("definition_unavailable_issue", result.DiagnosticCode);
    }

    [Fact]
    public void Procedure_Issue_Requires_Exact_Object_Name()
    {
        var record = Create(
            "procedure:dbo.usp_test",
            ObjectKind.StoredProcedure,
            "sql.server.structure",
            null,
            name: "usp_test",
            schemaName: "dbo");
        var issue = new CollectionIssue(
            CollectionIssueSeverity.Warning,
            "sql.procedure.definition_missing",
            "Definition unavailable",
            "dbo.usp_other",
            OperationId: KnownOperations.SqlProcedures);

        var result = Resolve([record], [], [issue])[record.ObjectId];

        Assert.Equal("definition_artifact_missing", result.DiagnosticCode);
    }

    [Fact]
    public void Hash_Mismatch_Makes_Definition_Unavailable()
    {
        var record = Create(
            "rule-1",
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"0"}""",
            definitionArtifactId: "artifact-1");
        var content = Encoding.UTF8.GetBytes("new body");
        var artifact = new DefinitionArtifactSnapshot("artifact-1", content, Hash(Encoding.UTF8.GetBytes("old body")), content.Length);

        var result = Resolve([record], [artifact])[record.ObjectId];

        Assert.Equal(DefinitionStatus.Unavailable, result.Status);
        Assert.Equal("definition_artifact_hash_mismatch", result.DiagnosticCode);
    }

    [Fact]
    public void Length_Mismatch_Makes_Definition_Unavailable()
    {
        var record = Create(
            "rule-1",
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"0"}""",
            definitionArtifactId: "artifact-1");
        var content = Encoding.UTF8.GetBytes("body");
        var artifact = new DefinitionArtifactSnapshot("artifact-1", content, Hash(content), content.Length + 1);

        var result = Resolve([record], [artifact])[record.ObjectId];

        Assert.Equal("definition_artifact_length_mismatch", result.DiagnosticCode);
    }

    [Fact]
    public void Missing_Artifact_Bytes_Make_Definition_Unavailable()
    {
        var record = Create(
            "rule-1",
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"0"}""",
            definitionArtifactId: "artifact-1");
        var artifact = new DefinitionArtifactSnapshot("artifact-1", null, Hash(Encoding.UTF8.GetBytes("body")), 4);

        var result = Resolve([record], [artifact])[record.ObjectId];

        Assert.Equal("definition_artifact_bytes_missing", result.DiagnosticCode);
    }

    [Fact]
    public void Empty_Definition_Is_Distinct_From_Unavailable_And_Has_No_Fingerprint()
    {
        var record = Create(
            "trigger:empty",
            ObjectKind.Trigger,
            "sql.server.structure",
            """{"isDisabled":false}""",
            definitionArtifactId: "empty-artifact",
            parentObjectId: "table:dbo.FT",
            name: "tr_empty",
            schemaName: "dbo");
        var emptyBytes = Array.Empty<byte>();

        var result = Resolve([record], [Artifact("empty-artifact", emptyBytes)])[record.ObjectId];

        Assert.Equal(DefinitionStatus.Empty, result.Status);
        Assert.Equal(new[] { "empty-artifact" }, result.ArtifactIds);
        Assert.False(result.HasFingerprint);
    }

    [Fact]
    public void Missing_Artifact_Reference_Is_Not_Confused_With_Empty_Definition()
    {
        var record = Create(
            "rule-1",
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"0"}""",
            definitionArtifactId: "missing");

        var result = Resolve([record], [])[record.ObjectId];

        Assert.Equal(DefinitionStatus.Unavailable, result.Status);
        Assert.Equal("definition_artifact_reference_missing", result.DiagnosticCode);
        Assert.NotEqual(DefinitionStatus.Empty, result.Status);
    }

    [Fact]
    public void Definition_Issue_And_Artifact_Reference_Conflict_Is_Unavailable()
    {
        var record = Create(
            "view:dbo.vw_test",
            ObjectKind.View,
            "sql.server.structure",
            null,
            definitionArtifactId: "artifact-1",
            name: "vw_test",
            schemaName: "dbo");
        var issue = new CollectionIssue(
            CollectionIssueSeverity.Warning,
            "sql.view.definition_missing",
            "Definition unavailable",
            "dbo.vw_test",
            OperationId: KnownOperations.SqlViews);
        var artifact = Artifact("artifact-1", "some content");

        var result = Resolve([record], [artifact], [issue])[record.ObjectId];

        Assert.Equal("definition_artifact_issue_conflict", result.DiagnosticCode);
    }

    [Fact]
    public void Duplicate_Artifact_Ids_Are_Not_Resolved_Arbitrarily()
    {
        var record = Create(
            "rule-1",
            ObjectKind.PhcUserRule,
            "phc.customizations",
            """{"source":"brule","stamp":"s1","tabela":"FT","inactivo":"0"}""",
            definitionArtifactId: "artifact-1");
        var artifacts = new[]
        {
            Artifact("artifact-1", "one"),
            Artifact("artifact-1", "two")
        };

        var result = Resolve([record], artifacts)[record.ObjectId];

        Assert.Equal("definition_artifact_reference_ambiguous", result.DiagnosticCode);
    }

    private static IReadOnlyDictionary<string, DefinitionProjectionResolution> Resolve(
        IEnumerable<TechnicalObjectRecord> objects,
        IEnumerable<DefinitionArtifactSnapshot> artifacts,
        IEnumerable<CollectionIssue>? issues = null) =>
        DefinitionProjectionResolver.ResolveAll(objects, artifacts, issues ?? Array.Empty<CollectionIssue>());

    private static DefinitionArtifactSnapshot Artifact(string id, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return Artifact(id, bytes);
    }

    private static DefinitionArtifactSnapshot Artifact(string id, byte[] content) =>
        new(id, content, Hash(content), content.LongLength);

    private static string Hash(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static TechnicalObjectRecord Create(
        string objectId,
        string kind,
        string collectorId,
        string? summaryJson,
        string? definitionArtifactId = null,
        string? parentObjectId = null,
        string name = "Test object",
        string? schemaName = null) =>
        new(
            objectId,
            kind,
            name,
            schemaName,
            parentObjectId,
            collectorId,
            definitionArtifactId,
            summaryJson,
            DateTimeOffset.UnixEpoch);
}
