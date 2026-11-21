using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Ikp.Analysis;

public sealed class PackageImportAnalysisNotFoundException : Exception
{
    public PackageImportAnalysisNotFoundException(Guid organizationId, Guid packageId)
        : base("Package analysis not available for the authenticated organization.")
    {
        OrganizationId = organizationId;
        PackageId = packageId;
    }

    public Guid OrganizationId { get; }
    public Guid PackageId { get; }
}

/// <summary>
/// Executes and persists package import analyses (POST) and reads persisted analyses (GET).
/// Does not materialize knowledge or confirm import.
/// </summary>
public sealed class IkpPackageAnalysisService
{
    private readonly IPackageReceiptAttemptRepository _attempts;
    private readonly IRecoverableImmutablePackageStore _packageStore;
    private readonly IkpPackageReader _reader;
    private readonly IPackageImportAnalysisRepository _analyses;
    private readonly IkpPackageAnalysisBuilder _builder;
    private readonly Func<DateTimeOffset> _clock;

    public IkpPackageAnalysisService(
        IPackageReceiptAttemptRepository attempts,
        IRecoverableImmutablePackageStore packageStore,
        IkpPackageReader reader,
        IPackageImportAnalysisRepository analyses,
        IkpPackageAnalysisBuilder? builder = null,
        Func<DateTimeOffset>? clock = null)
    {
        _attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));
        _packageStore = packageStore ?? throw new ArgumentNullException(nameof(packageStore));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _analyses = analyses ?? throw new ArgumentNullException(nameof(analyses));
        _builder = builder ?? new IkpPackageAnalysisBuilder();
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// POST semantics: return compatible persisted analysis or compute, persist, and return.
    /// The returned row carries the real persisted <see cref="PersistedPackageImportAnalysis.AnalysisId"/>.
    /// </summary>
    public async Task<PersistedPackageImportAnalysis> ExecuteAsync(
        Guid organizationId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await RequireCompletedReceiptAsync(organizationId, packageId, cancellationToken);

        var existing = await _analyses.GetAsync(
            organizationId,
            packageId,
            receipt.ZipSha256,
            PackageImportAnalysisSchema.Version,
            cancellationToken);
        if (existing is not null)
        {
            // Compatible key wins. ReceiptId on the row is first-writer provenance and is not rewritten.
            return existing;
        }

        var computed = await ComputeAsync(receipt, cancellationToken);
        var persisted = PersistedPackageImportAnalysis.FromAnalysis(
            Guid.NewGuid(),
            computed,
            receipt.ZipByteLength,
            _clock());
        // InsertOrGetExisting may return an earlier winner (incl. different ReceiptId); preserve that row.
        return await _analyses.InsertOrGetExistingAsync(persisted, cancellationToken);
    }

    /// <summary>
    /// GET semantics: read-only. Never opens the ZIP or creates rows.
    /// </summary>
    public async Task<PersistedPackageImportAnalysis> GetPersistedAsync(
        Guid organizationId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await RequireCompletedReceiptAsync(organizationId, packageId, cancellationToken);
        var existing = await _analyses.GetAsync(
            organizationId,
            packageId,
            receipt.ZipSha256,
            PackageImportAnalysisSchema.Version,
            cancellationToken);
        if (existing is null)
            throw new PackageImportAnalysisNotFoundException(organizationId, packageId);
        return existing;
    }

    /// <summary>
    /// Compatibility wrapper used by older call sites; prefer <see cref="ExecuteAsync"/>.
    /// </summary>
    public Task<PersistedPackageImportAnalysis> AnalyzeAsync(
        Guid organizationId,
        Guid packageId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(organizationId, packageId, cancellationToken);

    private async Task<PackageReceipt> RequireCompletedReceiptAsync(
        Guid organizationId,
        Guid packageId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId é obrigatório.", nameof(organizationId));
        if (packageId == Guid.Empty)
            throw new ArgumentException("PackageId é obrigatório.", nameof(packageId));

        var receipt = await _attempts.GetCompletedReceiptByPackageIdAsync(
            organizationId, packageId, cancellationToken);
        if (receipt is null)
            throw new PackageImportAnalysisNotFoundException(organizationId, packageId);
        return receipt;
    }

    private async Task<PackageImportAnalysis> ComputeAsync(
        PackageReceipt receipt,
        CancellationToken cancellationToken)
    {
        Stream packageStream;
        try
        {
            packageStream = await _packageStore.OpenArchivedPackageReadAsync(
                receipt.StorageKey,
                receipt.ZipSha256,
                receipt.ZipByteLength,
                cancellationToken);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException or ArgumentException)
        {
            return _builder.BuildBlocked(
                receipt.OrganizationId,
                receipt.ReceiptId,
                receipt.PackageId,
                receipt.CaptureId,
                receipt.ContractVersion,
                receipt.ProducerProductId,
                receipt.ProducerProductVersion,
                receipt.OriginDatabaseName,
                receipt.ZipSha256,
                TryReadLogicalContentDigest(receipt.ValidationSummaryJson),
                archiveSha256Verified: false,
                incompatibilities: [$"Falha ao reabrir ou verificar o arquivo: {ex.Message}"],
                analyzedAtUtc: _clock());
        }

        await using (packageStream)
        {
            try
            {
                var inspection = await _reader.ReadAsync(packageStream, cancellationToken);
                if (inspection.Validation.PackageId != receipt.PackageId ||
                    inspection.Validation.CaptureId != receipt.CaptureId ||
                    !string.Equals(inspection.Validation.ZipSha256, receipt.ZipSha256, StringComparison.Ordinal))
                {
                    return _builder.BuildBlocked(
                        receipt.OrganizationId,
                        receipt.ReceiptId,
                        receipt.PackageId,
                        receipt.CaptureId,
                        receipt.ContractVersion,
                        receipt.ProducerProductId,
                        receipt.ProducerProductVersion,
                        receipt.OriginDatabaseName,
                        receipt.ZipSha256,
                        inspection.Validation.LogicalContentDigest,
                        archiveSha256Verified: true,
                        incompatibilities:
                        [
                            "O conteúdo revalidado não corresponde ao PackageId/CaptureId/SHA-256 do receipt."
                        ],
                        analyzedAtUtc: _clock());
                }

                return _builder.Build(
                    receipt.OrganizationId,
                    receipt.ReceiptId,
                    inspection,
                    archiveSha256Verified: true,
                    analyzedAtUtc: _clock());
            }
            catch (IkpPackageValidationException ex)
            {
                return _builder.BuildBlocked(
                    receipt.OrganizationId,
                    receipt.ReceiptId,
                    receipt.PackageId,
                    receipt.CaptureId,
                    receipt.ContractVersion,
                    receipt.ProducerProductId,
                    receipt.ProducerProductVersion,
                    receipt.OriginDatabaseName,
                    receipt.ZipSha256,
                    TryReadLogicalContentDigest(receipt.ValidationSummaryJson),
                    archiveSha256Verified: true,
                    incompatibilities: [ex.Message],
                    analyzedAtUtc: _clock());
            }
        }
    }

    private static string? TryReadLogicalContentDigest(string validationSummaryJson)
    {
        if (string.IsNullOrWhiteSpace(validationSummaryJson))
            return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(validationSummaryJson);
            if (doc.RootElement.TryGetProperty("LogicalContentDigest", out var digest) &&
                digest.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return digest.GetString();
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Summary is opportunistic metadata only.
        }

        return null;
    }
}
