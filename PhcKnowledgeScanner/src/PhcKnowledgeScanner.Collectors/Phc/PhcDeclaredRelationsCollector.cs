using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Collectors.Coverage;

namespace PhcKnowledgeScanner.Collectors.Phc;

/// <summary>
/// Recolhe factos de relações declaradas PHC (<c>dbo.rel</c>, <c>dbo.dtl</c>) — Camada A.
/// Não deriva FKs nem faz parsing SQL das expressões.
/// </summary>
public sealed class PhcDeclaredRelationsCollector : ICollector
{
    public const string CollectorId = "phc.declared-relations";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public string Id => CollectorId;
    public string DisplayName => "Relações declaradas PHC (rel/dtl)";
    public IReadOnlyList<DeclaredCapability> DeclaredCapabilities => ScannerCapabilityCatalog.ForPhcDeclaredRelations;

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
            var hasRel = await TableExistsAsync(connection, "rel", cancellationToken).ConfigureAwait(false);
            var hasDtl = await TableExistsAsync(connection, "dtl", cancellationToken).ConfigureAwait(false);
            if (!hasRel && !hasDtl)
            {
                return new CollectorCapability(false, "Tabelas dbo.rel e dbo.dtl não encontradas.");
            }

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
        var records = new List<DeclaredRelationRecord>();

        await using var connection = new SqlConnection(context.SqlConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var tracker = new OperationCoverageTracker(issues);
        var caps = DeclaredCapabilities.ToDictionary(c => c.OperationId, StringComparer.OrdinalIgnoreCase);
        var relCount = 0;
        var dtlCount = 0;

        async Task Report(string message)
        {
            context.Progress?.Report(new ScanProgressEvent(
                "collector.running",
                Id,
                DisplayName,
                message,
                records.Count,
                records.Count,
                issues.Count(i => i.Severity == CollectionIssueSeverity.Warning),
                issues.Count(i => i.Severity == CollectionIssueSeverity.Error),
                null,
                sw.Elapsed));
            await Task.CompletedTask;
        }

        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcRel],
                () => TableExistsAsync(connection, "rel", cancellationToken),
                async () =>
                {
                    var relRows = await CollectRelAsync(connection, context, collectedAt, cancellationToken)
                        .ConfigureAwait(false);
                    records.AddRange(relRows);
                    relCount = relRows.Count;
                    await Report($"dbo.rel: {relRows.Count} registos.").ConfigureAwait(false);
                    if (relRows.Count == 0)
                    {
                        issues.Add(new CollectionIssue(
                            CollectionIssueSeverity.Info,
                            "phc.rel.empty",
                            "Tabela dbo.rel existe mas não contém registos."));
                    }
                },
                () => relCount,
                "dbo.rel")
            .ConfigureAwait(false);

        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcDtl],
                () => TableExistsAsync(connection, "dtl", cancellationToken),
                async () =>
                {
                    var dtlRows = await CollectDtlAsync(connection, context, collectedAt, cancellationToken)
                        .ConfigureAwait(false);
                    records.AddRange(dtlRows);
                    dtlCount = dtlRows.Count;
                    await Report($"dbo.dtl: {dtlRows.Count} registos.").ConfigureAwait(false);
                    if (dtlRows.Count == 0)
                    {
                        issues.Add(new CollectionIssue(
                            CollectionIssueSeverity.Info,
                            "phc.dtl.empty",
                            "Tabela dbo.dtl existe mas não contém registos."));
                    }
                },
                () => dtlCount,
                "dbo.dtl")
            .ConfigureAwait(false);

        var withCorrespondence = DeclaredRelationCorrespondence.Apply(records);
        await context.Workspace.UpsertDeclaredRelationsAsync(withCorrespondence, cancellationToken)
            .ConfigureAwait(false);

        var exact = withCorrespondence.Count(r => r.CorrespondenceKind == DeclaredRelationCorrespondenceKind.Exact) / 2;
        var probable = withCorrespondence.Count(r => r.CorrespondenceKind == DeclaredRelationCorrespondenceKind.Probable) / 2;
        var fromResolved = withCorrespondence.Count(r => r.FromTableResolution == TableReferenceResolution.Resolved);
        var toResolved = withCorrespondence.Count(r => r.ToTableResolution == TableReferenceResolution.Resolved);
        var fromAmbiguous = withCorrespondence.Count(r => r.FromTableResolution == TableReferenceResolution.Ambiguous);
        var toAmbiguous = withCorrespondence.Count(r => r.ToTableResolution == TableReferenceResolution.Ambiguous);
        var fromUnresolved = withCorrespondence.Count(r => r.FromTableResolution == TableReferenceResolution.Unresolved);
        var toUnresolved = withCorrespondence.Count(r => r.ToTableResolution == TableReferenceResolution.Unresolved);
        var factsWithGap = withCorrespondence.Count(r =>
            r.FromTableResolution != TableReferenceResolution.Resolved
            || r.ToTableResolution != TableReferenceResolution.Resolved);

        await Report(
                $"Persistidos: {withCorrespondence.Count}. Correspondências exactas≈{exact}, prováveis≈{probable}. "
                + $"Refs resolvidas from/to={fromResolved}/{toResolved}; "
                + $"ambíguas={fromAmbiguous}/{toAmbiguous}; "
                + $"não resolvidas={fromUnresolved}/{toUnresolved}.")
            .ConfigureAwait(false);

        if (factsWithGap > 0)
        {
            issues.Add(new CollectionIssue(
                CollectionIssueSeverity.Info,
                "phc.declared.unresolved_tables",
                $"{factsWithGap} factos têm pelo menos uma referência a tabela não resolvida ou ambígua (relação original preservada)."));
        }

        if (fromAmbiguous + toAmbiguous > 0)
        {
            issues.Add(new CollectionIssue(
                CollectionIssueSeverity.Info,
                "phc.declared.ambiguous_tables",
                $"{fromAmbiguous + toAmbiguous} referências ambíguas (mesmo nome em vários schemas) — nenhuma associação arbitrária."));
        }

        foreach (var issue in issues)
        {
            await context.Workspace.AddIssueAsync(Id, issue, cancellationToken).ConfigureAwait(false);
        }

        var status = issues.Any(i => i.Severity == CollectionIssueSeverity.Error)
            ? (withCorrespondence.Count > 0 ? CollectorStatus.Partial : CollectorStatus.Failed)
            : issues.Any(i => i.Severity == CollectionIssueSeverity.Warning)
                ? CollectorStatus.SucceededWithWarnings
                : CollectorStatus.Succeeded;

        return new CollectorResult(Id, status, Array.Empty<CollectedArtifact>(), issues, sw.Elapsed, tracker.Coverages);
    }

    private static async Task<List<DeclaredRelationRecord>> CollectRelAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        CancellationToken ct)
    {
        var cols = await GetColumnsAsync(connection, "rel", ct).ConfigureAwait(false);
        RequireColumns(cols, "rel", "tori", "tdes", "tliga");

        var select = BuildSelect(cols, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["relstamp"] = "relstamp",
            ["relid"] = "relid",
            ["tori"] = "tori",
            ["torinome"] = "torinome",
            ["tdes"] = "tdes",
            ["tdesnome"] = "tdesnome",
            ["tliga"] = "tliga",
            ["ousrinis"] = "ousrinis",
            ["ousrdata"] = "ousrdata",
            ["ousrhora"] = "ousrhora",
            ["usrinis"] = "usrinis",
            ["usrdata"] = "usrdata",
            ["usrhora"] = "usrhora",
            ["marcada"] = "marcada",
            ["camposlog"] = "camposlog"
        });

        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 120;
        cmd.CommandText = $"SELECT {select} FROM dbo.rel";
        var drafts = new List<DeclaredRelationRecord>();
        await using (var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
        {
            var map = BuildOrdinalMap(reader);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var stamp = GetString(reader, map, "relstamp");
                var tori = GetString(reader, map, "tori");
                var tdes = GetString(reader, map, "tdes");
                var tliga = GetString(reader, map, "tliga") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(tori) || string.IsNullOrWhiteSpace(tdes))
                {
                    continue;
                }

                var sourceId = string.IsNullOrWhiteSpace(stamp) ? null : stamp.Trim();
                var relationId = sourceId is not null
                    ? $"{DeclaredRelationSourceTypes.PhcRel}:{sourceId}"
                    : $"{DeclaredRelationSourceTypes.PhcRel}:hash:{StableHash(tori, tdes, tliga)}";

                var raw = new Dictionary<string, object?>();
                PutIfPresent(raw, "relid", GetObject(reader, map, "relid"));
                PutIfPresent(raw, "ousrhora", GetString(reader, map, "ousrhora"));
                PutIfPresent(raw, "usrhora", GetString(reader, map, "usrhora"));
                PutIfPresent(raw, "camposlog", GetString(reader, map, "camposlog"));

                drafts.Add(new DeclaredRelationRecord(
                    relationId,
                    DeclaredRelationSourceTypes.PhcRel,
                    sourceId,
                    tori.Trim(),
                    NullIfEmpty(GetString(reader, map, "torinome")),
                    tdes.Trim(),
                    NullIfEmpty(GetString(reader, map, "tdesnome")),
                    tliga,
                    NullIfEmpty(GetString(reader, map, "ousrinis")),
                    CombineDateTime(GetDateTime(reader, map, "ousrdata"), GetString(reader, map, "ousrhora")),
                    NullIfEmpty(GetString(reader, map, "usrinis")),
                    CombineDateTime(GetDateTime(reader, map, "usrdata"), GetString(reader, map, "usrhora")),
                    GetBool(reader, map, "marcada"),
                    raw.Count == 0 ? null : JsonSerializer.Serialize(raw, JsonOptions),
                    collectedAt,
                    null,
                    null,
                    TableReferenceResolution.Unresolved,
                    TableReferenceResolution.Unresolved,
                    DeclaredRelationCorrespondenceKind.None,
                    null));
            }
        }

        return await AttachResolutionsAsync(context, drafts, ct).ConfigureAwait(false);
    }

    private static async Task<List<DeclaredRelationRecord>> CollectDtlAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        CancellationToken ct)
    {
        var cols = await GetColumnsAsync(connection, "dtl", ct).ConfigureAwait(false);
        RequireColumns(cols, "dtl", "tabori", "tabdest", "expliga");

        var select = BuildSelect(cols, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["dtlstamp"] = "dtlstamp",
            ["tabori"] = "tabori",
            ["nmtabori"] = "nmtabori",
            ["tabdest"] = "tabdest",
            ["nmtabdest"] = "nmtabdest",
            ["expliga"] = "expliga",
            ["ousrinis"] = "ousrinis",
            ["ousrdata"] = "ousrdata",
            ["ousrhora"] = "ousrhora",
            ["usrinis"] = "usrinis",
            ["usrdata"] = "usrdata",
            ["usrhora"] = "usrhora",
            ["marcada"] = "marcada"
        });

        await using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = 120;
        cmd.CommandText = $"SELECT {select} FROM dbo.dtl";
        var drafts = new List<DeclaredRelationRecord>();
        await using (var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
        {
            var map = BuildOrdinalMap(reader);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var stamp = GetString(reader, map, "dtlstamp");
                var from = GetString(reader, map, "tabori");
                var to = GetString(reader, map, "tabdest");
                var expr = GetString(reader, map, "expliga") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                {
                    continue;
                }

                var sourceId = string.IsNullOrWhiteSpace(stamp) ? null : stamp.Trim();
                var relationId = sourceId is not null
                    ? $"{DeclaredRelationSourceTypes.PhcDtl}:{sourceId}"
                    : $"{DeclaredRelationSourceTypes.PhcDtl}:hash:{StableHash(from, to, expr)}";

                drafts.Add(new DeclaredRelationRecord(
                    relationId,
                    DeclaredRelationSourceTypes.PhcDtl,
                    sourceId,
                    from.Trim(),
                    NullIfEmpty(GetString(reader, map, "nmtabori")),
                    to.Trim(),
                    NullIfEmpty(GetString(reader, map, "nmtabdest")),
                    expr,
                    NullIfEmpty(GetString(reader, map, "ousrinis")),
                    CombineDateTime(GetDateTime(reader, map, "ousrdata"), GetString(reader, map, "ousrhora")),
                    NullIfEmpty(GetString(reader, map, "usrinis")),
                    CombineDateTime(GetDateTime(reader, map, "usrdata"), GetString(reader, map, "usrhora")),
                    GetBool(reader, map, "marcada"),
                    null,
                    collectedAt,
                    null,
                    null,
                    TableReferenceResolution.Unresolved,
                    TableReferenceResolution.Unresolved,
                    DeclaredRelationCorrespondenceKind.None,
                    null));
            }
        }

        return await AttachResolutionsAsync(context, drafts, ct).ConfigureAwait(false);
    }

    private static async Task<List<DeclaredRelationRecord>> AttachResolutionsAsync(
        ScanContext context,
        List<DeclaredRelationRecord> drafts,
        CancellationToken ct)
    {
        var tables = await context.Workspace!.GetObjectsAsync(ObjectKind.Table, search: null, ct)
            .ConfigureAwait(false);
        var nameIndex = TableReferenceResolver.BuildNameIndex(tables);
        var knownIds = new HashSet<string>(tables.Select(t => t.ObjectId), StringComparer.OrdinalIgnoreCase);
        var cache = new Dictionary<string, TableReferenceResolver.Result>(StringComparer.OrdinalIgnoreCase);

        var list = new List<DeclaredRelationRecord>(drafts.Count);
        foreach (var d in drafts)
        {
            var from = ResolveCached(d.FromTable, nameIndex, knownIds, cache);
            var to = ResolveCached(d.ToTable, nameIndex, knownIds, cache);
            list.Add(d with
            {
                FromObjectId = from.ObjectId,
                ToObjectId = to.ObjectId,
                FromTableResolution = from.Resolution,
                ToTableResolution = to.Resolution
            });
        }

        return list;
    }

    private static TableReferenceResolver.Result ResolveCached(
        string tableName,
        IReadOnlyDictionary<string, List<string>> nameIndex,
        IReadOnlySet<string> knownIds,
        Dictionary<string, TableReferenceResolver.Result> cache)
    {
        var key = tableName.Trim();
        if (cache.TryGetValue(key, out var hit))
        {
            return hit;
        }

        return cache[key] = TableReferenceResolver.Resolve(key, nameIndex, knownIds);
    }

    private static async Task<HashSet<string>> GetColumnsAsync(
        SqlConnection connection,
        string table,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = @t;
            """;
        cmd.Parameters.AddWithValue("@t", table);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            set.Add(reader.GetString(0));
        }

        return set;
    }

    private static void RequireColumns(HashSet<string> cols, string table, params string[] required)
    {
        var missing = required.Where(r => !cols.Contains(r)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Schema dbo.{table} incompleto; colunas em falta: {string.Join(", ", missing)}");
        }
    }

    private static string BuildSelect(HashSet<string> available, Dictionary<string, string> wanted)
    {
        var parts = new List<string>();
        foreach (var (alias, column) in wanted)
        {
            if (available.Contains(column))
            {
                parts.Add($"{column} AS [{alias}]");
            }
        }

        if (parts.Count == 0)
        {
            throw new InvalidOperationException("Nenhuma coluna seleccionável.");
        }

        return string.Join(", ", parts);
    }

    private static Dictionary<string, int> BuildOrdinalMap(SqlDataReader reader)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            map[reader.GetName(i)] = i;
        }

        return map;
    }

    private static string? GetString(SqlDataReader reader, Dictionary<string, int> map, string name)
    {
        if (!map.TryGetValue(name, out var i) || reader.IsDBNull(i))
        {
            return null;
        }

        return reader.GetValue(i)?.ToString();
    }

    private static object? GetObject(SqlDataReader reader, Dictionary<string, int> map, string name)
    {
        if (!map.TryGetValue(name, out var i) || reader.IsDBNull(i))
        {
            return null;
        }

        return reader.GetValue(i);
    }

    private static DateTime? GetDateTime(SqlDataReader reader, Dictionary<string, int> map, string name)
    {
        if (!map.TryGetValue(name, out var i) || reader.IsDBNull(i))
        {
            return null;
        }

        return reader.GetFieldType(i) == typeof(DateTime)
            ? reader.GetDateTime(i)
            : DateTime.TryParse(reader.GetValue(i)?.ToString(), out var dt) ? dt : null;
    }

    private static bool? GetBool(SqlDataReader reader, Dictionary<string, int> map, string name)
    {
        if (!map.TryGetValue(name, out var i) || reader.IsDBNull(i))
        {
            return null;
        }

        var v = reader.GetValue(i);
        return v switch
        {
            bool b => b,
            byte by => by != 0,
            short s => s != 0,
            int n => n != 0,
            long l => l != 0,
            _ => bool.TryParse(v.ToString(), out var b) ? b : null
        };
    }

    private static DateTimeOffset? CombineDateTime(DateTime? date, string? time)
    {
        if (date is null)
        {
            return null;
        }

        var dt = DateTime.SpecifyKind(date.Value, DateTimeKind.Unspecified);
        if (!string.IsNullOrWhiteSpace(time) && TimeSpan.TryParse(time.Trim(), out var ts))
        {
            dt = dt.Date + ts;
        }

        return new DateTimeOffset(dt, TimeSpan.Zero);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void PutIfPresent(Dictionary<string, object?> map, string key, object? value)
    {
        if (value is not null && value is not DBNull)
        {
            map[key] = value is string s ? s.Trim() : value;
        }
    }

    private static string StableHash(string from, string to, string expr)
    {
        var payload = $"{from.Trim().ToLowerInvariant()}|{to.Trim().ToLowerInvariant()}|{expr.Trim()}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash.AsSpan(0, 10)).ToLowerInvariant();
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT 1
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = @t;
            """;
        cmd.Parameters.AddWithValue("@t", tableName);
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return result is not null && result is not DBNull;
    }
}
