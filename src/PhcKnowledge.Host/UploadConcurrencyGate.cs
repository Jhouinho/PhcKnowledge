namespace PhcKnowledge.Host;

/// <summary>
/// Non-blocking admission control for uploads. Callers must dispose the lease
/// in a finally block so the semaphore is released on success, failure and cancellation.
/// </summary>
public sealed class UploadConcurrencyGate
{
    private readonly SemaphoreSlim _semaphore;

    public UploadConcurrencyGate(UploadHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _semaphore = new SemaphoreSlim(settings.MaxConcurrentUploads, settings.MaxConcurrentUploads);
    }

    /// <summary>
    /// Attempts to acquire immediately. Returns null when capacity is exhausted
    /// instead of waiting indefinitely.
    /// </summary>
    public async Task<UploadConcurrencyLease?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        if (!await _semaphore.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
            return null;
        return new UploadConcurrencyLease(_semaphore);
    }

    /// <summary>Available permits (for diagnostics and tests).</summary>
    public int CurrentCount => _semaphore.CurrentCount;
}

public sealed class UploadConcurrencyLease : IDisposable
{
    private SemaphoreSlim? _semaphore;

    internal UploadConcurrencyLease(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore;
    }

    public void Dispose()
    {
        var semaphore = Interlocked.Exchange(ref _semaphore, null);
        semaphore?.Release();
    }
}
