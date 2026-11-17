using PhcKnowledge.Import.Ikp;

namespace PhcKnowledge.Import.Receipts;

public sealed record PackageReceiptAttempt(
    Guid AttemptId,
    Guid OrganizationId,
    IkpPackageValidationResult Validation,
    string StorageKey,
    string StagingKey,
    string Status,
    int AttemptCount,
    DateTimeOffset NextAttemptAtUtc,
    Guid? LeaseToken,
    DateTimeOffset? LeaseUntilUtc,
    string? LastErrorCode,
    string? LastErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PackageReceiptAttemptRegistration(
    PackageReceiptAttempt Attempt,
    bool AlreadyExists);

public sealed record PackageReceiptAttemptLease(
    PackageReceiptAttempt Attempt,
    Guid LeaseToken);
