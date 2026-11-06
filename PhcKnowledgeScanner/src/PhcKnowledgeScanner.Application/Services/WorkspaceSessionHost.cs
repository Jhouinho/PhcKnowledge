using PhcKnowledgeScanner.Abstractions.Inventory;

namespace PhcKnowledgeScanner.Application.Services;

/// <summary>
/// Ciclo de vida da <see cref="ScanKnowledgeSession"/> activa (sem estado estático global).
/// Abertura concorrente rejeitada; falha preserva a sessão anterior.
/// </summary>
public sealed class WorkspaceSessionHost : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ScanKnowledgeSession? _session;
    private int _disposed;
    private int _opening; // 0 = idle, 1 = abertura em curso

    public bool HasSession =>
        Volatile.Read(ref _disposed) == 0
        && _session is { State: ScanKnowledgeSessionState.Open };

    public bool IsOpening => Volatile.Read(ref _opening) != 0;

    public ScanKnowledgeSession? ActiveSession =>
        HasSession ? _session : null;

    /// <summary>Hook de teste: atraso após reclamar a abertura (força sobreposição).</summary>
    internal Func<CancellationToken, Task>? OpeningDelayAsync { get; set; }

    /// <summary>
    /// Abre um workspace a partir da pasta ou do ficheiro <c>workspace.db</c>.
    /// </summary>
    public async Task<WorkspaceSessionOpenResult> OpenAsync(
        string workspacePathOrDbFile,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePathOrDbFile);

        // Rejeitar sobreposição imediata (antes de esperar no semáforo).
        if (Interlocked.CompareExchange(ref _opening, 1, 0) != 0)
        {
            return WorkspaceSessionOpenResult.Failed(
                "Já existe uma abertura de workspace em curso.");
        }

        try
        {
            if (OpeningDelayAsync is not null)
            {
                await OpeningDelayAsync(cancellationToken).ConfigureAwait(false);
            }

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                string root;
                try
                {
                    root = ResolveWorkspaceRoot(workspacePathOrDbFile);
                }
                catch (Exception ex)
                {
                    return WorkspaceSessionOpenResult.Failed(ex.Message);
                }

                ScanKnowledgeSession? opened;
                try
                {
                    opened = await ScanKnowledgeSession.OpenAsync(root, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return WorkspaceSessionOpenResult.Failed(
                        "Não foi possível abrir o workspace: " + ex.Message);
                }

                var previous = _session;
                _session = opened;
                if (previous is not null)
                {
                    await previous.DisposeAsync().ConfigureAwait(false);
                }

                return WorkspaceSessionOpenResult.Succeeded(opened);
            }
            finally
            {
                _gate.Release();
            }
        }
        finally
        {
            Volatile.Write(ref _opening, 0);
        }
    }

    public IReadOnlyList<TechnicalObjectRecord> FindTables(string? search)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var session = ActiveSession
            ?? throw new InvalidOperationException("Não existe workspace aberto.");
        return session.FindTables(search);
    }

    public TechnicalObjectRecord? GetTable(string objectId)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var session = ActiveSession;
        if (session is null)
        {
            return null;
        }

        return session.Tables.FirstOrDefault(t =>
            string.Equals(t.ObjectId, objectId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Fecha a sessão activa sem dispor o host (permite reabrir outro workspace).
    /// </summary>
    public async Task CloseSessionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_session is not null)
            {
                await _session.DisposeAsync().ConfigureAwait(false);
                _session = null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_session is not null)
            {
                await _session.DisposeAsync().ConfigureAwait(false);
                _session = null;
            }
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    internal static string ResolveWorkspaceRoot(string workspacePathOrDbFile)
    {
        var full = Path.GetFullPath(workspacePathOrDbFile);
        if (File.Exists(full)
            && full.EndsWith("workspace.db", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(full)
                   ?? throw new InvalidOperationException("Caminho de workspace inválido.");
        }

        if (Directory.Exists(full))
        {
            var db = Path.Combine(full, "workspace.db");
            if (!File.Exists(db))
            {
                throw new FileNotFoundException("workspace.db não encontrado na pasta.", db);
            }

            return full;
        }

        throw new FileNotFoundException(
            "Indique a pasta do workspace ou o ficheiro workspace.db.",
            full);
    }
}

public sealed record WorkspaceSessionOpenResult(
    bool Success,
    string? ErrorMessage,
    ScanKnowledgeSession? Session)
{
    public static WorkspaceSessionOpenResult Succeeded(ScanKnowledgeSession session) =>
        new(true, null, session);

    public static WorkspaceSessionOpenResult Failed(string message) =>
        new(false, message, null);
}
