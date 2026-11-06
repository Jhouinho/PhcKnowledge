using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;

namespace PhcKnowledgeScanner.Collectors.Sql.SchemaSnapshot;

/// <summary>
/// Extrai um <see cref="DatabaseSchemaSnapshot"/> exclusivamente a partir do catálogo SQL Server.
/// Não executa SELECT sobre tabelas de negócio.
/// </summary>
public sealed class DatabaseSchemaSnapshotExtractor
{
    public async Task<DatabaseSchemaSnapshot> ExtractAsync(
        string connectionString,
        string scannerVersion,
        SchemaSnapshotOptions? options = null,
        IScanProgressSink? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        options ??= new SchemaSnapshotOptions();

        var snapshot = new DatabaseSchemaSnapshot();
        var started = DateTimeOffset.UtcNow;
        snapshot.Manifest.CollectedAtUtc = started;
        snapshot.Manifest.ScannerVersion = scannerVersion;
        snapshot.Manifest.IncludesSqlDefinitions = options.IncludeSqlDefinitions;
        snapshot.Manifest.SafeForPublicShare = !options.IncludeSqlDefinitions;
        snapshot.Manifest.Limitations.Add(
            "Extração limitada a metadados de catálogo (sys.*). Sem registos de negócio.");
        if (!options.IncludeSqlDefinitions)
        {
            snapshot.Manifest.Limitations.Add(
                "Definições SQL completas de views/procedures/functions/triggers excluídas (modo seguro).");
        }
        else
        {
            snapshot.Manifest.Warnings.Add(
                "IncludesSqlDefinitions=true: o snapshot pode conter código personalizado e não é seguro para partilha pública.");
            snapshot.Manifest.SafeForPublicShare = false;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        snapshot.Manifest.OriginFingerprint = await BuildOriginFingerprintAsync(connection, cancellationToken)
            .ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "server",
            progress,
            "A recolher versão SQL Server...",
            async () => await CollectServerInfoAsync(connection, snapshot, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "phc_signature",
            progress,
            "A detectar assinatura PHC...",
            async () => await CollectPhcSignatureAsync(connection, snapshot, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "tables",
            progress,
            "A recolher tabelas...",
            async () => await CollectTablesAsync(connection, snapshot, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "columns",
            progress,
            "A recolher campos...",
            async () => await CollectColumnsAsync(connection, snapshot, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "primary_keys",
            progress,
            "A recolher chaves primárias...",
            async () => await CollectPrimaryKeysAsync(connection, snapshot, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "foreign_keys",
            progress,
            "A recolher chaves estrangeiras...",
            async () => await CollectForeignKeysAsync(connection, snapshot, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "indexes",
            progress,
            "A recolher índices...",
            async () => await CollectIndexesAsync(connection, snapshot, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "views",
            progress,
            "A recolher views...",
            async () => await CollectViewsAsync(connection, snapshot, options, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "triggers",
            progress,
            "A recolher triggers...",
            async () => await CollectTriggersAsync(connection, snapshot, options, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "procedures",
            progress,
            "A recolher stored procedures...",
            async () => await CollectProceduresAsync(connection, snapshot, options, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "functions",
            progress,
            "A recolher funções...",
            async () => await CollectFunctionsAsync(connection, snapshot, options, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "parameters",
            progress,
            "A recolher parâmetros...",
            async () => await CollectParametersAsync(connection, snapshot, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "user_types",
            progress,
            "A recolher tipos de utilizador...",
            async () => await CollectUserTypesAsync(connection, snapshot, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        await RunCategoryAsync(
            snapshot,
            "synonyms",
            progress,
            "A recolher synonyms...",
            async () => await CollectSynonymsAsync(connection, snapshot, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        FinalizeManifest(snapshot);
        progress?.Report(new ScanProgressEvent(
            "schema_snapshot.done",
            "sql.database.schema-snapshot",
            "Database Schema Snapshot",
            $"Snapshot concluído: {snapshot.Manifest.CompletionStatus}",
            snapshot.Manifest.ObjectCounts.Tables,
            snapshot.Manifest.ObjectCounts.Columns,
            snapshot.Manifest.Warnings.Count,
            snapshot.Manifest.CategoryStatuses.Count(c => c.Status == SchemaSnapshotCompletionStatus.Failed),
            100,
            DateTimeOffset.UtcNow - started));

        return snapshot;
    }

    private static async Task RunCategoryAsync(
        DatabaseSchemaSnapshot snapshot,
        string category,
        IScanProgressSink? progress,
        string message,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        progress?.Report(new ScanProgressEvent(
            "schema_snapshot.running",
            "sql.database.schema-snapshot",
            "Database Schema Snapshot",
            message,
            0, 0, snapshot.Manifest.Warnings.Count, 0, null, TimeSpan.Zero));

        try
        {
            await action().ConfigureAwait(false);
            snapshot.Manifest.CategoryStatuses.Add(new SchemaSnapshotCategoryStatus
            {
                Category = category,
                Status = SchemaSnapshotCompletionStatus.Succeeded
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var detail = ex.GetType().Name + ": " + ex.Message;
            snapshot.Manifest.Warnings.Add($"Categoria '{category}' incompleta: {detail}");
            snapshot.Manifest.CategoryStatuses.Add(new SchemaSnapshotCategoryStatus
            {
                Category = category,
                Status = SchemaSnapshotCompletionStatus.Failed,
                Detail = detail
            });
            snapshot.Manifest.Limitations.Add(
                $"Permissões ou falha de catálogo em '{category}'. Possível falta de VIEW DEFINITION / acesso a sys.*.");
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task CollectServerInfoAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = SchemaCatalogQueries.ServerInfo;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            snapshot.Manifest.SqlServerProductVersion = reader.IsDBNull(0) ? null : reader.GetString(0);
            snapshot.Manifest.SqlServerVersion = reader.IsDBNull(1) ? null : Truncate(reader.GetString(1), 240);
        }
    }

    private static async Task CollectPhcSignatureAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = SchemaCatalogQueries.PhcSignature;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var hits = 0;
            for (var i = 0; i < 4; i++)
            {
                hits += Convert.ToInt32(reader.GetValue(i), CultureInfo.InvariantCulture);
            }

            snapshot.Manifest.PhcSignatureDetected = hits >= 3;
            if (snapshot.Manifest.PhcSignatureDetected == true)
            {
                snapshot.Manifest.Limitations.Add(
                    "Assinatura PHC detectada por presença de tabelas clássicas (bo/cl/st/ft); versão PHC exacta não identificada.");
            }
        }
    }

    private static async Task CollectTablesAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 180;
        cmd.CommandText = SchemaCatalogQueries.Tables;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var name = reader.GetString(1);
            var isMsShipped = !reader.IsDBNull(3) && reader.GetBoolean(3);
            snapshot.Tables.Add(new SchemaTableSnapshot
            {
                SchemaName = schema,
                Name = name,
                ObjectType = reader.IsDBNull(2) ? "BASE TABLE" : reader.GetString(2),
                IsMsShipped = isMsShipped,
                Classification = SchemaObjectClassification.Classify(isMsShipped, name),
                CreateDateUtc = reader.IsDBNull(4) ? null : ToUtc(reader.GetDateTime(4)),
                ModifyDateUtc = reader.IsDBNull(5) ? null : ToUtc(reader.GetDateTime(5)),
                Description = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
    }

    private static async Task CollectColumnsAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        CancellationToken ct)
    {
        var tables = snapshot.Tables.ToDictionary(
            t => TableKey(t.SchemaName, t.Name),
            StringComparer.OrdinalIgnoreCase);

        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText = SchemaCatalogQueries.Columns;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var key = TableKey(reader.GetString(0), reader.GetString(1));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            var isIdentity = !reader.IsDBNull(9) && reader.GetBoolean(9);
            table.Columns.Add(new SchemaColumnSnapshot
            {
                Name = reader.GetString(2),
                Ordinal = Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                SqlType = reader.GetString(4),
                MaxLength = reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture),
                Precision = reader.IsDBNull(6) ? null : Convert.ToByte(reader.GetValue(6), CultureInfo.InvariantCulture),
                Scale = reader.IsDBNull(7) ? null : Convert.ToByte(reader.GetValue(7), CultureInfo.InvariantCulture),
                IsNullable = !reader.IsDBNull(8) && reader.GetBoolean(8),
                IsIdentity = isIdentity,
                IdentitySeed = isIdentity && !reader.IsDBNull(10)
                    ? Convert.ToInt64(reader.GetValue(10), CultureInfo.InvariantCulture)
                    : null,
                IdentityIncrement = isIdentity && !reader.IsDBNull(11)
                    ? Convert.ToInt64(reader.GetValue(11), CultureInfo.InvariantCulture)
                    : null,
                DefaultDefinition = reader.IsDBNull(12) ? null : reader.GetString(12),
                Collation = reader.IsDBNull(13) ? null : reader.GetString(13),
                IsComputed = !reader.IsDBNull(14) && reader.GetBoolean(14),
                ComputedDefinition = reader.IsDBNull(15) ? null : reader.GetString(15)
            });
        }
    }

    private static async Task CollectPrimaryKeysAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        CancellationToken ct)
    {
        var tables = snapshot.Tables.ToDictionary(
            t => TableKey(t.SchemaName, t.Name),
            StringComparer.OrdinalIgnoreCase);
        var map = new Dictionary<string, SchemaPrimaryKeySnapshot>(StringComparer.OrdinalIgnoreCase);

        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText = SchemaCatalogQueries.PrimaryKeys;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var tableName = reader.GetString(1);
            var pkName = reader.GetString(2);
            var mapKey = $"{schema}.{tableName}.{pkName}";
            if (!map.TryGetValue(mapKey, out var pk))
            {
                pk = new SchemaPrimaryKeySnapshot
                {
                    ConstraintName = pkName,
                    IsDisabled = !reader.IsDBNull(5) && reader.GetBoolean(5)
                };
                map[mapKey] = pk;
                if (tables.TryGetValue(TableKey(schema, tableName), out var table))
                {
                    table.PrimaryKey = pk;
                }
            }

            var columnName = reader.GetString(3);
            var ordinal = Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture);
            pk.Columns.Add(new SchemaKeyColumnSnapshot { ColumnName = columnName, Ordinal = ordinal });
            if (tables.TryGetValue(TableKey(schema, tableName), out var tbl))
            {
                var col = tbl.Columns.FirstOrDefault(c =>
                    string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
                if (col is not null)
                {
                    col.IsPrimaryKey = true;
                }
            }
        }
    }

    private static async Task CollectForeignKeysAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        CancellationToken ct)
    {
        var map = new Dictionary<string, SchemaForeignKeySnapshot>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText = SchemaCatalogQueries.ForeignKeys;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var fkName = reader.GetString(2);
            var key = $"{schema}.{table}.{fkName}";
            if (!map.TryGetValue(key, out var fk))
            {
                fk = new SchemaForeignKeySnapshot
                {
                    ParentSchema = schema,
                    ParentTable = table,
                    ConstraintName = fkName,
                    ReferencedSchema = reader.GetString(3),
                    ReferencedTable = reader.GetString(4),
                    OnDeleteAction = reader.IsDBNull(8) ? null : reader.GetString(8),
                    OnUpdateAction = reader.IsDBNull(9) ? null : reader.GetString(9),
                    IsDisabled = !reader.IsDBNull(10) && reader.GetBoolean(10),
                    IsNotTrusted = !reader.IsDBNull(11) && reader.GetBoolean(11),
                    RelationSource = "sql.foreign_key"
                };
                map[key] = fk;
                snapshot.Relationships.Add(fk);
            }

            fk.Columns.Add(new SchemaForeignKeyColumnSnapshot
            {
                ParentColumn = reader.GetString(5),
                ReferencedColumn = reader.GetString(6),
                Ordinal = Convert.ToInt32(reader.GetValue(7), CultureInfo.InvariantCulture)
            });
        }
    }

    private static async Task CollectIndexesAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        CancellationToken ct)
    {
        var map = new Dictionary<string, SchemaIndexSnapshot>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText = SchemaCatalogQueries.Indexes;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            var indexName = reader.GetString(2);
            var key = $"{schema}.{table}.{indexName}";
            if (!map.TryGetValue(key, out var index))
            {
                index = new SchemaIndexSnapshot
                {
                    SchemaName = schema,
                    TableName = table,
                    IndexName = indexName,
                    IndexType = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    IsUnique = !reader.IsDBNull(4) && reader.GetBoolean(4),
                    IsPrimaryKey = !reader.IsDBNull(5) && reader.GetBoolean(5),
                    IsUniqueConstraint = !reader.IsDBNull(6) && reader.GetBoolean(6),
                    IsDisabled = !reader.IsDBNull(7) && reader.GetBoolean(7),
                    FilterDefinition = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
                map[key] = index;
                snapshot.Indexes.Add(index);
            }

            var columnName = reader.GetString(9);
            var isIncluded = !reader.IsDBNull(12) && reader.GetBoolean(12);
            if (isIncluded)
            {
                if (!index.IncludedColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                {
                    index.IncludedColumns.Add(columnName);
                }
            }
            else
            {
                index.KeyColumns.Add(new SchemaIndexColumnSnapshot
                {
                    ColumnName = columnName,
                    Ordinal = reader.IsDBNull(10) ? 0 : Convert.ToInt32(reader.GetValue(10), CultureInfo.InvariantCulture),
                    IsDescending = !reader.IsDBNull(11) && reader.GetBoolean(11)
                });
            }
        }
    }

    private static async Task CollectViewsAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        SchemaSnapshotOptions options,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText = SchemaCatalogQueries.Views;
        cmd.Parameters.AddWithValue("@includeDefinitions", options.IncludeSqlDefinitions ? 1 : 0);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var name = reader.GetString(1);
            var isMsShipped = !reader.IsDBNull(2) && reader.GetBoolean(2);
            snapshot.Views.Add(new SchemaViewSnapshot
            {
                SchemaName = reader.GetString(0),
                Name = name,
                IsMsShipped = isMsShipped,
                Classification = SchemaObjectClassification.Classify(isMsShipped, name),
                CreateDateUtc = reader.IsDBNull(3) ? null : ToUtc(reader.GetDateTime(3)),
                ModifyDateUtc = reader.IsDBNull(4) ? null : ToUtc(reader.GetDateTime(4)),
                HasDefinitionAvailable = !reader.IsDBNull(5) && Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture) == 1,
                SqlDefinition = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
    }

    private static async Task CollectTriggersAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        SchemaSnapshotOptions options,
        CancellationToken ct)
    {
        var map = new Dictionary<string, SchemaTriggerSnapshot>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandTimeout = 300;
            cmd.CommandText = SchemaCatalogQueries.Triggers;
            cmd.Parameters.AddWithValue("@includeDefinitions", options.IncludeSqlDefinitions ? 1 : 0);
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var parentSchema = reader.IsDBNull(0) ? "" : reader.GetString(0);
                var parentName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var triggerSchema = reader.IsDBNull(2) ? parentSchema : reader.GetString(2);
                var triggerName = reader.GetString(3);
                var key = $"{triggerSchema}.{triggerName}";
                var trigger = new SchemaTriggerSnapshot
                {
                    SchemaName = triggerSchema,
                    Name = triggerName,
                    ParentSchema = parentSchema,
                    ParentObject = parentName,
                    ParentObjectType = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    IsDisabled = !reader.IsDBNull(5) && reader.GetBoolean(5),
                    IsInsteadOf = !reader.IsDBNull(6) && reader.GetBoolean(6),
                    Classification = SchemaObjectClassification.Classify(false, parentName),
                    HasDefinitionAvailable = !reader.IsDBNull(7) && Convert.ToInt32(reader.GetValue(7), CultureInfo.InvariantCulture) == 1,
                    SqlDefinition = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
                map[key] = trigger;
                snapshot.Triggers.Add(trigger);
            }
        }

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandTimeout = 180;
            cmd.CommandText = SchemaCatalogQueries.TriggerEvents;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var key = $"{reader.GetString(0)}.{reader.GetString(1)}";
                if (!map.TryGetValue(key, out var trigger))
                {
                    continue;
                }

                var evt = reader.IsDBNull(2) ? null : reader.GetString(2);
                if (!string.IsNullOrWhiteSpace(evt)
                    && !trigger.Events.Contains(evt, StringComparer.OrdinalIgnoreCase))
                {
                    trigger.Events.Add(evt);
                }
            }
        }
    }

    private static async Task CollectProceduresAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        SchemaSnapshotOptions options,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText = SchemaCatalogQueries.Procedures;
        cmd.Parameters.AddWithValue("@includeDefinitions", options.IncludeSqlDefinitions ? 1 : 0);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var name = reader.GetString(1);
            var isMsShipped = !reader.IsDBNull(2) && reader.GetBoolean(2);
            snapshot.Procedures.Add(new SchemaRoutineSnapshot
            {
                SchemaName = reader.GetString(0),
                Name = name,
                RoutineKind = "PROCEDURE",
                IsMsShipped = isMsShipped,
                Classification = SchemaObjectClassification.Classify(isMsShipped, name),
                CreateDateUtc = reader.IsDBNull(3) ? null : ToUtc(reader.GetDateTime(3)),
                ModifyDateUtc = reader.IsDBNull(4) ? null : ToUtc(reader.GetDateTime(4)),
                HasDefinitionAvailable = !reader.IsDBNull(5) && Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture) == 1,
                SqlDefinition = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
    }

    private static async Task CollectFunctionsAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        SchemaSnapshotOptions options,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText = SchemaCatalogQueries.Functions;
        cmd.Parameters.AddWithValue("@includeDefinitions", options.IncludeSqlDefinitions ? 1 : 0);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var name = reader.GetString(1);
            var isMsShipped = !reader.IsDBNull(3) && reader.GetBoolean(3);
            snapshot.Functions.Add(new SchemaRoutineSnapshot
            {
                SchemaName = reader.GetString(0),
                Name = name,
                RoutineKind = reader.IsDBNull(2) ? "FUNCTION" : reader.GetString(2),
                IsMsShipped = isMsShipped,
                Classification = SchemaObjectClassification.Classify(isMsShipped, name),
                CreateDateUtc = reader.IsDBNull(4) ? null : ToUtc(reader.GetDateTime(4)),
                ModifyDateUtc = reader.IsDBNull(5) ? null : ToUtc(reader.GetDateTime(5)),
                HasDefinitionAvailable = !reader.IsDBNull(6) && Convert.ToInt32(reader.GetValue(6), CultureInfo.InvariantCulture) == 1,
                SqlDefinition = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }
    }

    private static async Task CollectParametersAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        CancellationToken ct)
    {
        var routines = new Dictionary<string, SchemaRoutineSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in snapshot.Procedures)
        {
            routines[$"P:{p.SchemaName}.{p.Name}"] = p;
        }

        foreach (var f in snapshot.Functions)
        {
            routines[$"F:{f.SchemaName}.{f.Name}"] = f;
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 300;
        cmd.CommandText = SchemaCatalogQueries.Parameters;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var schema = reader.GetString(0);
            var name = reader.GetString(1);
            var objectType = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
            var keyPrefix = objectType is "P" or "PC" ? "P" : "F";
            if (!routines.TryGetValue($"{keyPrefix}:{schema}.{name}", out var routine))
            {
                continue;
            }

            var parameterName = reader.IsDBNull(3) ? "" : reader.GetString(3);
            // parameter_id 0 = return value for functions
            if (Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture) == 0)
            {
                routine.ReturnType = reader.IsDBNull(5) ? null : reader.GetString(5);
                continue;
            }

            routine.Parameters.Add(new SchemaParameterSnapshot
            {
                Name = parameterName,
                Ordinal = Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
                SqlType = reader.IsDBNull(5) ? "" : reader.GetString(5),
                MaxLength = reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetValue(6), CultureInfo.InvariantCulture),
                Precision = reader.IsDBNull(7) ? null : Convert.ToByte(reader.GetValue(7), CultureInfo.InvariantCulture),
                Scale = reader.IsDBNull(8) ? null : Convert.ToByte(reader.GetValue(8), CultureInfo.InvariantCulture),
                IsOutput = !reader.IsDBNull(9) && reader.GetBoolean(9),
                IsReadonly = !reader.IsDBNull(10) && reader.GetBoolean(10),
                HasDefaultValue = !reader.IsDBNull(11) && reader.GetBoolean(11)
            });
        }
    }

    private static async Task CollectUserTypesAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 120;
        cmd.CommandText = SchemaCatalogQueries.UserTypes;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            snapshot.UserTypes.Add(new SchemaUserTypeSnapshot
            {
                SchemaName = reader.GetString(0),
                Name = reader.GetString(1),
                SystemTypeName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                MaxLength = reader.IsDBNull(3) ? (short)0 : Convert.ToInt16(reader.GetValue(3), CultureInfo.InvariantCulture),
                Precision = reader.IsDBNull(4) ? (byte)0 : Convert.ToByte(reader.GetValue(4), CultureInfo.InvariantCulture),
                Scale = reader.IsDBNull(5) ? (byte)0 : Convert.ToByte(reader.GetValue(5), CultureInfo.InvariantCulture),
                IsNullable = !reader.IsDBNull(6) && reader.GetBoolean(6),
                IsTableType = !reader.IsDBNull(7) && reader.GetBoolean(7)
            });
        }
    }

    private static async Task CollectSynonymsAsync(
        SqlConnection connection,
        DatabaseSchemaSnapshot snapshot,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 60;
        cmd.CommandText = SchemaCatalogQueries.Synonyms;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            snapshot.Synonyms.Add(new SchemaSynonymSnapshot
            {
                SchemaName = reader.GetString(0),
                Name = reader.GetString(1),
                BaseObjectName = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }
    }

    private static void FinalizeManifest(DatabaseSchemaSnapshot snapshot)
    {
        snapshot.Tables = snapshot.Tables
            .OrderBy(t => t.SchemaName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var table in snapshot.Tables)
        {
            table.Columns = table.Columns.OrderBy(c => c.Ordinal).ToList();
            if (table.PrimaryKey is not null)
            {
                table.PrimaryKey.Columns = table.PrimaryKey.Columns.OrderBy(c => c.Ordinal).ToList();
            }
        }

        snapshot.Relationships = snapshot.Relationships
            .OrderBy(r => r.ParentSchema, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.ParentTable, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.ConstraintName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var fk in snapshot.Relationships)
        {
            fk.Columns = fk.Columns.OrderBy(c => c.Ordinal).ToList();
        }

        snapshot.Indexes = snapshot.Indexes
            .OrderBy(i => i.SchemaName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.TableName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.IndexName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        snapshot.Manifest.ObjectCounts = new SchemaSnapshotObjectCounts
        {
            Tables = snapshot.Tables.Count,
            Columns = snapshot.Tables.Sum(t => t.Columns.Count),
            PrimaryKeys = snapshot.Tables.Count(t => t.PrimaryKey is not null),
            ForeignKeys = snapshot.Relationships.Count,
            Indexes = snapshot.Indexes.Count,
            Views = snapshot.Views.Count,
            Triggers = snapshot.Triggers.Count,
            Procedures = snapshot.Procedures.Count,
            Functions = snapshot.Functions.Count,
            UserTypes = snapshot.UserTypes.Count,
            Synonyms = snapshot.Synonyms.Count
        };

        var classifications = snapshot.Tables.Select(t => t.Classification.Classification)
            .Concat(snapshot.Views.Select(v => v.Classification.Classification))
            .Concat(snapshot.Procedures.Select(p => p.Classification.Classification))
            .Concat(snapshot.Functions.Select(f => f.Classification.Classification))
            .Concat(snapshot.Triggers.Select(t => t.Classification.Classification));

        snapshot.Manifest.Classification = new SchemaSnapshotClassificationSummary
        {
            SqlSystem = classifications.Count(c => c == SchemaObjectClassification.SqlSystem),
            CandidateCustom = classifications.Count(c => c == SchemaObjectClassification.CandidateCustom),
            Unknown = classifications.Count(c => c == SchemaObjectClassification.Unknown)
        };

        var failed = snapshot.Manifest.CategoryStatuses.Count(c =>
            c.Status == SchemaSnapshotCompletionStatus.Failed);
        if (failed == 0)
        {
            snapshot.Manifest.CompletionStatus = snapshot.Manifest.Warnings.Count > 0
                ? SchemaSnapshotCompletionStatus.SucceededWithWarnings
                : SchemaSnapshotCompletionStatus.Succeeded;
        }
        else if (snapshot.Tables.Count > 0 || snapshot.Relationships.Count > 0)
        {
            snapshot.Manifest.CompletionStatus = SchemaSnapshotCompletionStatus.Partial;
        }
        else
        {
            snapshot.Manifest.CompletionStatus = SchemaSnapshotCompletionStatus.Failed;
        }
    }

    private static async Task<string> BuildOriginFingerprintAsync(SqlConnection connection, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DB_NAME(), @@SERVERNAME;";
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var db = "";
        var server = "";
        if (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            db = reader.IsDBNull(0) ? "" : reader.GetString(0);
            server = reader.IsDBNull(1) ? "" : reader.GetString(1);
        }

        var material = $"phc-schema-origin|{server.Trim().ToUpperInvariant()}|{db.Trim().ToUpperInvariant()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    public static string TableKey(string schema, string name) => $"{schema}.{name}";

    private static DateTimeOffset ToUtc(DateTime value)
    {
        var unspecified = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, TimeSpan.Zero);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
