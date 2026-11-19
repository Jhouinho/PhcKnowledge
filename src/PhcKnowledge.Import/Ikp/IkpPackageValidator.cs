namespace PhcKnowledge.Import.Ikp;

/// <summary>
/// Validates an IKP 0.1 ZIP without trusting its paths or extracting outside
/// an isolated temporary directory. It does not associate installations,
/// import observations or publish knowledge.
/// </summary>
public sealed class IkpPackageValidator
{
    private readonly IkpPackageInspectionEngine _engine;

    public IkpPackageValidator(
        string? schemaDirectory = null,
        long maxZipBytes = 250L * 1024 * 1024,
        long maxExpandedBytes = 500L * 1024 * 1024,
        long maxEntryBytes = 100L * 1024 * 1024,
        int maxEntries = 10_000)
        : this(new IkpPackageInspectionEngine(
            schemaDirectory, maxZipBytes, maxExpandedBytes, maxEntryBytes, maxEntries))
    {
    }

    public IkpPackageValidator(IkpPackageInspectionEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public IkpPackageInspectionEngine Engine => _engine;

    public async Task<IkpPackageValidationResult> ValidateAsync(
        Stream packageStream,
        CancellationToken cancellationToken = default)
    {
        var inspection = await _engine.InspectAsync(packageStream, cancellationToken);
        return inspection.Validation;
    }
}
