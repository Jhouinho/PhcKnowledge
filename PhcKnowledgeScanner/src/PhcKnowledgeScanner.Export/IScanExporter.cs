using PhcKnowledgeScanner.Abstractions.Manifest;

namespace PhcKnowledgeScanner.Export;

public interface IScanExporter
{
    /// <summary>
    /// Empacota o manifesto e artefactos num ZIP estruturado.
    /// </summary>
    Task ExportAsync(ScanManifest manifest, string zipPath, CancellationToken cancellationToken = default);
}
