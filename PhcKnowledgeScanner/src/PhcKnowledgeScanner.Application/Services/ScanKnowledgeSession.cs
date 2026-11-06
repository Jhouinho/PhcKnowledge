using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;
using PhcKnowledgeScanner.Abstractions.Workspace;
using PhcKnowledgeScanner.Analysis.Knowledge;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Application.Services;

/// <summary>
/// Sessão de leitura de um workspace SQLite existente.
/// Abre em modo estritamente read-only, projecta uma vez, fecha a ligação SQLite
/// e mantém inventário + <see cref="IRelationalKnowledgeQuery"/> em memória.
/// </summary>
public sealed class ScanKnowledgeSession : IAsyncDisposable
{
    private readonly RelationalKnowledgeSnapshot _knowledge;
    private readonly IReadOnlyList<TechnicalObjectRecord> _tables;
    private readonly WorkspaceSummary _summary;
    private int _disposed; // 0 = open, 1 = closed

    private ScanKnowledgeSession(
        string workspaceRootPath,
        string databasePath,
        WorkspaceSummary summary,
        IReadOnlyList<TechnicalObjectRecord> tables,
        RelationalKnowledgeSnapshot knowledge,
        int projectionBuildCount)
    {
        WorkspaceRootPath = workspaceRootPath;
        DatabasePath = databasePath;
        _summary = summary;
        _tables = tables;
        _knowledge = knowledge;
        ProjectionBuildCount = projectionBuildCount;
    }

    /// <summary>Pasta do workspace (caminho canónico).</summary>
    public string WorkspaceRootPath { get; }

    /// <summary>Caminho absoluto de <c>workspace.db</c>.</summary>
    public string DatabasePath { get; }

    public string ScanId => EnsureOpen()._summary.ScanId;

    public string InstallationLabel => EnsureOpen()._summary.InstallationLabel;

    public string Status => EnsureOpen()._summary.Status;

    public DateTimeOffset CreatedAtUtc => EnsureOpen()._summary.CreatedAtUtc;

    public WorkspaceSummary Summary => EnsureOpen()._summary;

    /// <summary>Inventário de tabelas físicas carregado na abertura.</summary>
    public IReadOnlyList<TechnicalObjectRecord> Tables => EnsureOpen()._tables;

    /// <summary>Projeção relacional (mesma instância durante a sessão).</summary>
    public IRelationalKnowledgeQuery Knowledge => EnsureOpen()._knowledge;

    public ScanKnowledgeSessionState State =>
        Volatile.Read(ref _disposed) == 0
            ? ScanKnowledgeSessionState.Open
            : ScanKnowledgeSessionState.Closed;

    /// <summary>
    /// Número de vezes que o projector foi invocado nesta sessão (sempre 1 após abertura bem-sucedida).
    /// Exposto para testes de “project once”.
    /// </summary>
    public int ProjectionBuildCount { get; }

    /// <summary>Abre um workspace existente em modo read-only e projecta uma vez.</summary>
    public static Task<ScanKnowledgeSession> OpenAsync(
        string workspaceRootPath,
        CancellationToken cancellationToken = default) =>
        OpenAsync(workspaceRootPath, new RelationalKnowledgeProjector(), cancellationToken);

    public static async Task<ScanKnowledgeSession> OpenAsync(
        string workspaceRootPath,
        IRelationalKnowledgeProjector projector,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        ArgumentNullException.ThrowIfNull(projector);

        var rootFull = Path.GetFullPath(workspaceRootPath);
        if (!Directory.Exists(rootFull))
        {
            throw new DirectoryNotFoundException($"Workspace não encontrado: {rootFull}");
        }

        var dbPath = Path.Combine(rootFull, "workspace.db");
        if (!File.Exists(dbPath))
        {
            throw new FileNotFoundException("workspace.db não encontrado.", dbPath);
        }

        // Ligação SQLite apenas durante a carga; a sessão não a mantém aberta.
        await using var workspace = await SqliteScanWorkspace
            .OpenExistingAsync(rootFull, readOnly: true, cancellationToken)
            .ConfigureAwait(false);

        var summary = await workspace.GetSummaryAsync(cancellationToken).ConfigureAwait(false);
        var tables = await workspace.GetObjectsAsync(ObjectKind.Table, search: null, cancellationToken)
            .ConfigureAwait(false);

        var knowledge = await projector.ProjectAsync(workspace, cancellationToken).ConfigureAwait(false);
        const int projectionBuildCount = 1;

        // workspace DisposeAsync fecha a conexão read-only aqui.
        return new ScanKnowledgeSession(
            rootFull,
            Path.GetFullPath(dbPath),
            summary,
            tables,
            knowledge,
            projectionBuildCount);
    }

    /// <summary>Pesquisa tabelas já carregadas (sem reabrir SQLite), com prioridade no nome.</summary>
    public IReadOnlyList<TechnicalObjectRecord> FindTables(string? search) =>
        TableSearch.FilterAndRank(Tables, search);

    public ValueTask DisposeAsync()
    {
        Volatile.Write(ref _disposed, 1);
        return ValueTask.CompletedTask;
    }

    private ScanKnowledgeSession EnsureOpen()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(
                nameof(ScanKnowledgeSession),
                "A sessão de conhecimento está fechada.");
        }

        return this;
    }
}
