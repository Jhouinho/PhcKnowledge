namespace PhcKnowledge.Import.Ikp;

public sealed record IkpPackageValidationResult(
    Guid PackageId,
    Guid CaptureId,
    string ContractVersion,
    string ProducerProductId,
    string ProducerProductVersion,
    string? OriginDatabaseName,
    string ZipSha256,
    long ZipByteLength,
    int ContractFileCount,
    int ChecksummedFileCount,
    int ArtifactCount,
    int ObjectCount,
    int OperationCount,
    int IssueCount,
    string LogicalContentDigest);