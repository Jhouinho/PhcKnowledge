using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;

namespace PhcKnowledgeScanner.Export.SchemaSnapshot;

/// <summary>
/// Escreve um Database Schema Snapshot em pasta versionável (e opcionalmente ZIP).
/// </summary>
public sealed class DatabaseSchemaSnapshotWriter
{
    private readonly SchemaSnapshotPrivacyValidator _privacy = new();

    public async Task<SchemaSnapshotWriteResult> WriteAsync(
        DatabaseSchemaSnapshot snapshot,
        string outputDirectory,
        SchemaSnapshotOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        options ??= new SchemaSnapshotOptions();

        RefreshManifestCounts(snapshot);
        snapshot.Manifest.SnapshotHash = SchemaSnapshotHasher.Compute(snapshot);
        var privacy = _privacy.Validate(snapshot);
        if (options.FailOnPrivacyFindings && !privacy.IsSafe)
        {
            return new SchemaSnapshotWriteResult(false, outputDirectory, null, privacy, snapshot.Manifest.SnapshotHash);
        }

        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, recursive: true);
        }

        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(Path.Combine(outputDirectory, "tables"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "relationships"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "indexes"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "views"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "triggers"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "procedures"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "functions"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "types"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "synonyms"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "documentation"));

        await WriteJsonAsync(Path.Combine(outputDirectory, "manifest.json"), snapshot.Manifest, cancellationToken)
            .ConfigureAwait(false);

        foreach (var table in snapshot.Tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = SanitizeFileName($"{table.SchemaName}.{table.Name}") + ".json";
            await WriteJsonAsync(Path.Combine(outputDirectory, "tables", fileName), table, cancellationToken)
                .ConfigureAwait(false);
        }

        await WriteJsonAsync(
            Path.Combine(outputDirectory, "relationships", "relationships.json"),
            snapshot.Relationships,
            cancellationToken).ConfigureAwait(false);

        await WriteJsonAsync(
            Path.Combine(outputDirectory, "indexes", "indexes.json"),
            snapshot.Indexes,
            cancellationToken).ConfigureAwait(false);

        await WriteJsonAsync(
            Path.Combine(outputDirectory, "views", "views.json"),
            snapshot.Views,
            cancellationToken).ConfigureAwait(false);

        await WriteJsonAsync(
            Path.Combine(outputDirectory, "triggers", "triggers.json"),
            snapshot.Triggers,
            cancellationToken).ConfigureAwait(false);

        await WriteJsonAsync(
            Path.Combine(outputDirectory, "procedures", "procedures.json"),
            snapshot.Procedures,
            cancellationToken).ConfigureAwait(false);

        await WriteJsonAsync(
            Path.Combine(outputDirectory, "functions", "functions.json"),
            snapshot.Functions,
            cancellationToken).ConfigureAwait(false);

        await WriteJsonAsync(
            Path.Combine(outputDirectory, "types", "user_types.json"),
            snapshot.UserTypes,
            cancellationToken).ConfigureAwait(false);

        await WriteJsonAsync(
            Path.Combine(outputDirectory, "synonyms", "synonyms.json"),
            snapshot.Synonyms,
            cancellationToken).ConfigureAwait(false);

        var overview = BuildOverviewMarkdown(snapshot, privacy);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "documentation", "overview.md"),
            overview,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);

        return new SchemaSnapshotWriteResult(true, outputDirectory, null, privacy, snapshot.Manifest.SnapshotHash);
    }

    public async Task<SchemaSnapshotWriteResult> WriteZipAsync(
        DatabaseSchemaSnapshot snapshot,
        string zipPath,
        SchemaSnapshotOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner", "schema-snapshot-" + Guid.NewGuid().ToString("N"));
        try
        {
            var written = await WriteAsync(snapshot, Path.Combine(tempRoot, "database-schema"), options, cancellationToken)
                .ConfigureAwait(false);
            if (!written.Succeeded)
            {
                return written;
            }

            var directory = Path.GetDirectoryName(Path.GetFullPath(zipPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(
                Path.Combine(tempRoot, "database-schema"),
                zipPath,
                CompressionLevel.Optimal,
                includeBaseDirectory: false);

            return written with { ZipPath = zipPath };
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    public static async Task<DatabaseSchemaSnapshot> ReadAsync(
        string snapshotDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotDirectory);
        var snapshot = new DatabaseSchemaSnapshot
        {
            Manifest = await ReadJsonAsync<SchemaSnapshotManifest>(
                Path.Combine(snapshotDirectory, "manifest.json"), cancellationToken).ConfigureAwait(false)
                ?? new SchemaSnapshotManifest()
        };

        var tablesDir = Path.Combine(snapshotDirectory, "tables");
        if (Directory.Exists(tablesDir))
        {
            foreach (var file in Directory.EnumerateFiles(tablesDir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var table = await ReadJsonAsync<SchemaTableSnapshot>(file, cancellationToken).ConfigureAwait(false);
                if (table is not null)
                {
                    snapshot.Tables.Add(table);
                }
            }
        }

        snapshot.Relationships = await ReadJsonAsync<List<SchemaForeignKeySnapshot>>(
            Path.Combine(snapshotDirectory, "relationships", "relationships.json"), cancellationToken).ConfigureAwait(false)
            ?? [];
        snapshot.Indexes = await ReadJsonAsync<List<SchemaIndexSnapshot>>(
            Path.Combine(snapshotDirectory, "indexes", "indexes.json"), cancellationToken).ConfigureAwait(false)
            ?? [];
        snapshot.Views = await ReadJsonAsync<List<SchemaViewSnapshot>>(
            Path.Combine(snapshotDirectory, "views", "views.json"), cancellationToken).ConfigureAwait(false)
            ?? [];
        snapshot.Triggers = await ReadJsonAsync<List<SchemaTriggerSnapshot>>(
            Path.Combine(snapshotDirectory, "triggers", "triggers.json"), cancellationToken).ConfigureAwait(false)
            ?? [];
        snapshot.Procedures = await ReadJsonAsync<List<SchemaRoutineSnapshot>>(
            Path.Combine(snapshotDirectory, "procedures", "procedures.json"), cancellationToken).ConfigureAwait(false)
            ?? [];
        snapshot.Functions = await ReadJsonAsync<List<SchemaRoutineSnapshot>>(
            Path.Combine(snapshotDirectory, "functions", "functions.json"), cancellationToken).ConfigureAwait(false)
            ?? [];
        snapshot.UserTypes = await ReadJsonAsync<List<SchemaUserTypeSnapshot>>(
            Path.Combine(snapshotDirectory, "types", "user_types.json"), cancellationToken).ConfigureAwait(false)
            ?? [];
        snapshot.Synonyms = await ReadJsonAsync<List<SchemaSynonymSnapshot>>(
            Path.Combine(snapshotDirectory, "synonyms", "synonyms.json"), cancellationToken).ConfigureAwait(false)
            ?? [];

        return snapshot;
    }

    private static void RefreshManifestCounts(DatabaseSchemaSnapshot snapshot)
    {
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
            .Concat(snapshot.Triggers.Select(t => t.Classification.Classification))
            .ToList();

        snapshot.Manifest.Classification = new SchemaSnapshotClassificationSummary
        {
            SqlSystem = classifications.Count(c => c == SchemaObjectClassification.SqlSystem),
            CandidateCustom = classifications.Count(c => c == SchemaObjectClassification.CandidateCustom),
            Unknown = classifications.Count(c => c == SchemaObjectClassification.Unknown),
            Policy = snapshot.Manifest.Classification.Policy
        };
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken ct)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, value, SchemaSnapshotJson.Options, ct).ConfigureAwait(false);
    }

    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, SchemaSnapshotJson.Options, ct).ConfigureAwait(false);
    }

    private static string BuildOverviewMarkdown(DatabaseSchemaSnapshot snapshot, SchemaSnapshotPrivacyReport privacy)
    {
        var m = snapshot.Manifest;
        var sb = new StringBuilder();
        sb.AppendLine("# Database Schema Snapshot");
        sb.AppendLine();
        sb.AppendLine("Snapshot estrutural de metadados SQL Server (sem registos de negócio).");
        sb.AppendLine();
        sb.AppendLine("| Campo | Valor |");
        sb.AppendLine("| --- | --- |");
        sb.AppendLine($"| SchemaVersion | {m.SchemaVersion} |");
        sb.AppendLine($"| ScannerVersion | {m.ScannerVersion} |");
        sb.AppendLine($"| CollectedAtUtc | {m.CollectedAtUtc:O} |");
        sb.AppendLine($"| OriginFingerprint | {m.OriginFingerprint} |");
        sb.AppendLine($"| SnapshotHash | {m.SnapshotHash} |");
        sb.AppendLine($"| CompletionStatus | {m.CompletionStatus} |");
        sb.AppendLine($"| IncludesSqlDefinitions | {m.IncludesSqlDefinitions} |");
        sb.AppendLine($"| SafeForPublicShare | {m.SafeForPublicShare} |");
        sb.AppendLine($"| Tables | {m.ObjectCounts.Tables} |");
        sb.AppendLine($"| Columns | {m.ObjectCounts.Columns} |");
        sb.AppendLine($"| ForeignKeys | {m.ObjectCounts.ForeignKeys} |");
        sb.AppendLine($"| Indexes | {m.ObjectCounts.Indexes} |");
        sb.AppendLine();
        sb.AppendLine("## Classificação");
        sb.AppendLine();
        sb.AppendLine(m.Classification.Policy);
        sb.AppendLine();
        sb.AppendLine($"- sql_system: {m.Classification.SqlSystem}");
        sb.AppendLine($"- candidate_custom: {m.Classification.CandidateCustom}");
        sb.AppendLine($"- unknown: {m.Classification.Unknown}");
        sb.AppendLine();
        sb.AppendLine("## Privacidade");
        sb.AppendLine();
        sb.AppendLine(privacy.IsSafe
            ? "Validação de privacidade: OK (modo seguro)."
            : "Validação de privacidade: FALHOU — não publicar automaticamente.");
        foreach (var finding in privacy.Findings)
        {
            sb.AppendLine($"- [{finding.Severity}] {finding.Code}: {finding.Message}");
        }

        if (m.Warnings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Avisos");
            sb.AppendLine();
            foreach (var warning in m.Warnings)
            {
                sb.AppendLine("- " + warning);
            }
        }

        if (m.Limitations.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Limitações");
            sb.AppendLine();
            foreach (var limitation in m.Limitations)
            {
                sb.AppendLine("- " + limitation);
            }
        }

        return sb.ToString();
    }

    private static string SanitizeFileName(string name)
    {
        var t = Regex.Replace(name, "[\\\\/:*?\"<>|]+", "_");
        return string.IsNullOrWhiteSpace(t) ? "object" : t;
    }
}

public sealed record SchemaSnapshotWriteResult(
    bool Succeeded,
    string OutputDirectory,
    string? ZipPath,
    SchemaSnapshotPrivacyReport Privacy,
    string SnapshotHash);
