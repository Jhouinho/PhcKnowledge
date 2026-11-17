using System.Security.Cryptography;
using PhcKnowledge.Import.Ikp;

namespace PhcKnowledge.Import.Storage;

/// <summary>
/// Content-addressed filesystem archive. Existing objects are never overwritten;
/// a pre-existing object is accepted only after its length and SHA-256 are rechecked.
/// </summary>
public sealed class FileSystemImmutablePackageStore : IImmutablePackageStore, IRecoverableImmutablePackageStore, IExpectedStorageKeyProvider
{
    private readonly string _root;

    public FileSystemImmutablePackageStore(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = Path.GetFullPath(root);
    }

    public string GetStorageKey(IkpPackageValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        var hash = validation.ZipSha256;
        if (hash.Length != 64 || hash.Any(c => !Uri.IsHexDigit(c)) ||
            hash != hash.ToLowerInvariant())
            throw new ArgumentException("SHA-256 inválido para arquivo.", nameof(validation));
        return $"sha256/{hash[..2]}/{hash}.ikp.zip";
    }

    public async Task<ArchivedPackage> ArchiveValidatedPackageAsync(
        string validatedPackagePath,
        IkpPackageValidationResult validation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validation);
        var hash = validation.ZipSha256;
        if (hash.Length != 64 || hash.Any(c => !Uri.IsHexDigit(c)) || hash != hash.ToLowerInvariant())
            throw new InvalidOperationException("SHA-256 inválido para arquivo.");
        if (!File.Exists(validatedPackagePath))
            throw new FileNotFoundException("O pacote validado deixou de existir.", validatedPackagePath);

        var (actualHash, actualLength) = await HashFileAsync(validatedPackagePath, cancellationToken);
        if (!string.Equals(actualHash, hash, StringComparison.Ordinal) || actualLength != validation.ZipByteLength)
            throw new InvalidOperationException("O ficheiro mudou depois da validação; o arquivo foi cancelado.");

        var relativeKey = Path.Combine("sha256", hash[..2], $"{hash}.ikp.zip");
        var destination = Path.Combine(_root, relativeKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        if (File.Exists(destination))
        {
            await EnsureSameContentAsync(destination, hash, actualLength, cancellationToken);
            return new ArchivedPackage(relativeKey.Replace(Path.DirectorySeparatorChar, '/'), true);
        }

        var temporaryDestination = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var source = File.OpenRead(validatedPackagePath))
            await using (var target = new FileStream(temporaryDestination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                await source.CopyToAsync(target, 81920, cancellationToken);
                await target.FlushAsync(cancellationToken);
            }

            await EnsureSameContentAsync(temporaryDestination, hash, actualLength, cancellationToken);
            try
            {
                File.Move(temporaryDestination, destination, overwrite: false);
                File.SetAttributes(destination, File.GetAttributes(destination) | FileAttributes.ReadOnly);
                return new ArchivedPackage(relativeKey.Replace(Path.DirectorySeparatorChar, '/'), false);
            }
            catch (IOException) when (File.Exists(destination))
            {
                await EnsureSameContentAsync(destination, hash, actualLength, cancellationToken);
                return new ArchivedPackage(relativeKey.Replace(Path.DirectorySeparatorChar, '/'), true);
            }
        }
        finally
        {
            try { if (File.Exists(temporaryDestination)) File.Delete(temporaryDestination); } catch { }
        }
    }


    public async Task<bool> VerifyArchivedPackageAsync(
        string storageKey,
        string expectedSha256,
        long expectedByteLength,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        if (Path.IsPathRooted(storageKey) || storageKey.Contains('\\') ||
            storageKey.Split('/').Any(part => part is "" or "." or ".."))
            throw new ArgumentException("StorageKey inválida.", nameof(storageKey));
        if (expectedSha256.Length != 64 || expectedSha256.Any(c => !Uri.IsHexDigit(c)) ||
            expectedSha256 != expectedSha256.ToLowerInvariant() || expectedByteLength <= 0)
            throw new ArgumentException("Integridade esperada inválida.");
        var expectedStorageKey = $"sha256/{expectedSha256[..2]}/{expectedSha256}.ikp.zip";
        if (!string.Equals(storageKey, expectedStorageKey, StringComparison.Ordinal))
            throw new ArgumentException("StorageKey não corresponde ao SHA-256 esperado.", nameof(storageKey));

        var path = Path.GetFullPath(Path.Combine(
            _root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new ArgumentException("StorageKey fora do arquivo.", nameof(storageKey));
        if (!File.Exists(path)) return false;

        await EnsureSameContentAsync(path, expectedSha256, expectedByteLength, cancellationToken);
        return true;
    }

    private static async Task EnsureSameContentAsync(
        string path, string expectedHash, long expectedLength, CancellationToken cancellationToken)
    {
        var (hash, length) = await HashFileAsync(path, cancellationToken);
        if (length != expectedLength || !string.Equals(hash, expectedHash, StringComparison.Ordinal))
            throw new InvalidDataException("Colisão ou corrupção no arquivo content-addressed; o ficheiro existente não foi substituído.");
    }

    private static async Task<(string Hash, long Length)> HashFileAsync(
        string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        return (hash, stream.Length);
    }
}