using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PhcKnowledge.Host;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Http;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Import.Tests.Http;

namespace PhcKnowledge.Import.Tests.Authentication;

/// <summary>
/// HTTP pipeline tests for continuous PortalCookie session validation (phase 1.3.3).
/// Uses EF InMemory via <see cref="PortalAuthHttpFactory"/> (not SQL).
/// </summary>
public sealed class PortalSessionValidationHttpTests : IClassFixture<PortalAuthHttpFactory>
{
    private readonly PortalAuthHttpFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PortalSessionValidationHttpTests(PortalAuthHttpFactory factory) => _factory = factory;

    [Fact]
    public async Task Active_user_session_remains_valid()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("active133@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "active133@example.com", "Str0ng!Passw0rd")).StatusCode);

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.True(session!.Authenticated);
        Assert.Equal("active133@example.com", session.Email);
    }

    [Fact]
    public async Task Session_issued_before_deactivation_fails_on_next_request()
    {
        var client = CreateCookieClient();
        var userId = await SeedUserAsync("deact133@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "deact133@example.com", "Str0ng!Passw0rd")).StatusCode);
        Assert.True((await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions))!.Authenticated);

        await DeactivateUserAsync(_factory, userId);

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.False(session!.Authenticated);

        var logout = await PostWithAntiforgeryAsync(client, PortalApiEndpoints.LogoutPath, content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, logout.StatusCode);
        Assert.DoesNotContain("Location", logout.Headers.Select(h => h.Key), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Session_issued_before_security_stamp_change_fails_on_next_request()
    {
        var client = CreateCookieClient();
        var userId = await SeedUserAsync("stamp133@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "stamp133@example.com", "Str0ng!Passw0rd")).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId.ToString("D"));
            Assert.NotNull(user);
            Assert.True((await users.UpdateSecurityStampAsync(user!)).Succeeded);
        }

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.False(session!.Authenticated);
    }

    [Fact]
    public async Task Deleted_user_loses_access_on_next_request()
    {
        var client = CreateCookieClient();
        var userId = await SeedUserAsync("gone133@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "gone133@example.com", "Str0ng!Passw0rd")).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId.ToString("D"));
            Assert.NotNull(user);
            Assert.True((await users.DeleteAsync(user!)).Succeeded);
        }

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.False(session!.Authenticated);
    }

    [Fact]
    public async Task Anonymous_request_does_not_run_session_validation_lookup()
    {
        var diagnostics = _factory.Services.GetRequiredService<PortalCookieValidationDiagnostics>();
        diagnostics.Reset();

        var client = _factory.CreateClient();
        var before = diagnostics.ValidationAttempts;
        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.False(session!.Authenticated);
        Assert.Equal(before, diagnostics.ValidationAttempts);
    }

    [Fact]
    public async Task Authenticated_request_with_validation_store_failure_is_rejected_fail_closed()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPortalUserSessionLookup>();
                services.AddSingleton<IPortalUserSessionLookup, ThrowingPortalUserSessionLookup>();
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync(factory, "failclosed133@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "failclosed133@example.com", "Str0ng!Passw0rd")).StatusCode);

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.False(session!.Authenticated);

        var body = await (await client.GetAsync(PortalApiEndpoints.SessionPath)).Content.ReadAsStringAsync();
        Assert.DoesNotContain("simulated", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_and_logout_still_work_with_session_validation()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("roundtrip133@example.com", "Str0ng!Passw0rd");

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "roundtrip133@example.com", "Str0ng!Passw0rd")).StatusCode);
        Assert.True((await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions))!.Authenticated);

        Assert.Equal(HttpStatusCode.OK,
            (await PostWithAntiforgeryAsync(client, PortalApiEndpoints.LogoutPath, content: null)).StatusCode);
        Assert.False((await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions))!.Authenticated);
    }

    [Fact]
    public async Task Jwt_does_not_satisfy_portal_session_or_bypass_cookie_validation()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, Guid.NewGuid().ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, AuthenticationHostSettings.ReadScope);

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.False(session!.Authenticated);

        var logout = await PostWithAntiforgeryAsync(client, PortalApiEndpoints.LogoutPath, content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, logout.StatusCode);
    }

    [Fact]
    public async Task Ikp_still_rejects_portal_cookie_after_valid_login()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("ikp133@example.com", "Str0ng!Passw0rd");
        await LoginAsync(client, "ikp133@example.com", "Str0ng!Passw0rd");

        var response = await client.GetAsync(
            IkpApiEndpoints.PackageByIdPath.Replace("{packageId:guid}", Guid.NewGuid().ToString("D")));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cookie_rejection_does_not_redirect_json_endpoints()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        var userId = await SeedUserAsync("noredirect133@example.com", "Str0ng!Passw0rd");
        await LoginAsync(client, "noredirect133@example.com", "Str0ng!Passw0rd");

        await DeactivateUserAsync(_factory, userId);

        var logout = await PostWithAntiforgeryAsync(client, PortalApiEndpoints.LogoutPath, content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, logout.StatusCode);
        Assert.Null(logout.Headers.Location);
        Assert.False(logout.Headers.Contains("Location"));
    }

    [Fact]
    public async Task Validation_rejects_principal_without_security_stamp_claim()
    {
        using var scope = _factory.Services.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IPortalCookiePrincipalValidator>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D"))],
            PhcAuthenticationSchemes.PortalCookie));

        Assert.False(await validator.IsSessionValidAsync(principal));
    }

    private HttpClient CreateCookieClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private Task<Guid> SeedUserAsync(string email, string password, bool isActive = true) =>
        SeedUserAsync(_factory, email, password, isActive);

    /// <summary>
    /// Mirrors UserActivationService outcome (IsActive + new stamp) without SQL transactions
    /// (InMemory provider). Full activation service is covered by SQL integration tests.
    /// </summary>
    private static async Task DeactivateUserAsync(WebApplicationFactory<Program> factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(userId.ToString("D"));
        Assert.NotNull(user);
        user!.IsActive = false;
        Assert.True((await users.UpdateSecurityStampAsync(user)).Succeeded);
    }

    private static async Task<Guid> SeedUserAsync(
        WebApplicationFactory<Program> factory,
        string email,
        string password,
        bool isActive = true)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        var orgs = scope.ServiceProvider.GetRequiredService<PhcKnowledge.Host.Organizations.ICanonicalOrganizationService>();
        var org = await orgs.EnsureCanonicalAsync("Portal Session Validation Org");
        if (!await db.Organizations.AnyAsync(o => o.OrganizationId == org.OrganizationId))
        {
            db.Organizations.Add(new OrganizationRow
            {
                OrganizationId = org.OrganizationId,
                Name = org.Name,
                CreatedAtUtc = DateTime.UtcNow,
                IsCanonical = true
            });
            await db.SaveChangesAsync();
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var roleName in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                Assert.True((await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                })).Succeeded);
            }
        }

        var factoryUser = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await factoryUser.CreateAsync(email, email, displayName: null);
        user.IsActive = isActive;
        Assert.True((await users.CreateAsync(user, password)).Succeeded);
        return user.Id;
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
    {
        using var content = new StringContent(
            JsonSerializer.Serialize(new { email, password }),
            Encoding.UTF8,
            "application/json");
        return await PostWithAntiforgeryAsync(client, PortalApiEndpoints.LoginPath, content);
    }

    private static async Task<HttpResponseMessage> PostWithAntiforgeryAsync(
        HttpClient client, string path, HttpContent? content)
    {
        var tokenResponse = await client.GetFromJsonAsync<AntiforgeryDto>(PortalApiEndpoints.AntiforgeryPath);
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse?.Token));

        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        request.Headers.Add(PortalApiEndpoints.AntiforgeryHeaderName, tokenResponse!.Token);
        return await client.SendAsync(request);
    }

    private sealed record AntiforgeryDto(string? Token);
}

file sealed class ThrowingPortalUserSessionLookup : IPortalUserSessionLookup
{
    public Task<PortalCookieSessionSnapshot?> FindAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("simulated persistence failure");
}

/// <summary>Unit coverage for fail-closed and stamp matching without HTTP.</summary>
public sealed class PortalCookiePrincipalValidatorTests
{
    [Fact]
    public async Task IsSessionValid_returns_false_when_lookup_throws()
    {
        var validator = CreateValidator(new ThrowingPortalUserSessionLookup());
        var principal = CreatePrincipal(Guid.NewGuid(), "stamp");
        Assert.False(await validator.IsSessionValidAsync(principal));
    }

    [Fact]
    public async Task IsSessionValid_returns_false_when_inactive()
    {
        var userId = Guid.NewGuid();
        var validator = CreateValidator(new FixedPortalUserSessionLookup(
            userId, new PortalCookieSessionSnapshot(IsActive: false, SecurityStamp: "stamp")));
        Assert.False(await validator.IsSessionValidAsync(CreatePrincipal(userId, "stamp")));
    }

    [Fact]
    public async Task IsSessionValid_returns_true_when_active_and_stamp_matches()
    {
        var userId = Guid.NewGuid();
        var validator = CreateValidator(new FixedPortalUserSessionLookup(
            userId, new PortalCookieSessionSnapshot(IsActive: true, SecurityStamp: "stamp")));
        Assert.True(await validator.IsSessionValidAsync(CreatePrincipal(userId, "stamp")));
    }

    [Fact]
    public async Task IsSessionValid_returns_false_when_stamp_differs()
    {
        var userId = Guid.NewGuid();
        var validator = CreateValidator(new FixedPortalUserSessionLookup(
            userId, new PortalCookieSessionSnapshot(IsActive: true, SecurityStamp: "db-stamp")));
        Assert.False(await validator.IsSessionValidAsync(CreatePrincipal(userId, "cookie-stamp")));
    }

    private static PortalCookiePrincipalValidator CreateValidator(IPortalUserSessionLookup lookup) =>
        new(
            lookup,
            Options.Create(new IdentityOptions()),
            NullLogger<PortalCookiePrincipalValidator>.Instance,
            new PortalCookieValidationDiagnostics());

    private static ClaimsPrincipal CreatePrincipal(Guid userId, string stamp)
    {
        var options = new IdentityOptions();
        return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(options.ClaimsIdentity.UserIdClaimType, userId.ToString("D")),
                new Claim(options.ClaimsIdentity.SecurityStampClaimType, stamp)
            ],
            authenticationType: PhcAuthenticationSchemes.PortalCookie));
    }
}

file sealed class FixedPortalUserSessionLookup(
    Guid userId,
    PortalCookieSessionSnapshot snapshot) : IPortalUserSessionLookup
{
    public Task<PortalCookieSessionSnapshot?> FindAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<PortalCookieSessionSnapshot?>(id == userId ? snapshot : null);
}
