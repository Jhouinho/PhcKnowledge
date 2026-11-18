using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PhcKnowledge.Host;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Http;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Tests.Http;

namespace PhcKnowledge.Import.Tests.Authentication;

public sealed class AuthSchemeSeparationJwtEnabledTests : IClassFixture<AuthSchemeJwtEnabledFactory>
{
    private readonly AuthSchemeJwtEnabledFactory _factory;

    public AuthSchemeSeparationJwtEnabledTests(AuthSchemeJwtEnabledFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task PortalCookie_and_JwtBearer_are_registered_as_distinct_schemes()
    {
        var schemes = _factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var all = (await schemes.GetAllSchemesAsync()).Select(s => s.Name).ToHashSet();

        Assert.Contains(PhcAuthenticationSchemes.PortalCookie, all);
        Assert.Contains(JwtBearerDefaults.AuthenticationScheme, all);
        Assert.DoesNotContain(IdentityConstants.ApplicationScheme, all);

        var options = _factory.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        Assert.True(string.IsNullOrEmpty(options.DefaultAuthenticateScheme));
        Assert.True(string.IsNullOrEmpty(options.DefaultScheme));
        Assert.True(string.IsNullOrEmpty(options.DefaultChallengeScheme));
    }

    [Fact]
    public async Task Cookie_authenticated_principal_cannot_access_ikp_endpoints()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var signIn = await client.PostAsync(PortalApiEndpoints.TestSignInPath, content: null);
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);

        var response = await client.GetAsync(
            IkpApiEndpoints.PackageByIdPath.Replace("{packageId:guid}", Guid.NewGuid().ToString("D")));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Jwt_authenticated_principal_does_not_authenticate_portal_session()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, Guid.NewGuid().ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, AuthenticationHostSettings.ReadScope);

        var response = await client.GetAsync(PortalApiEndpoints.SessionPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"authenticated\":false", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Portal_cookie_scheme_authenticates_without_jwt()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var signIn = await client.PostAsync(PortalApiEndpoints.TestSignInPath, content: null);
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);

        var response = await client.GetAsync(PortalApiEndpoints.TestAuthCheckPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"authenticated\":true", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ikp_read_still_requires_ikp_read_scope_via_jwt_scheme()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, Guid.NewGuid().ToString("D"));
        var response = await client.GetAsync(
            IkpApiEndpoints.PackageByIdPath.Replace("{packageId:guid}", Guid.NewGuid().ToString("D")));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ikp_read_succeeds_with_jwt_scheme_and_scope()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, Guid.NewGuid().ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, AuthenticationHostSettings.ReadScope);

        var response = await client.GetAsync(
            IkpApiEndpoints.PackageByIdPath.Replace("{packageId:guid}", Guid.NewGuid().ToString("D")));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Authorization_bearer_header_does_not_authenticate_portal_session()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync(PortalApiEndpoints.SessionPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"authenticated\":false", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Portal_logout_requires_portal_cookie_not_jwt()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, AuthenticationHostSettings.ReadScope);

        // Logout requires PortalAuthenticated (cookie). JWT alone → 401.
        var antiforgery = await client.GetFromJsonAsync<AntiforgeryDto>(PortalApiEndpoints.AntiforgeryPath);
        Assert.NotNull(antiforgery?.Token);
        using var request = new HttpRequestMessage(HttpMethod.Post, PortalApiEndpoints.LogoutPath);
        request.Headers.Add(PortalApiEndpoints.AntiforgeryHeaderName, antiforgery!.Token);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record AntiforgeryDto(string? Token);
}

public sealed class AuthSchemeSeparationJwtDisabledTests : IClassFixture<AuthSchemeJwtDisabledFactory>
{
    private readonly AuthSchemeJwtDisabledFactory _factory;

    public AuthSchemeSeparationJwtDisabledTests(AuthSchemeJwtDisabledFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Jwt_disabled_does_not_register_bearer_scheme()
    {
        var schemes = _factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var all = (await schemes.GetAllSchemesAsync()).Select(s => s.Name).ToHashSet();

        Assert.Contains(PhcAuthenticationSchemes.PortalCookie, all);
        Assert.DoesNotContain(JwtBearerDefaults.AuthenticationScheme, all);
    }

    [Fact]
    public void Jwt_disabled_allows_host_start_without_authority_or_audience()
    {
        var settings = _factory.Services.GetRequiredService<AuthenticationHostSettings>();
        Assert.False(settings.JwtEnabled);
        Assert.Null(settings.Authority);
        Assert.Null(settings.Audience);
    }

    [Fact]
    public async Task Jwt_disabled_keeps_ikp_api_disabled_503()
    {
        var client = _factory.CreateClient();
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.TryAddWithoutValidation("Content-Type", "application/zip");

        var response = await client.PostAsync(IkpApiEndpoints.PackagesPath, content);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ikp_api_disabled", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Portal_cookie_scheme_remains_available_when_jwt_disabled()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var signIn = await client.PostAsync(PortalApiEndpoints.TestSignInPath, content: null);
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);

        var response = await client.GetAsync(PortalApiEndpoints.TestAuthCheckPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"authenticated\":true", body, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class AuthSchemeJwtEnabledFactory : WebApplicationFactory<Program>
{
    private readonly string _archive = Path.Combine(
        Path.GetTempPath(), "ikp-auth-sep", Guid.NewGuid().ToString("N"), "archive");
    private readonly string _staging = Path.Combine(
        Path.GetTempPath(), "ikp-auth-sep", Guid.NewGuid().ToString("N"), "staging");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_archive);
        Directory.CreateDirectory(_staging);
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Knowledge",
            "Server=(local);Database=PhcKnowledgeAuthSepTests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Storage:ArchiveDirectory", _archive);
        builder.UseSetting("Storage:StagingDirectory", _staging);
        builder.UseSetting("Authentication:Jwt:Enabled", "true");
        builder.UseSetting("Authentication:Authority", "https://idp.example.test");
        builder.UseSetting("Authentication:Audience", "phcknowledge-ikp-api");
        builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
        builder.UseSetting("Upload:MaxConcurrentUploads", "1");
        builder.UseSetting("Upload:MaxUploadBytes", UploadHostSettings.DefaultMaxUploadBytes.ToString());

        builder.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToArray())
                services.Remove(descriptor);

            services.RemoveAll<IPackageReceiptAttemptRepository>();
            services.RemoveAll<HostReadinessState>();
            var readiness = new HostReadinessState();
            readiness.MarkSchemaReady();
            services.AddSingleton(readiness);
            services.AddSingleton<IPackageReceiptAttemptRepository>(new ControllableAttemptRepository());

            // Scheme harness uses /__test/portal-signin without Identity users/stamps.
            services.RemoveAll<IPortalCookiePrincipalValidator>();
            services.AddSingleton<IPortalCookiePrincipalValidator, PermissivePortalCookiePrincipalValidator>();

            // Host skipped real JwtBearer in Testing; register test handler as Bearer.
            // PortalCookie remains the real cookie handler (sign-in via /__test/portal-signin).
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            var root = Path.GetDirectoryName(_archive);
            if (root is not null && Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
        catch { /* best effort */ }
    }
}

public sealed class AuthSchemeJwtDisabledFactory : WebApplicationFactory<Program>
{
    private readonly string _archive = Path.Combine(
        Path.GetTempPath(), "ikp-auth-sep-off", Guid.NewGuid().ToString("N"), "archive");
    private readonly string _staging = Path.Combine(
        Path.GetTempPath(), "ikp-auth-sep-off", Guid.NewGuid().ToString("N"), "staging");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_archive);
        Directory.CreateDirectory(_staging);
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Knowledge",
            "Server=(local);Database=PhcKnowledgeAuthSepOffTests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Storage:ArchiveDirectory", _archive);
        builder.UseSetting("Storage:StagingDirectory", _staging);
        builder.UseSetting("Authentication:Jwt:Enabled", "false");
        builder.UseSetting("Upload:MaxConcurrentUploads", "1");
        builder.UseSetting("Upload:MaxUploadBytes", UploadHostSettings.DefaultMaxUploadBytes.ToString());

        builder.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToArray())
                services.Remove(descriptor);

            services.RemoveAll<IPackageReceiptAttemptRepository>();
            services.RemoveAll<HostReadinessState>();
            var readiness = new HostReadinessState();
            readiness.MarkSchemaReady();
            services.AddSingleton(readiness);
            services.AddSingleton<IPackageReceiptAttemptRepository>(new ControllableAttemptRepository());

            services.RemoveAll<IPortalCookiePrincipalValidator>();
            services.AddSingleton<IPortalCookiePrincipalValidator, PermissivePortalCookiePrincipalValidator>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            var root = Path.GetDirectoryName(_archive);
            if (root is not null && Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
        catch { /* best effort */ }
    }
}

/// <summary>Test-only: scheme harness cookies are not Identity-issued.</summary>
file sealed class PermissivePortalCookiePrincipalValidator : IPortalCookiePrincipalValidator
{
    public Task<bool> IsSessionValidAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(principal.Identity?.IsAuthenticated == true);

    public Task ValidatePrincipalAsync(
        Microsoft.AspNetCore.Authentication.Cookies.CookieValidatePrincipalContext context) =>
        Task.CompletedTask;
}
