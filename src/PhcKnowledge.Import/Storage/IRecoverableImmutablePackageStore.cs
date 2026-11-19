namespace PhcKnowledge.Import.Storage;

/// <summary>
/// Optional verification and read capability for content-addressed archived packages.
/// </summary>
public interface IRecoverableImmutablePackageStore
{
    Task<bool> VerifyArchivedPackageAsync(
        string storageKey,
        string expectedSha256,
        long expectedByteLength,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an archived package for read-only consumption after verifying length and
    /// SHA-256 on the same stream that is returned (repositioned to the start).
    /// The caller owns the returned stream and must dispose it.
    /// </summary>
    Task<Stream> OpenArchivedPackageReadAsync(
        string storageKey,
        string expectedSha256,
        long expectedByteLength,
        CancellationToken cancellationToken = default);
}
