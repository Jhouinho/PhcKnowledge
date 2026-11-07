using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;

namespace PhcKnowledgeScanner.Abstractions.Workspace;

public interface IScanWorkspace : IAsyncDisposable
{
    string RootPath { get; }
    string ScanId { get; }

    /// <summary>CaptureId IKP da captura representada por este workspace (uma captura, não histórico).</summary>
    string? CaptureId { get; }

    Task InitializeAsync(string scanId, string installationLabel, CancellationToken cancellationToken);

    Task SetCaptureIdAsync(string captureId, CancellationToken cancellationToken);

    Task SaveCaptureCoverageAsync(
        string captureId,
        IReadOnlyList<DeclaredCapability> capabilityCatalog,
        IReadOnlyList<OperationCoverage> operationCoverages,
        CancellationToken cancellationToken);

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

    /// <summary>Reads capture metadata without changing the workspace; absent legacy columns remain explicitly unavailable.</summary>
    Task<WorkspaceCaptureMetadata> GetCaptureMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists artifact metadata without reading or modifying artifact files.</summary>
    Task<IReadOnlyList<WorkspaceArtifactMetadata>> GetArtifactsAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the exact persisted artifact bytes. Returns null only when the artifact row or file is absent.</summary>
    Task<byte[]?> ReadArtifactBytesAsync(string artifactId, CancellationToken cancellationToken = default);

    /// <summary>Runs SQLite integrity_check using the current connection.</summary>
    Task<WorkspaceIntegrityResult> CheckIntegrityAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, int>> GetObjectCountsByKindAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CollectionIssue>> GetIssuesAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads capture metadata without assuming that older workspaces contain the newer JSON columns.</summary>
    Task<WorkspaceCaptureMetadata> GetCaptureMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists persisted artifact metadata without reading or modifying artifact files.</summary>
    Task<IReadOnlyList<WorkspaceArtifactMetadata>> GetArtifactsAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads metadata for one artifact; null means that no artifact row exists.</summary>
    Task<WorkspaceArtifactMetadata?> GetArtifactMetadataAsync(
        string artifactId,
        CancellationToken cancellationToken = default);

    /// <summary>Reads exact artifact bytes; null means the row or file is missing. Use GetArtifactMetadataAsync to distinguish them.</summary>
    Task<byte[]?> ReadArtifactBytesAsync(
        string artifactId,
        CancellationToken cancellationToken = default);

    /// <summary>Runs SQLite PRAGMA integrity_check in read-only mode.</summary>
    Task<WorkspaceIntegrityResult> CheckIntegrityAsync(CancellationToken cancellationToken = default);

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
