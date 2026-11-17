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

        var now = DateTimeOffset.UtcNow;
        var claimed = await _attempts.ClaimDueAsync(
            batchSize, _leaseDuration, now, cancellationToken);

        var completed = 0;
        foreach (var lease in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var attempt = lease.Attempt;
            try
            {
                var stagingPath = ResolveStagingPath(attempt.StagingKey);
                ArchivedPackage archive;
                if (File.Exists(stagingPath))
                {
                    archive = await _store.ArchiveValidatedPackageAsync(
                        stagingPath, attempt.Validation, cancellationToken);
                }
                else if (_store is IRecoverableImmutablePackageStore recoverable &&
                         await recoverable.VerifyArchivedPackageAsync(
                             attempt.StorageKey, attempt.Validation.ZipSha256,
                             attempt.Validation.ZipByteLength, cancellationToken))
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
                    continue;
                }

                await _attempts.CompleteAsync(
                    attempt.AttemptId, lease.LeaseToken, archive, cancellationToken);
                TryDeleteStaging(stagingPath);
                completed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                var delay = RetryDelay(attempt.AttemptCount + 1);
                await _attempts.ScheduleRetryAsync(
                    attempt.AttemptId, lease.LeaseToken,
                    DateTimeOffset.UtcNow.Add(delay),
                    ex.GetType().Name, SafeMessage(ex), cancellationToken);
            }
            catch (Exception ex)
            {
                await _attempts.MarkNeedsAttentionAsync(
                    attempt.AttemptId, lease.LeaseToken,
                    ex.GetType().Name, SafeMessage(ex), cancellationToken);
            }
        }
        return completed;
    }

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

    private static bool IsTransient(Exception exception) =>
        exception is IOException or TimeoutException or System.Data.Common.DbException;

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
            // Safe to retry cleanup: Completed attempts no longer depend on staging.
        }
    }
}
