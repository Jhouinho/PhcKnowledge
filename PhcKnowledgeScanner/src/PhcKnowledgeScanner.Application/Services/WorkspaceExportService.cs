using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using PhcKnowledgeScanner.Abstractions.Inventory;

namespace PhcKnowledgeScanner.Application.Services;

/// <summary>
/// Exporta apenas o pacote de personalizações PHC (pastas fixas, sem estrutura SQL / metadata).
/// </summary>
public sealed class WorkspaceExportService : IWorkspaceExportService
{
    private readonly IScanAppService _scanAppService;

    /// <summary>Pastas do ZIP — alinhadas com a captura de personalizações PHC.</summary>
    private static readonly string[] ExportFolders =
    [
        "brule",
        "ebrule",
        "udefs",
        "eudefs",
        "eventos",
        "eeventos",
        "teclas",
        "campos",
        "formc",
        "formb",
        "ftu"
    ];

    private static readonly Dictionary<string, string> KindToFolder = new(StringComparer.OrdinalIgnoreCase)
    {
        [ObjectKind.PhcUserRule] = "brule",
        [ObjectKind.PhcWebUserRule] = "ebrule",
        [ObjectKind.PhcDefaultValue] = "udefs",
        [ObjectKind.PhcWebDefaultValue] = "eudefs",
        [ObjectKind.PhcEvent] = "eventos",
        [ObjectKind.PhcWebEvent] = "eeventos",
        [ObjectKind.PhcHotkey] = "teclas",
        [ObjectKind.PhcUserField] = "campos",
        [ObjectKind.PhcHiddenGrid] = "formc",
        [ObjectKind.PhcCustomScreen] = "formb",
        [ObjectKind.PhcUserTsqlFunction] = "ftu"
    };

    public WorkspaceExportService(IScanAppService scanAppService)
    {
        _scanAppService = scanAppService;
    }

    public async Task ExportCurrentAsync(string zipPath, CancellationToken cancellationToken = default)
    {
        var workspace = _scanAppService.CurrentWorkspace
            ?? throw new InvalidOperationException("Não existe workspace ativo para exportar.");

        var directory = Path.GetDirectoryName(Path.GetFullPath(zipPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        var objects = await workspace.GetObjectsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var byFolder = ExportFolders.ToDictionary(
            f => f,
            _ => new List<(TechnicalObjectRecord Obj, string Content)>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var obj in objects)
        {
            if (!KindToFolder.TryGetValue(obj.Kind, out var folder))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(obj.DefinitionArtifactId))
            {
                continue;
            }

            var text = await workspace.ReadArtifactTextAsync(obj.DefinitionArtifactId, cancellationToken)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            byFolder[folder].Add((obj, text));
        }

        await using var fileStream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        foreach (var folder in ExportFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Garante pasta no ZIP mesmo sem ficheiros.
            archive.CreateEntry(folder.TrimEnd('/') + "/");

            var items = byFolder[folder];
            var index = 0;
            foreach (var (obj, content) in items)
            {
                index++;
                var fileName = BuildFileName(folder, index, obj);
                var entryPath = $"{folder}/{fileName}";
                var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(content.Replace("\r\n", "\n").Replace("\n", "\r\n"));
                await entryStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static string BuildFileName(string folder, int index, TechnicalObjectRecord obj)
    {
        var extension = folder switch
        {
            "campos" when obj.Kind == ObjectKind.PhcUserField => ".txt",
            "formb" => ".txt",
            "ftu" => ".sql",
            _ => ".prg"
        };
        var context = Sanitize(obj.SchemaName, 20);
        var name = Sanitize(obj.Name, 40);
        return $"{index:D3}_{context}_{name}{extension}";
    }

    private static string Sanitize(string? s, int max = 80)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return "sem_nome";
        }

        var t = Regex.Replace(s.Trim(), "[\\\\/:*?\"<>|\\r\\n\\t]+", "_");
        t = Regex.Replace(t, "\\s+", "_").Trim('.', '_', ' ');
        if (t.Length > max)
        {
            t = t[..max].TrimEnd('.', '_');
        }

        return string.IsNullOrWhiteSpace(t) ? "sem_nome" : t;
    }
}
