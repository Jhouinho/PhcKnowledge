using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host;
using PhcKnowledge.Host.Http;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;
using PhcKnowledge.Import.Tests.Http;

namespace PhcKnowledge.Import.Tests.Authentication;

/// <summary>HTTP tests for portal admin user list/detail/create/set-role/activation.</summary>
public sealed class PortalAdminUserHttpTests : IClassFixture<PortalAuthHttpFactory>
{
    private readonly PortalAuthHttpFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string StrongPassword = "Str0ng!Passw0rd";

    public PortalAdminUserHttpTests(PortalAuthHttpFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_gets_401_on_admin_user_endpoints()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(PortalAdminUserEndpoints.UsersPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(PortalAdminUserEndpoints.UsersPath + "/" + Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PutAsync(
                RolePath(Guid.NewGuid()),
                JsonBody(new { role = ApplicationRoles.Collaborator }))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(ActivatePath(Guid.NewGuid()), content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(DeactivatePath(Guid.NewGuid()), content: null)).StatusCode);
    }

    [Fact]
    public async Task Collaborator_gets_403_on_admin_user_endpoints()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("collab-adminapi@example.com", StrongPassword, roles: [ApplicationRoles.Collaborator]);
        await LoginAsync(client, "collab-adminapi@example.com", StrongPassword);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(PortalAdminUserEndpoints.UsersPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await PutRoleWithAntiforgeryAsync(client, Guid.NewGuid(), ApplicationRoles.Administrator)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await PostActivationWithAntiforgeryAsync(client, Guid.NewGuid(), activate: false)).StatusCode);
    }

    [Fact]
    public async Task User_without_roles_gets_403_on_admin_user_endpoints()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("norole-adminapi@example.com", StrongPassword, roles: []);
        await LoginAsync(client, "norole-adminapi@example.com", StrongPassword);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(PortalAdminUserEndpoints.UsersPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await PutRoleWithAntiforgeryAsync(client, Guid.NewGuid(), ApplicationRoles.Collaborator)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await PostActivationWithAntiforgeryAsync(client, Guid.NewGuid(), activate: true)).StatusCode);
    }

    [Fact]
    public async Task Administrator_can_list_and_get_users()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-list@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        await SeedUserAsync("target-list@example.com", StrongPassword, displayName: "Target User",
            roles: [ApplicationRoles.Collaborator]);
        await LoginAsync(client, "admin-list@example.com", StrongPassword);

        var list = await client.GetFromJsonAsync<PortalAdminUserListResponse>(
            PortalAdminUserEndpoints.UsersPath + "?page=1&pageSize=50", JsonOptions);
        Assert.NotNull(list);
        Assert.True(list!.TotalCount >= 2);
        Assert.Contains(list.Items, u => u.Email == "target-list@example.com");

        var target = list.Items.Single(u => u.Email == "target-list@example.com");
        var detail = await client.GetFromJsonAsync<PortalAdminUserDetailDto>(
            $"{PortalAdminUserEndpoints.UsersPath}/{target.UserId}", JsonOptions);
        Assert.Equal("Target User", detail!.DisplayName);
        Assert.Contains(ApplicationRoles.Collaborator, detail.Roles);
        AssertDoesNotExposeSecrets(await (await client.GetAsync(
            $"{PortalAdminUserEndpoints.UsersPath}/{target.UserId}")).Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_without_antiforgery_is_rejected()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-af@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        await LoginAsync(client, "admin-af@example.com", StrongPassword);

        using var content = JsonBody(new
        {
            email = "new-af@example.com",
            role = ApplicationRoles.Collaborator,
            password = StrongPassword
        });
        var response = await client.PostAsync(PortalAdminUserEndpoints.UsersPath, content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_supports_search_filters_and_pagination()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-page@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        await SeedUserAsync("alice-search@example.com", StrongPassword, displayName: "Alice Search",
            roles: [ApplicationRoles.Collaborator]);
        await SeedUserAsync("bob-inactive@example.com", StrongPassword, isActive: false,
            roles: [ApplicationRoles.Collaborator]);
        await LoginAsync(client, "admin-page@example.com", StrongPassword);

        var search = await client.GetFromJsonAsync<PortalAdminUserListResponse>(
            PortalAdminUserEndpoints.UsersPath + "?q=Alice&page=1&pageSize=10", JsonOptions);
        Assert.Contains(search!.Items, u => u.Email == "alice-search@example.com");

        var inactive = await client.GetFromJsonAsync<PortalAdminUserListResponse>(
            PortalAdminUserEndpoints.UsersPath + "?isActive=false&page=1&pageSize=50", JsonOptions);
        Assert.Contains(inactive!.Items, u => u.Email == "bob-inactive@example.com");
        Assert.All(inactive.Items, u => Assert.False(u.IsActive));

        var byRole = await client.GetFromJsonAsync<PortalAdminUserListResponse>(
            PortalAdminUserEndpoints.UsersPath + "?role=Collaborator&page=1&pageSize=50", JsonOptions);
        Assert.All(byRole!.Items, u => Assert.Contains(ApplicationRoles.Collaborator, u.Roles));

        var page1 = await client.GetFromJsonAsync<PortalAdminUserListResponse>(
            PortalAdminUserEndpoints.UsersPath + "?page=1&pageSize=1", JsonOptions);
        Assert.Equal(1, page1!.PageSize);
        Assert.True(page1.Items.Count <= 1);
        Assert.True(page1.TotalCount >= 1);
    }

    [Fact]
    public async Task Detail_returns_404_for_missing_or_other_organization_user()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-404@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        await LoginAsync(client, "admin-404@example.com", StrongPassword);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"{PortalAdminUserEndpoints.UsersPath}/{Guid.NewGuid()}")).StatusCode);

        var foreignId = await SeedForeignOrganizationUserAsync();
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"{PortalAdminUserEndpoints.UsersPath}/{foreignId}")).StatusCode);

        var list = await client.GetFromJsonAsync<PortalAdminUserListResponse>(
            PortalAdminUserEndpoints.UsersPath + "?pageSize=100", JsonOptions);
        Assert.DoesNotContain(list!.Items, u => u.UserId == foreignId);
    }

    [Fact]
    public async Task Create_collaborator_and_administrator_succeeds()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-create@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        await LoginAsync(client, "admin-create@example.com", StrongPassword);

        var collab = await PostCreateAsync(client, "created-collab@example.com", ApplicationRoles.Collaborator, "Collab");
        Assert.Equal(HttpStatusCode.Created, collab.StatusCode);
        var collabJson = await collab.Content.ReadAsStringAsync();
        AssertDoesNotExposeSecrets(collabJson);
        var collabBody = JsonSerializer.Deserialize<PortalAdminUserDetailDto>(collabJson, JsonOptions);
        Assert.True(collabBody!.IsActive);
        Assert.Equal([ApplicationRoles.Collaborator], collabBody.Roles.ToArray());

        var admin = await PostCreateAsync(client, "created-admin@example.com", ApplicationRoles.Administrator, null);
        Assert.Equal(HttpStatusCode.Created, admin.StatusCode);
        var adminBody = await admin.Content.ReadFromJsonAsync<PortalAdminUserDetailDto>(JsonOptions);
        Assert.Equal([ApplicationRoles.Administrator], adminBody!.Roles.ToArray());
    }

    [Fact]
    public async Task Create_rejects_duplicate_email_invalid_password_and_invalid_role()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-valid@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        await SeedUserAsync("dup-create@example.com", StrongPassword, roles: [ApplicationRoles.Collaborator]);
        await LoginAsync(client, "admin-valid@example.com", StrongPassword);

        var dup = await PostCreateAsync(client, "dup-create@example.com", ApplicationRoles.Collaborator, null);
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
        Assert.Contains(PortalAdminUserMessages.DuplicateEmail, await dup.Content.ReadAsStringAsync());

        var weak = await PostCreateAsync(client, "weak@example.com", ApplicationRoles.Collaborator, null, password: "short");
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.Null(await FindUserByEmailAsync("weak@example.com"));

        var badRole = await PostCreateAsync(client, "badrole@example.com", "SuperUser", null);
        Assert.Equal(HttpStatusCode.BadRequest, badRole.StatusCode);
        Assert.Contains(PortalAdminUserMessages.InvalidRole, await badRole.Content.ReadAsStringAsync());
        Assert.Null(await FindUserByEmailAsync("badrole@example.com"));
    }

    [Fact]
    public async Task Create_role_assignment_failure_does_not_leave_partial_user()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPortalUserRoleAssigner>();
                services.AddScoped<IPortalUserRoleAssigner, FailingPortalUserRoleAssigner>();
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await SeedUserAsync(factory, "admin-rolefail@example.com", StrongPassword,
            roles: [ApplicationRoles.Administrator]);
        await LoginAsync(client, "admin-rolefail@example.com", StrongPassword);

        var response = await PostCreateAsync(client, "partial-user@example.com", ApplicationRoles.Collaborator, null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(PortalAdminUserMessages.RoleAssignFailed, await response.Content.ReadAsStringAsync());
        Assert.Null(await FindUserByEmailAsync(factory, "partial-user@example.com"));
    }

    [Fact]
    public async Task Create_does_not_log_password()
    {
        var sink = new CapturingLoggerProvider();
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddLogging(b => b.AddProvider(sink));
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        await SeedUserAsync(factory, "admin-log@example.com", StrongPassword,
            roles: [ApplicationRoles.Administrator]);
        await LoginAsync(client, "admin-log@example.com", StrongPassword);

        var secret = "UniqueAdminCreate!99";
        var response = await PostCreateAsync(client, "logged-user@example.com", ApplicationRoles.Collaborator, null,
            password: secret);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var joined = string.Join('\n', sink.Messages);
        Assert.DoesNotContain(secret, joined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Legacy_user_without_roles_appears_with_empty_roles_array()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-legacy@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        var legacyId = await SeedUserAsync("legacy@example.com", StrongPassword, roles: []);
        await LoginAsync(client, "admin-legacy@example.com", StrongPassword);

        var detail = await client.GetFromJsonAsync<PortalAdminUserDetailDto>(
            $"{PortalAdminUserEndpoints.UsersPath}/{legacyId}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Empty(detail!.Roles);
    }

    [Fact]
    public async Task SetRole_without_antiforgery_is_rejected()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-role-af@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        var targetId = await SeedUserAsync("target-role-af@example.com", StrongPassword,
            roles: [ApplicationRoles.Collaborator]);
        await LoginAsync(client, "admin-role-af@example.com", StrongPassword);

        var response = await client.PutAsync(
            RolePath(targetId),
            JsonBody(new { role = ApplicationRoles.Administrator }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetRole_changes_between_application_roles_and_assigns_legacy()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-setrole@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        var collabId = await SeedUserAsync("to-admin@example.com", StrongPassword,
            roles: [ApplicationRoles.Collaborator]);
        var adminId = await SeedUserAsync("to-collab@example.com", StrongPassword,
            roles: [ApplicationRoles.Administrator]);
        var legacyId = await SeedUserAsync("legacy-setrole@example.com", StrongPassword, roles: []);
        await LoginAsync(client, "admin-setrole@example.com", StrongPassword);

        var toAdmin = await PutRoleWithAntiforgeryAsync(client, collabId, ApplicationRoles.Administrator);
        Assert.Equal(HttpStatusCode.OK, toAdmin.StatusCode);
        var toAdminDto = await toAdmin.Content.ReadFromJsonAsync<PortalAdminUserDetailDto>(JsonOptions);
        Assert.Equal([ApplicationRoles.Administrator], toAdminDto!.Roles.ToArray());

        var toCollab = await PutRoleWithAntiforgeryAsync(client, adminId, ApplicationRoles.Collaborator);
        Assert.Equal(HttpStatusCode.OK, toCollab.StatusCode);
        var toCollabDto = await toCollab.Content.ReadFromJsonAsync<PortalAdminUserDetailDto>(JsonOptions);
        Assert.Equal([ApplicationRoles.Collaborator], toCollabDto!.Roles.ToArray());

        var legacy = await PutRoleWithAntiforgeryAsync(client, legacyId, ApplicationRoles.Collaborator);
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        var legacyJson = await legacy.Content.ReadAsStringAsync();
        var legacyDto = JsonSerializer.Deserialize<PortalAdminUserDetailDto>(legacyJson, JsonOptions);
        Assert.Equal([ApplicationRoles.Collaborator], legacyDto!.Roles.ToArray());
        AssertDoesNotExposeSecrets(legacyJson);
    }

    [Fact]
    public async Task SetRole_rejects_invalid_role_and_missing_or_foreign_user()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-role-err@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        var targetId = await SeedUserAsync("target-role-err@example.com", StrongPassword,
            roles: [ApplicationRoles.Collaborator]);
        var foreignId = await SeedForeignOrganizationUserAsync();
        await LoginAsync(client, "admin-role-err@example.com", StrongPassword);

        var invalid = await PutRoleWithAntiforgeryAsync(client, targetId, "SuperUser");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await PutRoleWithAntiforgeryAsync(client, Guid.NewGuid(), ApplicationRoles.Collaborator)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await PutRoleWithAntiforgeryAsync(client, foreignId, ApplicationRoles.Collaborator)).StatusCode);
    }

    [Fact]
    public async Task SetRole_idempotent_does_not_change_security_stamp()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-role-idem@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        var targetId = await SeedUserAsync("idem-collab@example.com", StrongPassword,
            roles: [ApplicationRoles.Collaborator]);
        await LoginAsync(client, "admin-role-idem@example.com", StrongPassword);

        var before = await FindUserByEmailAsync("idem-collab@example.com");
        Assert.NotNull(before);
        var stamp = before!.SecurityStamp;

        var response = await PutRoleWithAntiforgeryAsync(client, targetId, ApplicationRoles.Collaborator);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var after = await FindUserByEmailAsync("idem-collab@example.com");
        Assert.Equal(stamp, after!.SecurityStamp);
    }

    [Fact]
    public async Task SetRole_effective_change_rotates_security_stamp()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-role-stamp@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        var targetId = await SeedUserAsync("stamp-collab@example.com", StrongPassword,
            roles: [ApplicationRoles.Collaborator]);
        await LoginAsync(client, "admin-role-stamp@example.com", StrongPassword);

        var before = await FindUserByEmailAsync("stamp-collab@example.com");
        var stamp = before!.SecurityStamp;

        Assert.Equal(HttpStatusCode.OK,
            (await PutRoleWithAntiforgeryAsync(client, targetId, ApplicationRoles.Administrator)).StatusCode);

        var after = await FindUserByEmailAsync("stamp-collab@example.com");
        Assert.NotEqual(stamp, after!.SecurityStamp);
    }

    [Fact]
    public async Task SetRole_rejects_demoting_last_active_administrator()
    {
        // Isolated InMemory database — shared fixture already contains other administrators.
        await using var factory = new PortalAuthHttpFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        var onlyAdminId = await SeedUserAsync(factory, "only-admin@example.com", StrongPassword,
            roles: [ApplicationRoles.Administrator]);
        await LoginAsync(client, "only-admin@example.com", StrongPassword);

        var response = await PutRoleWithAntiforgeryAsync(
            client, onlyAdminId, ApplicationRoles.Collaborator);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var user = await FindUserByEmailAsync(factory, "only-admin@example.com");
        Assert.True(await IsInRoleAsync(factory, user!.Id, ApplicationRoles.Administrator));
    }

    [Fact]
    public async Task Activate_deactivate_collaborator_and_admin_when_another_admin_exists()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-act@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        var collabId = await SeedUserAsync("collab-act@example.com", StrongPassword,
            roles: [ApplicationRoles.Collaborator]);
        var otherAdminId = await SeedUserAsync("admin2-act@example.com", StrongPassword,
            roles: [ApplicationRoles.Administrator]);
        await LoginAsync(client, "admin-act@example.com", StrongPassword);

        var deactivated = await PostActivationWithAntiforgeryAsync(client, collabId, activate: false);
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        var deactivatedDto = await deactivated.Content.ReadFromJsonAsync<PortalAdminUserDetailDto>(JsonOptions);
        Assert.False(deactivatedDto!.IsActive);

        var reactivated = await PostActivationWithAntiforgeryAsync(client, collabId, activate: true);
        Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
        Assert.True((await reactivated.Content.ReadFromJsonAsync<PortalAdminUserDetailDto>(JsonOptions))!.IsActive);

        var adminOff = await PostActivationWithAntiforgeryAsync(client, otherAdminId, activate: false);
        Assert.Equal(HttpStatusCode.OK, adminOff.StatusCode);
        Assert.False((await adminOff.Content.ReadFromJsonAsync<PortalAdminUserDetailDto>(JsonOptions))!.IsActive);

        var adminOn = await PostActivationWithAntiforgeryAsync(client, otherAdminId, activate: true);
        Assert.Equal(HttpStatusCode.OK, adminOn.StatusCode);
        Assert.True((await adminOn.Content.ReadFromJsonAsync<PortalAdminUserDetailDto>(JsonOptions))!.IsActive);
    }

    [Fact]
    public async Task Activate_and_deactivate_without_antiforgery_are_rejected()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-act-af@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        var targetId = await SeedUserAsync("target-act-af@example.com", StrongPassword,
            roles: [ApplicationRoles.Collaborator], isActive: false);
        await LoginAsync(client, "admin-act-af@example.com", StrongPassword);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsync(DeactivatePath(targetId), content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsync(ActivatePath(targetId), content: null)).StatusCode);
    }

    [Fact]
    public async Task Jwt_cannot_access_admin_user_endpoints()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, Guid.NewGuid().ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, AuthenticationHostSettings.ReadScope);
        client.DefaultRequestHeaders.Add("X-Role", ApplicationRoles.Administrator);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(PortalAdminUserEndpoints.UsersPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(ActivatePath(Guid.NewGuid()), content: null)).StatusCode);
    }

    [Fact]
    public async Task List_search_is_case_insensitive()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-case@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        await SeedUserAsync("CaseUser@example.com", StrongPassword, displayName: "MixedCase Name",
            roles: [ApplicationRoles.Collaborator]);
        await LoginAsync(client, "admin-case@example.com", StrongPassword);

        var byEmail = await client.GetFromJsonAsync<PortalAdminUserListResponse>(
            PortalAdminUserEndpoints.UsersPath + "?q=caseuser&pageSize=50", JsonOptions);
        Assert.Contains(byEmail!.Items, u => u.Email == "CaseUser@example.com");

        var byName = await client.GetFromJsonAsync<PortalAdminUserListResponse>(
            PortalAdminUserEndpoints.UsersPath + "?q=mixedcase&pageSize=50", JsonOptions);
        Assert.Contains(byName!.Items, u => u.DisplayName == "MixedCase Name");
    }

    [Fact]
    public async Task Create_failed_response_omits_identity_internal_messages()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-puberr@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        await LoginAsync(client, "admin-puberr@example.com", StrongPassword);

        var weak = await PostCreateAsync(client, "weak-pub@example.com", ApplicationRoles.Collaborator, null,
            password: "short");
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        var body = await weak.Content.ReadAsStringAsync();
        Assert.Contains(PortalAdminUserMessages.CreateFailed, body);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Passwords must", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("detail", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deactivate_rejects_missing_foreign_and_last_active_administrator()
    {
        await using var factory = new PortalAuthHttpFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
        var onlyAdminId = await SeedUserAsync(factory, "only-act-admin@example.com", StrongPassword,
            roles: [ApplicationRoles.Administrator]);
        await LoginAsync(client, "only-act-admin@example.com", StrongPassword);

        Assert.Equal(HttpStatusCode.NotFound,
            (await PostActivationWithAntiforgeryAsync(client, Guid.NewGuid(), activate: false)).StatusCode);

        Assert.Equal(HttpStatusCode.Conflict,
            (await PostActivationWithAntiforgeryAsync(client, onlyAdminId, activate: false)).StatusCode);

        var stillActive = await FindUserByEmailAsync(factory, "only-act-admin@example.com");
        Assert.True(stillActive!.IsActive);
    }

    [Fact]
    public async Task Activation_idempotent_keeps_security_stamp_effective_change_rotates()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-act-stamp@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        var targetId = await SeedUserAsync("stamp-act@example.com", StrongPassword,
            roles: [ApplicationRoles.Collaborator]);
        await LoginAsync(client, "admin-act-stamp@example.com", StrongPassword);

        var before = await FindUserByEmailAsync("stamp-act@example.com");
        var stamp = before!.SecurityStamp;

        Assert.Equal(HttpStatusCode.OK,
            (await PostActivationWithAntiforgeryAsync(client, targetId, activate: true)).StatusCode);
        Assert.Equal(stamp, (await FindUserByEmailAsync("stamp-act@example.com"))!.SecurityStamp);

        Assert.Equal(HttpStatusCode.OK,
            (await PostActivationWithAntiforgeryAsync(client, targetId, activate: false)).StatusCode);
        Assert.NotEqual(stamp, (await FindUserByEmailAsync("stamp-act@example.com"))!.SecurityStamp);
    }

    [Fact]
    public async Task Deactivate_returns_404_for_foreign_organization_user()
    {
        var client = CreateCookieClient();
        await SeedUserAsync("admin-act-foreign@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        var foreignId = await SeedForeignOrganizationUserAsync();
        await LoginAsync(client, "admin-act-foreign@example.com", StrongPassword);

        Assert.Equal(HttpStatusCode.NotFound,
            (await PostActivationWithAntiforgeryAsync(client, foreignId, activate: false)).StatusCode);
    }

    [Fact]
    public async Task Deactivate_blocks_login_and_reactivation_allows_new_login()
    {
        var adminClient = CreateCookieClient();
        await SeedUserAsync("admin-relogin@example.com", StrongPassword, roles: [ApplicationRoles.Administrator]);
        await SeedUserAsync("user-relogin@example.com", StrongPassword, roles: [ApplicationRoles.Collaborator]);
        var userId = (await FindUserByEmailAsync("user-relogin@example.com"))!.Id;
        await LoginAsync(adminClient, "admin-relogin@example.com", StrongPassword);

        Assert.Equal(HttpStatusCode.OK,
            (await PostActivationWithAntiforgeryAsync(adminClient, userId, activate: false)).StatusCode);

        var blocked = CreateCookieClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await LoginAsync(blocked, "user-relogin@example.com", StrongPassword)).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await PostActivationWithAntiforgeryAsync(adminClient, userId, activate: true)).StatusCode);

        var fresh = CreateCookieClient();
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(fresh, "user-relogin@example.com", StrongPassword)).StatusCode);
    }

    private HttpClient CreateCookieClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

    private Task<Guid> SeedUserAsync(
        string email,
        string password,
        string? displayName = null,
        bool isActive = true,
        IReadOnlyList<string>? roles = null) =>
        SeedUserAsync(_factory, email, password, displayName, isActive, roles);

    private static async Task<Guid> SeedUserAsync(
        WebApplicationFactory<Program> factory,
        string email,
        string password,
        string? displayName = null,
        bool isActive = true,
        IReadOnlyList<string>? roles = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        await EnsureCanonicalOrgRowAsync(scope.ServiceProvider);

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

        var userFactory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userFactory.CreateAsync(email, email, displayName);
        user.IsActive = isActive;
        Assert.True((await users.CreateAsync(user, password)).Succeeded);
        foreach (var role in roles ?? [])
            Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        return user.Id;
    }

    private async Task<Guid> SeedForeignOrganizationUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        var otherOrgId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        if (!await db.Organizations.AnyAsync(o => o.OrganizationId == otherOrgId))
        {
            // InMemory test double: EF model does not enforce SQL CK_IsCanonical.
            db.Organizations.Add(new OrganizationRow
            {
                OrganizationId = otherOrgId,
                Name = "Foreign Org",
                CreatedAtUtc = DateTime.UtcNow,
                IsCanonical = true
            });
            await db.SaveChangesAsync();
        }

        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"foreign-{userId:N}@example.com",
            NormalizedUserName = $"FOREIGN-{userId:N}@EXAMPLE.COM",
            Email = $"foreign-{userId:N}@example.com",
            NormalizedEmail = $"FOREIGN-{userId:N}@EXAMPLE.COM",
            EmailConfirmed = false,
            PasswordHash = "HASH",
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            IsActive = true,
            DisplayName = "Foreign",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        user.BindOrganizationOnCreate(otherOrgId);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return userId;
    }

    private static async Task EnsureCanonicalOrgRowAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var orgs = services.GetRequiredService<ICanonicalOrganizationService>();
        var org = await orgs.EnsureCanonicalAsync("Portal Admin Users Org");
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
    }

    private Task<ApplicationUser?> FindUserByEmailAsync(string email) =>
        FindUserByEmailAsync(_factory, email);

    private static async Task<ApplicationUser?> FindUserByEmailAsync(
        WebApplicationFactory<Program> factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.FindByEmailAsync(email);
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
    {
        using var content = JsonBody(new { email, password });
        return await PostWithAntiforgeryAsync(client, PortalApiEndpoints.LoginPath, content);
    }

    private static Task<HttpResponseMessage> PostCreateAsync(
        HttpClient client,
        string email,
        string role,
        string? displayName,
        string password = StrongPassword) =>
        PostWithAntiforgeryAsync(client, PortalAdminUserEndpoints.UsersPath,
            JsonBody(new { email, role, displayName, password }));

    private static string RolePath(Guid userId) =>
        PortalAdminUserEndpoints.UsersPath + "/" + userId.ToString("D") + "/role";

    private static string ActivatePath(Guid userId) =>
        PortalAdminUserEndpoints.UsersPath + "/" + userId.ToString("D") + "/activate";

    private static string DeactivatePath(Guid userId) =>
        PortalAdminUserEndpoints.UsersPath + "/" + userId.ToString("D") + "/deactivate";

    private static Task<HttpResponseMessage> PutRoleWithAntiforgeryAsync(
        HttpClient client, Guid userId, string role) =>
        SendWithAntiforgeryAsync(client, HttpMethod.Put, RolePath(userId),
            JsonBody(new { role }));

    private static Task<HttpResponseMessage> PostActivationWithAntiforgeryAsync(
        HttpClient client, Guid userId, bool activate) =>
        PostWithAntiforgeryAsync(client, activate ? ActivatePath(userId) : DeactivatePath(userId), content: null);

    private static Task<HttpResponseMessage> PostWithAntiforgeryAsync(
        HttpClient client, string path, HttpContent? content) =>
        SendWithAntiforgeryAsync(client, HttpMethod.Post, path, content);

    private static async Task<HttpResponseMessage> SendWithAntiforgeryAsync(
        HttpClient client, HttpMethod method, string path, HttpContent? content)
    {
        var tokenResponse = await client.GetFromJsonAsync<AntiforgeryDto>(PortalApiEndpoints.AntiforgeryPath);
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse?.Token));
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Add(PortalApiEndpoints.AntiforgeryHeaderName, tokenResponse!.Token);
        return await client.SendAsync(request);
    }

    private static async Task<bool> IsInRoleAsync(
        WebApplicationFactory<Program> factory, Guid userId, string role)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(userId.ToString("D"));
        Assert.NotNull(user);
        return await users.IsInRoleAsync(user!, role);
    }

    private static StringContent JsonBody(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static void AssertDoesNotExposeSecrets(string json)
    {
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecurityStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OrganizationId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConcurrencyStamp", json, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record AntiforgeryDto(string? Token);
}

file sealed class FailingPortalUserRoleAssigner : IPortalUserRoleAssigner
{
    public Task<IdentityResult> AssignAsync(
        ApplicationUser user,
        string roleName,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "ForcedRoleFailure",
            Description = "Forced role assignment failure."
        }));
}

file sealed class CapturingLoggerProvider : ILoggerProvider
{
    public List<string> Messages { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);
    public void Dispose() { }

    private sealed class CapturingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Add(formatter(state, exception));
            if (exception is not null)
                messages.Add(exception.ToString());
        }
    }
}
