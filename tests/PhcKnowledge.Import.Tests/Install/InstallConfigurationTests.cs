using Microsoft.Extensions.Configuration;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Install;

namespace PhcKnowledge.Import.Tests.Install;

/// <summary>Unit tests for install configuration and bootstrap message sanitization.</summary>
public sealed class InstallConfigurationTests
{
    [Fact]
    public void RequireConnectionString_throws_clear_error_when_missing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var ex = Assert.Throws<InvalidOperationException>(
            () => InstallConfiguration.RequireConnectionString(config));
        Assert.Contains("ConnectionStrings:Knowledge", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RequireConnectionString_returns_value_when_present()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Knowledge"] = "Server=(local);Database=X;Integrated Security=true"
            })
            .Build();
        Assert.Contains("Database=X", InstallConfiguration.RequireConnectionString(config));
    }

    [Fact]
    public void SanitizeBootstrapException_hides_unexpected_provider_messages()
    {
        var inner = new Exception("Login failed for user sa with password=SuperSecret!99");
        var sanitized = FirstAdministratorBootstrapService.SanitizeBootstrapException(inner);
        Assert.IsType<InvalidOperationException>(sanitized);
        Assert.DoesNotContain("SuperSecret", sanitized.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("password=", sanitized.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SanitizeBootstrapException_preserves_rejected_exceptions()
    {
        var rejected = new BootstrapAdminRejectedException("Já existe um administrador.");
        Assert.Same(rejected, FirstAdministratorBootstrapService.SanitizeBootstrapException(rejected));
    }
}
