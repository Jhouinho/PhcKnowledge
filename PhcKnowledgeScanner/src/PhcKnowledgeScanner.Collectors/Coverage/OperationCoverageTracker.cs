using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;

namespace PhcKnowledgeScanner.Collectors.Coverage;

/// <summary>
/// Instrumentação mínima de coverage por operação lógica dentro de um collector.
/// Publica resultados conclusivos em <see cref="CollectorExecutionProgress"/> quando fornecido.
/// </summary>
internal sealed class OperationCoverageTracker
{
    private readonly List<CollectionIssue> _issues;
    private readonly CollectorExecutionProgress? _progress;
    private readonly List<OperationCoverage> _coverages = new();
    private readonly HashSet<string> _completed = new(StringComparer.OrdinalIgnoreCase);
    private int _issueSeq;

    public OperationCoverageTracker(List<CollectionIssue> issues, CollectorExecutionProgress? progress = null)
    {
        _issues = issues;
        _progress = progress;
    }

    public IReadOnlyList<OperationCoverage> Coverages => _coverages;

    public async Task RunAsync(
        DeclaredCapability capability,
        Func<Task> action,
        Func<int> observedCount,
        IReadOnlyDictionary<string, string>? scope = null)
    {
        var startIssues = _issues.Count;
        var startObserved = observedCount();
        _progress?.BeginOperation(capability.OperationId);
        try
        {
            await action().ConfigureAwait(false);
            StampIssues(startIssues, capability.OperationId);
            var slice = _issues.Skip(startIssues).ToList();
            PublishIssues(slice);
            var observed = Math.Max(0, observedCount() - startObserved);
            Complete(
                CoverageSemantics.FromIssues(
                    capability,
                    requested: true,
                    observed,
                    slice,
                    SourceAvailability.Present,
                    scope));
        }
        catch (OperationCanceledException)
        {
            StampIssues(startIssues, capability.OperationId);
            PublishIssues(_issues.Skip(startIssues).ToList());
            // Sem resultado conclusivo: abandona in-flight; o motor trata como NotAttempted.
            _progress?.AbandonInFlight();
            throw;
        }
        catch (Exception ex)
        {
            var issue = new CollectionIssue(
                CollectionIssueSeverity.Error,
                capability.OperationId + ".unhandled",
                "Exceção não tratada na operação.",
                Detail: ex.GetType().FullName + ": " + ex.Message,
                IssueId: NextIssueId(capability.OperationId),
                OperationId: capability.OperationId);
            _issues.Add(issue);
            var observed = Math.Max(0, observedCount() - startObserved);
            StampIssues(startIssues, capability.OperationId);
            var slice = _issues.Skip(startIssues).ToList();
            PublishIssues(slice);
            Complete(
                CoverageSemantics.FromIssues(
                    capability,
                    requested: true,
                    observed,
                    slice,
                    SourceAvailability.Present,
                    scope));
        }
    }

    public async Task RunOptionalAsync(
        DeclaredCapability capability,
        Func<Task<bool>> sourceExistsAsync,
        Func<Task> action,
        Func<int> observedCount,
        string sourceName,
        IReadOnlyDictionary<string, string>? scope = null)
    {
        _progress?.BeginOperation(capability.OperationId);
        var exists = await sourceExistsAsync().ConfigureAwait(false);
        if (!exists)
        {
            var issue = new CollectionIssue(
                CollectionIssueSeverity.Info,
                capability.OperationId + ".absent",
                $"Estrutura de origem '{sourceName}' inexistente nesta instalação.",
                IssueId: NextIssueId(capability.OperationId),
                OperationId: capability.OperationId);
            _issues.Add(issue);
            PublishIssues([issue]);
            Complete(CoverageSemantics.NotPresent(
                capability,
                requested: true,
                [issue.IssueId!],
                scope ?? ScopeSource(sourceName)));
            return;
        }

        // RunAsync volta a fazer BeginOperation — abandonar o begin preliminar.
        _progress?.AbandonInFlight();
        await RunAsync(capability, action, observedCount, scope ?? ScopeSource(sourceName))
            .ConfigureAwait(false);
    }

    public void MarkNotAttempted(IEnumerable<DeclaredCapability> remaining, bool requested = true)
    {
        foreach (var capability in remaining)
        {
            if (_completed.Contains(capability.OperationId))
            {
                continue;
            }

            Complete(CoverageSemantics.NotAttempted(capability, requested));
        }
    }

    private void Complete(OperationCoverage coverage)
    {
        _coverages.Add(coverage);
        _completed.Add(coverage.OperationId);
        _progress?.CompleteOperation(coverage);
    }

    private void PublishIssues(IEnumerable<CollectionIssue> issues)
    {
        if (_progress is null)
        {
            return;
        }

        foreach (var issue in issues)
        {
            _progress.ReportIssue(issue);
        }
    }

    private void StampIssues(int fromIndex, string operationId)
    {
        for (var i = fromIndex; i < _issues.Count; i++)
        {
            var issue = _issues[i];
            if (!string.IsNullOrWhiteSpace(issue.IssueId) &&
                string.Equals(issue.OperationId, operationId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _issues[i] = issue with
            {
                IssueId = string.IsNullOrWhiteSpace(issue.IssueId)
                    ? NextIssueId(operationId)
                    : issue.IssueId,
                OperationId = string.IsNullOrWhiteSpace(issue.OperationId)
                    ? operationId
                    : issue.OperationId
            };
        }
    }

    private string NextIssueId(string operationId)
    {
        _issueSeq++;
        return $"issue:{operationId}:{_issueSeq}";
    }

    private static IReadOnlyDictionary<string, string> ScopeSource(string sourceName)
        => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["source"] = sourceName
        };
}
