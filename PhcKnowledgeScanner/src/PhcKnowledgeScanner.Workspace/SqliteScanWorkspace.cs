using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Workspace;

public sealed class SqliteScanWorkspace : IScanWorkspace
{
    private static readonly JsonSerializerOptions CoverageJsonOptions = new() { WriteIndented = false };

    private readonly SqliteConnection _connection;
    private bool _disposed;

    private SqliteScanWorkspace(string rootPath, SqliteConnection connection)
    {
        RootPath = rootPath;
        _connection = connection;
    }

    public string RootPath { get; }

    public string ScanId { get; private set; } = string.Empty;

    public string? CaptureId { get; private set; }

    public static async Task<SqliteScanWorkspace> CreateAsync(string rootPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(Path.Combine(rootPath, "artifacts"));

        var dbPath = Path.Combine(rootPath, "workspace.db");
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var workspace = new SqliteScanWorkspace(rootPath, connection);
        await workspace.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        return workspace;
    }

    /// <summary>
    /// Abre um workspace existente.
    /// Em <paramref name="readOnly"/>: <see cref="SqliteOpenMode.ReadOnly"/>, sem pooling,
    /// sem <c>EnsureSchema</c>, e <c>PRAGMA query_only=ON</c> — sem CREATE/INSERT/UPDATE/DELETE.
    /// </summary>
    public static async Task<SqliteScanWorkspace> OpenExistingAsync(
        string rootPath,
        bool readOnly,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        var rootFull = Path.GetFullPath(rootPath);
        var dbPath = Path.Combine(rootFull, "workspace.db");
        if (!File.Exists(dbPath))
        {
            throw new FileNotFoundException("workspace.db não encontrado.", dbPath);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
            // Evita reutilização de ligação que pudesse ter estado de escrita noutra sessão.
            Pooling = false
        };

        var connection = new SqliteConnection(builder.ToString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (readOnly)
        {
            await using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA query_only = ON;";
            await pragma.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var workspace = new SqliteScanWorkspace(rootFull, connection);
        if (!readOnly)
        {
            await workspace.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var meta = connection.CreateCommand();
        meta.CommandText = "SELECT scan_id FROM scan_meta LIMIT 1;";
        var scanId = await meta.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        if (!string.IsNullOrWhiteSpace(scanId))
        {
            workspace.ScanId = scanId;
        }

        // Workspaces antigos (read-only) podem não ter capture_id; não falhar a abertura.
        if (await ColumnExistsAsync(connection, "scan_meta", "capture_id", cancellationToken).ConfigureAwait(false))
        {
            await using var captureCmd = connection.CreateCommand();
            captureCmd.CommandText = "SELECT capture_id FROM scan_meta LIMIT 1;";
            var captureId = await captureCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
            if (!string.IsNullOrWhiteSpace(captureId))
            {
                workspace.CaptureId = captureId;
            }
        }

        return workspace;
    }

    private static async Task<bool> ColumnExistsAsync(
        SqliteConnection connection,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        await using var info = connection.CreateCommand();
        info.CommandText = $"PRAGMA table_info({table});";
        await using var reader = await info.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public async Task InitializeAsync(string scanId, string installationLabel, CancellationToken cancellationToken)
    {
        ScanId = scanId;
        CaptureId = null;
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            DELETE FROM objects;
            DELETE FROM artifacts;
            DELETE FROM issues;
            DELETE FROM relations;
            DELETE FROM declared_relations;
            DELETE FROM declared_relation_interpretations;
            DELETE FROM scan_meta;
            INSERT INTO scan_meta(scan_id, installation_label, status, created_at_utc, capture_id, capabilities_json, coverage_json)
            VALUES ($id, $label, 'initialized', $created, NULL, NULL, NULL);
            """;
        cmd.Parameters.AddWithValue("$id", scanId);
        cmd.Parameters.AddWithValue("$label", installationLabel);
        cmd.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetCaptureIdAsync(string captureId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(captureId);
        CaptureId = captureId;
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE scan_meta SET capture_id = $capture;";
        cmd.Parameters.AddWithValue("$capture", captureId);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveCaptureCoverageAsync(
        string captureId,
        IReadOnlyList<DeclaredCapability> capabilityCatalog,
        IReadOnlyList<OperationCoverage> operationCoverages,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(captureId);
        CaptureId = captureId;
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            UPDATE scan_meta
            SET capture_id = $capture,
                capabilities_json = $capabilities,
                coverage_json = $coverage;
            """;
        cmd.Parameters.AddWithValue("$capture", captureId);
        cmd.Parameters.AddWithValue("$capabilities", JsonSerializer.Serialize(capabilityCatalog, CoverageJsonOptions));
        cmd.Parameters.AddWithValue("$coverage", JsonSerializer.Serialize(operationCoverages, CoverageJsonOptions));
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task UpsertObjectAsync(TechnicalObjectRecord record, CancellationToken cancellationToken) =>
        UpsertObjectsAsync([record], cancellationToken);

    public async Task UpsertObjectsAsync(IEnumerable<TechnicalObjectRecord> records, CancellationToken cancellationToken)
    {
        var list = records as IList<TechnicalObjectRecord> ?? records.ToList();
        if (list.Count == 0)
        {
            return;
        }

        await using var tx = (SqliteTransaction)await _connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var cmd = _connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT INTO objects(object_id, kind, name, schema_name, parent_object_id, collector_id,
                                definition_artifact_id, summary_json, collected_at_utc)
            VALUES ($id, $kind, $name, $schema, $parent, $collector, $artifact, $summary, $collected)
            ON CONFLICT(object_id) DO UPDATE SET
                kind=excluded.kind,
                name=excluded.name,
                schema_name=excluded.schema_name,
                parent_object_id=excluded.parent_object_id,
                collector_id=excluded.collector_id,
                definition_artifact_id=excluded.definition_artifact_id,
                summary_json=excluded.summary_json,
                collected_at_utc=excluded.collected_at_utc;
            """;
        var pId = cmd.Parameters.Add("$id", SqliteType.Text);
        var pKind = cmd.Parameters.Add("$kind", SqliteType.Text);
        var pName = cmd.Parameters.Add("$name", SqliteType.Text);
        var pSchema = cmd.Parameters.Add("$schema", SqliteType.Text);
        var pParent = cmd.Parameters.Add("$parent", SqliteType.Text);
        var pCollector = cmd.Parameters.Add("$collector", SqliteType.Text);
        var pArtifact = cmd.Parameters.Add("$artifact", SqliteType.Text);
        var pSummary = cmd.Parameters.Add("$summary", SqliteType.Text);
        var pCollected = cmd.Parameters.Add("$collected", SqliteType.Text);

        foreach (var record in list)
        {
            cancellationToken.ThrowIfCancellationRequested();
            pId.Value = record.ObjectId;
            pKind.Value = record.Kind;
            pName.Value = record.Name;
            pSchema.Value = (object?)record.SchemaName ?? DBNull.Value;
            pParent.Value = (object?)record.ParentObjectId ?? DBNull.Value;
            pCollector.Value = record.CollectorId;
            pArtifact.Value = (object?)record.DefinitionArtifactId ?? DBNull.Value;
            pSummary.Value = (object?)record.SummaryJson ?? DBNull.Value;
            pCollected.Value = record.CollectedAtUtc.ToString("O");
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> SaveArtifactAsync(
        string collectorId,
        string relativePath,
        ReadOnlyMemory<byte> content,
        string contentType,
        IReadOnlyDictionary<string, string>? metadata,
        CancellationToken cancellationToken)
    {
        var safeRelative = relativePath.Replace('\\', '/').TrimStart('/');
        var artifactId = $"{collectorId}/{safeRelative}";
        var fullPath = Path.Combine(RootPath, "artifacts", collectorId, safeRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, content.ToArray(), cancellationToken).ConfigureAwait(false);

        var hash = Convert.ToHexString(SHA256.HashData(content.Span));
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO artifacts(artifact_id, collector_id, relative_path, content_type, sha256, byte_length, metadata_json)
            VALUES ($id, $collector, $path, $type, $hash, $len, $meta)
            ON CONFLICT(artifact_id) DO UPDATE SET
                sha256=excluded.sha256,
                byte_length=excluded.byte_length,
                metadata_json=excluded.metadata_json;
            """;
        cmd.Parameters.AddWithValue("$id", artifactId);
        cmd.Parameters.AddWithValue("$collector", collectorId);
        cmd.Parameters.AddWithValue("$path", safeRelative);
        cmd.Parameters.AddWithValue("$type", contentType);
        cmd.Parameters.AddWithValue("$hash", hash);
        cmd.Parameters.AddWithValue("$len", content.Length);
        cmd.Parameters.AddWithValue("$meta", metadata is null ? DBNull.Value : JsonSerializer.Serialize(metadata));
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return artifactId;
    }

    public async Task AddIssueAsync(string? collectorId, CollectionIssue issue, CancellationToken cancellationToken)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO issues(collector_id, severity, code, message, object_name, detail, created_at_utc, issue_id, operation_id)
            VALUES ($collector, $severity, $code, $message, $object, $detail, $created, $issueId, $operationId);
            """;
        cmd.Parameters.AddWithValue("$collector", (object?)collectorId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$severity", issue.Severity.ToString());
        cmd.Parameters.AddWithValue("$code", issue.Code);
        cmd.Parameters.AddWithValue("$message", issue.Message);
        cmd.Parameters.AddWithValue("$object", (object?)issue.ObjectName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$detail", (object?)issue.Detail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$issueId", (object?)issue.IssueId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$operationId", (object?)issue.OperationId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetScanStatusAsync(string status, CancellationToken cancellationToken)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE scan_meta SET status = $status;";
        cmd.Parameters.AddWithValue("$status", status);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TechnicalObjectRecord>> GetObjectsAsync(
        string? kind = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        await using var cmd = _connection.CreateCommand();
        var sql = new StringBuilder(
            """
            SELECT object_id, kind, name, schema_name, parent_object_id, collector_id,
                   definition_artifact_id, summary_json, collected_at_utc
            FROM objects WHERE 1=1
            """);
        if (!string.IsNullOrWhiteSpace(kind))
        {
            sql.Append(" AND kind = $kind");
            cmd.Parameters.AddWithValue("$kind", kind);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Pesquisa alargada: nome, schema/tabela/ecran, object_id e summary JSON.
            // Tokens PHC: "FT" também cobre ecrãs SFT e variantes FT2/FT3.
            var tokens = ExpandPhcSearchTokens(search);
            sql.Append(" AND (");
            for (var i = 0; i < tokens.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(" OR ");
                }

                var p = "$q" + i;
                sql.Append(
                    $"(name LIKE {p} OR IFNULL(schema_name,'') LIKE {p} OR object_id LIKE {p} OR IFNULL(summary_json,'') LIKE {p} OR IFNULL(parent_object_id,'') LIKE {p})");
                cmd.Parameters.AddWithValue(p, "%" + tokens[i] + "%");
            }

            sql.Append(')');
            // Com pesquisa: limitar para a UI. Sem pesquisa + kind (ex. inventário completo): sem LIMIT.
            sql.Append(" ORDER BY kind, schema_name, name LIMIT 5000;");
        }
        else
        {
            sql.Append(" ORDER BY kind, schema_name, name;");
        }

        cmd.CommandText = sql.ToString();

        var list = new List<TechnicalObjectRecord>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(ReadObject(reader));
        }

        return list;
    }

    public async Task<TechnicalObjectRecord?> GetObjectAsync(string objectId, CancellationToken cancellationToken = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT object_id, kind, name, schema_name, parent_object_id, collector_id,
                   definition_artifact_id, summary_json, collected_at_utc
            FROM objects WHERE object_id = $id LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$id", objectId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadObject(reader);
    }

    public async Task<string?> ReadArtifactTextAsync(string artifactId, CancellationToken cancellationToken = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT collector_id, relative_path FROM artifacts WHERE artifact_id = $id LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", artifactId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var collectorId = reader.GetString(0);
        var relative = reader.GetString(1);
        var fullPath = ResolveArtifactPath(collectorId, relative);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(fullPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkspaceCaptureMetadata> GetCaptureMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        var hasCaptureId = await ColumnExistsAsync(_connection, "scan_meta", "capture_id", cancellationToken)
            .ConfigureAwait(false);
        var hasCapabilities = await ColumnExistsAsync(_connection, "scan_meta", "capabilities_json", cancellationToken)
            .ConfigureAwait(false);
        var hasCoverage = await ColumnExistsAsync(_connection, "scan_meta", "coverage_json", cancellationToken)
            .ConfigureAwait(false);

        var columns = new List<string> { "status" };
        if (hasCaptureId) columns.Add("capture_id");
        if (hasCapabilities) columns.Add("capabilities_json");
        if (hasCoverage) columns.Add("coverage_json");

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT {string.Join(", ", columns)} FROM scan_meta LIMIT 1;";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return new WorkspaceCaptureMetadata(null, null, null, null, hasCaptureId, hasCapabilities, hasCoverage);
        }

        var index = 0;
        var status = reader.IsDBNull(index) ? null : reader.GetString(index);
        index++;
        string? captureId = null;
        string? capabilitiesJson = null;
        string? coverageJson = null;

        if (hasCaptureId) captureId = reader.IsDBNull(index) ? null : reader.GetString(index);
        if (hasCaptureId) index++;
        if (hasCapabilities) capabilitiesJson = reader.IsDBNull(index) ? null : reader.GetString(index);
        if (hasCapabilities) index++;
        if (hasCoverage) coverageJson = reader.IsDBNull(index) ? null : reader.GetString(index);

        var capabilities = string.IsNullOrWhiteSpace(capabilitiesJson)
            ? null
            : JsonSerializer.Deserialize<List<DeclaredCapability>>(capabilitiesJson);
        var coverage = string.IsNullOrWhiteSpace(coverageJson)
            ? null
            : JsonSerializer.Deserialize<List<OperationCoverage>>(coverageJson);

        return new WorkspaceCaptureMetadata(
            captureId,
            status,
            capabilities,
            coverage,
            hasCaptureId,
            hasCapabilities,
            hasCoverage);
    }

    public async Task<IReadOnlyList<WorkspaceArtifactMetadata>> GetArtifactsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT artifact_id, collector_id, relative_path, content_type, sha256, byte_length, metadata_json
            FROM artifacts
            ORDER BY artifact_id;
            """;
        var artifacts = new List<WorkspaceArtifactMetadata>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            IReadOnlyDictionary<string, string>? metadata = null;
            if (!reader.IsDBNull(6))
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(6));
            }

            artifacts.Add(new WorkspaceArtifactMetadata(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt64(5),
                metadata));
        }

        return artifacts;
    }

    public async Task<byte[]?> ReadArtifactBytesAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT collector_id, relative_path
            FROM artifacts
            WHERE artifact_id = $id
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$id", artifactId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var collectorId = reader.GetString(0);
        var relativePath = reader.GetString(1);
        var fullPath = ResolveArtifactPath(collectorId, relativePath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkspaceIntegrityResult> CheckIntegrityAsync(
        CancellationToken cancellationToken = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check;";
        var messages = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            messages.Add(reader.GetString(0));
        }

        return new WorkspaceIntegrityResult(
            messages.Count == 1 && string.Equals(messages[0], "ok", StringComparison.OrdinalIgnoreCase),
            messages);
    }

    private string ResolveArtifactPath(string collectorId, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(collectorId) || Path.IsPathRooted(collectorId)
            || string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException("Artifact path must be relative to the workspace artifact directory.");
        }

        var artifactsRoot = Path.GetFullPath(Path.Combine(RootPath, "artifacts"));
        var fullPath = Path.GetFullPath(Path.Combine(
            artifactsRoot,
            collectorId,
            relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
        var prefix = artifactsRoot.EndsWith(Path.DirectorySeparatorChar)
            ? artifactsRoot
            : artifactsRoot + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Artifact path resolves outside the workspace artifact directory.");
        }

        return fullPath;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetObjectCountsByKindAsync(CancellationToken cancellationToken = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT kind, COUNT(*) FROM objects GROUP BY kind;";
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            map[reader.GetString(0)] = reader.GetInt32(1);
        }

        return map;
    }

    public async Task<IReadOnlyList<CollectionIssue>> GetIssuesAsync(CancellationToken cancellationToken = default)
    {
        var hasIssueId = await ColumnExistsAsync(_connection, "issues", "issue_id", cancellationToken)
            .ConfigureAwait(false);
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = hasIssueId
            ? """
              SELECT severity, code, message, object_name, detail, issue_id, operation_id
              FROM issues
              ORDER BY rowid;
              """
            : """
              SELECT severity, code, message, object_name, detail
              FROM issues
              ORDER BY rowid;
              """;
        var list = new List<CollectionIssue>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var severity = Enum.TryParse<CollectionIssueSeverity>(reader.GetString(0), out var s)
                ? s
                : CollectionIssueSeverity.Info;
            list.Add(new CollectionIssue(
                severity,
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                hasIssueId && !reader.IsDBNull(5) ? reader.GetString(5) : null,
                hasIssueId && !reader.IsDBNull(6) ? reader.GetString(6) : null));
        }

        return list;
    }

    public async Task SaveRelationAsync(DerivedRelationRecord relation, CancellationToken cancellationToken = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO relations(relation_id, relation_type, from_object_id, to_object_id, evidence,
                                  layer, confidence, explanation, validation_state)
            VALUES ($id, $type, $from, $to, $evidence, $layer, $confidence, $explanation, $validation)
            ON CONFLICT(relation_id) DO UPDATE SET
                evidence=excluded.evidence,
                confidence=excluded.confidence,
                explanation=excluded.explanation,
                validation_state=excluded.validation_state;
            """;
        cmd.Parameters.AddWithValue("$id", relation.RelationId);
        cmd.Parameters.AddWithValue("$type", relation.RelationType);
        cmd.Parameters.AddWithValue("$from", relation.FromObjectId);
        cmd.Parameters.AddWithValue("$to", relation.ToObjectId);
        cmd.Parameters.AddWithValue("$evidence", relation.Evidence);
        cmd.Parameters.AddWithValue("$layer", relation.Layer.ToString());
        cmd.Parameters.AddWithValue("$confidence", (object?)relation.Confidence ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$explanation", (object?)relation.Explanation ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$validation", relation.ValidationState);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DerivedRelationRecord>> GetRelationsAsync(CancellationToken cancellationToken = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT relation_id, relation_type, from_object_id, to_object_id, evidence,
                   layer, confidence, explanation, validation_state
            FROM relations ORDER BY relation_type, from_object_id;
            """;
        var list = new List<DerivedRelationRecord>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var layer = Enum.TryParse<KnowledgeLayer>(reader.GetString(5), out var l) ? l : KnowledgeLayer.Relation;
            list.Add(new DerivedRelationRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                layer,
                reader.IsDBNull(6) ? null : reader.GetDouble(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.GetString(8)));
        }

        return list;
    }

    public async Task UpsertDeclaredRelationsAsync(
        IEnumerable<DeclaredRelationRecord> records,
        CancellationToken cancellationToken = default)
    {
        var list = records as IList<DeclaredRelationRecord> ?? records.ToList();
        if (list.Count == 0)
        {
            return;
        }

        await using var tx = (SqliteTransaction)await _connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var cmd = _connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT INTO declared_relations(
              relation_id, source_type, source_record_id, from_table, from_table_name,
              to_table, to_table_name, original_expression, created_by, created_at,
              modified_by, modified_at, is_marked, raw_metadata_json, collected_at_utc,
              from_object_id, to_object_id, from_table_resolved, to_table_resolved,
              correspondence_kind, correspondence_group_id)
            VALUES (
              $id, $source, $sourceId, $from, $fromName, $to, $toName, $expr, $createdBy, $createdAt,
              $modifiedBy, $modifiedAt, $marked, $raw, $collected,
              $fromObj, $toObj, $fromResolved, $toResolved, $corrKind, $corrGroup)
            ON CONFLICT(relation_id) DO UPDATE SET
              source_type=excluded.source_type,
              source_record_id=excluded.source_record_id,
              from_table=excluded.from_table,
              from_table_name=excluded.from_table_name,
              to_table=excluded.to_table,
              to_table_name=excluded.to_table_name,
              original_expression=excluded.original_expression,
              created_by=excluded.created_by,
              created_at=excluded.created_at,
              modified_by=excluded.modified_by,
              modified_at=excluded.modified_at,
              is_marked=excluded.is_marked,
              raw_metadata_json=excluded.raw_metadata_json,
              collected_at_utc=excluded.collected_at_utc,
              from_object_id=excluded.from_object_id,
              to_object_id=excluded.to_object_id,
              from_table_resolved=excluded.from_table_resolved,
              to_table_resolved=excluded.to_table_resolved,
              correspondence_kind=excluded.correspondence_kind,
              correspondence_group_id=excluded.correspondence_group_id;
            """;

        var pId = cmd.Parameters.Add("$id", SqliteType.Text);
        var pSource = cmd.Parameters.Add("$source", SqliteType.Text);
        var pSourceId = cmd.Parameters.Add("$sourceId", SqliteType.Text);
        var pFrom = cmd.Parameters.Add("$from", SqliteType.Text);
        var pFromName = cmd.Parameters.Add("$fromName", SqliteType.Text);
        var pTo = cmd.Parameters.Add("$to", SqliteType.Text);
        var pToName = cmd.Parameters.Add("$toName", SqliteType.Text);
        var pExpr = cmd.Parameters.Add("$expr", SqliteType.Text);
        var pCreatedBy = cmd.Parameters.Add("$createdBy", SqliteType.Text);
        var pCreatedAt = cmd.Parameters.Add("$createdAt", SqliteType.Text);
        var pModifiedBy = cmd.Parameters.Add("$modifiedBy", SqliteType.Text);
        var pModifiedAt = cmd.Parameters.Add("$modifiedAt", SqliteType.Text);
        var pMarked = cmd.Parameters.Add("$marked", SqliteType.Integer);
        var pRaw = cmd.Parameters.Add("$raw", SqliteType.Text);
        var pCollected = cmd.Parameters.Add("$collected", SqliteType.Text);
        var pFromObj = cmd.Parameters.Add("$fromObj", SqliteType.Text);
        var pToObj = cmd.Parameters.Add("$toObj", SqliteType.Text);
        var pFromResolved = cmd.Parameters.Add("$fromResolved", SqliteType.Integer);
        var pToResolved = cmd.Parameters.Add("$toResolved", SqliteType.Integer);
        var pCorrKind = cmd.Parameters.Add("$corrKind", SqliteType.Text);
        var pCorrGroup = cmd.Parameters.Add("$corrGroup", SqliteType.Text);

        foreach (var r in list)
        {
            pId.Value = r.RelationId;
            pSource.Value = r.SourceType;
            pSourceId.Value = (object?)r.SourceRecordId ?? DBNull.Value;
            pFrom.Value = r.FromTable;
            pFromName.Value = (object?)r.FromTableName ?? DBNull.Value;
            pTo.Value = r.ToTable;
            pToName.Value = (object?)r.ToTableName ?? DBNull.Value;
            pExpr.Value = r.OriginalExpression;
            pCreatedBy.Value = (object?)r.CreatedBy ?? DBNull.Value;
            pCreatedAt.Value = r.CreatedAt?.ToString("O") ?? (object)DBNull.Value;
            pModifiedBy.Value = (object?)r.ModifiedBy ?? DBNull.Value;
            pModifiedAt.Value = r.ModifiedAt?.ToString("O") ?? (object)DBNull.Value;
            pMarked.Value = r.IsMarked is null ? DBNull.Value : r.IsMarked.Value ? 1 : 0;
            pRaw.Value = (object?)r.RawMetadataJson ?? DBNull.Value;
            pCollected.Value = r.CollectedAtUtc.ToString("O");
            pFromObj.Value = (object?)r.FromObjectId ?? DBNull.Value;
            pToObj.Value = (object?)r.ToObjectId ?? DBNull.Value;
            // 0=Unresolved, 1=Resolved, 2=Ambiguous (compatível com booleanos antigos 0/1).
            pFromResolved.Value = (int)r.FromTableResolution;
            pToResolved.Value = (int)r.ToTableResolution;
            pCorrKind.Value = r.CorrespondenceKind.ToString();
            pCorrGroup.Value = (object?)r.CorrespondenceGroupId ?? DBNull.Value;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DeclaredRelationRecord>> GetDeclaredRelationsAsync(
        string? sourceType = null,
        string? fromTable = null,
        string? toTable = null,
        CancellationToken cancellationToken = default)
    {
        await using var cmd = _connection.CreateCommand();
        var sql =
            """
            SELECT relation_id, source_type, source_record_id, from_table, from_table_name,
                   to_table, to_table_name, original_expression, created_by, created_at,
                   modified_by, modified_at, is_marked, raw_metadata_json, collected_at_utc,
                   from_object_id, to_object_id, from_table_resolved, to_table_resolved,
                   correspondence_kind, correspondence_group_id
            FROM declared_relations
            WHERE 1=1
            """;
        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            sql += " AND source_type = $source";
            cmd.Parameters.AddWithValue("$source", sourceType);
        }

        if (!string.IsNullOrWhiteSpace(fromTable))
        {
            sql += " AND from_table = $from COLLATE NOCASE";
            cmd.Parameters.AddWithValue("$from", fromTable);
        }

        if (!string.IsNullOrWhiteSpace(toTable))
        {
            sql += " AND to_table = $to COLLATE NOCASE";
            cmd.Parameters.AddWithValue("$to", toTable);
        }

        sql += " ORDER BY source_type, from_table, to_table, relation_id;";
        cmd.CommandText = sql;

        var list = new List<DeclaredRelationRecord>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var corr = Enum.TryParse<DeclaredRelationCorrespondenceKind>(reader.GetString(19), out var k)
                ? k
                : DeclaredRelationCorrespondenceKind.None;
            list.Add(new DeclaredRelationRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9)),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : DateTimeOffset.Parse(reader.GetString(11)),
                reader.IsDBNull(12) ? null : reader.GetInt64(12) != 0,
                reader.IsDBNull(13) ? null : reader.GetString(13),
                DateTimeOffset.Parse(reader.GetString(14)),
                reader.IsDBNull(15) ? null : reader.GetString(15),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                ReadTableResolution(reader.GetInt64(17)),
                ReadTableResolution(reader.GetInt64(18)),
                corr,
                reader.IsDBNull(20) ? null : reader.GetString(20)));
        }

        return list;
    }

    public async Task UpsertDeclaredRelationInterpretationsAsync(
        IEnumerable<DeclaredRelationInterpretationRecord> records,
        CancellationToken cancellationToken = default)
    {
        var list = records as IList<DeclaredRelationInterpretationRecord> ?? records.ToList();
        if (list.Count == 0)
        {
            return;
        }

        await using var tx = (SqliteTransaction)await _connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var cmd = _connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT INTO declared_relation_interpretations(
              relation_id, parser_version, parse_status, confidence, ast_json, diagnostics_json,
              field_ref_count, table_refs_resolved, table_refs_ambiguous, table_refs_unresolved,
              interpreted_at_utc)
            VALUES (
              $id, $ver, $status, $conf, $ast, $diag,
              $fields, $resolved, $amb, $unresolved, $at)
            ON CONFLICT(relation_id) DO UPDATE SET
              parser_version=excluded.parser_version,
              parse_status=excluded.parse_status,
              confidence=excluded.confidence,
              ast_json=excluded.ast_json,
              diagnostics_json=excluded.diagnostics_json,
              field_ref_count=excluded.field_ref_count,
              table_refs_resolved=excluded.table_refs_resolved,
              table_refs_ambiguous=excluded.table_refs_ambiguous,
              table_refs_unresolved=excluded.table_refs_unresolved,
              interpreted_at_utc=excluded.interpreted_at_utc;
            """;

        var pId = cmd.Parameters.Add("$id", SqliteType.Text);
        var pVer = cmd.Parameters.Add("$ver", SqliteType.Text);
        var pStatus = cmd.Parameters.Add("$status", SqliteType.Text);
        var pConf = cmd.Parameters.Add("$conf", SqliteType.Text);
        var pAst = cmd.Parameters.Add("$ast", SqliteType.Text);
        var pDiag = cmd.Parameters.Add("$diag", SqliteType.Text);
        var pFields = cmd.Parameters.Add("$fields", SqliteType.Integer);
        var pResolved = cmd.Parameters.Add("$resolved", SqliteType.Integer);
        var pAmb = cmd.Parameters.Add("$amb", SqliteType.Integer);
        var pUnresolved = cmd.Parameters.Add("$unresolved", SqliteType.Integer);
        var pAt = cmd.Parameters.Add("$at", SqliteType.Text);

        foreach (var r in list)
        {
            pId.Value = r.RelationId;
            pVer.Value = r.ParserVersion;
            pStatus.Value = r.ParseStatus.ToString();
            pConf.Value = r.Confidence.ToString();
            pAst.Value = r.Ast is null
                ? DBNull.Value
                : JsonSerializer.Serialize(r.Ast, InterpretationJson.Options);
            pDiag.Value = (object?)r.DiagnosticsJson ?? DBNull.Value;
            pFields.Value = r.FieldRefCount;
            pResolved.Value = r.TableRefsResolved;
            pAmb.Value = r.TableRefsAmbiguous;
            pUnresolved.Value = r.TableRefsUnresolved;
            pAt.Value = r.InterpretedAtUtc.ToString("O");
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DeclaredRelationInterpretationRecord>> GetDeclaredRelationInterpretationsAsync(
        string? relationId = null,
        CancellationToken cancellationToken = default)
    {
        await using var cmd = _connection.CreateCommand();
        var sql =
            """
            SELECT relation_id, parser_version, parse_status, confidence, ast_json, diagnostics_json,
                   field_ref_count, table_refs_resolved, table_refs_ambiguous, table_refs_unresolved,
                   interpreted_at_utc
            FROM declared_relation_interpretations
            WHERE 1=1
            """;
        if (!string.IsNullOrWhiteSpace(relationId))
        {
            sql += " AND relation_id = $id";
            cmd.Parameters.AddWithValue("$id", relationId);
        }

        sql += " ORDER BY relation_id;";
        cmd.CommandText = sql;

        var list = new List<DeclaredRelationInterpretationRecord>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var status = Enum.TryParse<ExpressionParseStatus>(reader.GetString(2), out var st)
                ? st
                : ExpressionParseStatus.Invalid;
            var confidence = Enum.TryParse<InterpretationConfidence>(reader.GetString(3), out var conf)
                ? conf
                : InterpretationConfidence.None;
            ExprAstNode? ast = null;
            if (!reader.IsDBNull(4))
            {
                ast = JsonSerializer.Deserialize<ExprAstNode>(reader.GetString(4), InterpretationJson.Options);
            }

            list.Add(new DeclaredRelationInterpretationRecord(
                reader.GetString(0),
                reader.GetString(1),
                status,
                confidence,
                ast,
                reader.IsDBNull(5) ? null : reader.GetString(5),
                (int)reader.GetInt64(6),
                (int)reader.GetInt64(7),
                (int)reader.GetInt64(8),
                (int)reader.GetInt64(9),
                DateTimeOffset.Parse(reader.GetString(10))));
        }

        return list;
    }

    public async Task<WorkspaceSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        string label = string.Empty;
        string status = "unknown";
        var created = DateTimeOffset.UtcNow;

        await using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "SELECT scan_id, installation_label, status, created_at_utc FROM scan_meta LIMIT 1;";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                ScanId = reader.GetString(0);
                label = reader.GetString(1);
                status = reader.GetString(2);
                created = DateTimeOffset.Parse(reader.GetString(3));
            }
        }

        var counts = await GetObjectCountsByKindAsync(cancellationToken).ConfigureAwait(false);
        var issues = await GetIssuesAsync(cancellationToken).ConfigureAwait(false);
        var relations = await GetRelationsAsync(cancellationToken).ConfigureAwait(false);

        return new WorkspaceSummary(
            ScanId,
            label,
            status,
            created,
            counts,
            issues.Count,
            issues.Count(i => i.Severity == CollectionIssueSeverity.Warning),
            issues.Count(i => i.Severity == CollectionIssueSeverity.Error),
            relations.Count);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS scan_meta (
              scan_id TEXT PRIMARY KEY,
              installation_label TEXT NOT NULL,
              status TEXT NOT NULL,
              created_at_utc TEXT NOT NULL,
              capture_id TEXT,
              capabilities_json TEXT,
              coverage_json TEXT
            );
            CREATE TABLE IF NOT EXISTS objects (
              object_id TEXT PRIMARY KEY,
              kind TEXT NOT NULL,
              name TEXT NOT NULL,
              schema_name TEXT,
              parent_object_id TEXT,
              collector_id TEXT NOT NULL,
              definition_artifact_id TEXT,
              summary_json TEXT,
              collected_at_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS artifacts (
              artifact_id TEXT PRIMARY KEY,
              collector_id TEXT NOT NULL,
              relative_path TEXT NOT NULL,
              content_type TEXT NOT NULL,
              sha256 TEXT NOT NULL,
              byte_length INTEGER NOT NULL,
              metadata_json TEXT
            );
            CREATE TABLE IF NOT EXISTS issues (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              collector_id TEXT,
              severity TEXT NOT NULL,
              code TEXT NOT NULL,
              message TEXT NOT NULL,
              object_name TEXT,
              detail TEXT,
              created_at_utc TEXT NOT NULL,
              issue_id TEXT,
              operation_id TEXT
            );
            CREATE TABLE IF NOT EXISTS relations (
              relation_id TEXT PRIMARY KEY,
              relation_type TEXT NOT NULL,
              from_object_id TEXT NOT NULL,
              to_object_id TEXT NOT NULL,
              evidence TEXT NOT NULL,
              layer TEXT NOT NULL,
              confidence REAL,
              explanation TEXT,
              validation_state TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS declared_relations (
              relation_id TEXT PRIMARY KEY,
              source_type TEXT NOT NULL,
              source_record_id TEXT,
              from_table TEXT NOT NULL,
              from_table_name TEXT,
              to_table TEXT NOT NULL,
              to_table_name TEXT,
              original_expression TEXT NOT NULL,
              created_by TEXT,
              created_at TEXT,
              modified_by TEXT,
              modified_at TEXT,
              is_marked INTEGER,
              raw_metadata_json TEXT,
              collected_at_utc TEXT NOT NULL,
              from_object_id TEXT,
              to_object_id TEXT,
              from_table_resolved INTEGER NOT NULL,
              to_table_resolved INTEGER NOT NULL,
              correspondence_kind TEXT NOT NULL,
              correspondence_group_id TEXT
            );
            CREATE TABLE IF NOT EXISTS declared_relation_interpretations (
              relation_id TEXT PRIMARY KEY,
              parser_version TEXT NOT NULL,
              parse_status TEXT NOT NULL,
              confidence TEXT NOT NULL,
              ast_json TEXT,
              diagnostics_json TEXT,
              field_ref_count INTEGER NOT NULL,
              table_refs_resolved INTEGER NOT NULL,
              table_refs_ambiguous INTEGER NOT NULL,
              table_refs_unresolved INTEGER NOT NULL,
              interpreted_at_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_objects_kind ON objects(kind);
            CREATE INDEX IF NOT EXISTS ix_objects_name ON objects(name);
            CREATE INDEX IF NOT EXISTS ix_declared_from ON declared_relations(from_table);
            CREATE INDEX IF NOT EXISTS ix_declared_to ON declared_relations(to_table);
            CREATE INDEX IF NOT EXISTS ix_declared_source ON declared_relations(source_type);
            CREATE INDEX IF NOT EXISTS ix_decl_interp_status ON declared_relation_interpretations(parse_status);
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // Migração mínima para workspaces já criados antes de CaptureId/coverage.
        await EnsureColumnAsync("scan_meta", "capture_id", "TEXT", cancellationToken).ConfigureAwait(false);
        await EnsureColumnAsync("scan_meta", "capabilities_json", "TEXT", cancellationToken).ConfigureAwait(false);
        await EnsureColumnAsync("scan_meta", "coverage_json", "TEXT", cancellationToken).ConfigureAwait(false);
        await EnsureColumnAsync("issues", "issue_id", "TEXT", cancellationToken).ConfigureAwait(false);
        await EnsureColumnAsync("issues", "operation_id", "TEXT", cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureColumnAsync(
        string table,
        string column,
        string typeSql,
        CancellationToken cancellationToken)
    {
        await using var info = _connection.CreateCommand();
        info.CommandText = $"PRAGMA table_info({table});";
        await using var reader = await info.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await using var alter = _connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {typeSql};";
        await alter.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Expande tokens curtos de tabelas/ecrãs PHC.
    /// Ex.: <c>FT</c> → FT, SFT, FT2, FT3; <c>BO</c> → BO, SBO, BO2, BO3.
    /// </summary>
    internal static IReadOnlyList<string> ExpandPhcSearchTokens(string search)
    {
        var raw = search.Trim();
        if (raw.Length == 0)
        {
            return Array.Empty<string>();
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { raw };
        // Token único alfanumérico curto (ex. FT, BO, CL, ST, SFT)
        if (raw.Length is >= 2 and <= 8
            && raw.All(c => char.IsLetterOrDigit(c) || c is '_' or '-'))
        {
            // SFT → FT; FT → FT (não cortar SST para T)
            var core = raw.Length >= 3
                       && (raw[0] is 'S' or 's')
                       && char.IsLetter(raw[1])
                ? raw[1..]
                : raw;

            if (core.Length >= 2)
            {
                set.Add(core);
                set.Add("S" + core);
                set.Add(core + "2");
                set.Add(core + "3");
                set.Add("S" + core + "2");
                set.Add("S" + core + "3");
            }
        }

        return set.ToList();
    }

    private static TechnicalObjectRecord ReadObject(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            DateTimeOffset.Parse(reader.GetString(8)));

    private static TableReferenceResolution ReadTableResolution(long value) =>
        value switch
        {
            1 => TableReferenceResolution.Resolved,
            2 => TableReferenceResolution.Ambiguous,
            _ => TableReferenceResolution.Unresolved
        };
}
