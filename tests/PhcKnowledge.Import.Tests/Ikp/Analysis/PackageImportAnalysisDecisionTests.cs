using PhcKnowledge.Import.Ikp.Analysis;

namespace PhcKnowledge.Import.Tests.Ikp.Analysis;

public sealed class PackageImportAnalysisDecisionTests
{
    private static readonly DateTimeOffset FixedUtc = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid FixedDecisionId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task Confirm_ready_analysis_appends_sequence_1()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var actor = Actor(analysis.OrganizationId);
        var sut = CreateSut();

        var result = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, rationale: null, actor);

        Assert.False(result.IdempotentReplay);
        Assert.Equal(1, result.Decision.DecisionSequence);
        Assert.Equal(PackageImportAnalysisDecisionKind.Confirmed, result.Decision.Decision);
        Assert.Null(result.Decision.Rationale);
        Assert.Equal(analysis.AnalysisId, result.Decision.AnalysisId);
        Assert.Equal(analysis.OrganizationId, result.Decision.OrganizationId);
        Assert.Equal(actor.Subject, result.Decision.DecidedBySubject);
        Assert.Equal(PackageImportAnalysisDecisionIdentity.Issuer, result.Decision.DecidedByIssuer);
        Assert.Equal(FixedUtc, result.Decision.DecidedAtUtc);
        Assert.Equal(FixedDecisionId, result.Decision.DecisionId);
    }

    [Fact]
    public async Task Confirm_ready_with_warnings_is_allowed()
    {
        var analysis = Persisted(MaterializationReadiness.ReadyWithWarnings);
        var sut = CreateSut();

        var result = await sut.DecideAsync(
            analysis,
            PackageImportAnalysisDecisionKind.Confirmed,
            rationale: null,
            Actor(analysis.OrganizationId));

        Assert.False(result.IdempotentReplay);
        Assert.Equal(PackageImportAnalysisDecisionKind.Confirmed, result.Decision.Decision);
        Assert.Equal(1, result.Decision.DecisionSequence);
    }

    [Fact]
    public async Task Reject_ready_analysis_requires_and_stores_original_rationale()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var sut = CreateSut();

        var result = await sut.DecideAsync(
            analysis,
            PackageImportAnalysisDecisionKind.Rejected,
            rationale: "  incompleto  ",
            Actor(analysis.OrganizationId));

        Assert.False(result.IdempotentReplay);
        Assert.Equal(PackageImportAnalysisDecisionKind.Rejected, result.Decision.Decision);
        Assert.Equal("  incompleto  ", result.Decision.Rationale);
    }

    [Fact]
    public async Task Reject_blocked_analysis_is_allowed()
    {
        var analysis = Persisted(MaterializationReadiness.Blocked);
        var sut = CreateSut();

        var result = await sut.DecideAsync(
            analysis,
            PackageImportAnalysisDecisionKind.Rejected,
            rationale: "bloqueado pelo readiness",
            Actor(analysis.OrganizationId));

        Assert.False(result.IdempotentReplay);
        Assert.Equal(PackageImportAnalysisDecisionKind.Rejected, result.Decision.Decision);
    }

    [Fact]
    public async Task Confirm_blocked_analysis_is_not_eligible()
    {
        var analysis = Persisted(MaterializationReadiness.Blocked);
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<PackageImportAnalysisDecisionNotEligibleException>(() =>
            sut.DecideAsync(
                analysis,
                PackageImportAnalysisDecisionKind.Confirmed,
                rationale: null,
                Actor(analysis.OrganizationId)));

        Assert.Equal(MaterializationReadiness.Blocked, ex.Readiness);
        Assert.Equal(PackageImportAnalysisDecisionKind.Confirmed, ex.Decision);
        Assert.Null(await sut.GetEffectiveAsync(analysis.AnalysisId, analysis.OrganizationId));
    }

    [Fact]
    public async Task Reject_with_null_rationale_is_rejected()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var sut = CreateSut();

        await Assert.ThrowsAsync<PackageImportAnalysisDecisionValidationException>(() =>
            sut.DecideAsync(
                analysis,
                PackageImportAnalysisDecisionKind.Rejected,
                rationale: null,
                Actor(analysis.OrganizationId)));
    }

    [Fact]
    public async Task Reject_with_empty_rationale_is_rejected()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var sut = CreateSut();

        await Assert.ThrowsAsync<PackageImportAnalysisDecisionValidationException>(() =>
            sut.DecideAsync(
                analysis,
                PackageImportAnalysisDecisionKind.Rejected,
                rationale: "",
                Actor(analysis.OrganizationId)));
    }

    [Fact]
    public async Task Reject_with_whitespace_only_rationale_is_rejected()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var sut = CreateSut();

        await Assert.ThrowsAsync<PackageImportAnalysisDecisionValidationException>(() =>
            sut.DecideAsync(
                analysis,
                PackageImportAnalysisDecisionKind.Rejected,
                rationale: "   ",
                Actor(analysis.OrganizationId)));
    }

    [Fact]
    public async Task Confirm_without_rationale_is_allowed()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var sut = CreateSut();

        var result = await sut.DecideAsync(
            analysis,
            PackageImportAnalysisDecisionKind.Confirmed,
            rationale: null,
            Actor(analysis.OrganizationId));

        Assert.Null(result.Decision.Rationale);
        Assert.False(result.IdempotentReplay);
    }

    [Fact]
    public async Task Confirm_with_optional_rationale_persists_original_text()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var sut = CreateSut();

        var result = await sut.DecideAsync(
            analysis,
            PackageImportAnalysisDecisionKind.Confirmed,
            rationale: "  revisto  ",
            Actor(analysis.OrganizationId));

        Assert.Equal("  revisto  ", result.Decision.Rationale);
    }

    [Fact]
    public async Task Exact_same_decision_and_rationale_is_idempotent_replay()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var actor = Actor(analysis.OrganizationId);
        var sut = CreateSut();

        var first = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, "ok", actor);
        var second = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, "ok", actor);

        Assert.False(first.IdempotentReplay);
        Assert.True(second.IdempotentReplay);
        Assert.Equal(first.Decision.DecisionId, second.Decision.DecisionId);
        Assert.Equal(1, second.Decision.DecisionSequence);

        var history = await sut.ListHistoryAsync(analysis.AnalysisId, analysis.OrganizationId);
        Assert.Single(history);
    }

    [Fact]
    public async Task Leading_whitespace_difference_creates_new_decision()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var actor = Actor(analysis.OrganizationId);
        var sut = CreateSut();

        var first = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, "ok", actor);
        var second = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, " ok", actor);

        Assert.False(first.IdempotentReplay);
        Assert.False(second.IdempotentReplay);
        Assert.Equal(1, first.Decision.DecisionSequence);
        Assert.Equal(2, second.Decision.DecisionSequence);
        Assert.Equal("ok", first.Decision.Rationale);
        Assert.Equal(" ok", second.Decision.Rationale);
    }

    [Fact]
    public async Task Trailing_whitespace_difference_creates_new_decision()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var actor = Actor(analysis.OrganizationId);
        var sut = CreateSut();

        var first = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, "ok", actor);
        var second = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, "ok ", actor);

        Assert.False(second.IdempotentReplay);
        Assert.Equal(2, second.Decision.DecisionSequence);
        Assert.Equal("ok ", second.Decision.Rationale);
    }

    [Fact]
    public async Task Case_difference_creates_new_decision()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var actor = Actor(analysis.OrganizationId);
        var sut = CreateSut();

        var first = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, "ok", actor);
        var second = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, "OK", actor);

        Assert.False(second.IdempotentReplay);
        Assert.Equal(2, second.Decision.DecisionSequence);
        Assert.Equal("OK", second.Decision.Rationale);
    }

    [Fact]
    public async Task Same_decision_different_rationale_appends_new_sequence()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var actor = Actor(analysis.OrganizationId);
        var sut = CreateSut();

        var first = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, "primeira", actor);
        var second = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, "segunda", actor);

        Assert.False(first.IdempotentReplay);
        Assert.False(second.IdempotentReplay);
        Assert.Equal(1, first.Decision.DecisionSequence);
        Assert.Equal(2, second.Decision.DecisionSequence);

        var effective = await sut.GetEffectiveAsync(analysis.AnalysisId, analysis.OrganizationId);
        Assert.NotNull(effective);
        Assert.Equal(2, effective!.DecisionSequence);
        Assert.Equal("segunda", effective.Rationale);

        var history = await sut.ListHistoryAsync(analysis.AnalysisId, analysis.OrganizationId);
        Assert.Equal(new[] { 2, 1 }, history.Select(x => x.DecisionSequence).ToArray());
    }

    [Fact]
    public async Task Changing_decision_appends_new_sequence_and_keeps_history()
    {
        var analysis = Persisted(MaterializationReadiness.ReadyWithWarnings);
        var actor = Actor(analysis.OrganizationId);
        var sut = CreateSut();

        var confirmed = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Confirmed, null, actor);
        var rejected = await sut.DecideAsync(
            analysis, PackageImportAnalysisDecisionKind.Rejected, "reavaliado", actor);

        Assert.Equal(1, confirmed.Decision.DecisionSequence);
        Assert.Equal(2, rejected.Decision.DecisionSequence);
        Assert.Equal(PackageImportAnalysisDecisionKind.Rejected, rejected.Decision.Decision);

        var history = await sut.ListHistoryAsync(analysis.AnalysisId, analysis.OrganizationId);
        Assert.Equal(2, history.Count);
        Assert.Equal(PackageImportAnalysisDecisionKind.Rejected, history[0].Decision);
        Assert.Equal(PackageImportAnalysisDecisionKind.Confirmed, history[1].Decision);
        Assert.Equal(confirmed.Decision.DecisionId, history[1].DecisionId);
    }

    [Fact]
    public async Task Snapshot_copies_fingerprint_from_persisted_analysis_only()
    {
        var fingerprint = new string('a', 64);
        var analysis = Persisted(MaterializationReadiness.Ready, fingerprint);
        var sut = CreateSut();

        var result = await sut.DecideAsync(
            analysis,
            PackageImportAnalysisDecisionKind.Confirmed,
            rationale: null,
            Actor(analysis.OrganizationId));

        Assert.Equal(fingerprint, result.Decision.SemanticFingerprintSnapshot);
        Assert.Equal(analysis.SemanticFingerprint, result.Decision.SemanticFingerprintSnapshot);
    }

    [Fact]
    public async Task Organization_mismatch_is_rejected_before_persist()
    {
        var analysis = Persisted(MaterializationReadiness.Ready);
        var sut = CreateSut();

        await Assert.ThrowsAsync<PackageImportAnalysisDecisionOrganizationMismatchException>(() =>
            sut.DecideAsync(
                analysis,
                PackageImportAnalysisDecisionKind.Confirmed,
                null,
                new PackageImportAnalysisDecisionActor(Guid.NewGuid(), Guid.NewGuid())));

        Assert.Null(await sut.GetEffectiveAsync(analysis.AnalysisId, analysis.OrganizationId));
    }

    [Fact]
    public void Rationale_over_2000_is_rejected_without_truncation()
    {
        var tooLong = new string('x', 2001);
        Assert.Throws<PackageImportAnalysisDecisionValidationException>(() =>
            PackageImportAnalysisDecisionRules.ValidateRationale(
                PackageImportAnalysisDecisionKind.Confirmed, tooLong));
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData("", "", true)]
    [InlineData("ok", "ok", true)]
    [InlineData(" ok", "ok", false)]
    [InlineData("ok ", "ok", false)]
    [InlineData("ok", "OK", false)]
    [InlineData("", null, false)]
    [InlineData("  ", null, false)]
    [InlineData("a b", "a  b", false)]
    public void Rationale_equivalence_is_ordinal_exact_without_trim(
        string? left, string? right, bool expected)
    {
        Assert.Equal(expected, PackageImportAnalysisDecisionRules.AreRationalesEquivalent(left, right));
    }

    private static PackageImportAnalysisDecisionService CreateSut(
        IPackageImportAnalysisDecisionRepository? repository = null)
    {
        var decisionIds = new Queue<Guid>([
            FixedDecisionId,
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Guid.NewGuid()]);
        return new PackageImportAnalysisDecisionService(
            repository ?? new InMemoryPackageImportAnalysisDecisionRepository(),
            clock: () => FixedUtc,
            newDecisionId: () => decisionIds.Count > 0 ? decisionIds.Dequeue() : Guid.NewGuid());
    }

    private static PackageImportAnalysisDecisionActor Actor(Guid organizationId) =>
        new(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), organizationId);

    private static PersistedPackageImportAnalysis Persisted(
        MaterializationReadiness readiness,
        string? semanticFingerprint = null)
    {
        var fingerprint = semanticFingerprint ?? new string('b', 64);
        return new PersistedPackageImportAnalysis(
            AnalysisId: Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
            OrganizationId: Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
            ReceiptId: Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
            PackageId: Guid.Parse("dddddddd-0000-0000-0000-000000000001"),
            CaptureId: Guid.Parse("eeeeeeee-0000-0000-0000-000000000001"),
            ZipSha256: new string('c', 64),
            ZipByteLength: 10,
            LogicalContentDigest: new string('d', 64),
            AnalysisSchemaVersion: PackageImportAnalysisSchema.Version,
            ContractVersion: "0.1",
            ReadinessStatus: readiness.ToString(),
            SemanticFingerprint: fingerprint,
            AnalysisJson: """{"placeholder":true}""",
            ObjectCount: 0,
            ArtifactCount: 0,
            IssueCount: 0,
            UnavailableObjectCount: 0,
            AnalyzedAtUtc: FixedUtc,
            CreatedAtUtc: FixedUtc);
    }
}
