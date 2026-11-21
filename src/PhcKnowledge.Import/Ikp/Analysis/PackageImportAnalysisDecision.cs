namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Human confirmation/rejection of a persisted package import analysis (Increment C).
/// Append-only; logical order is <see cref="DecisionSequence"/>, not <see cref="DecidedAtUtc"/>.
/// </summary>
public sealed record PackageImportAnalysisDecision(
    Guid DecisionId,
    Guid AnalysisId,
    Guid OrganizationId,
    PackageImportAnalysisDecisionKind Decision,
    int DecisionSequence,
    DateTimeOffset DecidedAtUtc,
    string DecidedBySubject,
    string DecidedByIssuer,
    string? Rationale,
    string SemanticFingerprintSnapshot);

public enum PackageImportAnalysisDecisionKind
{
    Confirmed = 0,
    Rejected = 1
}

/// <summary>Stable Portal issuer for decision audit; never supplied by the client.</summary>
public static class PackageImportAnalysisDecisionIdentity
{
    public const string Issuer = "phcknowledge.portal";
}

/// <summary>
/// Result of a decide attempt. <see cref="IdempotentReplay"/> is true when no new row was appended.
/// </summary>
public sealed record PackageImportAnalysisDecisionWriteResult(
    PackageImportAnalysisDecision Decision,
    bool IdempotentReplay);

/// <summary>Server-resolved actor for a decision (never from the HTTP body).</summary>
public sealed record PackageImportAnalysisDecisionActor(
    Guid UserId,
    Guid OrganizationId)
{
    public string Subject => UserId.ToString("D");
}
