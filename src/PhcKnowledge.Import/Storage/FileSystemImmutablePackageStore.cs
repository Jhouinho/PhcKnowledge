using System.Security.Cryptography;
using PhcKnowledge.Import.Ikp;

namespace PhcKnowledge.Import.Storage;

/// <summary>
/// Content-addressed filesystem archive. Existing objects are never overwritten;
/// a pre-existing object is accepted only after its length and SHA-256 are rechecked.
/// </summary>
public sealed class FileSystemImmutablePackageStore : IImmutablePackageStore
{
    private readonly string _root;

    public FileSystemImmutablePackageStore(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = Path.GetFullPath(root);
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

        try
        {
            File.Move(validatedPackagePath, destination, overwrite: false);
            File.SetAttributes(destination, File.GetAttributes(destination) | FileAttributes.ReadOnly);
            return new ArchivedPackage(relativeKey.Replace(Path.DirectorySeparatorChar, '/'), false);
        }
        catch (IOException) when (File.Exists(destination))
        {
            await EnsureSameContentAsync(destination, hash, actualLength, cancellationToken);
            return new ArchivedPackage(relativeKey.Replace(Path.DirectorySeparatorChar, '/'), true);
        }
    }

    private static async Task EnsureSameContentAsync(
        string path, string expectedHash, long expectedLength, CancellationToken cancellationToken)
    {
        var (hash, length) = await HashFileAsync(path, cancellationToken);
        if (length != expectedLength || !string.Equals(hash, expectedHash, StringComparison.Ordinal))
            throw new IOException("Colisão ou corrupção no arquivo content-addressed; o ficheiro existente não foi substituído.");
    }

    private static async Task<(string Hash, long Length)> HashFileAsync(
        string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        return (hash, stream.Length);
    }
}