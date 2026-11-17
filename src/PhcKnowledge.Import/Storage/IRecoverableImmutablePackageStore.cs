namespace PhcKnowledge.Import.Storage;

/// <summary>
/// Optional verification capability used when recovery finds no staging copy.
/// </summary>
public interface IRecoverableImmutablePackageStore
{
    Task<bool> VerifyArchivedPackageAsync(
        string storageKey,
        string expectedSha256,
        long expectedByteLength,
        CancellationToken cancellationToken = default);
}
