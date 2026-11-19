namespace PhcKnowledge.Import.Ikp.Analysis;

public interface IPackageImportAnalysisRepository
{
    Task<PersistedPackageImportAnalysis?> GetAsync(
        Guid organizationId,
        Guid packageId,
        string zipSha256,
        string analysisSchemaVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new analysis. On unique-key conflict, returns the existing compatible row
    /// (winner) without replacing it. Never updates an existing completed analysis.
    /// </summary>
    Task<PersistedPackageImportAnalysis> InsertOrGetExistingAsync(
        PersistedPackageImportAnalysis analysis,
        CancellationToken cancellationToken = default);
}
