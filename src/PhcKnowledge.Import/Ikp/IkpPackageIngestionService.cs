using System.Security.Cryptography;
using PhcKnowledge.Import.Storage;
using PhcKnowledge.Import.Receipts;

namespace PhcKnowledge.Import.Ikp;

/// <summary>
/// Receives and validates IKP uploads. When durable attempt persistence is configured,
/// it records the attempt before touching the immutable archive and delegates completion
/// to PackageReceiptRecoveryService.
/// </summary>
public sealed class IkpPackageIngestionService
{
    private readonly IkpPackageValidator _validator;
    private readonly IImmutablePackageStore _store;
    private readonly string _stagingDirectory;
    private readonly long _maxUploadBytes;
    private readonly IPackageReceiptAttemptRepository? _attemptRepository;

    public IkpPackageIngestionService(
        IkpPackageValidator validator,
        IImmutablePackageStore store,
        string stagingDirectory,
        long maxUploadBytes = 250L * 1024 * 1024,
        IPackageReceiptAttemptRepository? attemptRepository = null)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingDirectory);
        _stagingDirectory = Path.GetFullPath(stagingDirectory);
        _maxUploadBytes = maxUploadBytes > 0 ? maxUploadBytes : throw new ArgumentOutOfRangeException(nameof(maxUploadBytes));
        _attemptRepository = attemptRepository;

        if (_attemptRepository is not null &&
            (_store is not IExpectedStorageKeyProvider || _store is not IRecoverableImmutablePackageStore))
            throw new ArgumentException(
                "A recuperação durável exige store com chave determinística e verificação de arquivo.");
    }

    public async Task<IkpIngestionResult> ReceiveValidateArchiveAsync(
        Stream upload,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        if (_attemptRepository is not null &&
            (!organizationId.HasValue || organizationId.Value == Guid.Empty))
            throw new ArgumentException("OrganizationId é obrigatório para registar a recepção.", nameof(organizationId));

        Directory.CreateDirectory(_stagingDirectory);
        var stagingKey = $"receive-{Guid.NewGuid():N}.tmp";
        var stagedPath = Path.Combine(_stagingDirectory, stagingKey);
        var preserveStaging = false;
        try
        {
            var (hash, length) = await StageUploadAsync(upload, stagedPath, cancellationToken);
            await using var validationStream = File.OpenRead(stagedPath);
            var validation = await _validator.ValidateAsync(validationStream, cancellationToken);
            if (validation.ZipByteLength != length ||
                !string.Equals(validation.ZipSha256, hash, StringComparison.Ordinal))
                throw new IkpPackageValidationException("O conteúdo recebido mudou entre a recepção e a validação.");

            if (_attemptRepository is not null)
            {
                var keyProvider = (IExpectedStorageKeyProvider)_store;
                var recoverableStore = (IRecoverableImmutablePackageStore)_store;
                var storageKey = keyProvider.GetStorageKey(validation);
                var registration = await _attemptRepository.CreateOrGetPendingAsync(
                    organizationId!.Value, validation, storageKey, stagingKey, cancellationToken);

                // Once this exact staged file is referenced by a durable pending attempt,
                // preserve it even if immediate processing is interrupted by cancellation
                // or a repository/store failure. A different existing staging key belongs
                // to the previously registered attempt, so this new upload remains disposable.
                preserveStaging =
                    registration.Attempt.Status == "PendingArchive" &&
                    string.Equals(registration.Attempt.StagingKey, stagingKey, StringComparison.Ordinal);

                if (registration.Attempt.Status == "NeedsAttention")
                    throw new PackageReceiptNeedsAttentionException(registration.Attempt.AttemptId);

                if (registration.Attempt.Status == "Completed")
                {
                    var exists = await recoverableStore.VerifyArchivedPackageAsync(
                        registration.Attempt.StorageKey, validation.ZipSha256,
                        validation.ZipByteLength, cancellationToken);
                    if (!exists)
                        throw new PackageReceiptConflictException(
                            "A tentativa está concluída, mas o objecto imutável não existe no arquivo.");
                    var archived = new ArchivedPackage(registration.Attempt.StorageKey, true);
                    var existingReceipt = await _attemptRepository.GetCompletedReceiptAsync(
                        registration.Attempt.AttemptId, cancellationToken);
                    if (existingReceipt is null)
                        throw new PackageReceiptConflictException(
                            "A tentativa está marcada como concluída, mas o receipt correspondente não existe.");
                    return new IkpIngestionResult(validation, archived, existingReceipt);
                }

                var recovery = new PackageReceiptRecoveryService(
                    _attemptRepository, _store, _stagingDirectory);
                var recovered = await recovery.ProcessAttemptAsync(
                    registration.Attempt.AttemptId, cancellationToken);
                if (recovered is null)
                {
                    preserveStaging = true;
                    throw new PackageReceiptPendingException(registration.Attempt.AttemptId);
                }

                return new IkpIngestionResult(
                    validation, recovered.Archive, recovered.Registration.Receipt);
            }

            // Validation/archive-only mode for harnesses. Persisted receipts always use
            // the durable attempt path above; archive->receipt is intentionally not exposed.
            var archive = await _store.ArchiveValidatedPackageAsync(stagedPath, validation, cancellationToken);
            return new IkpIngestionResult(validation, archive, null);
        }
        finally
        {
            if (!preserveStaging)
                TryDeleteStaging(stagedPath);
        }
    }

    private async Task<(string Hash, long Length)> StageUploadAsync(
        Stream upload, string destination, CancellationToken cancellationToken)
    {
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await upload.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            total += read;
            if (total > _maxUploadBytes)
                throw new IkpPackageValidationException("O upload excede o limite de tamanho permitido.");
            hash.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        if (total == 0) throw new IkpPackageValidationException("O upload está vazio.");
        await output.FlushAsync(cancellationToken);
        return (Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(), total);
    }

    private static void TryDeleteStaging(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
        catch
        {
            // A staging janitor may remove unreferenced leftovers after the retention period.
        }
    }
}

public sealed record IkpIngestionResult(
    IkpPackageValidationResult Validation,
    ArchivedPackage Archive,
    PackageReceipt? Receipt);

public sealed class PackageReceiptPendingException(Guid attemptId)
    : Exception($"A recepção foi registada e aguarda recuperação automática. AttemptId: {attemptId}.")
{
    public Guid AttemptId { get; } = attemptId;
}

public sealed class PackageReceiptNeedsAttentionException(Guid attemptId)
    : Exception($"A recepção requer intervenção. AttemptId: {attemptId}.")
{
    public Guid AttemptId { get; } = attemptId;
}
