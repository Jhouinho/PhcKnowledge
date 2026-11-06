using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Application.Services;

public interface IInventoryAppService
{
    Task<WorkspaceSummary?> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TechnicalObjectRecord>> SearchObjectsAsync(
        string? kind,
        string? search,
        CancellationToken cancellationToken = default);

    Task<TechnicalObjectRecord?> GetObjectAsync(string objectId, CancellationToken cancellationToken = default);

    Task<string?> GetArtifactTextAsync(string artifactId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DerivedRelationRecord>> GetRelationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CollectionIssue>> GetIssuesAsync(CancellationToken cancellationToken = default);
}
