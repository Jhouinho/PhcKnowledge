using PhcKnowledgeScanner.Abstractions.Collectors;

namespace PhcKnowledgeScanner.Abstractions.Coverage;

/// <summary>
/// Progresso vivo de coverage durante <c>CollectAsync</c>.
/// Permite ao motor preservar operações já concluídas quando o collector
/// lança uma exceção antes de devolver <see cref="Collectors.CollectorResult"/>.
/// </summary>
public sealed class CollectorExecutionProgress
{
    private readonly List<OperationCoverage> _coverages = new();
    private readonly List<CollectionIssue> _issues = new();
    private readonly HashSet<string> _operationIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Operação iniciada sem resultado conclusivo ainda registado.</summary>
    public string? InFlightOperationId { get; private set; }

    public IReadOnlyList<OperationCoverage> Coverages => _coverages;

    public IReadOnlyList<CollectionIssue> Issues => _issues;

    public void Reset()
    {
        _coverages.Clear();
        _issues.Clear();
        _operationIds.Clear();
        InFlightOperationId = null;
    }

    public void BeginOperation(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        InFlightOperationId = operationId;
    }

    /// <summary>
    /// Regista o resultado conclusivo de uma operação e limpa o estado in-flight.
    /// Ignora duplicados do mesmo <paramref name="coverage"/>.OperationId (mantém o primeiro).
    /// </summary>
    public void CompleteOperation(OperationCoverage coverage)
    {
        ArgumentNullException.ThrowIfNull(coverage);
        if (string.Equals(InFlightOperationId, coverage.OperationId, StringComparison.OrdinalIgnoreCase))
        {
            InFlightOperationId = null;
        }

        if (!_operationIds.Add(coverage.OperationId))
        {
            return;
        }

        _coverages.Add(coverage);
    }

    public void AbandonInFlight()
    {
        InFlightOperationId = null;
    }

    public void ReportIssue(CollectionIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        if (!string.IsNullOrWhiteSpace(issue.IssueId)
            && _issues.Any(i => string.Equals(i.IssueId, issue.IssueId, StringComparison.Ordinal)))
        {
            return;
        }

        _issues.Add(issue);
    }
}
