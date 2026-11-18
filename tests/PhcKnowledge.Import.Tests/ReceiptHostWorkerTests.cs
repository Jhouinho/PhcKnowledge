using Microsoft.Extensions.Logging.Abstractions;
using PhcKnowledge.Host;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Tests;

public sealed class ReceiptHostWorkerTests
{
    [Fact]
    public async Task Recovery_worker_runs_immediately_and_continues_after_a_failed_cycle()
    {
        var repository = new RecordingAttemptRepository();
        var recovery = new PackageReceiptRecoveryService(
            repository,
            new NoOpPackageStore(),
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            TimeSpan.FromSeconds(1));
        var settings = new ReceiptHostSettings
        {
            ConnectionString = "unused",
            ArchiveDirectory = Path.Combine(Path.GetTempPath(), "archive"),
            StagingDirectory = Path.Combine(Path.GetTempPath(), "staging"),
            RecoveryInterval = TimeSpan.FromMilliseconds(20)
        };
        var worker = new PackageReceiptRecoveryWorker(
            recovery, settings, NullLogger<PackageReceiptRecoveryWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            await repository.SecondCall.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(repository.CallCount >= 2);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            worker.Dispose();
        }
    }

    private sealed class RecordingAttemptRepository : IPackageReceiptAttemptRepository
    {
        private int _callCount;
        public int CallCount => Volatile.Read(ref _callCount);
        public TaskCompletionSource SecondCall { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IReadOnlyList<PackageReceiptAttemptLease>> ClaimDueAsync(
            int batchSize, TimeSpan leaseDuration, DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default)
        {
            var call = Interlocked.Increment(ref _callCount);
            if (call == 1)
                throw new IOException("Simulated transient repository failure.");
            if (call >= 2)
                SecondCall.TrySetResult();
            return Task.FromResult<IReadOnlyList<PackageReceiptAttemptLease>>(
                Array.Empty<PackageReceiptAttemptLease>());
        }

        public Task<PackageReceiptAttemptRegistration> CreateOrGetPendingAsync(
            Guid organizationId, IkpPackageValidationResult validation, string storageKey,
            string stagingKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PackageReceiptAttemptLease?> ClaimByIdAsync(
            Guid attemptId, TimeSpan leaseDuration, DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PackageReceipt?> GetCompletedReceiptAsync(
            Guid attemptId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PackageIngestionStatus?> GetAttemptAsync(
            Guid organizationId, Guid attemptId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PackageIngestionStatus?> GetByPackageIdAsync(
            Guid organizationId, Guid packageId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlySet<string>> GetProtectedStagingKeysAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> RenewLeaseAsync(
            Guid attemptId, Guid leaseToken, DateTimeOffset leaseUntilUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PackageReceiptRegistration> CompleteAsync(
            Guid attemptId, Guid leaseToken, ArchivedPackage archive,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ScheduleRetryAsync(
            Guid attemptId, Guid leaseToken, DateTimeOffset nextAttemptAtUtc,
            string errorCode, string errorMessage, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> MarkNeedsAttentionAsync(
            Guid attemptId, Guid leaseToken, string errorCode, string errorMessage,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NoOpPackageStore : IImmutablePackageStore
    {
        public Task<ArchivedPackage> ArchiveValidatedPackageAsync(
            string validatedPackagePath, IkpPackageValidationResult validation,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
