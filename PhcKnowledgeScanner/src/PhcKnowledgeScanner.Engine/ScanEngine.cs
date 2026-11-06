using System.Diagnostics;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Manifest;
using PhcKnowledgeScanner.Abstractions.Progress;

namespace PhcKnowledgeScanner.Engine;

/// <summary>
/// Orquestra collectors registados: seleção, probe, execução e progresso.
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
    }

    public IReadOnlyList<ICollector> Collectors => _collectors;

    public async Task<ScanManifest> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var started = DateTimeOffset.UtcNow;
        var swTotal = Stopwatch.StartNew();
        var selected = SelectCollectors(context);
        var results = new List<CollectorResult>();
        var success = 0;
        var warnings = 0;
        var errors = 0;
        var processed = 0;

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
                    capability.ReasonUnavailable ?? "Collector indisponível neste contexto.");
                results.Add(new CollectorResult(
                    collector.Id,
                    CollectorStatus.Skipped,
                    Array.Empty<CollectedArtifact>(),
                    [skipIssue],
                    TimeSpan.Zero));
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
                results.Add(new CollectorResult(
                    collector.Id,
                    CollectorStatus.Succeeded,
                    Array.Empty<CollectedArtifact>(),
                    Array.Empty<CollectionIssue>(),
                    TimeSpan.Zero));
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
            try
            {
                var result = await collector.CollectAsync(context, cancellationToken).ConfigureAwait(false);
                sw.Stop();
                var finalized = result with
                {
                    Duration = result.Duration == TimeSpan.Zero ? sw.Elapsed : result.Duration
                };
                results.Add(finalized);

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
                results.Add(new CollectorResult(
                    collector.Id,
                    CollectorStatus.Failed,
                    Array.Empty<CollectedArtifact>(),
                    [
                        new CollectionIssue(
                            CollectionIssueSeverity.Error,
                            "collector.unhandled",
                            "Exceção não tratada no collector.",
                            Detail: ex.GetType().FullName + ": " + ex.Message)
                    ],
                    sw.Elapsed));
            }
        }

        Report(context, "scan.completed", null, null, "Recolha concluída.",
            processed, success, warnings, errors, 100, swTotal.Elapsed);

        if (context.Workspace is not null)
        {
            await context.Workspace.SetScanStatusAsync("completed", cancellationToken).ConfigureAwait(false);
        }

        return new ScanManifest(
            context.InstallationId,
            started,
            DateTimeOffset.UtcNow,
            context.Mode,
            ScannerVersion.Current,
            results);
    }

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
