using System.IO.Compression;
using System.Text;
using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Manifest;

namespace PhcKnowledgeScanner.Export;

/// <summary>
/// Exportador ZIP estruturado. Implementação mínima de fundação —
/// a estrutura de pastas por collector será refinada com os primeiros collectors reais.
/// </summary>
public sealed class ZipScanExporter : IScanExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task ExportAsync(ScanManifest manifest, string zipPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(zipPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        await using var fileStream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
        await using (var entryStream = manifestEntry.Open())
        {
            await JsonSerializer.SerializeAsync(entryStream, ToDto(manifest), JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var result in manifest.Results)
        {
            foreach (var artifact in result.Artifacts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relative = NormalizeEntryPath($"collectors/{result.CollectorId}/{artifact.RelativePath}");
                var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(artifact.Content, cancellationToken).ConfigureAwait(false);
            }
        }

        var readme = archive.CreateEntry("README.txt", CompressionLevel.Optimal);
        await using (var entryStream = readme.Open())
        {
            var text = Encoding.UTF8.GetBytes(
                "PhcKnowledgeScanner export package\n" +
                $"InstallationId: {manifest.InstallationId}\n" +
                $"StartedAtUtc: {manifest.StartedAtUtc:O}\n" +
                $"FinishedAtUtc: {manifest.FinishedAtUtc:O}\n" +
                $"Mode: {manifest.Mode}\n" +
                $"ScannerVersion: {manifest.ScannerVersion}\n");
            await entryStream.WriteAsync(text, cancellationToken).ConfigureAwait(false);
        }
    }

    private static object ToDto(ScanManifest manifest) => new
    {
        manifest.InstallationId,
        StartedAtUtc = manifest.StartedAtUtc,
        FinishedAtUtc = manifest.FinishedAtUtc,
        Mode = manifest.Mode.ToString(),
        manifest.ScannerVersion,
        Results = manifest.Results.Select(r => new
        {
            r.CollectorId,
            Status = r.Status.ToString(),
            DurationMs = (long)r.Duration.TotalMilliseconds,
            ArtifactCount = r.Artifacts.Count,
            Issues = r.Issues.Select(i => new
            {
                Severity = i.Severity.ToString(),
                i.Code,
                i.Message,
                i.ObjectName,
                i.Detail
            }),
            Artifacts = r.Artifacts.Select(a => new
            {
                a.RelativePath,
                a.ContentType,
                ByteLength = a.Content.Length,
                a.Metadata
            })
        })
    };

    private static string NormalizeEntryPath(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(path))
        {
            throw new InvalidOperationException($"Caminho de artefacto inválido: '{path}'.");
        }

        return normalized;
    }
}
