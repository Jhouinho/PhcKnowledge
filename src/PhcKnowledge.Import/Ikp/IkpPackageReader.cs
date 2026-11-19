namespace PhcKnowledge.Import.Ikp;

/// <summary>
/// Reads a validated IKP 0.1 package in a single parse pass, exposing both the
/// validation summary and the typed inspection used by import analysis.
/// </summary>
public sealed class IkpPackageReader
{
    private readonly IkpPackageInspectionEngine _engine;

    public IkpPackageReader(IkpPackageInspectionEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public IkpPackageReader(
        string? schemaDirectory = null,
        long maxZipBytes = 250L * 1024 * 1024,
        long maxExpandedBytes = 500L * 1024 * 1024,
        long maxEntryBytes = 100L * 1024 * 1024,
        int maxEntries = 10_000)
        : this(new IkpPackageInspectionEngine(
            schemaDirectory, maxZipBytes, maxExpandedBytes, maxEntryBytes, maxEntries))
    {
    }

    public IkpPackageInspectionEngine Engine => _engine;

    public Task<IkpPackageInspection> ReadAsync(
        Stream packageStream,
        CancellationToken cancellationToken = default) =>
        _engine.InspectAsync(packageStream, cancellationToken);
}
