namespace PhcKnowledge.Import.Receipts;

/// <summary>
/// Safe read model for ingestion status queries. Omits internal error text,
/// staging paths, leases and raw validation JSON.
/// </summary>
public sealed record PackageIngestionStatus(
    Guid AttemptId,
    Guid OrganizationId,
    Guid PackageId,
    Guid CaptureId,
    string Status,
    string ZipSha256,
    long ZipByteLength,
    string StorageKey,
    int AttemptCount,
    int ObjectCount,
    int ArtifactCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    Guid? ReceiptId,
    string? LastErrorCode);
