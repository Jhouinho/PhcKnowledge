using PhcKnowledge.Import.Ikp;

namespace PhcKnowledge.Import.Storage;

/// <summary>
/// Exposes the deterministic archive key before copying, so a durable receipt attempt
/// can be committed before the filesystem side effect.
/// </summary>
public interface IExpectedStorageKeyProvider
{
    string GetStorageKey(IkpPackageValidationResult validation);
}
