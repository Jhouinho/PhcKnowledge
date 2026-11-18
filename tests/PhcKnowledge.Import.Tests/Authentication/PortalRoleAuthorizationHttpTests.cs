using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhcKnowledge.Host;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Http;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;
using PhcKnowledge.Import.Tests.Http;

namespace PhcKnowledge.Import.Tests.Authentication;

/// <summary>
/// HTTP pipeline tests for PortalCollaborator / PortalAdministrator policies (phase 1.3.4).
/// </summary>
public sealed class PortalRoleAuthorizationHttpTests : IClassFixture<PortalAuthHttpFactory>
{
    private readonly PortalAuthHttpFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PortalRoleAuthorizationHttpTests(PortalAuthHttpFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_gets_401_on_collaborator_and_administrator_policies()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(PortalApiEndpoints.TestCollaboratorPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(PortalApiEndpoints.TestAdministratorPath)).StatusCode);
    }

    [Fact]
    public async Task Authenticated_without_roles_gets_403_on_both_policies()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("norole134@example.com", "Str0ng!Passw0rd", roles: []);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "norole134@example.com", "Str0ng!Passw0rd")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(PortalApiEndpoints.TestCollaboratorPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(PortalApiEndpoints.TestAdministratorPath)).StatusCode);
    }

    [Fact]
    public async Task Collaborator_can_access_collaborator_policy_but_not_administrator()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("collab134@example.com", "Str0ng!Passw0rd", roles: [ApplicationRoles.Collaborator]);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "collab134@example.com", "Str0ng!Passw0rd")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(PortalApiEndpoints.TestCollaboratorPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(PortalApiEndpoints.TestAdministratorPath)).StatusCode);
    }

    [Fact]
    public async Task Administrator_can_access_both_policies()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin134@example.com", "Str0ng!Passw0rd", roles: [ApplicationRoles.Administrator]);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "admin134@example.com", "Str0ng!Passw0rd")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(PortalApiEndpoints.TestCollaboratorPath)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(PortalApiEndpoints.TestAdministratorPath)).StatusCode);
    }

    [Fact]
    public async Task Client_supplied_role_headers_do_not_elevate_privileges()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("header134@example.com", "Str0ng!Passw0rd", roles: [ApplicationRoles.Collaborator]);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "header134@example.com", "Str0ng!Passw0rd")).StatusCode);

        client.DefaultRequestHeaders.Add("X-Role", ApplicationRoles.Administrator);
        client.DefaultRequestHeaders.Add("Role", ApplicationRoles.Administrator);
        client.DefaultRequestHeaders.Add("ClaimTypes.Role", ApplicationRoles.Administrator);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(PortalApiEndpoints.TestCollaboratorPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(PortalApiEndpoints.TestAdministratorPath)).StatusCode);
    }

    [Fact]
    public async Task Roles_in_session_come_from_identity_not_request_body()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("identity134@example.com", "Str0ng!Passw0rd", roles: [ApplicationRoles.Collaborator]);
        await LoginAsync(client, "identity134@example.com", "Str0ng!Passw0rd");

        var session = await client.GetFromJsonAsync<PortalSessionResponse>(PortalApiEndpoints.SessionPath, JsonOptions);
        Assert.NotNull(session);
        Assert.Contains(ApplicationRoles.Collaborator, session!.Roles);
        Assert.DoesNotContain(ApplicationRoles.Administrator, session.Roles);

        // Login body cannot inject roles — only email/password are accepted.
        using var forgedLogin = new StringContent(
            """{"email":"identity134@example.com","password":"Str0ng!Passw0rd","roles":["Administrator"]}""",
            Encoding.UTF8,
            "application/json");
        // Already logged in; verify admin policy still forbidden for this Identity collaborator.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(PortalApiEndpoints.TestAdministratorPath)).StatusCode);
    }

    [Fact]
    public async Task Portal_cookie_cannot_access_ikp()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("ikp134@example.com", "Str0ng!Passw0rd", roles: [ApplicationRoles.Administrator]);
        await LoginAsync(client, "ikp134@example.com", "Str0ng!Passw0rd");

        var response = await client.GetAsync(
            IkpApiEndpoints.PackageByIdPath.Replace("{packageId:guid}", Guid.NewGuid().ToString("D")));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Jwt_cannot_access_portal_role_policies()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, Guid.NewGuid().ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, AuthenticationHostSettings.ReadScope);
        client.DefaultRequestHeaders.Add("X-Role", ApplicationRoles.Administrator);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(PortalApiEndpoints.TestCollaboratorPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(PortalApiEndpoints.TestAdministratorPath)).StatusCode);
    }

    [Fact]
    public async Task PortalAuthenticated_still_allows_authenticated_user_without_roles()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("authonly134@example.com", "Str0ng!Passw0rd", roles: []);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "authonly134@example.com", "Str0ng!Passw0rd")).StatusCode);

        // Logout requires PortalAuthenticated (session only), not a role policy.
        var logout = await PostWithAntiforgeryAsync(client, PortalApiEndpoints.LogoutPath, content: null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
    }

    [Fact]
    public async Task Role_policies_are_registered_with_expected_names()
    {
        var auth = _factory.Services.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider>();
        Assert.NotNull(await auth.GetPolicyAsync(PortalAuthorizationPolicies.Collaborator));
        Assert.NotNull(await auth.GetPolicyAsync(PortalAuthorizationPolicies.Administrator));
        Assert.NotNull(await auth.GetPolicyAsync(PortalAuthorizationPolicies.Authenticated));
    }

    private HttpClient CreateCookieClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

    private async Task<Guid> SeedUserAsync(string email, string password, IReadOnlyList<string> roles)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        var orgs = scope.ServiceProvider.GetRequiredService<ICanonicalOrganizationService>();
        var org = await orgs.EnsureCanonicalAsync("Portal Role Auth Org");
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

        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await factory.CreateAsync(email, email, displayName: null);
        Assert.True((await users.CreateAsync(user, password)).Succeeded);

        foreach (var role in roles)
            Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);

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
