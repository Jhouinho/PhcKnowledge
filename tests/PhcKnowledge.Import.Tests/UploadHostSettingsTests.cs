using Microsoft.Extensions.Configuration;
using PhcKnowledge.Host;

namespace PhcKnowledge.Import.Tests;

public sealed class UploadHostSettingsTests
{
    private static IConfiguration Configuration(IDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>();
        if (overrides is not null)
            foreach (var pair in overrides)
                values[pair.Key] = pair.Value;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void Load_uses_contract_defaults()
    {
        var settings = UploadHostSettings.Load(Configuration());

        Assert.Equal(UploadHostSettings.DefaultMaxUploadBytes, settings.MaxUploadBytes);
        Assert.Equal(1, settings.MaxConcurrentUploads);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("262144001")]
    public void Load_rejects_max_upload_outside_allowed_range(string value)
    {
        Assert.Throws<InvalidOperationException>(() =>
            UploadHostSettings.Load(Configuration(new Dictionary<string, string?>
            {
                ["Upload:MaxUploadBytes"] = value
            })));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("33")]
    public void Load_rejects_invalid_concurrency(string value)
    {
        Assert.Throws<InvalidOperationException>(() =>
            UploadHostSettings.Load(Configuration(new Dictionary<string, string?>
            {
                ["Upload:MaxConcurrentUploads"] = value
            })));
    }
}
