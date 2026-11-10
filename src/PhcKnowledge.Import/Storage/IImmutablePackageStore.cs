using PhcKnowledge.Import.Ikp;

namespace PhcKnowledge.Import.Storage;

public sealed record ArchivedPackage(string StorageKey, bool AlreadyExisted);

public interface IImmutablePackageStore
{
    Task<ArchivedPackage> ArchiveValidatedPackageAsync(
        string validatedPackagePath,
        IkpPackageValidationResult validation,
        CancellationToken cancellationToken = default);
}