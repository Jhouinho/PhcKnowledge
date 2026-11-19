namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Explicit, testable readiness rules for a future confirmation step.
/// Missing kinds/operations and partial coverage are limitations, not automatic blockers.
/// </summary>
public sealed class MaterializationReadinessEvaluator
{
    public const string BlockedValidationFailed = "analysis.blocked.validation_failed";
    public const string BlockedArchiveIntegrity = "analysis.blocked.archive_integrity";
    public const string BlockedInternalChecksums = "analysis.blocked.internal_checksums";
    public const string BlockedContractIncompatible = "analysis.blocked.contract_incompatible";
    public const string WarningIssue = "analysis.warning.issue";
    public const string WarningCoverageResult = "analysis.warning.coverage_result";
    public const string WarningCoveragePartial = "analysis.warning.coverage_partial";

    public PackageImportReadinessAnalysis Evaluate(
        PackageImportIntegrityAnalysis integrity,
        string contractVersion,
        IReadOnlyList<IkpInspectedIssue> issues,
        IReadOnlyList<IkpInspectedCoverageOperation> coverage)
    {
        var findings = new List<PackageImportReadinessFinding>();

        if (!integrity.ValidationSucceeded)
        {
            findings.Add(new PackageImportReadinessFinding(
                BlockedValidationFailed,
                MaterializationFindingSeverity.Blocker,
                "A validação IKP do pacote falhou.",
                null));
        }

        if (!integrity.ArchiveSha256Verified)
        {
            findings.Add(new PackageImportReadinessFinding(
                BlockedArchiveIntegrity,
                MaterializationFindingSeverity.Blocker,
                "A verificação SHA-256 do ZIP arquivado falhou.",
                null));
        }

        if (!integrity.InternalChecksumsVerified)
        {
            findings.Add(new PackageImportReadinessFinding(
                BlockedInternalChecksums,
                MaterializationFindingSeverity.Blocker,
                "Os checksums internos do pacote não foram verificados.",
                null));
        }

        if (!string.Equals(contractVersion, "0.1", StringComparison.Ordinal))
        {
            findings.Add(new PackageImportReadinessFinding(
                BlockedContractIncompatible,
                MaterializationFindingSeverity.Blocker,
                $"Contrato IKP incompatível: {contractVersion}.",
                contractVersion));
        }

        foreach (var incompatibility in integrity.Incompatibilities)
        {
            findings.Add(new PackageImportReadinessFinding(
                BlockedContractIncompatible,
                MaterializationFindingSeverity.Blocker,
                incompatibility,
                null));
        }

        foreach (var issue in issues)
        {
            if (!IsWarningSeverity(issue.Severity))
                continue;

            findings.Add(new PackageImportReadinessFinding(
                WarningIssue,
                MaterializationFindingSeverity.Warning,
                issue.Message,
                issue.IssueId));
        }

        foreach (var operation in coverage)
        {
            if (string.Equals(operation.Result, "SucceededWithWarnings", StringComparison.Ordinal))
            {
                findings.Add(new PackageImportReadinessFinding(
                    WarningCoverageResult,
                    MaterializationFindingSeverity.Warning,
                    $"Operação {operation.OperationId} concluiu com avisos ({operation.Result}).",
                    operation.OperationId));
            }

            if (string.Equals(operation.ContentAvailability, "Partial", StringComparison.Ordinal))
            {
                findings.Add(new PackageImportReadinessFinding(
                    WarningCoveragePartial,
                    MaterializationFindingSeverity.Warning,
                    $"Operação {operation.OperationId} reporta contentAvailability Partial.",
                    operation.OperationId));
            }
        }

        var status = findings.Any(x => x.Severity == MaterializationFindingSeverity.Blocker)
            ? MaterializationReadiness.Blocked
            : findings.Any(x => x.Severity == MaterializationFindingSeverity.Warning)
                ? MaterializationReadiness.ReadyWithWarnings
                : MaterializationReadiness.Ready;

        return new PackageImportReadinessAnalysis(status, findings);
    }

    private static bool IsWarningSeverity(string severity) =>
        string.Equals(severity, "Warning", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(severity, "Warn", StringComparison.OrdinalIgnoreCase);
}
