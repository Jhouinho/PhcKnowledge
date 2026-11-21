using System.Text.Json;
using Microsoft.Data.SqlClient;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Collectors.Sql;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public sealed class SqlServerTriggerIntegrationFactAttribute : FactAttribute
{
    public SqlServerTriggerIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING"))
            || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_SQL_TEST_RESTRICTED_CONNECTION_STRING")))
        {
            Skip = "Define PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING e PHC_KNOWLEDGE_SQL_TEST_RESTRICTED_CONNECTION_STRING para executar os testes SQL Server.";
        }
    }
}

/// <summary>
/// Integração real com SQL Server. Usa uma base de dados de testes dedicada:
/// a ligação principal cria/apaga objetos; a ligação restrita permite testar
/// visibilidade por SELECT e DENY de VIEW DEFINITION ao nível de um objeto.
/// </summary>
public sealed class SqlServerTriggerIntegrationTests
{
    [SqlServerTriggerIntegrationFact]
    public async Task Collects_Table_Triggers_And_Reports_Restricted_Metadata_Visibility()
    {
        var adminConnectionString = Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING")!;
        var restrictedConnectionString = Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_SQL_TEST_RESTRICTED_CONNECTION_STRING")!;
        AssertSameDatabase(adminConnectionString, restrictedConnectionString);

        var suffix = Guid.NewGuid().ToString("N")[..12];
        var standardTable = $"it_{suffix}";
        var customTable = $"u_it_{suffix}";
        var standardTrigger = $"tr_{suffix}_standard";
        var disabledTrigger = $"tr_{suffix}_disabled";
        var insteadOfTrigger = $"tr_{suffix}_instead";
        var customTrigger = $"tr_{suffix}_custom";

        try
        {
            await CreateFixtureAsync(
                adminConnectionString,
                standardTable,
                customTable,
                standardTrigger,
                disabledTrigger,
                insteadOfTrigger,
                customTrigger);

            var full = await RunCollectorAsync(adminConnectionString, $"integration/full/{suffix}");
            var fullCoverage = Assert.Single(full.Coverages, x => x.OperationId == KnownOperations.SqlTriggers);

            Assert.Equal(OperationExecutionResult.Succeeded, fullCoverage.Result);
            Assert.Equal("verified", fullCoverage.Scope!["completeness"]);
            Assert.Equal("database_view_definition", fullCoverage.Scope["metadataVisibility"]);

            var standard = Assert.Single(full.Triggers, x => x.Name == standardTrigger);
            var standardSummary = ParseSummary(standard);
            Assert.Equal(standardTable, standardSummary.RootElement.GetProperty("parentName").GetString());
            Assert.False(standardSummary.RootElement.GetProperty("isDisabled").GetBoolean());
            Assert.Equal(
                new[] { "DELETE", "INSERT", "UPDATE" },
                standardSummary.RootElement.GetProperty("events").EnumerateArray()
                    .Select(x => x.GetString()).OrderBy(x => x, StringComparer.Ordinal).ToArray());
            Assert.NotNull(standard.DefinitionArtifactId);
            Assert.Contains("CREATE TRIGGER", full.ArtifactTexts[standard.DefinitionArtifactId!], StringComparison.OrdinalIgnoreCase);

            var disabled = Assert.Single(full.Triggers, x => x.Name == disabledTrigger);
            Assert.True(ParseSummary(disabled).RootElement.GetProperty("isDisabled").GetBoolean());

            var insteadOf = Assert.Single(full.Triggers, x => x.Name == insteadOfTrigger);
            Assert.True(ParseSummary(insteadOf).RootElement.GetProperty("isInsteadOf").GetBoolean());

            var custom = Assert.Single(full.Triggers, x => x.Name == customTrigger);
            Assert.Equal(customTable, ParseSummary(custom).RootElement.GetProperty("parentName").GetString());

            var restricted = await RunCollectorAsync(restrictedConnectionString, $"integration/restricted/{suffix}");
            var restrictedCoverage = Assert.Single(restricted.Coverages, x => x.OperationId == KnownOperations.SqlTriggers);

            Assert.Equal(OperationExecutionResult.Partial, restrictedCoverage.Result);
            Assert.Equal(ContentAvailability.Partial, restrictedCoverage.ContentAvailability);
            Assert.Equal("unknown", restrictedCoverage.Scope!["completeness"]);
            Assert.Equal("not_verified", restrictedCoverage.Scope["metadataVisibility"]);
            Assert.Contains(restricted.Issues, x => x.Code == "sql.trigger.metadata_completeness_unverified");

            var restrictedStandard = Assert.Single(restricted.Triggers, x => x.Name == standardTrigger);
            Assert.Null(restrictedStandard.DefinitionArtifactId);
            Assert.Equal("Unavailable", ParseSummary(restrictedStandard).RootElement.GetProperty("definitionStatus").GetString());
            Assert.Contains(restricted.Issues, x =>
                x.Code == "sql.trigger.definition_missing"
                && x.ObjectName != null
                && x.ObjectName.Contains(standardTrigger, StringComparison.OrdinalIgnoreCase));

            // A database-level VIEW DEFINITION grant does not prove complete visibility:
            // an object-level DENY can still hide a table and its DML triggers.
            await ConfigureObjectLevelViewDefinitionDenyAsync(
                adminConnectionString,
                restrictedConnectionString,
                customTable);

            var objectDenied = await RunCollectorAsync(
                restrictedConnectionString,
                $"integration/object-denied/{suffix}");
            var objectDeniedCoverage = Assert.Single(
                objectDenied.Coverages,
                x => x.OperationId == KnownOperations.SqlTriggers);

            Assert.Equal(OperationExecutionResult.Partial, objectDeniedCoverage.Result);
            Assert.Equal(ContentAvailability.Partial, objectDeniedCoverage.ContentAvailability);
            Assert.Equal("unknown", objectDeniedCoverage.Scope!["completeness"]);
            Assert.Equal("database_view_definition", objectDeniedCoverage.Scope["metadataVisibility"]);
            Assert.Contains(objectDenied.Issues, x =>
                x.Code == "sql.trigger.metadata_completeness_unverified");
            Assert.Contains(objectDenied.Triggers, x => x.Name == standardTrigger);
            Assert.DoesNotContain(objectDenied.Triggers, x => x.Name == customTrigger);
        }
        finally
        {
            await DropFixtureAsync(
                adminConnectionString,
                standardTable,
                customTable,
                standardTrigger,
                disabledTrigger,
                insteadOfTrigger,
                customTrigger);
        }
    }

    private static async Task<CollectorSnapshot> RunCollectorAsync(string connectionString, string scanId)
    {
        var root = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner-sql-integration", Guid.NewGuid().ToString("N"));
        await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
        await workspace.InitializeAsync(scanId, "sql-server-integration", CancellationToken.None);

        var context = new ScanContext
        {
            InstallationId = "sql-server-integration",
            OutputRoot = root,
            Mode = ScanMode.Full,
            SqlConnectionString = connectionString,
            Workspace = workspace
        };

        var result = await new SqlServerStructureCollector().CollectAsync(context, CancellationToken.None);
        var triggers = await workspace.GetObjectsAsync(kind: ObjectKind.Trigger);
        var artifactTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var trigger in triggers.Where(x => x.DefinitionArtifactId is not null))
        {
            artifactTexts[trigger.DefinitionArtifactId!] =
                await workspace.ReadArtifactTextAsync(trigger.DefinitionArtifactId!)
                ?? throw new InvalidOperationException("O artefacto da trigger não foi encontrado no workspace.");
        }

        return new CollectorSnapshot(triggers, result.Coverages, result.Issues, artifactTexts);
    }

    private static async Task ConfigureObjectLevelViewDefinitionDenyAsync(
        string adminConnectionString,
        string restrictedConnectionString,
        string deniedTable)
    {
        string databaseUser;
        await using (var restricted = new SqlConnection(restrictedConnectionString))
        {
            await restricted.OpenAsync();
            await using var userCommand = restricted.CreateCommand();
            userCommand.CommandText = "SELECT USER_NAME();";
            databaseUser = Convert.ToString(await userCommand.ExecuteScalarAsync())
                ?? throw new InvalidOperationException("Não foi possível identificar o utilizador SQL de teste.");
        }

        var safeUser = databaseUser.Replace("]", "]]", StringComparison.Ordinal);
        await using var admin = new SqlConnection(adminConnectionString);
        await admin.OpenAsync();
        await ExecuteAsync(admin, $"REVOKE SELECT ON SCHEMA::dbo FROM [{safeUser}];");
        await ExecuteAsync(admin, $"GRANT VIEW DEFINITION ON DATABASE::[{admin.Database}] TO [{safeUser}];");
        await ExecuteAsync(admin, $"DENY VIEW DEFINITION ON OBJECT::dbo.[{deniedTable}] TO [{safeUser}];");
    }

    private static async Task CreateFixtureAsync(
        string connectionString,
        string standardTable,
        string customTable,
        string standardTrigger,
        string disabledTrigger,
        string insteadOfTrigger,
        string customTrigger)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await ExecuteAsync(connection, $"CREATE TABLE dbo.[{standardTable}] (id int NOT NULL PRIMARY KEY);");
        await ExecuteAsync(connection, $"CREATE TABLE dbo.[{customTable}] (id int NOT NULL PRIMARY KEY);");

        await ExecuteAsync(connection, $"""
            CREATE TRIGGER dbo.[{standardTrigger}]
            ON dbo.[{standardTable}]
            AFTER INSERT, UPDATE, DELETE
            AS
            BEGIN
                SET NOCOUNT ON;
            END;
            """);

        await ExecuteAsync(connection, $"""
            CREATE TRIGGER dbo.[{disabledTrigger}]
            ON dbo.[{standardTable}]
            AFTER UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
            END;
            """);
        await ExecuteAsync(connection, $"DISABLE TRIGGER dbo.[{disabledTrigger}] ON dbo.[{standardTable}];");

        await ExecuteAsync(connection, $"""
            CREATE TRIGGER dbo.[{insteadOfTrigger}]
            ON dbo.[{standardTable}]
            INSTEAD OF INSERT
            AS
            BEGIN
                SET NOCOUNT ON;
            END;
            """);

        await ExecuteAsync(connection, $"""
            CREATE TRIGGER dbo.[{customTrigger}]
            ON dbo.[{customTable}]
            AFTER INSERT
            AS
            BEGIN
                SET NOCOUNT ON;
            END;
            """);
    }

    private static async Task DropFixtureAsync(
        string connectionString,
        string standardTable,
        string customTable,
        string standardTrigger,
        string disabledTrigger,
        string insteadOfTrigger,
        string customTrigger)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var trigger in new[] { standardTrigger, disabledTrigger, insteadOfTrigger })
        {
            await ExecuteAsync(connection, $"IF OBJECT_ID(N'dbo.[{trigger}]', N'TR') IS NOT NULL DROP TRIGGER dbo.[{trigger}];");
        }

        await ExecuteAsync(connection, $"IF OBJECT_ID(N'dbo.[{customTrigger}]', N'TR') IS NOT NULL DROP TRIGGER dbo.[{customTrigger}];");
        await ExecuteAsync(connection, $"IF OBJECT_ID(N'dbo.[{standardTable}]', N'U') IS NOT NULL DROP TABLE dbo.[{standardTable}];");
        await ExecuteAsync(connection, $"IF OBJECT_ID(N'dbo.[{customTable}]', N'U') IS NOT NULL DROP TABLE dbo.[{customTable}];");
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static JsonDocument ParseSummary(TechnicalObjectRecord record) =>
        JsonDocument.Parse(record.SummaryJson ?? throw new InvalidOperationException("A trigger não tem SummaryJson."));

    private static void AssertSameDatabase(string adminConnectionString, string restrictedConnectionString)
    {
        var admin = new SqlConnectionStringBuilder(adminConnectionString);
        var restricted = new SqlConnectionStringBuilder(restrictedConnectionString);
        Assert.Equal(admin.DataSource, restricted.DataSource, ignoreCase: true);
        Assert.Equal(admin.InitialCatalog, restricted.InitialCatalog, ignoreCase: true);
        Assert.False(string.IsNullOrWhiteSpace(admin.InitialCatalog), "A ligação de testes tem de indicar uma base de dados dedicada.");
    }

    private sealed record CollectorSnapshot(
        IReadOnlyList<TechnicalObjectRecord> Triggers,
        IReadOnlyList<OperationCoverage> Coverages,
        IReadOnlyList<CollectionIssue> Issues,
        IReadOnlyDictionary<string, string> ArtifactTexts);
}
