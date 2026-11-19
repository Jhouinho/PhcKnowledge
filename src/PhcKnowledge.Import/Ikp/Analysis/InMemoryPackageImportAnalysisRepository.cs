using System.Collections.Concurrent;

namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Process-local repository for HTTP/unit tests. Not a substitute for SQL Server uniqueness tests.
/// </summary>
public sealed class InMemoryPackageImportAnalysisRepository : IPackageImportAnalysisRepository
{
    private readonly ConcurrentDictionary<string, PersistedPackageImportAnalysis> _items = new(StringComparer.Ordinal);

    public Task<PersistedPackageImportAnalysis?> GetAsync(
        Guid organizationId,
        Guid packageId,
        string zipSha256,
        string analysisSchemaVersion,
        CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(Key(organizationId, packageId, zipSha256, analysisSchemaVersion), out var value);
        return Task.FromResult(value);
    }

    public Task<PersistedPackageImportAnalysis> InsertOrGetExistingAsync(
        PersistedPackageImportAnalysis analysis,
        CancellationToken cancellationToken = default)
    {
        var key = Key(
            analysis.OrganizationId,
            analysis.PackageId,
            analysis.ZipSha256,
            analysis.AnalysisSchemaVersion);
        var stored = _items.GetOrAdd(key, analysis);
        return Task.FromResult(stored);
    }

    private static string Key(
        Guid organizationId, Guid packageId, string zipSha256, string analysisSchemaVersion) =>
        $"{organizationId:D}|{packageId:D}|{zipSha256}|{analysisSchemaVersion}";
}
