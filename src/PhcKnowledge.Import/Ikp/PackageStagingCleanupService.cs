using PhcKnowledge.Import.Receipts;

namespace PhcKnowledge.Import.Ikp;

/// <summary>
/// Removes only old top-level staging files that are not referenced by pending attempts.
/// Invoke periodically from the product host; default retention is 24 hours.
/// </summary>
public sealed class PackageStagingCleanupService
{
    private readonly IPackageReceiptAttemptRepository _attempts;
    private readonly string _stagingDirectory;
    private readonly TimeSpan _retention;

    public PackageStagingCleanupService(
        IPackageReceiptAttemptRepository attempts,
        string stagingDirectory,
        TimeSpan? retention = null)
    {
        _attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingDirectory);
        _stagingDirectory = Path.GetFullPath(stagingDirectory);
        _retention = retention ?? TimeSpan.FromHours(24);
        if (_retention <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(retention));
    }

    public async Task<int> CleanupAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_stagingDirectory))
            return 0;

        var protectedKeys = await _attempts.GetProtectedStagingKeysAsync(cancellationToken);
        var cutoff = nowUtc.Subtract(_retention).UtcDateTime;
        var deleted = 0;

        foreach (var path in Directory.EnumerateFiles(_stagingDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = Path.GetFileName(path);
            if (protectedKeys.Contains(key))
                continue;

            try
            {
                if (File.GetLastWriteTimeUtc(path) >= cutoff)
                    continue;
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
                deleted++;
            }
            catch (FileNotFoundException)
            {
                // Another cleanup/worker removed it first.
            }
            catch (DirectoryNotFoundException)
            {
                // Staging directory was concurrently removed during shutdown.
                break;
            }
        }

        return deleted;
    }
}
