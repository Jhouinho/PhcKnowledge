namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Append-only store for package import analysis decisions (Increment C).
/// SQL implementation and transactions belong to a later phase; InMemory supports domain tests.
/// </summary>
public interface IPackageImportAnalysisDecisionRepository
{
    /// <summary>
    /// Effective decision: highest <see cref="PackageImportAnalysisDecision.DecisionSequence"/>
    /// for the analysis within the organization, or null when unconfirmed.
    /// </summary>
    Task<PackageImportAnalysisDecision?> GetEffectiveAsync(
        Guid analysisId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// History ordered by <see cref="PackageImportAnalysisDecision.DecisionSequence"/> descending.
    /// </summary>
    Task<IReadOnlyList<PackageImportAnalysisDecision>> ListHistoryAsync(
        Guid analysisId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a new decision row. Does not update or delete existing rows.
    /// Caller supplies the next <see cref="PackageImportAnalysisDecision.DecisionSequence"/>.
    /// </summary>
    Task AppendAsync(
        PackageImportAnalysisDecision decision,
        CancellationToken cancellationToken = default);
}
