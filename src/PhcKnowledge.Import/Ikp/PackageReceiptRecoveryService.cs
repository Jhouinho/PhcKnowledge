using Microsoft.Data.SqlClient;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Ikp;

/// <summary>
/// Processes durable receipt attempts. A host (for example ASP.NET Core BackgroundService)
/// is responsible for invoking ProcessDueAsync periodically.
/// </summary>
public sealed class PackageReceiptRecoveryService
{
    private readonly IPackageReceiptAttemptRepository _attempts;
    private readonly IImmutablePackageStore _store;
    private readonly string _stagingDirectory;
    private readonly TimeSpan _leaseDuration;

    public PackageReceiptRecoveryService(
        IPackageReceiptAttemptRepository attempts,
        IImmutablePackageStore store,
        string stagingDirectory,
        TimeSpan? leaseDuration = null)
    {
        _attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingDirectory);
        _stagingDirectory = Path.GetFullPath(stagingDirectory);
        _leaseDuration = leaseDuration ?? TimeSpan.FromMinutes(5);
        if (_leaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
    }

    public async Task<int> ProcessDueAsync(
        int batchSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 500)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        var claimed = await _attempts.ClaimDueAsync(
            batchSize, _leaseDuration, DateTimeOffset.UtcNow, cancellationToken);
        var results = await Task.WhenAll(
            claimed.Select(lease => ProcessLeaseAsync(lease, cancellationToken)));
        return results.Count(result => result is not null);
    }

    /// <summary>
    /// Attempts immediate processing after the upload path has persisted an attempt.
    /// Returns null when another worker owns it, it is not due, or recovery was deferred.
    /// </summary>
    public async Task<PackageReceiptAttemptProcessingResult?> ProcessAttemptAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        var lease = await _attempts.ClaimByIdAsync(
            attemptId, _leaseDuration, DateTimeOffset.UtcNow, cancellationToken);
        return lease is null ? null : await ProcessLeaseAsync(lease, cancellationToken);
    }

    private async Task<PackageReceiptAttemptProcessingResult?> ProcessLeaseAsync(
        PackageReceiptAttemptLease lease,
        CancellationToken cancellationToken)
    {
        var attempt = lease.Attempt;
        string stagingPath;
        try
        {
            stagingPath = ResolveStagingPath(attempt.StagingKey);
            ArchivedPackage archive;
            if (File.Exists(stagingPath))
            {
                archive = await WithLeaseHeartbeatAsync(
                    attempt, lease.LeaseToken,
                    token => _store.ArchiveValidatedPackageAsync(stagingPath, attempt.Validation, token),
                    cancellationToken);
            }
            else if (_store is IRecoverableImmutablePackageStore recoverable &&
                     await WithLeaseHeartbeatAsync(
                         attempt, lease.LeaseToken,
                         token => recoverable.VerifyArchivedPackageAsync(
                             attempt.StorageKey, attempt.Validation.ZipSha256,
                             attempt.Validation.ZipByteLength, token),
                         cancellationToken))
            {
                archive = new ArchivedPackage(attempt.StorageKey, true);
            }
            else
            {
                await _attempts.MarkNeedsAttentionAsync(
                    attempt.AttemptId, lease.LeaseToken,
                    "StagingAndArchiveMissing",
                    "O ficheiro de staging e o objecto correspondente no arquivo não existem.",
                    cancellationToken);
                return null;
            }

            // Fence the completion after potentially long filesystem work.
            var renewed = await _attempts.RenewLeaseAsync(
                attempt.AttemptId, lease.LeaseToken,
                DateTimeOffset.UtcNow.Add(_leaseDuration), cancellationToken);
            if (!renewed)
                return null;

            var registration = await _attempts.CompleteAsync(
                attempt.AttemptId, lease.LeaseToken, archive, cancellationToken);
            TryDeleteStaging(stagingPath);
            return new PackageReceiptAttemptProcessingResult(archive, registration);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            var delay = RetryDelay(attempt.AttemptCount);
            await _attempts.ScheduleRetryAsync(
                attempt.AttemptId, lease.LeaseToken,
                DateTimeOffset.UtcNow.Add(delay),
                ex.GetType().Name, SafeMessage(ex), cancellationToken);
            return null;
        }
        catch (Exception ex)
        {
            await _attempts.MarkNeedsAttentionAsync(
                attempt.AttemptId, lease.LeaseToken,
                ex.GetType().Name, SafeMessage(ex), cancellationToken);
            return null;
        }
    }

    private async Task<T> WithLeaseHeartbeatAsync<T>(
        PackageReceiptAttempt attempt,
        Guid leaseToken,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var heartbeatStop = new CancellationTokenSource();
        var heartbeat = MaintainLeaseAsync(
            attempt.AttemptId, leaseToken, operationCts, heartbeatStop.Token);
        try
        {
            return await operation(operationCts.Token);
        }
        finally
        {
            heartbeatStop.Cancel();
            await heartbeat;
        }
    }

    private async Task MaintainLeaseAsync(
        Guid attemptId,
        Guid leaseToken,
        CancellationTokenSource operationCts,
        CancellationToken stopToken)
    {
        // Keep the heartbeat safely inside the lease window, including tests or
        // deployments that configure a lease shorter than the five-minute default.
        var heartbeatInterval = TimeSpan.FromTicks(
            Math.Min(TimeSpan.FromMinutes(1).Ticks, Math.Max(1, _leaseDuration.Ticks / 3)));
        using var timer = new PeriodicTimer(heartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stopToken))
            {
                var renewed = await _attempts.RenewLeaseAsync(
                    attemptId, leaseToken,
                    DateTimeOffset.UtcNow.Add(_leaseDuration), stopToken);
                if (renewed)
                    continue;

                operationCts.Cancel();
                throw new LeaseLostException();
            }
        }
        catch (OperationCanceledException) when (stopToken.IsCancellationRequested)
        {
            // Normal shutdown after the filesystem operation completes.
        }
        catch
        {
            operationCts.Cancel();
            throw;
        }
    }

    private sealed class LeaseLostException()
        : IOException("A lease da tentativa foi perdida durante o processamento.");

    private string ResolveStagingPath(string stagingKey)
    {
        if (string.IsNullOrWhiteSpace(stagingKey) ||
            Path.IsPathRooted(stagingKey) ||
            stagingKey.Contains('/') || stagingKey.Contains('\\') ||
            stagingKey is "." or "..")
            throw new InvalidOperationException("StagingKey inválida.");

        var path = Path.GetFullPath(Path.Combine(_stagingDirectory, stagingKey));
        var prefix = _stagingDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? _stagingDirectory
            : _stagingDirectory + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidOperationException("StagingKey fora da pasta de staging.");
        return path;
    }

    private static TimeSpan RetryDelay(int attempt)
    {
        var minutes = Math.Min(15, Math.Pow(2, Math.Clamp(attempt - 1, 0, 4)));
        return TimeSpan.FromMinutes(minutes);
    }

    private static bool IsTransient(Exception exception)
    {
        if (exception is InvalidDataException)
            return false;

        // Only known temporary SQL Server conditions are retried. Treating every
        // DbException as transient can loop forever on permanent schema, data,
        // constraint, or permission errors.
        if (exception is SqlException sqlException)
            return sqlException.Errors.Cast<SqlError>().Any(error => IsTransientSqlError(error.Number));

        if (exception is AggregateException aggregate)
            return aggregate.InnerExceptions.Count > 0 &&
                   aggregate.InnerExceptions.All(IsTransient);

        if (exception.InnerException is not null)
            return IsTransient(exception.InnerException);

        return exception is IOException or TimeoutException;
    }

    private static bool IsTransientSqlError(int number) => number is
        -2 or       // command timeout
        2 or 53 or 64 or 121 or 233 or // connection/network interruptions
        1205 or 1222 or                // deadlock / lock request timeout
        10053 or 10054 or 10060 or     // transport-level interruptions
        10928 or 10929 or              // Azure SQL resource throttling
        40197 or 40501 or 40613 or     // transient Azure SQL service conditions
        49918 or 49919 or 49920;       // Azure SQL resource limits

    private static string SafeMessage(Exception exception)
    {
        var message = exception.Message;
        return message.Length <= 1900 ? message : message[..1900];
    }

    private static void TryDeleteStaging(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
        catch
        {
            // Cleanup is idempotent; completed attempts no longer depend on staging.
        }
    }
}

public sealed record PackageReceiptAttemptProcessingResult(
    ArchivedPackage Archive,
    PackageReceiptRegistration Registration);
