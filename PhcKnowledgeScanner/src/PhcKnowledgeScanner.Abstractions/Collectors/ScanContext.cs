using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Abstractions.Collectors;

/// <summary>
/// Contexto partilhado de uma execução de scan. Sem assumir schema PHC universal.
/// </summary>
public sealed class ScanContext
{
    /// <summary>
    /// Identificador UUID da captura (IKP CaptureId). Gerado no início da recolha.
    /// Não confundir com InstallationId, scanId do workspace ou PackageId.
    /// </summary>
    public string CaptureId { get; set; } = string.Empty;

    public required string InstallationId { get; init; }

    public required string OutputRoot { get; init; }

    public required ScanMode Mode { get; init; }

    /// <summary>IDs de collectors a executar. Vazio = todos os registados (modo completo).</summary>
    public IReadOnlySet<string> SelectedCollectorIds { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Connection string apenas em memória; nunca registar em logs.</summary>
    public string? SqlConnectionString { get; init; }

    public IScanWorkspace? Workspace { get; init; }

    public IScanProgressSink? Progress { get; init; }
}
