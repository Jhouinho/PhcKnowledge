using System.Security.Cryptography;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Ikp;

/// <summary>
/// Receives an upload, validates it before storage, then archives the original
/// ZIP by content hash. It deliberately does not associate an installation or
/// materialise observations.
/// </summary>
public sealed class IkpPackageIngestionService
{
    private readonly IkpPackageValidator _validator;
    private readonly IImmutablePackageStore _store;
    private readonly string _stagingDirectory;
    private readonly long _maxUploadBytes;

    public IkpPackageIngestionService(
        IkpPackageValidator validator,
        IImmutablePackageStore store,
        string stagingDirectory,
        long maxUploadBytes = 250L * 1024 * 1024)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingDirectory);
        _stagingDirectory = Path.GetFullPath(stagingDirectory);
        _maxUploadBytes = maxUploadBytes;
    }

    public async Task<IkpIngestionResult> ReceiveValidateArchiveAsync(
        Stream upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        Directory.CreateDirectory(_stagingDirectory);
        var stagedPath = Path.Combine(_stagingDirectory, $"receive-{Guid.NewGuid():N}.tmp");
        try
        {
            var (hash, length) = await StageUploadAsync(upload, stagedPath, cancellationToken);
            await using var validationStream = File.OpenRead(stagedPath);
            var validation = await _validator.ValidateAsync(validationStream, cancellationToken);
            if (validation.ZipByteLength != length ||
                !string.Equals(validation.ZipSha256, hash, StringComparison.Ordinal))
                throw new IkpPackageValidationException("O conteúdo recebido mudou entre a recepção e a validação.");

            var archive = await _store.ArchiveValidatedPackageAsync(stagedPath, validation, cancellationToken);
            return new IkpIngestionResult(validation, archive);
        }
        finally
        {
            try
            {
                if (File.Exists(stagedPath))
                {
                    File.SetAttributes(stagedPath, FileAttributes.Normal);
                    File.Delete(stagedPath);
                }
            }
            catch { /* Best-effort cleanup; a later staging cleanup can remove leftovers. */ }
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
}

public sealed record IkpIngestionResult(
    IkpPackageValidationResult Validation,
    ArchivedPackage Archive);