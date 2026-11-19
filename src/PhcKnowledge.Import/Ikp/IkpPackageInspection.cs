namespace PhcKnowledge.Import.Ikp;

/// <summary>
/// Typed read model produced by a single IKP validation/parse pass.
/// Distinct from the IKP JSON contract and from PackageImportAnalysis.
/// </summary>
public sealed record IkpPackageInspection(
    IkpPackageValidationResult Validation,
    bool InternalChecksumsVerified,
    string ContentType,
    string CaptureMode,
    string MetadataStatus,
    string OriginEngine,
    string? OriginServerLabel,
    string? OriginPhcVersion,
    string? ScannerVersion,
    IReadOnlyList<IkpInspectedCapabilityOperation> Capabilities,
    IReadOnlyList<IkpInspectedCoverageOperation> Coverage,
    IReadOnlyList<IkpInspectedObject> Objects,
    IReadOnlyList<IkpInspectedArtifact> Artifacts,
    IReadOnlyList<IkpInspectedIssue> Issues,
    IkpInspectedRelations? Relations);

public sealed record IkpInspectedCapabilityOperation(
    string OperationId,
    string CategoryId,
    string CapabilityVersion,
    string CollectorId,
    bool Supported,
    string? Description);

public sealed record IkpInspectedCoverageOperation(
    string OperationId,
    string CapabilityVersion,
    bool Requested,
    string Result,
    string SourceAvailability,
    string ContentAvailability,
    int? ObservedCount,
    IReadOnlyList<string> IssueIds,
    IReadOnlyDictionary<string, string>? Scope);

public sealed record IkpInspectedObject(
    string SourceObjectId,
    string ObjectKind,
    string OperationId,
    string DefinitionStatus,
    IReadOnlyList<string> DefinitionArtifactIds,
    bool HasDefinitionArtifacts,
    bool HasSourceIdentity,
    bool HasMetadataFingerprint,
    bool HasDefinitionFingerprint,
    string? Name,
    string? SchemaName);

public sealed record IkpInspectedArtifact(
    string ArtifactId,
    string Path,
    string Category,
    string Format,
    string? Role,
    bool Required,
    long ByteLength,
    string Sha256);

public sealed record IkpInspectedIssue(
    string IssueId,
    string? OperationId,
    string Severity,
    string Code,
    string Message,
    string? SourceObjectId,
    string? DetailsJson);

public sealed record IkpInspectedRelations(
    string SchemaVersion,
    Guid CaptureId,
    IReadOnlyList<IkpInspectedDeclaredRelation> DeclaredRelations,
    IReadOnlyList<IkpInspectedRelationInterpretation> Interpretations,
    int OrphanInterpretationCount);

public sealed record IkpInspectedDeclaredRelation(
    string SourceObjectId,
    string? SourceType,
    string? SourceStamp);

public sealed record IkpInspectedRelationInterpretation(
    string InterpretationId,
    string SourceObjectId,
    string? Status);
