namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Explicit version of the persisted/ephemeral analysis model (not IKP ContractVersion).
/// Increment B starts at 0.2 (adds UnavailableObjects and persistence contract).
/// </summary>
public static class PackageImportAnalysisSchema
{
    public const string Version = "0.3";
}

public enum MaterializationReadiness
{
    Ready = 0,
    ReadyWithWarnings = 1,
    Blocked = 2
}

public enum MaterializationFindingSeverity
{
    Warning = 0,
    Blocker = 1
}

/// <summary>
/// Analysis result for human review and Increment B persistence.
/// <see cref="ReceiptId"/> is first-writer provenance; <see cref="AnalyzedAtUtc"/> is temporal
/// metadata. Neither participates in semantic equality or <c>SemanticFingerprint</c>.
/// </summary>
public sealed record PackageImportAnalysis(
    Guid OrganizationId,
    Guid ReceiptId,
    Guid PackageId,
    Guid CaptureId,
    string ContractVersion,
    string ProducerProductId,
    string ProducerProductVersion,
    string? OriginDatabaseName,
    string OriginEngine,
    string? OriginServerLabel,
    string? OriginPhcVersion,
    string? CaptureMode,
    string? MetadataStatus,
    string? ScannerVersion,
    string ZipSha256,
    string LogicalContentDigest,
    string AnalysisSchemaVersion,
    DateTimeOffset AnalyzedAtUtc,
    PackageImportIntegrityAnalysis Integrity,
    IReadOnlyList<IkpInspectedCapabilityOperation> Capabilities,
    IReadOnlyList<IkpInspectedCoverageOperation> Coverage,
    PackageImportObjectIndexAnalysis ObjectIndex,
    PackageImportArtifactsAnalysis Artifacts,
    PackageImportRelationsAnalysis Relations,
    IReadOnlyList<IkpInspectedIssue> Issues,
    IReadOnlyList<string> Limitations,
    PackageImportReadinessAnalysis Readiness,
    IReadOnlyList<PackageImportUnavailableObject> UnavailableObjects)
{
    /// <summary>
    /// Content-aware equality excluding <see cref="ReceiptId"/> and <see cref="AnalyzedAtUtc"/>.
    /// </summary>
    public bool SemanticEquals(PackageImportAnalysis other) =>
        other is not null &&
        OrganizationId == other.OrganizationId &&
        PackageId == other.PackageId &&
        CaptureId == other.CaptureId &&
        ContractVersion == other.ContractVersion &&
        ProducerProductId == other.ProducerProductId &&
        ProducerProductVersion == other.ProducerProductVersion &&
        OriginDatabaseName == other.OriginDatabaseName &&
        OriginEngine == other.OriginEngine &&
        OriginServerLabel == other.OriginServerLabel &&
        OriginPhcVersion == other.OriginPhcVersion &&
        CaptureMode == other.CaptureMode &&
        MetadataStatus == other.MetadataStatus &&
        ScannerVersion == other.ScannerVersion &&
        ZipSha256 == other.ZipSha256 &&
        LogicalContentDigest == other.LogicalContentDigest &&
        AnalysisSchemaVersion == other.AnalysisSchemaVersion &&
        Integrity.ValidationSucceeded == other.Integrity.ValidationSucceeded &&
        Integrity.ArchiveSha256Verified == other.Integrity.ArchiveSha256Verified &&
        Integrity.InternalChecksumsVerified == other.Integrity.InternalChecksumsVerified &&
        Integrity.Incompatibilities.SequenceEqual(other.Integrity.Incompatibilities) &&
        Capabilities.SequenceEqual(other.Capabilities) &&
        CoverageEquals(Coverage, other.Coverage) &&
        ObjectIndexEquals(ObjectIndex, other.ObjectIndex) &&
        ArtifactsEquals(Artifacts, other.Artifacts) &&
        RelationsEquals(Relations, other.Relations) &&
        Issues.SequenceEqual(other.Issues) &&
        Limitations.SequenceEqual(other.Limitations) &&
        Readiness.Status == other.Readiness.Status &&
        Readiness.Findings.SequenceEqual(other.Readiness.Findings) &&
        UnavailableObjects.SequenceEqual(other.UnavailableObjects);

    private static bool CoverageEquals(
        IReadOnlyList<IkpInspectedCoverageOperation> left,
        IReadOnlyList<IkpInspectedCoverageOperation> right)
    {
        if (left.Count != right.Count) return false;
        for (var i = 0; i < left.Count; i++)
        {
            var a = left[i];
            var b = right[i];
            if (a.OperationId != b.OperationId ||
                a.CapabilityVersion != b.CapabilityVersion ||
                a.Requested != b.Requested ||
                a.Result != b.Result ||
                a.SourceAvailability != b.SourceAvailability ||
                a.ContentAvailability != b.ContentAvailability ||
                a.ObservedCount != b.ObservedCount ||
                !a.IssueIds.SequenceEqual(b.IssueIds) ||
                !ScopeEquals(a.Scope, b.Scope))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ScopeEquals(
        IReadOnlyDictionary<string, string>? left,
        IReadOnlyDictionary<string, string>? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        if (left.Count != right.Count) return false;
        return left.OrderBy(x => x.Key, StringComparer.Ordinal)
            .SequenceEqual(right.OrderBy(x => x.Key, StringComparer.Ordinal));
    }

    private static bool ObjectIndexEquals(
        PackageImportObjectIndexAnalysis left, PackageImportObjectIndexAnalysis right) =>
        left.TotalObjects == right.TotalObjects &&
        left.ObjectsWithDefinitionArtifacts == right.ObjectsWithDefinitionArtifacts &&
        left.ObjectsWithoutDefinitionArtifacts == right.ObjectsWithoutDefinitionArtifacts &&
        left.Available == right.Available &&
        left.Empty == right.Empty &&
        left.Unavailable == right.Unavailable &&
        left.NotCollected == right.NotCollected &&
        left.SourceIdentityCount == right.SourceIdentityCount &&
        left.MetadataFingerprintCount == right.MetadataFingerprintCount &&
        left.DefinitionFingerprintAvailableCount == right.DefinitionFingerprintAvailableCount &&
        left.DefinitionFingerprintUnavailableCount == right.DefinitionFingerprintUnavailableCount &&
        DictionaryEquals(left.CountByObjectKind, right.CountByObjectKind) &&
        DictionaryEquals(left.CountByDefinitionStatus, right.CountByDefinitionStatus);

    private static bool ArtifactsEquals(
        PackageImportArtifactsAnalysis left, PackageImportArtifactsAnalysis right) =>
        left.TotalArtifacts == right.TotalArtifacts &&
        left.ReferencesValidated == right.ReferencesValidated &&
        left.Artifacts.SequenceEqual(right.Artifacts) &&
        DictionaryEquals(left.CountByCategory, right.CountByCategory);

    private static bool RelationsEquals(
        PackageImportRelationsAnalysis left, PackageImportRelationsAnalysis right) =>
        left.Present == right.Present &&
        left.DeclaredRelationCount == right.DeclaredRelationCount &&
        left.InterpretationCount == right.InterpretationCount &&
        left.OrphanInterpretationCount == right.OrphanInterpretationCount &&
        DictionaryEquals(left.DeclaredCountBySourceType, right.DeclaredCountBySourceType);

    private static bool DictionaryEquals(
        IReadOnlyDictionary<string, int> left, IReadOnlyDictionary<string, int> right) =>
        left.Count == right.Count &&
        left.OrderBy(x => x.Key, StringComparer.Ordinal)
            .SequenceEqual(right.OrderBy(x => x.Key, StringComparer.Ordinal));
}

public sealed record PackageImportIntegrityAnalysis(
    bool ValidationSucceeded,
    bool ArchiveSha256Verified,
    bool InternalChecksumsVerified,
    IReadOnlyList<string> Incompatibilities);

public sealed record PackageImportObjectIndexAnalysis(
    int TotalObjects,
    IReadOnlyDictionary<string, int> CountByObjectKind,
    IReadOnlyDictionary<string, int> CountByDefinitionStatus,
    int ObjectsWithDefinitionArtifacts,
    int ObjectsWithoutDefinitionArtifacts,
    int Available,
    int Empty,
    int Unavailable,
    int NotCollected,
    int SourceIdentityCount,
    int MetadataFingerprintCount,
    int DefinitionFingerprintAvailableCount,
    int DefinitionFingerprintUnavailableCount);

public sealed record PackageImportArtifactsAnalysis(
    int TotalArtifacts,
    bool ReferencesValidated,
    IReadOnlyList<IkpInspectedArtifact> Artifacts,
    IReadOnlyDictionary<string, int> CountByCategory);

public sealed record PackageImportRelationsAnalysis(
    bool Present,
    int DeclaredRelationCount,
    int InterpretationCount,
    int OrphanInterpretationCount,
    IReadOnlyDictionary<string, int> DeclaredCountBySourceType);

public sealed record PackageImportReadinessAnalysis(
    MaterializationReadiness Status,
    IReadOnlyList<PackageImportReadinessFinding> Findings);

public sealed record PackageImportReadinessFinding(
    string Code,
    MaterializationFindingSeverity Severity,
    string Message,
    string? RelatedRef);

/// <summary>
/// Object Index entries with definitionStatus Unavailable, for human review.
/// Issue linkage is best-effort by operationId only; missing sourceObjectId is preserved as null.
/// </summary>
public sealed record PackageImportUnavailableObject(
    string SourceObjectId,
    string ObjectKind,
    string DefinitionStatus,
    string OperationId,
    string? Name,
    string? AssociatedIssueId,
    string? AssociatedIssueCode,
    string? AssociatedIssueMessage);
