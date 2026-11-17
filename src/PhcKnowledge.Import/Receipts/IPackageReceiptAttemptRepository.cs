using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Receipts;

/// <summary>
/// Durable coordination contract for the filesystem/SQL boundary.
/// Implementations must use atomic SQL operations for create, claim and complete.
/// </summary>
public interface IPackageReceiptAttemptRepository
{
    Task<PackageReceiptAttemptRegistration> CreateOrGetPendingAsync(
        Guid organizationId,
        IkpPackageValidationResult validation,
        string storageKey,
        string stagingKey,
        CancellationToken cancellationToken = default);

    Task<PackageReceiptAttemptLease?> ClaimByIdAsync(
        Guid attemptId,
        TimeSpan leaseDuration,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PackageReceiptAttemptLease>> ClaimDueAsync(
        int batchSize,
        TimeSpan leaseDuration,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<bool> RenewLeaseAsync(
        Guid attemptId,
        Guid leaseToken,
        DateTimeOffset leaseUntilUtc,
        CancellationToken cancellationToken = default);

    Task<PackageReceiptRegistration> CompleteAsync(
        Guid attemptId,
        Guid leaseToken,
        ArchivedPackage archive,
        CancellationToken cancellationToken = default);

    Task<bool> ScheduleRetryAsync(
        Guid attemptId,
        Guid leaseToken,
        DateTimeOffset nextAttemptAtUtc,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task<bool> MarkNeedsAttentionAsync(
        Guid attemptId,
        Guid leaseToken,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
