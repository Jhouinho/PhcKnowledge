using System.Collections.Concurrent;

namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Process-local append-only decision store for unit tests. Not a substitute for SQL uniqueness tests.
/// </summary>
public sealed class InMemoryPackageImportAnalysisDecisionRepository : IPackageImportAnalysisDecisionRepository
{
    private readonly ConcurrentDictionary<string, List<PackageImportAnalysisDecision>> _byAnalysis =
        new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public Task<PackageImportAnalysisDecision?> GetEffectiveAsync(
        Guid analysisId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_byAnalysis.TryGetValue(Key(analysisId, organizationId), out var list) || list.Count == 0)
                return Task.FromResult<PackageImportAnalysisDecision?>(null);

            return Task.FromResult<PackageImportAnalysisDecision?>(
                list.OrderByDescending(x => x.DecisionSequence).First());
        }
    }

    public Task<IReadOnlyList<PackageImportAnalysisDecision>> ListHistoryAsync(
        Guid analysisId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_byAnalysis.TryGetValue(Key(analysisId, organizationId), out var list) || list.Count == 0)
                return Task.FromResult<IReadOnlyList<PackageImportAnalysisDecision>>(
                    Array.Empty<PackageImportAnalysisDecision>());

            return Task.FromResult<IReadOnlyList<PackageImportAnalysisDecision>>(
                list.OrderByDescending(x => x.DecisionSequence).ToArray());
        }
    }

    public Task AppendAsync(
        PackageImportAnalysisDecision decision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        cancellationToken.ThrowIfCancellationRequested();

        if (decision.DecisionSequence < 1)
            throw new ArgumentOutOfRangeException(nameof(decision), "DecisionSequence deve ser >= 1.");

        lock (_gate)
        {
            var key = Key(decision.AnalysisId, decision.OrganizationId);
            var list = _byAnalysis.GetOrAdd(key, _ => []);
            if (list.Any(x => x.DecisionSequence == decision.DecisionSequence))
                throw new PackageImportAnalysisDecisionSequenceConflictException(
                    decision.AnalysisId, decision.OrganizationId, decision.DecisionSequence);

            list.Add(decision);
        }

        return Task.CompletedTask;
    }

    private static string Key(Guid analysisId, Guid organizationId) =>
        $"{analysisId:D}|{organizationId:D}";
}
