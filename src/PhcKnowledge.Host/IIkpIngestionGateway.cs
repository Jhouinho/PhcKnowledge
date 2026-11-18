using PhcKnowledge.Import.Ikp;

namespace PhcKnowledge.Host;

/// <summary>
/// Host-facing ingress seam over <see cref="IkpPackageIngestionService"/>.
/// Keeps HTTP mapping testable without changing Import semantics.
/// </summary>
public interface IIkpIngestionGateway
{
    Task<IkpIngestionResult> ReceiveAsync(
        Stream upload,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}

public sealed class IkpIngestionGateway(IkpPackageIngestionService ingestion) : IIkpIngestionGateway
{
    public Task<IkpIngestionResult> ReceiveAsync(
        Stream upload,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        ingestion.ReceiveValidateArchiveAsync(upload, organizationId, cancellationToken);
}
