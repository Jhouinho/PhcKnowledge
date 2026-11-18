using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PhcKnowledge.Host;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Http;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Tests.Http;

namespace PhcKnowledge.Import.Tests.Authentication;

/// <summary>
/// HTTP pipeline tests for portal login/logout/session with antiforgery.
/// Uses EF InMemory for Identity; not a SQL integration suite.
/// </summary>
public sealed class PortalAuthHttpTests : IClassFixture<PortalAuthHttpFactory>
{
    private readonly PortalAuthHttpFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PortalAuthHttpTests(PortalAuthHttpFactory factory) => _factory = factory;

    [Fact]
    public async Task Valid_login_creates_portal_cookie_and_session()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync(email: "alice@example.com", password: "Str0ng!Passw0rd", displayName: "Alice", roles: [ApplicationRoles.Collaborator]);

        var login = await LoginAsync(client, "alice@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.NotNull(session);
        Assert.True(session!.Authenticated);
        Assert.Equal("alice@example.com", session.Email);
        Assert.Equal("Alice", session.DisplayName);
        Assert.Contains(ApplicationRoles.Collaborator, session.Roles);
        Assert.DoesNotContain("OrganizationId", await (await client.GetAsync(PortalApiEndpoints.SessionPath)).Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Wrong_password_does_not_create_session()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync("bob@example.com", "Str0ng!Passw0rd");

        var login = await LoginAsync(client, "bob@example.com", "Wrong!Passw0rd");
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Contains(PortalAuthMessages.InvalidCredentials, await login.Content.ReadAsStringAsync());

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.False(session!.Authenticated);
    }

    [Fact]
    public async Task Unknown_email_does_not_create_session()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var login = await LoginAsync(client, "nobody@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Contains(PortalAuthMessages.InvalidCredentials, await login.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Inactive_user_does_not_create_session()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var userId = await SeedUserAsync("inactive@example.com", "Str0ng!Passw0rd", isActive: false);

        var login = await LoginAsync(client, "inactive@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Contains(PortalAuthMessages.InvalidCredentials, await login.Content.ReadAsStringAsync());
        Assert.DoesNotContain("inativ", await login.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.False(session!.Authenticated);
        Assert.NotEqual(Guid.Empty, userId);
    }

    [Fact]
    public async Task Locked_out_user_cannot_authenticate()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync("locked@example.com", "Str0ng!Passw0rd");

        for (var i = 0; i < 5; i++)
        {
            var failed = await LoginAsync(client, "locked@example.com", "Wrong!Passw0rd");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var stillWrongGeneric = await LoginAsync(client, "locked@example.com", "Wrong!Passw0rd");
        Assert.Equal(HttpStatusCode.Unauthorized, stillWrongGeneric.StatusCode);

        var evenCorrect = await LoginAsync(client, "locked@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.Unauthorized, evenCorrect.StatusCode);
        Assert.Contains(PortalAuthMessages.InvalidCredentials, await evenCorrect.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Logout_ends_portal_session()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync("carol@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "carol@example.com", "Str0ng!Passw0rd")).StatusCode);

        var logout = await PostWithAntiforgeryAsync(client, PortalApiEndpoints.LogoutPath, content: null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.False(session!.Authenticated);
    }

    [Fact]
    public async Task Session_returns_only_public_fields()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync("dave@example.com", "Str0ng!Passw0rd", displayName: "Dave");
        await LoginAsync(client, "dave@example.com", "Str0ng!Passw0rd");

        var raw = await (await client.GetAsync(PortalApiEndpoints.SessionPath)).Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(raw);
        var props = doc.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();
        Assert.Equal(["authenticated", "displayName", "email", "roles", "userId"], props);
    }

    [Fact]
    public async Task User_without_roles_has_empty_roles_array()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync("norole@example.com", "Str0ng!Passw0rd", roles: []);
        await LoginAsync(client, "norole@example.com", "Str0ng!Passw0rd");

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.NotNull(session);
        Assert.True(session!.Authenticated);
        Assert.Empty(session.Roles);
    }

    [Fact]
    public async Task Login_without_antiforgery_is_rejected()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync("af@example.com", "Str0ng!Passw0rd");

        using var content = new StringContent(
            """{"email":"af@example.com","password":"Str0ng!Passw0rd"}""",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync(PortalApiEndpoints.LoginPath, content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_without_antiforgery_is_rejected()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync("af2@example.com", "Str0ng!Passw0rd");
        await LoginAsync(client, "af2@example.com", "Str0ng!Passw0rd");

        var response = await client.PostAsync(PortalApiEndpoints.LogoutPath, content: null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Portal_login_does_not_accept_jwt_as_session()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, Guid.NewGuid().ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, AuthenticationHostSettings.ReadScope);

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.False(session!.Authenticated);
    }

    [Fact]
    public async Task Ikp_still_rejects_portal_cookie_after_login()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync("ikp@example.com", "Str0ng!Passw0rd");
        await LoginAsync(client, "ikp@example.com", "Str0ng!Passw0rd");

        var response = await client.GetAsync(
            IkpApiEndpoints.PackageByIdPath.Replace("{packageId:guid}", Guid.NewGuid().ToString("D")));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_session_is_stable()
    {
        var client = _factory.CreateClient();
        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.NotNull(session);
        Assert.False(session!.Authenticated);
        Assert.Null(session.UserId);
        Assert.Null(session.Email);
        Assert.Null(session.DisplayName);
        Assert.Empty(session.Roles);
    }

    private async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
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

    private async Task<Guid> SeedUserAsync(
        string email,
        string password,
        string? displayName = null,
        bool isActive = true,
        IReadOnlyList<string>? roles = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        var orgs = scope.ServiceProvider.GetRequiredService<ICanonicalOrganizationService>();
        var org = await orgs.EnsureCanonicalAsync("Portal Auth Test Org");
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

        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
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

        var user = await factory.CreateAsync(email, email, displayName);
        user.IsActive = isActive;
        var create = await users.CreateAsync(user, password);
        Assert.True(create.Succeeded, string.Join("; ", create.Errors.Select(e => e.Description)));

        foreach (var role in roles ?? [])
        {
            var added = await users.AddToRoleAsync(user, role);
            Assert.True(added.Succeeded, string.Join("; ", added.Errors.Select(e => e.Description)));
        }

        return user.Id;
    }

    private sealed record AntiforgeryDto(string? Token);
}

public sealed class PortalAuthHttpFactory : WebApplicationFactory<Program>
{
    private readonly string _archive = Path.Combine(
        Path.GetTempPath(), "portal-auth", Guid.NewGuid().ToString("N"), "archive");
    private readonly string _staging = Path.Combine(
        Path.GetTempPath(), "portal-auth", Guid.NewGuid().ToString("N"), "staging");
    private readonly string _dbName = "portal-auth-" + Guid.NewGuid().ToString("N");
    private readonly Guid _orgId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_archive);
        Directory.CreateDirectory(_staging);
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Knowledge",
            "Server=(local);Database=PhcKnowledgePortalAuthHttp;Integrated Security=true;TrustServerCertificate=true");
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
            services.RemoveAll<ICanonicalOrganizationService>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            // EF Core 8+: remove provider configuration so SqlServer + InMemory are not both active.
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            var readiness = new HostReadinessState();
            readiness.MarkSchemaReady();
            services.AddSingleton(readiness);
            services.AddSingleton<IPackageReceiptAttemptRepository>(new ControllableAttemptRepository());

            services.AddSingleton<ICanonicalOrganizationService>(
                new FixedCanonicalOrganizationService(
                    new CanonicalOrganization(_orgId, "Portal Auth Test Org", DateTimeOffset.UtcNow, true)));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

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

file sealed class FixedCanonicalOrganizationService(CanonicalOrganization org) : ICanonicalOrganizationService
{
    public Task<CanonicalOrganization?> TryGetCanonicalAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<CanonicalOrganization?>(org);

    public Task<CanonicalOrganization> EnsureCanonicalAsync(
        string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(org);
}
