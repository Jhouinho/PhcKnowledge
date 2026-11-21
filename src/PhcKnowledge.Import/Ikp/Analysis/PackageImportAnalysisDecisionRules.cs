namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Pure domain rules for Increment C decisions (eligibility, rationale, idempotency comparison).
/// Sequence assignment and persistence orchestration live in the decision service.
/// </summary>
public static class PackageImportAnalysisDecisionRules
{
    public const int RationaleMaxLength = 2000;

    public static bool IsEligible(
        MaterializationReadiness readiness,
        PackageImportAnalysisDecisionKind decision) =>
        readiness switch
        {
            MaterializationReadiness.Ready => true,
            MaterializationReadiness.ReadyWithWarnings => true,
            MaterializationReadiness.Blocked => decision == PackageImportAnalysisDecisionKind.Rejected,
            _ => false
        };

    public static MaterializationReadiness ParseReadinessStatus(string readinessStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readinessStatus);
        if (Enum.TryParse<MaterializationReadiness>(readinessStatus.Trim(), ignoreCase: false, out var status))
            return status;
        throw new PackageImportAnalysisDecisionValidationException(
            $"ReadinessStatus inválido: '{readinessStatus}'.");
    }

    /// <summary>
    /// Validates rationale for the decision kind. Does not trim or otherwise alter the text.
    /// Rejected: required non-empty, not whitespace-only, length ≤ 2000.
    /// Confirmed: optional; when present, length ≤ 2000 (empty / whitespace-only allowed as literal values).
    /// Returns the original <paramref name="rationale"/> unchanged (including null).
    /// </summary>
    public static string? ValidateRationale(
        PackageImportAnalysisDecisionKind decision,
        string? rationale)
    {
        if (decision == PackageImportAnalysisDecisionKind.Rejected)
        {
            if (string.IsNullOrWhiteSpace(rationale))
                throw new PackageImportAnalysisDecisionValidationException(
                    "A rejeição exige justificação.");

            if (rationale.Length > RationaleMaxLength)
                throw new PackageImportAnalysisDecisionValidationException(
                    $"A justificação não pode exceder {RationaleMaxLength} caracteres.");

            return rationale;
        }

        // Confirmed: optional. Persist the original value; do not trim.
        if (rationale is not null && rationale.Length > RationaleMaxLength)
            throw new PackageImportAnalysisDecisionValidationException(
                $"A justificação não pode exceder {RationaleMaxLength} caracteres.");

        return rationale;
    }

    /// <summary>
    /// Idempotency comparison: ordinal exact equality of the original rationale text.
    /// No Trim, case-folding, Unicode normalization, or other transforms.
    /// </summary>
    public static bool AreRationalesEquivalent(string? left, string? right) =>
        string.Equals(left, right, StringComparison.Ordinal);

    public static bool IsIdempotentReplay(
        PackageImportAnalysisDecision effective,
        PackageImportAnalysisDecisionKind decision,
        string? rationale) =>
        effective.Decision == decision &&
        AreRationalesEquivalent(effective.Rationale, rationale);
}

public sealed class PackageImportAnalysisDecisionNotEligibleException : Exception
{
    public PackageImportAnalysisDecisionNotEligibleException(
        MaterializationReadiness readiness,
        PackageImportAnalysisDecisionKind decision)
        : base($"A decisão '{decision}' não é elegível para readiness '{readiness}'.")
    {
        Readiness = readiness;
        Decision = decision;
    }

    public MaterializationReadiness Readiness { get; }
    public PackageImportAnalysisDecisionKind Decision { get; }
}

public sealed class PackageImportAnalysisDecisionValidationException : Exception
{
    public PackageImportAnalysisDecisionValidationException(string message)
        : base(message)
    {
    }
}

public sealed class PackageImportAnalysisDecisionOrganizationMismatchException : Exception
{
    public PackageImportAnalysisDecisionOrganizationMismatchException(
        Guid analysisOrganizationId,
        Guid actorOrganizationId)
        : base("A organização do decisor não coincide com a da análise.")
    {
        AnalysisOrganizationId = analysisOrganizationId;
        ActorOrganizationId = actorOrganizationId;
    }

    public Guid AnalysisOrganizationId { get; }
    public Guid ActorOrganizationId { get; }
}

/// <summary>
/// Raised when an append hits UNIQUE (AnalysisId, DecisionSequence) (SQL 2601/2627).
/// Callers must re-read the effective decision and resolve idempotency or retry with a new sequence.
/// Must not be treated as a successful idempotent replay by itself.
/// </summary>
public sealed class PackageImportAnalysisDecisionSequenceConflictException : Exception
{
    public PackageImportAnalysisDecisionSequenceConflictException(
        Guid analysisId,
        Guid organizationId,
        int attemptedSequence,
        Exception? innerException = null)
        : base(
            $"Conflito de DecisionSequence {attemptedSequence} para AnalysisId {analysisId:D}.",
            innerException)
    {
        AnalysisId = analysisId;
        OrganizationId = organizationId;
        AttemptedSequence = attemptedSequence;
    }

    public Guid AnalysisId { get; }
    public Guid OrganizationId { get; }
    public int AttemptedSequence { get; }
}
