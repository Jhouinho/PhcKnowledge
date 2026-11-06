using PhcKnowledgeScanner.Abstractions.Connection;
using PhcKnowledgeScanner.Abstractions.Manifest;
using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Application.Services;

public interface IScanAppService
{
    IScanWorkspace? CurrentWorkspace { get; }

    Task<ScanManifest> RunStructureScanAsync(
        SqlConnectionRequest request,
        string workspaceRoot,
        IScanProgressSink? progress,
        CancellationToken cancellationToken = default,
        IReadOnlySet<string>? selectedCollectorIds = null);
}
