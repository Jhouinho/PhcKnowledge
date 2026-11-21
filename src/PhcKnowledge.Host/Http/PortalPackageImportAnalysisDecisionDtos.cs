namespace PhcKnowledge.Host.Http;

public static class PortalPackageImportAnalysisDecisionPaths
{
    public const string DecisionPath = "/api/portal/package-import-analyses/{analysisId:guid}/decision";
    public const string DecisionsPath = "/api/portal/package-import-analyses/{analysisId:guid}/decisions";
}

public static class PortalPackageImportAnalysisDecisionMessages
{
    public const string InvalidRequest = "Pedido inválido.";
    public const string InvalidDecision = "Decisão inválida.";
    public const string InvalidRationale = "Justificação inválida.";
    public const string NotEligible = "A análise não é elegível para esta decisão.";
    public const string Conflict = "Não foi possível gravar a decisão devido a um conflito.";
}

/// <summary>
/// Effective decision state. <see cref="Decision"/> is confirmed|rejected|unconfirmed.
/// <see cref="MaterializationReadiness"/> mirrors the persisted analysis readiness
/// (Ready|ReadyWithWarnings|Blocked) so the Portal can present confirm/reject eligibility
/// without calling the IKP JWT surface.
/// </summary>
public sealed record PortalPackageImportAnalysisDecisionStateDto(
    Guid AnalysisId,
    Guid PackageId,
    string Decision,
    int? DecisionSequence,
    DateTimeOffset? DecidedAtUtc,
    string? DecidedBySubject,
    string? DecidedByIssuer,
    string? Rationale,
    string? SemanticFingerprintSnapshot,
    string MaterializationReadiness);

public sealed record PortalPackageImportAnalysisDecisionWriteResponseDto(
    Guid AnalysisId,
    Guid PackageId,
    string Decision,
    int DecisionSequence,
    DateTimeOffset DecidedAtUtc,
    string DecidedBySubject,
    string DecidedByIssuer,
    string? Rationale,
    string SemanticFingerprintSnapshot,
    bool IdempotentReplay);

public sealed record PortalPackageImportAnalysisDecisionHistoryItemDto(
    Guid DecisionId,
    string Decision,
    int DecisionSequence,
    DateTimeOffset DecidedAtUtc,
    string DecidedBySubject,
    string DecidedByIssuer,
    string? Rationale,
    string SemanticFingerprintSnapshot);

public sealed record PortalPackageImportAnalysisDecisionHistoryResponseDto(
    Guid AnalysisId,
    Guid PackageId,
    IReadOnlyList<PortalPackageImportAnalysisDecisionHistoryItemDto> Items);
