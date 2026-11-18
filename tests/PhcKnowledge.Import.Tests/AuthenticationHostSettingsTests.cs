using Microsoft.Extensions.Configuration;
using PhcKnowledge.Host;

namespace PhcKnowledge.Import.Tests;

public sealed class AuthenticationHostSettingsTests
{
    private static IConfiguration Configuration(IDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Enabled"] = "true",
            ["Authentication:Authority"] = "https://idp.example.test/",
            ["Authentication:Audience"] = "phcknowledge-ikp-api",
            ["Authentication:OrganizationClaimType"] = "organization_id",
            ["Authentication:RequireHttpsMetadata"] = "true"
        };

        if (overrides is not null)
            foreach (var pair in overrides)
                values[pair.Key] = pair.Value;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void Load_jwt_enabled_absent_defaults_to_false()
    {
        var settings = AuthenticationHostSettings.Load(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build());

        Assert.False(settings.JwtEnabled);
        Assert.Null(settings.Authority);
        Assert.Null(settings.Audience);
        Assert.Null(settings.Issuer);
    }

    [Fact]
    public void Load_jwt_disabled_allows_missing_authority_and_audience()
    {
        var settings = AuthenticationHostSettings.Load(Configuration(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Enabled"] = "false",
            ["Authentication:Authority"] = null,
            ["Authentication:Audience"] = null
        }));

        Assert.False(settings.JwtEnabled);
        Assert.Null(settings.Authority);
        Assert.Null(settings.Audience);
        Assert.Null(settings.Issuer);
    }

    [Fact]
    public void Load_uses_defaults_for_optional_values()
    {
        var settings = AuthenticationHostSettings.Load(Configuration(new Dictionary<string, string?>
        {
            ["Authentication:OrganizationClaimType"] = null,
            ["Authentication:RequireHttpsMetadata"] = null
        }));

        Assert.True(settings.JwtEnabled);
        Assert.Equal("https://idp.example.test", settings.Authority);
        Assert.Equal("https://idp.example.test", settings.Issuer);
        Assert.Equal("phcknowledge-ikp-api", settings.Audience);
        Assert.Equal(AuthenticationHostSettings.DefaultOrganizationClaimType, settings.OrganizationClaimType);
        Assert.True(settings.RequireHttpsMetadata);
    }

    [Fact]
    public void Load_allows_issuer_distinct_from_authority()
    {
        var settings = AuthenticationHostSettings.Load(Configuration(new Dictionary<string, string?>
        {
            ["Authentication:Issuer"] = "https://tokens.example.test/real-issuer"
        }));

        Assert.Equal("https://idp.example.test", settings.Authority);
        Assert.Equal("https://tokens.example.test/real-issuer", settings.Issuer);
    }

    [Fact]
    public void Load_preserves_issuer_trailing_slash_exactly()
    {
        var settings = AuthenticationHostSettings.Load(Configuration(new Dictionary<string, string?>
        {
            ["Authentication:Authority"] = "https://idp.example.test",
            ["Authentication:Issuer"] = "https://tokens.example.test/real-issuer/"
        }));

        Assert.Equal("https://idp.example.test", settings.Authority);
        Assert.Equal("https://tokens.example.test/real-issuer/", settings.Issuer);
    }

    [Theory]
    [InlineData("Authentication:Authority", null)]
    [InlineData("Authentication:Authority", "")]
    [InlineData("Authentication:Audience", null)]
    [InlineData("Authentication:Audience", " ")]
    public void Load_requires_authority_and_audience_when_jwt_enabled(string key, string? value)
    {
        Assert.Throws<InvalidOperationException>(() =>
            AuthenticationHostSettings.Load(Configuration(new Dictionary<string, string?> { [key] = value })));
    }

    [Fact]
    public void Load_rejects_invalid_jwt_enabled()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AuthenticationHostSettings.Load(Configuration(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:Enabled"] = "yes"
            })));
    }

    [Fact]
    public void Load_rejects_invalid_require_https_metadata()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AuthenticationHostSettings.Load(Configuration(new Dictionary<string, string?>
            {
                ["Authentication:RequireHttpsMetadata"] = "yes"
            })));
    }
}
