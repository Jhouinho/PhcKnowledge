namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Domain orchestration for human confirmation/rejection of a persisted analysis (Increment C).
/// Does not authenticate; the host supplies the server-resolved <see cref="PackageImportAnalysisDecisionActor"/>.
/// Does not load analysis by id — the caller passes the persisted analysis row (source of fingerprint/readiness).
/// </summary>
public sealed class PackageImportAnalysisDecisionService
{
    /// <summary>
    /// Limited retries when append races on DecisionSequence (unique violation) or equivalent conflicts.
    /// Aligns with <see cref="SqlServerDeadlockRetry.MaxAttempts"/>.
    /// </summary>
    public const int MaxSequenceConflictAttempts = 4;

    private readonly IPackageImportAnalysisDecisionRepository _decisions;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newDecisionId;

    public PackageImportAnalysisDecisionService(
        IPackageImportAnalysisDecisionRepository decisions,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newDecisionId = null)
    {
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newDecisionId = newDecisionId ?? Guid.NewGuid;
    }

    public Task<PackageImportAnalysisDecision?> GetEffectiveAsync(
        Guid analysisId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        _decisions.GetEffectiveAsync(analysisId, organizationId, cancellationToken);

    public Task<IReadOnlyList<PackageImportAnalysisDecision>> ListHistoryAsync(
        Guid analysisId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        _decisions.ListHistoryAsync(analysisId, organizationId, cancellationToken);

    /// <summary>
    /// Applies eligibility → rationale validation → idempotency → append (or replay).
    /// Identity fields on the stored row always come from <paramref name="actor"/> and
    /// <see cref="PackageImportAnalysisDecisionIdentity.Issuer"/> — never from the client.
    /// Sequence conflicts from concurrent appends trigger a limited re-read/retry loop.
    /// </summary>
    public async Task<PackageImportAnalysisDecisionWriteResult> DecideAsync(
        PersistedPackageImportAnalysis analysis,
        PackageImportAnalysisDecisionKind decision,
        string? rationale,
        PackageImportAnalysisDecisionActor actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(actor);

        if (actor.UserId == Guid.Empty)
            throw new PackageImportAnalysisDecisionValidationException("DecidedBySubject é obrigatório.");
        if (actor.OrganizationId == Guid.Empty)
            throw new PackageImportAnalysisDecisionValidationException("OrganizationId do decisor é obrigatório.");
        if (analysis.AnalysisId == Guid.Empty)
            throw new PackageImportAnalysisDecisionValidationException("AnalysisId é obrigatório.");
        if (analysis.OrganizationId == Guid.Empty)
            throw new PackageImportAnalysisDecisionValidationException("OrganizationId da análise é obrigatório.");

        if (analysis.OrganizationId != actor.OrganizationId)
            throw new PackageImportAnalysisDecisionOrganizationMismatchException(
                analysis.OrganizationId, actor.OrganizationId);

        var readiness = PackageImportAnalysisDecisionRules.ParseReadinessStatus(analysis.ReadinessStatus);
        if (!PackageImportAnalysisDecisionRules.IsEligible(readiness, decision))
            throw new PackageImportAnalysisDecisionNotEligibleException(readiness, decision);

        // Validation is separate from idempotency: do not trim or rewrite the original text.
        var validatedRationale = PackageImportAnalysisDecisionRules.ValidateRationale(
            decision, rationale);

        if (string.IsNullOrWhiteSpace(analysis.SemanticFingerprint) ||
            analysis.SemanticFingerprint.Length != 64)
        {
            throw new PackageImportAnalysisDecisionValidationException(
                "SemanticFingerprint da análise persistida é inválido.");
        }

        PackageImportAnalysisDecisionSequenceConflictException? lastConflict = null;
        for (var attempt = 1; attempt <= MaxSequenceConflictAttempts; attempt++)
        {
            var effective = await _decisions.GetEffectiveAsync(
                analysis.AnalysisId, analysis.OrganizationId, cancellationToken);

            if (effective is not null &&
                PackageImportAnalysisDecisionRules.IsIdempotentReplay(effective, decision, validatedRationale))
            {
                return new PackageImportAnalysisDecisionWriteResult(effective, IdempotentReplay: true);
            }

            var nextSequence = effective is null ? 1 : effective.DecisionSequence + 1;
            var row = new PackageImportAnalysisDecision(
                DecisionId: _newDecisionId(),
                AnalysisId: analysis.AnalysisId,
                OrganizationId: analysis.OrganizationId,
                Decision: decision,
                DecisionSequence: nextSequence,
                DecidedAtUtc: _clock(),
                DecidedBySubject: actor.Subject,
                DecidedByIssuer: PackageImportAnalysisDecisionIdentity.Issuer,
                Rationale: validatedRationale,
                SemanticFingerprintSnapshot: analysis.SemanticFingerprint);

            try
            {
                await _decisions.AppendAsync(row, cancellationToken);
                return new PackageImportAnalysisDecisionWriteResult(row, IdempotentReplay: false);
            }
            catch (PackageImportAnalysisDecisionSequenceConflictException ex)
            {
                lastConflict = ex;
                if (attempt >= MaxSequenceConflictAttempts)
                    break;

                // Another writer won the sequence. Re-read: may become idempotent replay or retry next seq.
                await Task.Delay(SqlServerDeadlockRetry.DelayBeforeRetry(attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Falha a gravar decisão após conflitos de DecisionSequence repetidos.",
            lastConflict);
    }
}
