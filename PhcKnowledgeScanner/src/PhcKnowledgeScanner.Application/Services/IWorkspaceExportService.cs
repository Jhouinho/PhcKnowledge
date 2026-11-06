namespace PhcKnowledgeScanner.Application.Services;

public interface IWorkspaceExportService
{
    Task ExportCurrentAsync(string zipPath, CancellationToken cancellationToken = default);
}
