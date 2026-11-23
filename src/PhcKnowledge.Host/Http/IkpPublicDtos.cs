using PhcKnowledge.Import.Ikp.Analysis;
using PhcKnowledge.Import.Receipts;

namespace PhcKnowledge.Host.Http;

/// <summary>
/// Public HTTP allowlist for ingestion status. Never serialize Import entities directly.
/// </summary>
public sealed record IkpIngestionStatusDto(
    Guid AttemptId,
    Guid PackageId,
    Guid CaptureId,
    string Status,
    int AttemptCount,
    int ObjectCount,
    int ArtifactCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record IkpIngestionAcceptedDto(
    Guid AttemptId,
    Guid PackageId,
    Guid CaptureId,
    string Status,
    int AttemptCount,
    int ObjectCount,
    int ArtifactCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string StatusUrl);

/// <summary>Minimal 202 body when the full status row cannot be loaded.</summary>
public sealed record IkpIngestionPendingDto(
    Guid AttemptId,
    string Status,
    string StatusUrl);

public static class IkpPublicDtoMapper
{
    public static IkpIngestionStatusDto ToStatusDto(PackageIngestionStatus status) =>
        new(
            status.AttemptId,
            status.PackageId,
            status.CaptureId,
            status.Status,
            status.AttemptCount,
            status.ObjectCount,
            status.ArtifactCount,
            status.CreatedAtUtc,
            status.UpdatedAtUtc,
            status.CompletedAtUtc);

    public static IkpIngestionAcceptedDto ToAcceptedDto(PackageIngestionStatus status, string statusUrl) =>
        new(
            status.AttemptId,
            status.PackageId,
            status.CaptureId,
            status.Status,
            status.AttemptCount,
            status.ObjectCount,
            status.ArtifactCount,
            status.CreatedAtUtc,
            status.UpdatedAtUtc,
            status.CompletedAtUtc,
            statusUrl);

    /// <summary>
    /// Maps a persisted analysis row. <paramref name="persisted"/>.AnalysisId is the real stored id —
    /// never invent a new identifier here.
    /// </summary>
    public static IkpPackageAnalysisDto ToAnalysisDto(PersistedPackageImportAnalysis persisted)
    {
        ArgumentNullException.ThrowIfNull(persisted);
        return ToAnalysisDto(persisted.AnalysisId, persisted.ToAnalysis());
    }

    public static IkpPackageAnalysisDto ToAnalysisDto(Guid analysisId, PackageImportAnalysis analysis) =>
        new(
            analysisId,
            analysis.ReceiptId,
            analysis.PackageId,
            analysis.CaptureId,
            analysis.ContractVersion,
            analysis.ProducerProductId,
            analysis.ProducerProductVersion,
            analysis.OriginDatabaseName,
            analysis.OriginEngine,
            analysis.CaptureMode,
            analysis.MetadataStatus,
            analysis.ZipSha256,
            analysis.LogicalContentDigest,
            analysis.AnalysisSchemaVersion,
            analysis.AnalyzedAtUtc,
            new IkpPackageIntegrityDto(
                analysis.Integrity.ValidationSucceeded,
                analysis.Integrity.ArchiveSha256Verified,
                analysis.Integrity.InternalChecksumsVerified,
                analysis.Integrity.Incompatibilities),
            analysis.Capabilities.Select(x => new IkpCapabilityOperationDto(
                x.OperationId, x.CategoryId, x.Supported, x.Description)).ToArray(),
            analysis.Coverage.Select(x => new IkpCoverageOperationDto(
                x.OperationId, x.Requested, x.Result, x.SourceAvailability, x.ContentAvailability,
                x.ObservedCount, x.IssueIds)).ToArray(),
            new IkpObjectIndexAnalysisDto(
                analysis.ObjectIndex.TotalObjects,
                analysis.ObjectIndex.CountByObjectKind,
                analysis.ObjectIndex.CountByDefinitionStatus,
                analysis.ObjectIndex.ObjectsWithDefinitionArtifacts,
                analysis.ObjectIndex.ObjectsWithoutDefinitionArtifacts,
                analysis.ObjectIndex.Available,
                analysis.ObjectIndex.Empty,
                analysis.ObjectIndex.Unavailable,
                analysis.ObjectIndex.NotCollected,
                analysis.ObjectIndex.DefinitionFingerprintAvailableCount,
                analysis.ObjectIndex.DefinitionFingerprintUnavailableCount),
            new IkpArtifactsAnalysisDto(
                analysis.Artifacts.TotalArtifacts,
                analysis.Artifacts.ReferencesValidated,
                analysis.Artifacts.CountByCategory),
            new IkpRelationsAnalysisDto(
                analysis.Relations.Present,
                analysis.Relations.DeclaredRelationCount,
                analysis.Relations.InterpretationCount,
                analysis.Relations.OrphanInterpretationCount,
                analysis.Relations.DeclaredCountBySourceType),
            analysis.Issues.Select(x => new IkpIssueDto(
                x.IssueId, x.OperationId, x.Severity, x.Code, x.Message, x.SourceObjectId)).ToArray(),
            analysis.Limitations,
            new IkpReadinessDto(
                analysis.Readiness.Status.ToString(),
                analysis.Readiness.Findings.Select(x => new IkpReadinessFindingDto(
                    x.Code, x.Severity.ToString(), x.Message, x.RelatedRef)).ToArray()),
            analysis.UnavailableObjects.Select(x => new IkpUnavailableObjectDto(
                x.SourceObjectId, x.ObjectKind, x.DefinitionStatus, x.OperationId, x.Name,
                x.AssociatedIssueId, x.AssociatedIssueCode, x.AssociatedIssueMessage)).ToArray());
}

public sealed record IkpPackageAnalysisDto(
    Guid AnalysisId,
    Guid ReceiptId,
    Guid PackageId,
    Guid CaptureId,
    string ContractVersion,
    string ProducerProductId,
    string ProducerProductVersion,
    string? OriginDatabaseName,
    string OriginEngine,
    string? CaptureMode,
    string? MetadataStatus,
    string ZipSha256,
    string LogicalContentDigest,
    string AnalysisSchemaVersion,
    DateTimeOffset AnalyzedAtUtc,
    IkpPackageIntegrityDto Integrity,
    IReadOnlyList<IkpCapabilityOperationDto> Capabilities,
    IReadOnlyList<IkpCoverageOperationDto> Coverage,
    IkpObjectIndexAnalysisDto ObjectIndex,
    IkpArtifactsAnalysisDto Artifacts,
    IkpRelationsAnalysisDto Relations,
    IReadOnlyList<IkpIssueDto> Issues,
    IReadOnlyList<string> Limitations,
    IkpReadinessDto Readiness,
    IReadOnlyList<IkpUnavailableObjectDto> UnavailableObjects);

public sealed record IkpPackageIntegrityDto(
    bool ValidationSucceeded,
    bool ArchiveSha256Verified,
    bool InternalChecksumsVerified,
    IReadOnlyList<string> Incompatibilities);

public sealed record IkpCapabilityOperationDto(
    string OperationId,
    string CategoryId,
    bool Supported,
    string? Description);

public sealed record IkpCoverageOperationDto(
    string OperationId,
    bool Requested,
    string Result,
    string SourceAvailability,
    string ContentAvailability,
    int? ObservedCount,
    IReadOnlyList<string> IssueIds);

public sealed record IkpObjectIndexAnalysisDto(
    int TotalObjects,
    IReadOnlyDictionary<string, int> CountByObjectKind,
    IReadOnlyDictionary<string, int> CountByDefinitionStatus,
    int ObjectsWithDefinitionArtifacts,
    int ObjectsWithoutDefinitionArtifacts,
    int Available,
    int Empty,
    int Unavailable,
    int NotCollected,
    int DefinitionFingerprintAvailableCount,
    int DefinitionFingerprintUnavailableCount);

public sealed record IkpArtifactsAnalysisDto(
    int TotalArtifacts,
    bool ReferencesValidated,
    IReadOnlyDictionary<string, int> CountByCategory);

public sealed record IkpRelationsAnalysisDto(
    bool Present,
    int DeclaredRelationCount,
    int InterpretationCount,
    int OrphanInterpretationCount,
    IReadOnlyDictionary<string, int> DeclaredCountBySourceType);

public sealed record IkpIssueDto(
    string IssueId,
    string? OperationId,
    string Severity,
    string Code,
    string Message,
    string? SourceObjectId);

public sealed record IkpReadinessDto(
    string Status,
    IReadOnlyList<IkpReadinessFindingDto> Findings);

public sealed record IkpReadinessFindingDto(
    string Code,
    string Severity,
    string Message,
    string? RelatedRef);

public sealed record IkpUnavailableObjectDto(
    string SourceObjectId,
    string ObjectKind,
    string DefinitionStatus,
    string OperationId,
    string? Name,
    string? AssociatedIssueId,
    string? AssociatedIssueCode,
    string? AssociatedIssueMessage);
