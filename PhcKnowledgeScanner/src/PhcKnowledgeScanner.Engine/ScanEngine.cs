using System.Diagnostics;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Manifest;
using PhcKnowledgeScanner.Abstractions.Progress;

namespace PhcKnowledgeScanner.Engine;

/// <summary>
/// Orquestra collectors registados: seleção, probe, execução, CaptureId e coverage.
/// </summary>
public sealed class ScanEngine
{
    private readonly IReadOnlyList<ICollector> _collectors;

    public ScanEngine(IEnumerable<ICollector> collectors)
    {
        _collectors = collectors.ToList();
        if (_collectors.Count == 0)
        {
            throw new ArgumentException("É necessário pelo menos um collector registado.", nameof(collectors));
        }

        var duplicates = _collectors
            .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new ArgumentException(
                $"IDs de collector duplicados: {string.Join(", ", duplicates)}",
                nameof(collectors));
        }

        // Valida o catálogo na construção (conflitos de operationId incompatíveis).
        _ = BuildCapabilityCatalog();
    }

    public IReadOnlyList<ICollector> Collectors => _collectors;

    /// <summary>Catálogo agregado das capabilities declaradas pelos collectors registados.</summary>
    public IReadOnlyList<DeclaredCapability> BuildCapabilityCatalog()
        => CapabilityCatalogBuilder.Build(_collectors.SelectMany(c => c.DeclaredCapabilities));

    public async Task<ScanManifest> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.CaptureId))
        {
            context.CaptureId = Guid.NewGuid().ToString("D");
        }

        var captureId = context.CaptureId;
        var started = DateTimeOffset.UtcNow;
        var swTotal = Stopwatch.StartNew();
        var catalog = BuildCapabilityCatalog();
        var selected = SelectCollectors(context);
        var selectedIds = new HashSet<string>(selected.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);
        var results = new List<CollectorResult>();
        var operationCoverages = new List<OperationCoverage>();
        var success = 0;
        var warnings = 0;
        var errors = 0;
        var processed = 0;

        if (context.Workspace is not null)
        {
            await context.Workspace.SetCaptureIdAsync(captureId, cancellationToken).ConfigureAwait(false);
        }

        Report(context, "scan.started", null, null, "Recolha iniciada.", processed, success, warnings, errors, 0, swTotal.Elapsed);

        for (var i = 0; i < selected.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var collector = selected[i];
            var percent = selected.Count == 0 ? 100 : (100.0 * i / selected.Count);

            Report(
                context,
                "collector.probe",
                collector.Id,
                collector.DisplayName,
                $"A verificar disponibilidade: {collector.DisplayName}",
                processed,
                success,
                warnings,
                errors,
                percent,
                swTotal.Elapsed);

            var capability = await collector.ProbeAsync(context, cancellationToken).ConfigureAwait(false);
            if (!capability.IsAvailable)
            {
                var skipIssue = new CollectionIssue(
                    CollectionIssueSeverity.Warning,
                    "collector.unavailable",
                    capability.ReasonUnavailable ?? "Collector indisponível neste contexto.",
                    IssueId: $"issue:{collector.Id}:unavailable",
                    OperationId: null);
                var skipCoverages = collector.DeclaredCapabilities
                    .Select(CoverageSemantics.SkippedProbeFailed)
                    .ToArray();
                results.Add(new CollectorResult(
                    collector.Id,
                    CollectorStatus.Skipped,
                    Array.Empty<CollectedArtifact>(),
                    [skipIssue],
                    TimeSpan.Zero,
                    skipCoverages));
                operationCoverages.AddRange(skipCoverages);
                if (context.Workspace is not null)
                {
                    await context.Workspace.AddIssueAsync(collector.Id, skipIssue, cancellationToken)
                        .ConfigureAwait(false);
                }

                warnings++;
                processed++;
                continue;
            }

            if (context.Mode == ScanMode.ProbeOnly)
            {
                var probeCoverages = collector.DeclaredCapabilities
                    .Select(CoverageSemantics.ProbeOnly)
                    .ToArray();
                results.Add(new CollectorResult(
                    collector.Id,
                    CollectorStatus.Succeeded,
                    Array.Empty<CollectedArtifact>(),
                    Array.Empty<CollectionIssue>(),
                    TimeSpan.Zero,
                    probeCoverages));
                operationCoverages.AddRange(probeCoverages);
                success++;
                processed++;
                continue;
            }

            Report(
                context,
                "collector.running",
                collector.Id,
                collector.DisplayName,
                $"A executar: {collector.DisplayName}",
                processed,
                success,
                warnings,
                errors,
                percent,
                swTotal.Elapsed);

            if (context.Workspace is not null)
            {
                await context.Workspace.SetScanStatusAsync($"running:{collector.Id}", cancellationToken)
                    .ConfigureAwait(false);
            }

            var sw = Stopwatch.StartNew();
            context.ExecutionProgress.Reset();
            try
            {
                var result = await collector.CollectAsync(context, cancellationToken).ConfigureAwait(false);
                sw.Stop();
                var coverages = DeduplicateCoverages(
                    result.Coverages.Count > 0
                        ? result.Coverages
                        : SynthesizeCollectorCoverage(collector, result));
                var finalized = result with
                {
                    Duration = result.Duration == TimeSpan.Zero ? sw.Elapsed : result.Duration,
                    OperationCoverages = coverages
                };
                results.Add(finalized);
                operationCoverages.AddRange(coverages);

                warnings += finalized.Issues.Count(i => i.Severity == CollectionIssueSeverity.Warning);
                errors += finalized.Issues.Count(i => i.Severity == CollectionIssueSeverity.Error);
                if (finalized.Status is CollectorStatus.Succeeded or CollectorStatus.SucceededWithWarnings or CollectorStatus.Partial)
                {
                    success++;
                }
                else if (finalized.Status == CollectorStatus.Failed)
                {
                    errors++;
                }

                processed++;
            }
            catch (OperationCanceledException)
            {
                Report(context, "scan.cancelled", collector.Id, collector.DisplayName, "Recolha cancelada.",
                    processed, success, warnings, errors, percent, swTotal.Elapsed);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                errors++;
                processed++;
                // Preservar coverages já publicadas no progresso; completar o resto com NotAttempted.
                // A exceção fica ao nível do collector (OperationId null) — não inventar a operação responsável.
                var failIssue = new CollectionIssue(
                    CollectionIssueSeverity.Error,
                    "collector.unhandled",
                    "Exceção não tratada no collector.",
                    Detail: ex.GetType().FullName + ": " + ex.Message,
                    IssueId: $"issue:{collector.Id}:unhandled",
                    OperationId: null);
                var known = context.ExecutionProgress.Coverages;
                var failCoverages = MergeKnownCoveragesWithNotAttempted(
                    collector.DeclaredCapabilities,
                    known);
                var issues = MergeIssues(context.ExecutionProgress.Issues, failIssue);

                results.Add(new CollectorResult(
                    collector.Id,
                    CollectorStatus.Failed,
                    Array.Empty<CollectedArtifact>(),
                    issues,
                    sw.Elapsed,
                    failCoverages));
                operationCoverages.AddRange(failCoverages);

                if (context.Workspace is not null)
                {
                    await context.Workspace.AddIssueAsync(collector.Id, failIssue, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        foreach (var collector in _collectors)
        {
            if (selectedIds.Contains(collector.Id))
            {
                continue;
            }

            foreach (var declared in collector.DeclaredCapabilities)
            {
                operationCoverages.Add(CoverageSemantics.SkippedNotRequested(declared));
            }
        }

        Report(context, "scan.completed", null, null, "Recolha concluída.",
            processed, success, warnings, errors, 100, swTotal.Elapsed);

        if (context.Workspace is not null)
        {
            await context.Workspace.SetScanStatusAsync("completed", cancellationToken).ConfigureAwait(false);
            await context.Workspace
                .SaveCaptureCoverageAsync(captureId, catalog, operationCoverages, cancellationToken)
                .ConfigureAwait(false);
        }

        return new ScanManifest(
            captureId,
            context.InstallationId,
            started,
            DateTimeOffset.UtcNow,
            context.Mode,
            ScannerVersion.Current,
            catalog,
            operationCoverages,
            results);
    }

    private static IReadOnlyList<OperationCoverage> SynthesizeCollectorCoverage(
        ICollector collector,
        CollectorResult result)
    {
        if (collector.DeclaredCapabilities.Count == 0)
        {
            return Array.Empty<OperationCoverage>();
        }

        var issueIds = result.Issues
            .Select(i => i.IssueId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .ToArray();

        var (exec, source, content) = MapCollectorStatus(result.Status);
        return collector.DeclaredCapabilities
            .Select(c => new OperationCoverage(
                c.OperationId,
                c.CapabilityVersion,
                Requested: true,
                exec,
                source,
                content,
                0,
                issueIds))
            .ToArray();
    }

    /// <summary>
    /// Combina coverages já conhecidas com NotAttempted para operações declaradas sem resultado.
    /// Operações in-flight sem CompleteOperation não estão em <paramref name="known"/> e ficam NotAttempted.
    /// </summary>
    public static IReadOnlyList<OperationCoverage> MergeKnownCoveragesWithNotAttempted(
        IReadOnlyList<DeclaredCapability> declared,
        IReadOnlyList<OperationCoverage> known)
    {
        var merged = new List<OperationCoverage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var coverage in known)
        {
            if (!seen.Add(coverage.OperationId))
            {
                continue;
            }

            merged.Add(coverage);
        }

        foreach (var capability in declared)
        {
            if (!seen.Add(capability.OperationId))
            {
                continue;
            }

            merged.Add(CoverageSemantics.NotAttempted(capability, requested: true));
        }

        return merged;
    }

    private static IReadOnlyList<OperationCoverage> DeduplicateCoverages(
        IReadOnlyList<OperationCoverage> coverages)
    {
        var merged = new List<OperationCoverage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var coverage in coverages)
        {
            if (!seen.Add(coverage.OperationId))
            {
                continue;
            }

            merged.Add(coverage);
        }

        return merged;
    }

    private static IReadOnlyList<CollectionIssue> MergeIssues(
        IReadOnlyList<CollectionIssue> known,
        CollectionIssue collectorIssue)
    {
        var list = new List<CollectionIssue>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var issue in known)
        {
            if (!string.IsNullOrWhiteSpace(issue.IssueId) && !seenIds.Add(issue.IssueId))
            {
                continue;
            }

            list.Add(issue);
        }

        if (string.IsNullOrWhiteSpace(collectorIssue.IssueId) || seenIds.Add(collectorIssue.IssueId))
        {
            list.Add(collectorIssue);
        }

        return list;
    }

    private static (OperationExecutionResult Result, SourceAvailability Source, ContentAvailability Content)
        MapCollectorStatus(CollectorStatus status)
        => status switch
        {
            CollectorStatus.Succeeded => (OperationExecutionResult.Succeeded, SourceAvailability.Present, ContentAvailability.Available),
            CollectorStatus.SucceededWithWarnings => (OperationExecutionResult.SucceededWithWarnings, SourceAvailability.Present, ContentAvailability.Available),
            CollectorStatus.Partial => (OperationExecutionResult.Partial, SourceAvailability.Present, ContentAvailability.Partial),
            CollectorStatus.Failed => (OperationExecutionResult.Failed, SourceAvailability.Unknown, ContentAvailability.Unavailable),
            CollectorStatus.Skipped => (OperationExecutionResult.Skipped, SourceAvailability.Unknown, ContentAvailability.NotCollected),
            _ => (OperationExecutionResult.NotAttempted, SourceAvailability.Unknown, ContentAvailability.NotCollected)
        };

    private static void Report(
        ScanContext context,
        string stage,
        string? collectorId,
        string? displayName,
        string message,
        int processed,
        int success,
        int warnings,
        int errors,
        double? percent,
        TimeSpan elapsed)
    {
        context.Progress?.Report(new ScanProgressEvent(
            stage,
            collectorId,
            displayName,
            message,
            processed,
            success,
            warnings,
            errors,
            percent,
            elapsed));
    }

    private IReadOnlyList<ICollector> SelectCollectors(ScanContext context)
    {
        if (context.Mode == ScanMode.Full || context.SelectedCollectorIds.Count == 0)
        {
            return _collectors;
        }

        var map = _collectors.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<ICollector>();
        foreach (var id in context.SelectedCollectorIds)
        {
            if (!map.TryGetValue(id, out var collector))
            {
                throw new InvalidOperationException($"Collector desconhecido: '{id}'.");
            }

            ordered.Add(collector);
        }

        return ordered;
    }
}
