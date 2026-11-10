using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Receipts;

public interface IPackageReceiptRepository
{
    Task<PackageReceiptRegistration> RegisterArchivedPackageAsync(
        Guid organizationId,
        IkpPackageValidationResult validation,
        ArchivedPackage archive,
        CancellationToken cancellationToken = default);
}