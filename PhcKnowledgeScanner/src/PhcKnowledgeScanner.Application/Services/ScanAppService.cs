using Microsoft.Extensions.Logging;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Connection;
using PhcKnowledgeScanner.Abstractions.Manifest;
using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Abstractions.Workspace;
using PhcKnowledgeScanner.Analysis;
using PhcKnowledgeScanner.Analysis.DeclaredRelations;
using PhcKnowledgeScanner.Collectors.Sql;
using PhcKnowledgeScanner.Engine;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Application.Services;

public sealed class ScanAppService : IScanAppService
{
    private readonly ScanEngine _engine;
    private readonly ForeignKeyRelationAnalyzer _fkAnalyzer;
    private readonly SchemaOwnershipInferenceAnalyzer _inferenceAnalyzer;
    private readonly DeclaredRelationInterpretationService _declaredInterpretation;
    private readonly ILogger<ScanAppService> _logger;

    public ScanAppService(
        ScanEngine engine,
        ForeignKeyRelationAnalyzer fkAnalyzer,
        SchemaOwnershipInferenceAnalyzer inferenceAnalyzer,
        DeclaredRelationInterpretationService declaredInterpretation,
        ILogger<ScanAppService> logger)
    {
        _engine = engine;
        _fkAnalyzer = fkAnalyzer;
        _inferenceAnalyzer = inferenceAnalyzer;
        _declaredInterpretation = declaredInterpretation;
        _logger = logger;
    }

    public IScanWorkspace? CurrentWorkspace { get; private set; }

    public async Task<ScanManifest> RunStructureScanAsync(
        SqlConnectionRequest request,
        string workspaceRoot,
        IScanProgressSink? progress,
        CancellationToken cancellationToken = default,
        IReadOnlySet<string>? selectedCollectorIds = null)
    {
        var connectionString = SqlConnectionStringBuilderService.Build(request);
        _logger.LogInformation("Início de scan estrutural: {Target}", SqlConnectionStringBuilderService.ToLogSafe(request));

        if (CurrentWorkspace is not null)
        {
            await CurrentWorkspace.DisposeAsync().ConfigureAwait(false);
            CurrentWorkspace = null;
        }

        var scanId = Guid.NewGuid().ToString("N");
        var workspace = await SqliteScanWorkspace.CreateAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
        await workspace.InitializeAsync(
            scanId,
            $"{request.Server}/{request.Database}",
            cancellationToken).ConfigureAwait(false);
        CurrentWorkspace = workspace;

        var selected = selectedCollectorIds is { Count: > 0 }
            ? new HashSet<string>(selectedCollectorIds, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var context = new ScanContext
        {
            InstallationId = $"{request.Server}/{request.Database}",
            OutputRoot = workspaceRoot,
            Mode = selected.Count == 0 ? ScanMode.Full : ScanMode.Selective,
            SelectedCollectorIds = selected,
            SqlConnectionString = connectionString,
            Workspace = workspace,
            Progress = progress
        };

        ScanManifest manifest;
        try
        {
            manifest = await _engine.RunAsync(context, cancellationToken).ConfigureAwait(false);

            progress?.Report(new ScanProgressEvent(
                "analysis.running",
                null,
                null,
                "A derivar relações a partir de factos...",
                0, 0, 0, 0, 95,
                TimeSpan.Zero));

            var relations = await _fkAnalyzer.AnalyzeAsync(workspace, cancellationToken).ConfigureAwait(false);
            var inferences = await _inferenceAnalyzer.AnalyzeAsync(workspace, cancellationToken).ConfigureAwait(false);

            progress?.Report(new ScanProgressEvent(
                "analysis.interpretation",
                null,
                null,
                "A interpretar expressões de relações declaradas...",
                0, 0, 0, 0, 97,
                TimeSpan.Zero));

            var (interpretations, interpMetrics) = await _declaredInterpretation
                .InterpretWorkspaceAsync(workspace, cancellationToken).ConfigureAwait(false);
            await workspace.UpsertDeclaredRelationInterpretationsAsync(interpretations, cancellationToken)
                .ConfigureAwait(false);

            progress?.Report(new ScanProgressEvent(
                "analysis.interpretation",
                null,
                null,
                $"Interpretação {DeclaredRelationInterpretationService.ParserVersion}: "
                + $"analisadas={interpMetrics.Analyzed}, parsed={interpMetrics.Parsed}, "
                + $"partial={interpMetrics.PartiallyParsed}, unsupported={interpMetrics.Unsupported}, "
                + $"invalid={interpMetrics.Invalid}, empty={interpMetrics.Empty}; "
                + $"fieldRefs={interpMetrics.FieldRefsIdentified} "
                + $"(qual={interpMetrics.QualifiedFieldRefs}, unqual={interpMetrics.UnqualifiedFieldRefs}); "
                + $"tblRes={interpMetrics.TableRefsResolved}/amb={interpMetrics.TableRefsAmbiguous}/unres={interpMetrics.TableRefsUnresolved}.",
                interpMetrics.Analyzed,
                interpMetrics.Parsed,
                0,
                interpMetrics.Invalid,
                98,
                TimeSpan.Zero));

            _logger.LogInformation(
                "Análise concluída: {Relations} relações FK, {Inferences} inferências, interpretação declared={Parsed}/{Analyzed} parsed",
                relations,
                inferences,
                interpMetrics.Parsed,
                interpMetrics.Analyzed);

            await workspace.SetScanStatusAsync("analyzed", cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Limpar password da connection string da memória do contexto (best effort).
            context = null!;
        }

        return manifest;
    }
}
