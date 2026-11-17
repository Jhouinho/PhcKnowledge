using Microsoft.Extensions.Configuration;
using PhcKnowledge.Host;

namespace PhcKnowledge.Import.Tests;

public sealed class ReceiptHostSettingsTests
{
    private static IConfiguration Configuration(
        string? archive = null,
        string? staging = null,
        IDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Knowledge"] = "Server=(local);Database=Knowledge;Integrated Security=true;TrustServerCertificate=true",
            ["Storage:ArchiveDirectory"] = archive ?? Path.Combine(Path.GetTempPath(), "phc-knowledge-tests", "archive"),
            ["Storage:StagingDirectory"] = staging ?? Path.Combine(Path.GetTempPath(), "phc-knowledge-tests", "staging")
        };

        if (overrides is not null)
            foreach (var pair in overrides)
                values[pair.Key] = pair.Value;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void Load_uses_operational_defaults()
    {
        var settings = ReceiptHostSettings.Load(Configuration());

        Assert.Equal(20, settings.RecoveryBatchSize);
        Assert.Equal(TimeSpan.FromMinutes(1), settings.RecoveryInterval);
        Assert.Equal(TimeSpan.FromMinutes(5), settings.LeaseDuration);
        Assert.Equal(TimeSpan.FromMinutes(15), settings.CleanupInterval);
        Assert.Equal(TimeSpan.FromHours(24), settings.StagingRetention);
        Assert.Equal(TimeSpan.FromMinutes(5), settings.OperationsMonitorInterval);
        Assert.Equal(TimeSpan.FromMinutes(60), settings.PendingAlertThreshold);
        Assert.Equal(TimeSpan.FromMinutes(60), settings.AlertRepeatInterval);
    }

    [Fact]
    public void Load_rejects_identical_storage_directories()
    {
        var path = Path.Combine(Path.GetTempPath(), "phc-knowledge-tests", "same");

        Assert.Throws<InvalidOperationException>(() =>
            ReceiptHostSettings.Load(Configuration(archive: path, staging: path)));
    }

    [Fact]
    public void Load_rejects_nested_storage_directories()
    {
        var root = Path.Combine(Path.GetTempPath(), "phc-knowledge-tests", "root");

        Assert.Throws<InvalidOperationException>(() =>
            ReceiptHostSettings.Load(Configuration(
                archive: root,
                staging: Path.Combine(root, "staging"))));
    }

    [Fact]
    public void Load_rejects_a_storage_directory_nested_under_the_filesystem_root()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;

        Assert.Throws<InvalidOperationException>(() =>
            ReceiptHostSettings.Load(Configuration(
                archive: root,
                staging: Path.Combine(Path.GetTempPath(), "phc-knowledge-tests", "staging"))));
    }

    [Theory]
    [InlineData("Recovery:BatchSize", "0")]
    [InlineData("Recovery:BatchSize", "501")]
    [InlineData("Recovery:IntervalSeconds", "0")]
    [InlineData("Recovery:LeaseSeconds", "-1")]
    [InlineData("Staging:CleanupIntervalMinutes", "0")]
    [InlineData("Staging:RetentionHours", "0")]
    [InlineData("Operations:MonitorIntervalMinutes", "0")]
    [InlineData("Operations:PendingAlertAfterMinutes", "0")]
    [InlineData("Operations:AlertRepeatIntervalMinutes", "0")]
    public void Load_rejects_invalid_numeric_settings(string key, string value)
    {
        var overrides = new Dictionary<string, string?> { [key] = value };

        Assert.Throws<InvalidOperationException>(() =>
            ReceiptHostSettings.Load(Configuration(overrides: overrides)));
    }

    [Fact]
    public void Load_rejects_missing_connection_string()
    {
        var overrides = new Dictionary<string, string?> { ["ConnectionStrings:Knowledge"] = null };

        Assert.Throws<InvalidOperationException>(() =>
            ReceiptHostSettings.Load(Configuration(overrides: overrides)));
    }
}
