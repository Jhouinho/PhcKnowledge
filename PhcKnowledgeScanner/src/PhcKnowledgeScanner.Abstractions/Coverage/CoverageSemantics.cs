using PhcKnowledgeScanner.Abstractions.Collectors;

namespace PhcKnowledgeScanner.Abstractions.Coverage;

/// <summary>Regras partilhadas para construir coverage a partir de contagens e issues.</summary>
public static class CoverageSemantics
{
    public static OperationCoverage Succeeded(
        DeclaredCapability capability,
        bool requested,
        int observedCount,
        IReadOnlyList<string> issueIds,
        SourceAvailability sourceAvailability = SourceAvailability.Present,
        IReadOnlyDictionary<string, string>? scope = null)
    {
        var hasWarnings = issueIds.Count > 0;
        return new OperationCoverage(
            capability.OperationId,
            capability.CapabilityVersion,
            requested,
            hasWarnings ? OperationExecutionResult.SucceededWithWarnings : OperationExecutionResult.Succeeded,
            sourceAvailability,
            observedCount == 0 ? ContentAvailability.Empty : ContentAvailability.Available,
            observedCount,
            issueIds,
            scope);
    }

    public static OperationCoverage NotPresent(
        DeclaredCapability capability,
        bool requested,
        IReadOnlyList<string> issueIds,
        IReadOnlyDictionary<string, string>? scope = null)
        => new(
            capability.OperationId,
            capability.CapabilityVersion,
            requested,
            OperationExecutionResult.Succeeded,
            SourceAvailability.NotPresent,
            ContentAvailability.Empty,
            0,
            issueIds,
            scope);

    public static OperationCoverage Failed(
        DeclaredCapability capability,
        bool requested,
        int observedCount,
        IReadOnlyList<string> issueIds,
        SourceAvailability sourceAvailability = SourceAvailability.Present,
        IReadOnlyDictionary<string, string>? scope = null)
        => new(
            capability.OperationId,
            capability.CapabilityVersion,
            requested,
            observedCount > 0 ? OperationExecutionResult.Partial : OperationExecutionResult.Failed,
            sourceAvailability,
            observedCount > 0 ? ContentAvailability.Partial : ContentAvailability.Unavailable,
            observedCount,
            issueIds,
            scope);

    public static OperationCoverage Partial(
        DeclaredCapability capability,
        bool requested,
        int observedCount,
        IReadOnlyList<string> issueIds,
        IReadOnlyDictionary<string, string>? scope = null)
        => new(
            capability.OperationId,
            capability.CapabilityVersion,
            requested,
            OperationExecutionResult.Partial,
            SourceAvailability.Present,
            ContentAvailability.Partial,
            observedCount,
            issueIds,
            scope);

    public static OperationCoverage SkippedNotRequested(DeclaredCapability capability)
        => new(
            capability.OperationId,
            capability.CapabilityVersion,
            Requested: false,
            OperationExecutionResult.Skipped,
            SourceAvailability.NotApplicable,
            ContentAvailability.NotCollected,
            0,
            Array.Empty<string>());

    public static OperationCoverage SkippedProbeFailed(DeclaredCapability capability)
        => new(
            capability.OperationId,
            capability.CapabilityVersion,
            Requested: true,
            OperationExecutionResult.Skipped,
            SourceAvailability.Unknown,
            ContentAvailability.NotCollected,
            0,
            Array.Empty<string>());

    public static OperationCoverage ProbeOnly(DeclaredCapability capability)
        => new(
            capability.OperationId,
            capability.CapabilityVersion,
            Requested: true,
            OperationExecutionResult.Succeeded,
            SourceAvailability.Present,
            ContentAvailability.NotCollected,
            0,
            Array.Empty<string>());

    public static OperationCoverage NotAttempted(DeclaredCapability capability, bool requested)
        => new(
            capability.OperationId,
            capability.CapabilityVersion,
            requested,
            OperationExecutionResult.NotAttempted,
            SourceAvailability.Unknown,
            ContentAvailability.NotCollected,
            0,
            Array.Empty<string>());

    public static OperationCoverage FromIssues(
        DeclaredCapability capability,
        bool requested,
        int observedCount,
        IReadOnlyList<CollectionIssue> operationIssues,
        SourceAvailability sourceAvailability = SourceAvailability.Present,
        IReadOnlyDictionary<string, string>? scope = null)
    {
        var issueIds = operationIssues
            .Select(i => i.IssueId ?? string.Empty)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();

        var hasError = operationIssues.Any(i => i.Severity == CollectionIssueSeverity.Error);
        if (hasError)
        {
            return Failed(capability, requested, observedCount, issueIds, sourceAvailability, scope);
        }

        return Succeeded(capability, requested, observedCount, issueIds, sourceAvailability, scope);
    }
}
