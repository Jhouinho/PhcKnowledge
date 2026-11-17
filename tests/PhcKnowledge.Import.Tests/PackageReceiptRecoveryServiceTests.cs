using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Tests;

public sealed class PackageReceiptRecoveryServiceTests
{
    [Fact]
    public async Task Pending_attempt_archives_and_completes_then_removes_staging()
    {
        var root = Path.Combine(Path.GetTempPath(), $"phc-recovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var staging = Path.Combine(root, "staging");
        Directory.CreateDirectory(staging);
        var stagingFile = Path.Combine(staging, "upload.tmp");
        await File.WriteAllTextAsync(stagingFile, "validated package bytes");

        var validation = CreateValidation();
        var attempt = CreateAttempt(validation, "upload.tmp", "sha256/ab/package.ikp.zip");
        var repository = new RecordingAttemptRepository(attempt);
        var store = new RecordingPackageStore();
        try
        {
            var service = new PackageReceiptRecoveryService(repository, store, staging);
            var processed = await service.ProcessDueAsync();

            Assert.Equal(1, processed);
            Assert.Equal(1, store.ArchiveCalls);
            Assert.Equal(1, repository.CompletedCalls);
            Assert.False(File.Exists(stagingFile));
            Assert.Equal(attempt.AttemptId, repository.CompletedAttemptId);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Missing_staging_is_recovered_from_verified_archive()
    {
        var root = Path.Combine(Path.GetTempPath(), $"phc-recovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var staging = Path.Combine(root, "staging");
        Directory.CreateDirectory(staging);
        var validation = CreateValidation();
        var attempt = CreateAttempt(validation, "missing.tmp", "sha256/ab/package.ikp.zip");
        var repository = new RecordingAttemptRepository(attempt);
        var store = new RecordingPackageStore { ArchiveExists = true };
        try
        {
            var service = new PackageReceiptRecoveryService(repository, store, staging);
            var processed = await service.ProcessDueAsync();

            Assert.Equal(1, processed);
            Assert.Equal(0, store.ArchiveCalls);
            Assert.Equal(1, store.VerifyCalls);
            Assert.Equal(1, repository.CompletedCalls);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Missing_staging_and_archive_moves_attempt_to_needs_attention()
    {
        var root = Path.Combine(Path.GetTempPath(), $"phc-recovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var staging = Path.Combine(root, "staging");
        Directory.CreateDirectory(staging);
        var attempt = CreateAttempt(CreateValidation(), "missing.tmp", "sha256/ab/package.ikp.zip");
        var repository = new RecordingAttemptRepository(attempt);
        var store = new RecordingPackageStore();
        try
        {
            var service = new PackageReceiptRecoveryService(repository, store, staging);
            var processed = await service.ProcessDueAsync();

            Assert.Equal(0, processed);
            Assert.Equal(1, repository.NeedsAttentionCalls);
            Assert.Equal("StagingAndArchiveMissing", repository.LastErrorCode);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Transient_archive_failure_schedules_retry_without_completing()
    {
        var root = Path.Combine(Path.GetTempPath(), $"phc-recovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var staging = Path.Combine(root, "staging");
        Directory.CreateDirectory(staging);
        await File.WriteAllTextAsync(Path.Combine(staging, "upload.tmp"), "bytes");
        var attempt = CreateAttempt(CreateValidation(), "upload.tmp", "sha256/ab/package.ikp.zip");
        var repository = new RecordingAttemptRepository(attempt);
        var store = new RecordingPackageStore { ThrowOnArchive = true };
        try
        {
            var service = new PackageReceiptRecoveryService(repository, store, staging);
            var processed = await service.ProcessDueAsync();

            Assert.Equal(0, processed);
            Assert.Equal(1, repository.RetryCalls);
            Assert.Equal(0, repository.CompletedCalls);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static IkpPackageValidationResult CreateValidation() => new(
        Guid.NewGuid(), Guid.NewGuid(), "0.1", "PhcKnowledgeScanner", "1.0",
        "TEST", new string('a', 64), 24, 1, 1, 1, 1, 1, 0, new string('b', 64));

    private static PackageReceiptAttempt CreateAttempt(
        IkpPackageValidationResult validation, string stagingKey, string storageKey)
    {
        var now = DateTimeOffset.UtcNow;
        return new PackageReceiptAttempt(
            Guid.NewGuid(), Guid.NewGuid(), validation, storageKey, stagingKey,
            "PendingArchive", 0, now, null, null, null, null, now, now);
    }

    private sealed class RecordingAttemptRepository(PackageReceiptAttempt attempt)
        : IPackageReceiptAttemptRepository
    {
        public int CompletedCalls { get; private set; }
        public int NeedsAttentionCalls { get; private set; }
        public int RetryCalls { get; private set; }
        public Guid? CompletedAttemptId { get; private set; }
        public string? LastErrorCode { get; private set; }

        public Task<PackageReceiptAttemptRegistration> CreateOrGetPendingAsync(
            Guid organizationId, IkpPackageValidationResult validation, string storageKey,
            string stagingKey, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PackageReceiptAttemptLease?> ClaimByIdAsync(
            Guid attemptId, TimeSpan leaseDuration, DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PackageReceiptAttemptLease?>(
                attempt.AttemptId == attemptId
                    ? new PackageReceiptAttemptLease(attempt with
                    {
                        LeaseToken = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                        LeaseUntilUtc = nowUtc.Add(leaseDuration)
                    }, Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"))
                    : null);

        public Task<IReadOnlyList<PackageReceiptAttemptLease>> ClaimDueAsync(
            int batchSize, TimeSpan leaseDuration, DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PackageReceiptAttemptLease>>(
                [new PackageReceiptAttemptLease(attempt with
                {
                    LeaseToken = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                    LeaseUntilUtc = nowUtc.Add(leaseDuration)
                }, Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"))]);

        public Task<bool> RenewLeaseAsync(Guid attemptId, Guid leaseToken,
            DateTimeOffset leaseUntilUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<PackageReceiptRegistration> CompleteAsync(
            Guid attemptId, Guid leaseToken, ArchivedPackage archive,
            CancellationToken cancellationToken = default)
        {
            CompletedCalls++;
            CompletedAttemptId = attemptId;
            var receipt = new PackageReceipt(Guid.NewGuid(), attempt.OrganizationId,
                attempt.Validation.PackageId, attempt.Validation.CaptureId,
                attempt.Validation.ContractVersion, attempt.Validation.ProducerProductId,
                attempt.Validation.ProducerProductVersion, attempt.Validation.OriginDatabaseName,
                attempt.Validation.ZipSha256, attempt.Validation.ZipByteLength, archive.StorageKey,
                DateTimeOffset.UtcNow, "{}");
            return Task.FromResult(new PackageReceiptRegistration(receipt, false));
        }

        public Task<bool> ScheduleRetryAsync(Guid attemptId, Guid leaseToken,
            DateTimeOffset nextAttemptAtUtc, string errorCode, string errorMessage,
            CancellationToken cancellationToken = default)
        {
            RetryCalls++;
            LastErrorCode = errorCode;
            return Task.FromResult(true);
        }

        public Task<bool> MarkNeedsAttentionAsync(Guid attemptId, Guid leaseToken,
            string errorCode, string errorMessage, CancellationToken cancellationToken = default)
        {
            NeedsAttentionCalls++;
            LastErrorCode = errorCode;
            return Task.FromResult(true);
        }
    }

    private sealed class RecordingPackageStore : IImmutablePackageStore, IRecoverableImmutablePackageStore
    {
        public int ArchiveCalls { get; private set; }
        public int VerifyCalls { get; private set; }
        public bool ArchiveExists { get; init; }
        public bool ThrowOnArchive { get; init; }

        public Task<ArchivedPackage> ArchiveValidatedPackageAsync(
            string validatedPackagePath, IkpPackageValidationResult validation,
            CancellationToken cancellationToken = default)
        {
            ArchiveCalls++;
            if (ThrowOnArchive) throw new IOException("Temporary archive failure.");
            return Task.FromResult(new ArchivedPackage("sha256/ab/package.ikp.zip", false));
        }

        public Task<bool> VerifyArchivedPackageAsync(string storageKey, string expectedSha256,
            long expectedByteLength, CancellationToken cancellationToken = default)
        {
            VerifyCalls++;
            return Task.FromResult(ArchiveExists);
        }
    }
}
