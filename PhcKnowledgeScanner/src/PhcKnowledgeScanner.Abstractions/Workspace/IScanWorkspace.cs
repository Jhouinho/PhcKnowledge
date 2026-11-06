using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;

namespace PhcKnowledgeScanner.Abstractions.Workspace;

public interface IScanWorkspace : IAsyncDisposable
{
    string RootPath { get; }
    string ScanId { get; }

    Task InitializeAsync(string scanId, string installationLabel, CancellationToken cancellationToken);

    Task UpsertObjectAsync(TechnicalObjectRecord record, CancellationToken cancellationToken);

    Task UpsertObjectsAsync(IEnumerable<TechnicalObjectRecord> records, CancellationToken cancellationToken);

    Task<string> SaveArtifactAsync(
        string collectorId,
        string relativePath,
        ReadOnlyMemory<byte> content,
        string contentType,
        IReadOnlyDictionary<string, string>? metadata,
        CancellationToken cancellationToken);

    Task AddIssueAsync(string? collectorId, CollectionIssue issue, CancellationToken cancellationToken);

    Task SetScanStatusAsync(string status, CancellationToken cancellationToken);

    Task<IReadOnlyList<TechnicalObjectRecord>> GetObjectsAsync(
        string? kind = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<TechnicalObjectRecord?> GetObjectAsync(string objectId, CancellationToken cancellationToken = default);

    Task<string?> ReadArtifactTextAsync(string artifactId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, int>> GetObjectCountsByKindAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CollectionIssue>> GetIssuesAsync(CancellationToken cancellationToken = default);

    Task SaveRelationAsync(DerivedRelationRecord relation, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DerivedRelationRecord>> GetRelationsAsync(CancellationToken cancellationToken = default);

    /// <summary>Persiste factos de relações declaradas PHC (Camada A). Não misturar com <see cref="SaveRelationAsync"/>.</summary>
    Task UpsertDeclaredRelationsAsync(
        IEnumerable<DeclaredRelationRecord> records,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeclaredRelationRecord>> GetDeclaredRelationsAsync(
        string? sourceType = null,
        string? fromTable = null,
        string? toTable = null,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste interpretações de expressões declaradas (independente dos factos).</summary>
    Task UpsertDeclaredRelationInterpretationsAsync(
        IEnumerable<DeclaredRelationInterpretationRecord> records,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeclaredRelationInterpretationRecord>> GetDeclaredRelationInterpretationsAsync(
        string? relationId = null,
        CancellationToken cancellationToken = default);

    Task<WorkspaceSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}
