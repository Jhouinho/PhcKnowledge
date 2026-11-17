using Microsoft.Extensions.Configuration;

namespace PhcKnowledge.Host;

public sealed class ReceiptHostSettings
{
    public required string ConnectionString { get; init; }
    public required string ArchiveDirectory { get; init; }
    public required string StagingDirectory { get; init; }
    public int RecoveryBatchSize { get; init; } = 20;
    public TimeSpan RecoveryInterval { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan StagingRetention { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan OperationsMonitorInterval { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan PendingAlertThreshold { get; init; } = TimeSpan.FromHours(1);

    public static ReceiptHostSettings Load(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Knowledge");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("A configuração ConnectionStrings:Knowledge é obrigatória.");

        var archive = GetRequiredPath(configuration, "Storage:ArchiveDirectory");
        var staging = GetRequiredPath(configuration, "Storage:StagingDirectory");
        if (PathsOverlap(archive, staging))
            throw new InvalidOperationException("As pastas de arquivo e staging têm de ser distintas e não podem estar aninhadas.");

        var settings = new ReceiptHostSettings
        {
            ConnectionString = connectionString,
            ArchiveDirectory = archive,
            StagingDirectory = staging,
            RecoveryBatchSize = ReadInt(configuration, "Recovery:BatchSize", 20),
            RecoveryInterval = TimeSpan.FromSeconds(ReadInt(configuration, "Recovery:IntervalSeconds", 60)),
            LeaseDuration = TimeSpan.FromSeconds(ReadInt(configuration, "Recovery:LeaseSeconds", 300)),
            CleanupInterval = TimeSpan.FromMinutes(ReadInt(configuration, "Staging:CleanupIntervalMinutes", 15)),
            StagingRetention = TimeSpan.FromHours(ReadInt(configuration, "Staging:RetentionHours", 24)),
            OperationsMonitorInterval = TimeSpan.FromMinutes(ReadInt(configuration, "Operations:MonitorIntervalMinutes", 5)),
            PendingAlertThreshold = TimeSpan.FromMinutes(ReadInt(configuration, "Operations:PendingAlertAfterMinutes", 60))
        };

        if (settings.RecoveryBatchSize is < 1 or > 500)
            throw new InvalidOperationException("Recovery:BatchSize tem de estar entre 1 e 500.");
        if (settings.RecoveryInterval <= TimeSpan.Zero ||
            settings.LeaseDuration <= TimeSpan.Zero ||
            settings.CleanupInterval <= TimeSpan.Zero ||
            settings.StagingRetention <= TimeSpan.Zero ||
            settings.OperationsMonitorInterval <= TimeSpan.Zero ||
            settings.PendingAlertThreshold <= TimeSpan.Zero)
            throw new InvalidOperationException("Os intervalos e durações do host têm de ser positivos.");

        return settings;
    }

    private static string GetRequiredPath(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"A configuração {key} é obrigatória.");
        return Path.GetFullPath(value);
    }

    private static int ReadInt(IConfiguration configuration, string key, int defaultValue)
    {
        var raw = configuration[key];
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;
        if (!int.TryParse(raw, out var value))
            throw new InvalidOperationException($"A configuração {key} tem de ser um número inteiro.");
        return value;
    }

    private static bool PathsOverlap(string first, string second)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var firstPrefix = Path.TrimEndingDirectorySeparator(first) + Path.DirectorySeparatorChar;
        var secondPrefix = Path.TrimEndingDirectorySeparator(second) + Path.DirectorySeparatorChar;
        return string.Equals(first, second, comparison) ||
               first.StartsWith(secondPrefix, comparison) ||
               second.StartsWith(firstPrefix, comparison);
    }
}
