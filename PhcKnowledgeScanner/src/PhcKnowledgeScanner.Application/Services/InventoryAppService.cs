using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Application.Services;

public sealed class InventoryAppService : IInventoryAppService
{
    private readonly IScanAppService _scanAppService;

    public InventoryAppService(IScanAppService scanAppService)
    {
        _scanAppService = scanAppService;
    }

    public async Task<WorkspaceSummary?> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        if (_scanAppService.CurrentWorkspace is null)
        {
            return null;
        }

        return await _scanAppService.CurrentWorkspace.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TechnicalObjectRecord>> SearchObjectsAsync(
        string? kind,
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (_scanAppService.CurrentWorkspace is null)
        {
            return Array.Empty<TechnicalObjectRecord>();
        }

        return await _scanAppService.CurrentWorkspace.GetObjectsAsync(kind, search, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TechnicalObjectRecord?> GetObjectAsync(string objectId, CancellationToken cancellationToken = default)
    {
        if (_scanAppService.CurrentWorkspace is null)
        {
            return null;
        }

        return await _scanAppService.CurrentWorkspace.GetObjectAsync(objectId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> GetArtifactTextAsync(string artifactId, CancellationToken cancellationToken = default)
    {
        if (_scanAppService.CurrentWorkspace is null)
        {
            return null;
        }

        return await _scanAppService.CurrentWorkspace.ReadArtifactTextAsync(artifactId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DerivedRelationRecord>> GetRelationsAsync(CancellationToken cancellationToken = default)
    {
        if (_scanAppService.CurrentWorkspace is null)
        {
            return Array.Empty<DerivedRelationRecord>();
        }

        return await _scanAppService.CurrentWorkspace.GetRelationsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CollectionIssue>> GetIssuesAsync(CancellationToken cancellationToken = default)
    {
        if (_scanAppService.CurrentWorkspace is null)
        {
            return Array.Empty<CollectionIssue>();
        }

        return await _scanAppService.CurrentWorkspace.GetIssuesAsync(cancellationToken).ConfigureAwait(false);
    }
}
