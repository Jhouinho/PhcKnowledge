using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Collectors.Coverage;

namespace PhcKnowledgeScanner.Collectors.Sql;

/// <summary>
/// Collector de estrutura SQL Server (apenas leitura). Não interpreta significado PHC.
/// </summary>
public sealed class SqlServerStructureCollector : ICollector
{
    public const string CollectorId = "sql.server.structure";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public string Id => CollectorId;
    public string DisplayName => "Estrutura SQL Server";
    public IReadOnlyList<DeclaredCapability> DeclaredCapabilities => ScannerCapabilityCatalog.ForSqlServerStructure;

    public async Task<CollectorCapability> ProbeAsync(ScanContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.SqlConnectionString))
        {
            return new CollectorCapability(false, "Connection string SQL em falta no contexto.");
        }

        try
        {
            await using var connection = new SqlConnection(context.SqlConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1;";
            _ = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return new CollectorCapability(true);
        }
        catch (Exception ex)
        {
            return new CollectorCapability(false, "Não foi possível abrir ligação SQL: " + ex.Message);
        }
    }

    public async Task<CollectorResult> CollectAsync(ScanContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context.Workspace);

        var sw = Stopwatch.StartNew();
        var issues = new List<CollectionIssue>();
        var collectedAt = DateTimeOffset.UtcNow;
        var objectCount = 0;
        // Artefactos ficam no workspace em disco; não acumular bytes em memória (BD PHC grandes).

        await using var connection = new SqlConnection(context.SqlConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var tracker = new OperationCoverageTracker(issues, context.ExecutionProgress);
        var caps = DeclaredCapabilities.ToDictionary(c => c.OperationId, StringComparer.OrdinalIgnoreCase);

        async Task Report(string message)
        {
            context.Progress?.Report(new ScanProgressEvent(
                "collector.running",
                Id,
                DisplayName,
                message,
                objectCount,
                objectCount,
                issues.Count(i => i.Severity == CollectionIssueSeverity.Warning),
                issues.Count(i => i.Severity == CollectionIssueSeverity.Error),
                null,
                sw.Elapsed));
            await Task.CompletedTask;
        }

        await tracker.RunAsync(
                caps[KnownOperations.SqlDatabase],
                () => CollectDatabaseAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        await tracker.RunAsync(
                caps[KnownOperations.SqlTablesInventory],
                () => CollectAllTablesAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        await tracker.RunAsync(
                caps[KnownOperations.SqlPrimaryKeys],
                () => CollectPrimaryKeysAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        await tracker.RunAsync(
                caps[KnownOperations.SqlForeignKeys],
                () => CollectForeignKeysAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        await tracker.RunAsync(
                caps[KnownOperations.SqlViews],
                () => CollectViewsAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        await tracker.RunAsync(
                caps[KnownOperations.SqlProcedures],
                () => CollectProceduresAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        var triggerScope = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["parentScope"] = "tables",
            ["tableNameFilter"] = "none",
            ["eventSource"] = "sys.trigger_events",
            ["includesInsteadOf"] = "true",
            ["definitionCapture"] = "attempted",
            ["completeness"] = "unknown",
            ["metadataVisibility"] = "not_verified"
        };
        await tracker.RunAsync(
                caps[KnownOperations.SqlTriggers],
                () => CollectTriggersAsync(connection, context, collectedAt, issues, triggerScope, () => objectCount++, Report, cancellationToken),
                () => objectCount,
                triggerScope)
            .ConfigureAwait(false);

        foreach (var issue in issues)
        {
            await context.Workspace.AddIssueAsync(Id, issue, cancellationToken).ConfigureAwait(false);
        }

        var status = issues.Any(i => i.Severity == CollectionIssueSeverity.Error)
            ? (objectCount > 0 ? CollectorStatus.Partial : CollectorStatus.Failed)
            : issues.Any(i => i.Severity == CollectionIssueSeverity.Warning)
                ? CollectorStatus.SucceededWithWarnings
                : CollectorStatus.Succeeded;

        return new CollectorResult(Id, status, Array.Empty<CollectedArtifact>(), issues, sw.Elapsed, tracker.Coverages);
    }

    private static async Task CollectDatabaseAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DB_NAME(), CAST(DB_ID() AS int), CONVERT(nvarchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Version'));";
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var name = reader.GetString(0);
                var summary = JsonSerializer.Serialize(new
                {
                    name,
                    databaseId = reader.IsDBNull(1) ? (int?)null : Convert.ToInt32(reader.GetValue(1)),
                    versionProperty = reader.IsDBNull(2) ? null : reader.GetString(2)
                }, JsonOptions);

                await context.Workspace!.UpsertObjectAsync(new TechnicalObjectRecord(
                    $"db:{name}",
                    ObjectKind.Database,
                    name,
                    null,
                    null,
                    CollectorId,
                    null,
                    summary,
                    collectedAt), ct).ConfigureAwait(false);
                bump();
                await report($"Base de dados: {name}").ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            issues.Add(Issue("sql.database", "Não foi possível recolher metadados da base de dados.", ex));
        }
    }

    private static async Task CollectAllTablesAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 120;
            // Inventário completo de tabelas físicas — leve (sem colunas/índices/constraints aqui).
            cmd.CommandText =
                """
                SELECT
                    s.name AS schema_name,
                    t.name AS table_name,
                    t.object_id,
                    t.type_desc,
                    t.is_ms_shipped,
                    t.create_date,
                    t.modify_date
                FROM sys.tables t
                INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                ORDER BY s.name, t.name;
                """;

            var tableRecords = new List<TechnicalObjectRecord>();
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [TableInventoryClassification.SqlSystem] = 0,
                [TableInventoryClassification.CandidateCustom] = 0,
                [TableInventoryClassification.ApplicationOrUnknown] = 0
            };

            await using (var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var schema = reader.GetString(0);
                    var name = reader.GetString(1);
                    var sqlObjectId = reader.GetInt32(2);
                    var typeDesc = reader.IsDBNull(3) ? null : reader.GetString(3);
                    var isMsShipped = !reader.IsDBNull(4) && reader.GetBoolean(4);
                    var createDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
                    var modifyDate = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6);
                    var classification = TableInventoryClassification.Classify(isMsShipped, name);
                    counts[classification] = counts.GetValueOrDefault(classification) + 1;

                    var summary = JsonSerializer.Serialize(new
                    {
                        objectId = sqlObjectId,
                        typeDesc,
                        isMsShipped,
                        classification,
                        namePrefixU = TableInventoryClassification.IsCandidateCustomName(name),
                        createDate = createDate?.ToString("O"),
                        modifyDate = modifyDate?.ToString("O")
                    }, JsonOptions);

                    tableRecords.Add(new TechnicalObjectRecord(
                        TableInventoryClassification.BuildObjectId(schema, name),
                        ObjectKind.Table,
                        name,
                        schema,
                        null,
                        CollectorId,
                        null,
                        summary,
                        collectedAt));
                }
            }

            if (tableRecords.Count == 0)
            {
                // Distinguir "catálogo consultado, 0 tabelas" de falha de permissões (catch abaixo).
                issues.Add(new CollectionIssue(
                    CollectionIssueSeverity.Warning,
                    "sql.tables.catalog_empty",
                    "A consulta a sys.tables/sys.schemas devolveu 0 tabelas. Confirmar permissões VIEW DEFINITION / acesso ao catálogo."));
                await report("Tabelas físicas: 0 (catálogo vazio — ver aviso).").ConfigureAwait(false);
                return;
            }

            const int batchSize = 500;
            for (var offset = 0; offset < tableRecords.Count; offset += batchSize)
            {
                var batch = tableRecords.Skip(offset).Take(batchSize).ToList();
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                for (var i = 0; i < batch.Count; i++)
                {
                    bump();
                }

                await report($"Tabelas físicas materializadas: {Math.Min(offset + batch.Count, tableRecords.Count)}/{tableRecords.Count}...")
                    .ConfigureAwait(false);
            }

            await report(
                    $"Tabelas físicas: {tableRecords.Count} encontradas e materializadas "
                    + $"(sql_system={counts[TableInventoryClassification.SqlSystem]}, "
                    + $"candidate_custom={counts[TableInventoryClassification.CandidateCustom]}, "
                    + $"application_or_unknown={counts[TableInventoryClassification.ApplicationOrUnknown]}).")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue(
                "sql.tables.catalog",
                "Não foi possível consultar o inventário de tabelas (sys.tables/sys.schemas). Possível falta de permissões.",
                ex));
        }
    }

    private static async Task CollectPrimaryKeysAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText = SqlKeyInventory.PrimaryKeysSql;

            // objectId → acumulador (colunas por ordinal)
            var map = new Dictionary<string, (
                string Schema,
                string Table,
                string PkName,
                int UniqueIndexId,
                List<(string Name, int Ordinal)> Columns)>(StringComparer.OrdinalIgnoreCase);

            await using (var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    ct.ThrowIfCancellationRequested();
                    var schema = reader.GetString(0);
                    var table = reader.GetString(1);
                    var pkName = reader.GetString(2);
                    var indexId = reader.GetInt32(3);
                    var columnName = reader.GetString(4);
                    var ordinal = Convert.ToInt32(reader.GetValue(5));
                    var objectId = SqlKeyInventory.BuildPrimaryKeyObjectId(schema, table, pkName);
                    if (!map.TryGetValue(objectId, out var entry))
                    {
                        entry = (schema, table, pkName, indexId, []);
                        map[objectId] = entry;
                    }

                    entry.Columns.Add((columnName, ordinal));
                }
            }

            var batch = new List<TechnicalObjectRecord>(Math.Min(1000, map.Count));
            var custom = 0;
            var standard = 0;
            foreach (var entry in map.Values.OrderBy(e => e.Schema, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(e => e.Table, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(e => e.PkName, StringComparer.OrdinalIgnoreCase))
            {
                var ordered = entry.Columns.OrderBy(c => c.Ordinal).ToList();
                batch.Add(SqlKeyInventory.CreatePrimaryKey(
                    entry.Schema,
                    entry.Table,
                    entry.PkName,
                    entry.UniqueIndexId,
                    ordered,
                    collectedAt));
                bump();
                if (TableInventoryClassification.IsCandidateCustomName(entry.Table))
                {
                    custom++;
                }
                else
                {
                    standard++;
                }

                if (batch.Count >= 1000)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                    await report($"Chaves primárias processadas: {custom + standard}...").ConfigureAwait(false);
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report(
                    $"Chaves primárias: {map.Count} "
                    + $"(application_or_system≈{standard}, candidate_custom={custom}).")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("sql.primary_keys", "Não foi possível recolher chaves primárias.", ex));
        }
    }

    private static async Task CollectForeignKeysAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText = SqlKeyInventory.ForeignKeysSql;

            var map = new Dictionary<string, (
                string Schema,
                string Table,
                string Fk,
                string RefSchema,
                string RefTable,
                string? DeleteAction,
                string? UpdateAction,
                List<(string Parent, string Referenced, int Ordinal)> Cols)>(StringComparer.OrdinalIgnoreCase);

            await using (var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    ct.ThrowIfCancellationRequested();
                    var schema = reader.GetString(0);
                    var table = reader.GetString(1);
                    var fk = reader.GetString(2);
                    var objectId = SqlKeyInventory.BuildForeignKeyObjectId(schema, table, fk);
                    if (!map.TryGetValue(objectId, out var entry))
                    {
                        entry = (
                            schema,
                            table,
                            fk,
                            reader.GetString(3),
                            reader.GetString(4),
                            reader.IsDBNull(8) ? null : reader.GetString(8),
                            reader.IsDBNull(9) ? null : reader.GetString(9),
                            []);
                        map[objectId] = entry;
                    }

                    entry.Cols.Add((
                        reader.GetString(5),
                        reader.GetString(6),
                        Convert.ToInt32(reader.GetValue(7))));
                }
            }

            var batch = new List<TechnicalObjectRecord>(Math.Min(500, map.Count));
            var parentCustom = 0;
            var parentStandard = 0;
            foreach (var entry in map.Values.OrderBy(e => e.Schema, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(e => e.Table, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(e => e.Fk, StringComparer.OrdinalIgnoreCase))
            {
                var ordered = entry.Cols.OrderBy(c => c.Ordinal).ToList();
                batch.Add(SqlKeyInventory.CreateForeignKey(
                    entry.Schema,
                    entry.Table,
                    entry.Fk,
                    entry.RefSchema,
                    entry.RefTable,
                    ordered,
                    collectedAt,
                    entry.DeleteAction,
                    entry.UpdateAction));
                bump();
                if (TableInventoryClassification.IsCandidateCustomName(entry.Table))
                {
                    parentCustom++;
                }
                else
                {
                    parentStandard++;
                }

                if (batch.Count >= 500)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                    await report($"Chaves estrangeiras processadas: {parentCustom + parentStandard}...")
                        .ConfigureAwait(false);
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            // 0 FKs é válido (BD sem constraints FK) — não é erro.
            await report(
                    $"Chaves estrangeiras: {map.Count} "
                    + $"(parent application_or_system≈{parentStandard}, parent candidate_custom={parentCustom}).")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("sql.foreign_keys", "Não foi possível recolher chaves estrangeiras.", ex));
        }
    }

    private static async Task CollectViewsAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT s.name, v.name, m.definition
                FROM sys.views v
                INNER JOIN sys.schemas s ON s.schema_id = v.schema_id
                LEFT JOIN sys.sql_modules m ON m.object_id = v.object_id
                ORDER BY s.name, v.name;
                """;
            var batch = new List<TechnicalObjectRecord>(500);
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            var count = 0;
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var schema = reader.GetString(0);
                var name = reader.GetString(1);
                var definition = reader.IsDBNull(2) ? null : reader.GetString(2);
                string? artifactId = null;
                if (definition is not null)
                {
                    artifactId = await SaveDefinitionAsync(
                        context, $"views/{schema}.{name}.sql", definition, ct).ConfigureAwait(false);
                }
                else
                {
                    issues.Add(new CollectionIssue(
                        CollectionIssueSeverity.Warning,
                        "sql.view.definition_missing",
                        "Definição de view indisponível (possível encriptação ou permissão).",
                        $"{schema}.{name}"));
                }

                batch.Add(new TechnicalObjectRecord(
                    $"view:{schema}.{name}",
                    ObjectKind.View,
                    name,
                    schema,
                    null,
                    CollectorId,
                    artifactId,
                    null,
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 200)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                    await report($"Views processadas: {count}...").ConfigureAwait(false);
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"Views: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("sql.views", "Não foi possível recolher views.", ex));
        }
    }

    private static async Task CollectProceduresAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 600;
            cmd.CommandText =
                """
                SELECT s.name, p.name, m.definition
                FROM sys.procedures p
                INNER JOIN sys.schemas s ON s.schema_id = p.schema_id
                LEFT JOIN sys.sql_modules m ON m.object_id = p.object_id
                ORDER BY s.name, p.name;
                """;
            var batch = new List<TechnicalObjectRecord>(500);
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            var count = 0;
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var schema = reader.GetString(0);
                var name = reader.GetString(1);
                var definition = reader.IsDBNull(2) ? null : reader.GetString(2);
                string? artifactId = null;
                if (definition is not null)
                {
                    artifactId = await SaveDefinitionAsync(
                        context, $"procedures/{schema}.{name}.sql", definition, ct).ConfigureAwait(false);
                }
                else
                {
                    issues.Add(new CollectionIssue(
                        CollectionIssueSeverity.Warning,
                        "sql.procedure.definition_missing",
                        "Definição de procedure indisponível (possível encriptação ou permissão).",
                        $"{schema}.{name}"));
                }

                batch.Add(new TechnicalObjectRecord(
                    $"procedure:{schema}.{name}",
                    ObjectKind.StoredProcedure,
                    name,
                    schema,
                    null,
                    CollectorId,
                    artifactId,
                    null,
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 200)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                    await report($"Procedures processadas: {count}...").ConfigureAwait(false);
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"Stored procedures: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("sql.procedures", "Não foi possível recolher stored procedures.", ex));
        }
    }

    private static async Task CollectTriggersAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        IDictionary<string, string> coverageScope,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            var metadataVisibilityVerified = false;
            try
            {
                await using var visibilityCmd = connection.CreateCommand();
                visibilityCmd.CommandText =
                    "SELECT HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'VIEW DEFINITION');";
                var visibilityResult = await visibilityCmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                metadataVisibilityVerified = visibilityResult is not null
                    && visibilityResult is not DBNull
                    && Convert.ToInt32(visibilityResult) == 1;
            }
            catch (SqlException)
            {
                // Failure to inspect permissions must not discard visible trigger rows.
                // The operation remains explicitly partial in its coverage contract.
            }

            coverageScope["metadataVisibility"] = metadataVisibilityVerified
                ? "database_view_definition"
                : "not_verified";
            coverageScope["completeness"] = metadataVisibilityVerified ? "verified" : "unknown";

            if (!metadataVisibilityVerified)
            {
                issues.Add(new CollectionIssue(
                    CollectionIssueSeverity.Warning,
                    "sql.trigger.metadata_visibility_unverified",
                    "Não foi possível confirmar VIEW DEFINITION ao nível da base de dados; o inventário visível de triggers pode estar incompleto."));
            }

            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT
                    tr.object_id,
                    tr.parent_id,
                    ps.name AS parent_schema,
                    t.name AS parent_name,
                    COALESCE(OBJECT_SCHEMA_NAME(tr.object_id), ps.name) AS trigger_schema,
                    tr.name AS trigger_name,
                    tr.is_disabled,
                    tr.is_instead_of_trigger,
                    m.definition,
                    CASE
                        WHEN m.definition IS NULL THEN N'Unavailable'
                        WHEN DATALENGTH(m.definition) = 0 THEN N'Empty'
                        ELSE N'Available'
                    END AS definition_status,
                    STUFF((
                        SELECT N',' + te.type_desc
                        FROM sys.trigger_events te
                        WHERE te.object_id = tr.object_id
                        ORDER BY te.type_desc
                        FOR XML PATH(''), TYPE
                    ).value('.', 'nvarchar(max)'), 1, 1, N'') AS trigger_events
                FROM sys.triggers tr
                INNER JOIN sys.tables t ON t.object_id = tr.parent_id
                INNER JOIN sys.schemas ps ON ps.schema_id = t.schema_id
                LEFT JOIN sys.sql_modules m ON m.object_id = tr.object_id
                WHERE tr.parent_class_desc = N'OBJECT_OR_COLUMN'
                ORDER BY ps.name, t.name, trigger_schema, tr.name;
                """;

            var batch = new List<TechnicalObjectRecord>(500);
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            var count = 0;
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var triggerObjectId = reader.GetInt32(0);
                var parentObjectId = reader.GetInt32(1);
                var parentSchema = reader.GetString(2);
                var parentName = reader.GetString(3);
                var triggerSchema = reader.IsDBNull(4) ? parentSchema : reader.GetString(4);
                var triggerName = reader.GetString(5);
                var disabled = reader.GetBoolean(6);
                var insteadOf = reader.GetBoolean(7);
                var definition = reader.IsDBNull(8) ? null : reader.GetString(8);
                var definitionStatus = reader.GetString(9);
                var events = reader.IsDBNull(10)
                    ? Array.Empty<string>()
                    : reader.GetString(10)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                string? artifactId = null;
                if (definition is not null)
                {
                    artifactId = await SaveDefinitionAsync(
                        context,
                        BuildTriggerArtifactPath(triggerSchema, triggerName, parentSchema, parentName),
                        definition,
                        ct).ConfigureAwait(false);
                }
                else
                {
                    issues.Add(new CollectionIssue(
                        CollectionIssueSeverity.Warning,
                        "sql.trigger.definition_missing",
                        "Definição de trigger indisponível; a causa não é inferida.",
                        $"{parentSchema}.{parentName}.{triggerName}"));
                }

                var summary = JsonSerializer.Serialize(new
                {
                    sqlObjectId = triggerObjectId,
                    parentObjectId,
                    parentSchema,
                    parentName,
                    triggerSchema,
                    triggerName,
                    events,
                    isDisabled = disabled,
                    isInsteadOf = insteadOf,
                    definitionStatus
                }, JsonOptions);

                batch.Add(new TechnicalObjectRecord(
                    BuildTriggerObjectId(triggerSchema, triggerName, parentSchema, parentName),
                    ObjectKind.Trigger,
                    triggerName,
                    triggerSchema,
                    $"table:{parentSchema}.{parentName}",
                    CollectorId,
                    artifactId,
                    summary,
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 200)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                    await report($"Triggers processadas: {count}...").ConfigureAwait(false);
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"Triggers DML associadas a tabelas: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("sql.triggers", "Não foi possível recolher triggers DML associadas a tabelas.", ex));
        }
    }

    private static string BuildTriggerObjectId(
        string triggerSchema,
        string triggerName,
        string parentSchema,
        string parentName)
        => "trigger:" + JsonSerializer.Serialize(
            new[] { triggerSchema, triggerName, parentSchema, parentName });

    private static string BuildTriggerArtifactPath(
        string triggerSchema,
        string triggerName,
        string parentSchema,
        string parentName)
        => $"triggers/trigger-{EncodePathPart(triggerSchema)}-{EncodePathPart(triggerName)}"
            + $"--{EncodePathPart(parentSchema)}-{EncodePathPart(parentName)}.sql";

    private static string EncodePathPart(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static async Task<string> SaveDefinitionAsync(
        ScanContext context,
        string relativePath,
        string definition,
        CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(definition);
        return await context.Workspace!.SaveArtifactAsync(
            CollectorId,
            relativePath,
            bytes,
            "text/sql",
            new Dictionary<string, string> { ["encoding"] = "utf-8" },
            ct).ConfigureAwait(false);
    }

    private static CollectionIssue Issue(string code, string message, Exception ex, string? objectName = null) =>
        new(
            CollectionIssueSeverity.Error,
            code,
            message,
            objectName,
            ex.GetType().Name + ": " + ex.Message);
}
