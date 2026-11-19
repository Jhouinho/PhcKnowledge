using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
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
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Ikp.Analysis;
using PhcKnowledge.Import.Receipts;

namespace PhcKnowledge.Import.Tests.Http;

/// <summary>
/// HTTP tests for Portal Increment C decision endpoints.
/// Uses InMemory Identity + InMemory analysis/decision stores — not SQL persistence validation.
/// </summary>
public sealed class PortalPackageImportAnalysisDecisionHttpTests
    : IClassFixture<PortalPackageImportAnalysisDecisionHttpFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly PortalPackageImportAnalysisDecisionHttpFactory _factory;

    public PortalPackageImportAnalysisDecisionHttpTests(
        PortalPackageImportAnalysisDecisionHttpFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Anonymous_requests_are_unauthorized()
    {
        var client = _factory.CreateClient();
        var analysisId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(DecisionUrl(analysisId))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(HistoryUrl(analysisId))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await PostDecisionAsync(client, analysisId, """{"decision":"confirmed"}""", withAntiforgery: false))
                .StatusCode);
    }

    [Fact]
    public async Task Authenticated_without_collaborator_role_is_forbidden()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync("norole@example.com", "Str0ng!Passw0rd", roles: []);
        await LoginAsync(client, "norole@example.com", "Str0ng!Passw0rd");

        var analysisId = await SeedAnalysisAsync(MaterializationReadiness.Ready);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(DecisionUrl(analysisId))).StatusCode);
    }

    [Theory]
    [InlineData(ApplicationRoles.Collaborator)]
    [InlineData(ApplicationRoles.Administrator)]
    public async Task Collaborator_and_administrator_can_confirm_ready(string role)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var email = $"{role.ToLowerInvariant()}@example.com";
        await SeedUserAsync(email, "Str0ng!Passw0rd", roles: [role]);
        await LoginAsync(client, email, "Str0ng!Passw0rd");

        var analysisId = await SeedAnalysisAsync(MaterializationReadiness.Ready);
        var unconfirmed = await client.GetFromJsonAsync<PortalPackageImportAnalysisDecisionStateDto>(
            DecisionUrl(analysisId), JsonOptions);
        Assert.Equal("unconfirmed", unconfirmed!.Decision);
        Assert.Equal(nameof(MaterializationReadiness.Ready), unconfirmed.MaterializationReadiness);

        var post = await PostDecisionAsync(client, analysisId, """{"decision":"confirmed"}""");
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        var body = await post.Content.ReadFromJsonAsync<PortalPackageImportAnalysisDecisionWriteResponseDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("confirmed", body!.Decision);
        Assert.False(body.IdempotentReplay);
        Assert.Equal(PackageImportAnalysisDecisionIdentity.Issuer, body.DecidedByIssuer);
        Assert.Equal(1, body.DecisionSequence);

        var replay = await PostDecisionAsync(client, analysisId, """{"decision":"confirmed"}""");
        var replayBody = await replay.Content.ReadFromJsonAsync<PortalPackageImportAnalysisDecisionWriteResponseDto>(JsonOptions);
        Assert.True(replayBody!.IdempotentReplay);
    }

    [Fact]
    public async Task Reject_blocked_is_allowed_confirm_blocked_is_conflict()
    {
        var client = await LoginAsCollaboratorAsync();
        var analysisId = await SeedAnalysisAsync(MaterializationReadiness.Blocked);

        var confirm = await PostDecisionAsync(client, analysisId, """{"decision":"confirmed"}""");
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);

        var reject = await PostDecisionAsync(
            client, analysisId, """{"decision":"rejected","rationale":"bloqueado"}""");
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
    }

    [Fact]
    public async Task Reject_without_rationale_is_bad_request()
    {
        var client = await LoginAsCollaboratorAsync();
        var analysisId = await SeedAnalysisAsync(MaterializationReadiness.Ready);
        var response = await PostDecisionAsync(client, analysisId, """{"decision":"rejected"}""");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Identity_fields_in_body_are_rejected()
    {
        var client = await LoginAsCollaboratorAsync();
        var analysisId = await SeedAnalysisAsync(MaterializationReadiness.Ready);
        var response = await PostDecisionAsync(
            client,
            analysisId,
            """{"decision":"confirmed","userId":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"}""");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_json_properties_are_rejected()
    {
        var client = await LoginAsCollaboratorAsync();
        var analysisId = await SeedAnalysisAsync(MaterializationReadiness.Ready);
        var response = await PostDecisionAsync(
            client, analysisId, """{"decision":"confirmed","extra":"nope"}""");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Missing_antiforgery_is_bad_request()
    {
        var client = await LoginAsCollaboratorAsync();
        var analysisId = await SeedAnalysisAsync(MaterializationReadiness.Ready);
        using var content = new StringContent("""{"decision":"confirmed"}""", Encoding.UTF8, "application/json");
        var response = await client.PostAsync(HistoryUrl(analysisId), content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Whitespace_rationale_difference_creates_new_sequence()
    {
        var client = await LoginAsCollaboratorAsync();
        var analysisId = await SeedAnalysisAsync(MaterializationReadiness.ReadyWithWarnings);

        Assert.Equal(HttpStatusCode.OK,
            (await PostDecisionAsync(client, analysisId, """{"decision":"confirmed","rationale":"ok"}""")).StatusCode);
        var second = await PostDecisionAsync(
            client, analysisId, """{"decision":"confirmed","rationale":"ok "}""");
        var body = await second.Content.ReadFromJsonAsync<PortalPackageImportAnalysisDecisionWriteResponseDto>(JsonOptions);
        Assert.False(body!.IdempotentReplay);
        Assert.Equal(2, body.DecisionSequence);

        var history = await client.GetFromJsonAsync<PortalPackageImportAnalysisDecisionHistoryResponseDto>(
            HistoryUrl(analysisId), JsonOptions);
        Assert.Equal(2, history!.Items.Count);
        Assert.Equal(new[] { 2, 1 }, history.Items.Select(x => x.DecisionSequence).ToArray());
    }

    [Fact]
    public async Task Foreign_or_missing_analysis_is_not_found()
    {
        var client = await LoginAsCollaboratorAsync();
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync(DecisionUrl(Guid.NewGuid()))).StatusCode);

        var foreign = await SeedAnalysisAsync(MaterializationReadiness.Ready, organizationId: Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(DecisionUrl(foreign))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await PostDecisionAsync(client, foreign, """{"decision":"confirmed"}""")).StatusCode);
    }

    [Fact]
    public async Task Inactive_user_cannot_use_session_for_decision()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await SeedUserAsync("inactive-decider@example.com", "Str0ng!Passw0rd",
            isActive: false, roles: [ApplicationRoles.Collaborator]);
        var login = await LoginAsync(client, "inactive-decider@example.com", "Str0ng!Passw0rd");
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Authenticated_collaborator_without_organization_is_forbidden_and_does_not_touch_repositories()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var email = $"no-org-{Guid.NewGuid():N}@example.com";
        await SeedUserWithoutOrganizationAsync(email, "Str0ng!Passw0rd", roles: [ApplicationRoles.Collaborator]);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, email, "Str0ng!Passw0rd")).StatusCode);

        var analysisId = await SeedAnalysisAsync(MaterializationReadiness.Ready);
        _factory.Analyses.ResetCounters();
        _factory.Decisions.ResetCounters();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(DecisionUrl(analysisId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(HistoryUrl(analysisId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await PostDecisionAsync(client, analysisId, """{"decision":"confirmed"}""")).StatusCode);

        Assert.Equal(0, _factory.Analyses.GetByAnalysisIdCalls);
        Assert.Equal(0, _factory.Analyses.GetCalls);
        Assert.Equal(0, _factory.Analyses.InsertCalls);
        Assert.Equal(0, _factory.Decisions.GetEffectiveCalls);
        Assert.Equal(0, _factory.Decisions.ListHistoryCalls);
        Assert.Equal(0, _factory.Decisions.AppendCalls);
        Assert.Empty(await _factory.Decisions.ListHistoryAsync(analysisId, _factory.OrganizationId));
    }

    private async Task<HttpClient> LoginAsCollaboratorAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var email = $"collab-{Guid.NewGuid():N}@example.com";
        await SeedUserAsync(email, "Str0ng!Passw0rd", roles: [ApplicationRoles.Collaborator]);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, email, "Str0ng!Passw0rd")).StatusCode);
        return client;
    }

    private static string DecisionUrl(Guid analysisId) =>
        PortalPackageImportAnalysisDecisionPaths.DecisionPath.Replace("{analysisId:guid}", analysisId.ToString("D"));

    private static string HistoryUrl(Guid analysisId) =>
        PortalPackageImportAnalysisDecisionPaths.DecisionsPath.Replace("{analysisId:guid}", analysisId.ToString("D"));

    private async Task<HttpResponseMessage> PostDecisionAsync(
        HttpClient client, Guid analysisId, string json, bool withAntiforgery = true)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        if (!withAntiforgery)
            return await client.PostAsync(HistoryUrl(analysisId), content);

        var tokenResponse = await client.GetFromJsonAsync<AntiforgeryDto>(PortalApiEndpoints.AntiforgeryPath);
        using var request = new HttpRequestMessage(HttpMethod.Post, HistoryUrl(analysisId)) { Content = content };
        request.Headers.Add(PortalApiEndpoints.AntiforgeryHeaderName, tokenResponse!.Token);
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
    {
        using var content = new StringContent(
            JsonSerializer.Serialize(new { email, password }), Encoding.UTF8, "application/json");
        var tokenResponse = await client.GetFromJsonAsync<AntiforgeryDto>(PortalApiEndpoints.AntiforgeryPath);
        using var request = new HttpRequestMessage(HttpMethod.Post, PortalApiEndpoints.LoginPath) { Content = content };
        request.Headers.Add(PortalApiEndpoints.AntiforgeryHeaderName, tokenResponse!.Token);
        return await client.SendAsync(request);
    }

    private async Task<Guid> SeedAnalysisAsync(
        MaterializationReadiness readiness,
        Guid? organizationId = null)
    {
        var org = organizationId ?? _factory.OrganizationId;
        var analysisId = Guid.NewGuid();
        var fingerprint = new string('a', 64);
        var domain = new PackageImportAnalysis(
            org, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "0.1",
            "p", "1", "DB", "sqlserver", null, null, "Full", "Complete", "1.0",
            fingerprint, fingerprint, PackageImportAnalysisSchema.Version,
            DateTimeOffset.UtcNow,
            new PackageImportIntegrityAnalysis(true, true, true, Array.Empty<string>()),
            Array.Empty<IkpInspectedCapabilityOperation>(),
            Array.Empty<IkpInspectedCoverageOperation>(),
            new PackageImportObjectIndexAnalysis(
                0, new SortedDictionary<string, int>(), new SortedDictionary<string, int>(),
                0, 0, 0, 0, 0, 0, 0, 0, 0),
            new PackageImportArtifactsAnalysis(
                0, true, Array.Empty<IkpInspectedArtifact>(), new SortedDictionary<string, int>()),
            new PackageImportRelationsAnalysis(
                false, 0, 0, 0, new SortedDictionary<string, int>()),
            Array.Empty<IkpInspectedIssue>(),
            Array.Empty<string>(),
            new PackageImportReadinessAnalysis(readiness, Array.Empty<PackageImportReadinessFinding>()),
            Array.Empty<PackageImportUnavailableObject>());

        var persisted = PersistedPackageImportAnalysis.FromAnalysis(
            analysisId, domain, 10, DateTimeOffset.UtcNow);
        await _factory.Analyses.InsertOrGetExistingAsync(persisted);
        _factory.Analyses.ResetCounters();
        return analysisId;
    }

    /// <summary>
    /// Test-only: create a valid user then clear OrganizationId via EF, bypassing Identity validators.
    /// Production creation still requires a canonical organization.
    /// </summary>
    private async Task SeedUserWithoutOrganizationAsync(
        string email,
        string password,
        IReadOnlyList<string>? roles = null)
    {
        await SeedUserAsync(email, password, roles: roles);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        typeof(ApplicationUser)
            .GetProperty(nameof(ApplicationUser.OrganizationId))!
            .SetValue(user, Guid.Empty);
        await db.SaveChangesAsync();

        var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);
        Assert.Equal(Guid.Empty, reloaded.OrganizationId);
    }

    private async Task SeedUserAsync(
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
        var org = await orgs.EnsureCanonicalAsync("Portal Decision Test Org");
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

        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
            return;

        var user = await factory.CreateAsync(email, email, displayName);
        user.IsActive = isActive;
        Assert.True((await users.CreateAsync(user, password)).Succeeded);
        foreach (var role in roles ?? [])
            Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
    }

    private sealed record AntiforgeryDto(string? Token);
}

public sealed class PortalPackageImportAnalysisDecisionHttpFactory
    : WebApplicationFactory<Program>
{
    public Guid OrganizationId { get; } = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    internal DecisionCountingAnalysisRepository Analyses { get; } = new();
    internal DecisionCountingDecisionRepository Decisions { get; } = new();

    private readonly string _archive = Path.Combine(
        Path.GetTempPath(), "portal-decision", Guid.NewGuid().ToString("N"), "archive");
    private readonly string _staging = Path.Combine(
        Path.GetTempPath(), "portal-decision", Guid.NewGuid().ToString("N"), "staging");
    private readonly string _dbName = "portal-decision-" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_archive);
        Directory.CreateDirectory(_staging);
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Knowledge",
            "Server=(local);Database=PhcKnowledgePortalDecisionHttp;Integrated Security=true;TrustServerCertificate=true");
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
            services.RemoveAll<IPackageImportAnalysisRepository>();
            services.RemoveAll<IPackageImportAnalysisDecisionRepository>();
            services.RemoveAll<PackageImportAnalysisDecisionService>();
            services.RemoveAll<HostReadinessState>();
            services.RemoveAll<ICanonicalOrganizationService>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            var readiness = new HostReadinessState();
            readiness.MarkSchemaReady();
            services.AddSingleton(readiness);
            services.AddSingleton<IPackageReceiptAttemptRepository>(new ControllableAttemptRepository());
            services.AddSingleton<IPackageImportAnalysisRepository>(Analyses);
            services.AddSingleton<IPackageImportAnalysisDecisionRepository>(Decisions);
            services.AddSingleton<PackageImportAnalysisDecisionService>();
            services.AddSingleton<ICanonicalOrganizationService>(
                new DecisionFixedCanonicalOrganizationService(
                    new CanonicalOrganization(OrganizationId, "Portal Decision Test Org", DateTimeOffset.UtcNow, true)));
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
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

file sealed class DecisionFixedCanonicalOrganizationService(CanonicalOrganization org)
    : ICanonicalOrganizationService
{
    public Task<CanonicalOrganization?> TryGetCanonicalAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<CanonicalOrganization?>(org);

    public Task<CanonicalOrganization> EnsureCanonicalAsync(
        string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(org);
}

/// <summary>Test double that counts repository access for isolation assertions.</summary>
internal sealed class DecisionCountingAnalysisRepository : IPackageImportAnalysisRepository
{
    private readonly InMemoryPackageImportAnalysisRepository _inner = new();
    public int GetCalls { get; private set; }
    public int GetByAnalysisIdCalls { get; private set; }
    public int InsertCalls { get; private set; }

    public void ResetCounters()
    {
        GetCalls = 0;
        GetByAnalysisIdCalls = 0;
        InsertCalls = 0;
    }

    public Task<PersistedPackageImportAnalysis?> GetAsync(
        Guid organizationId, Guid packageId, string zipSha256, string analysisSchemaVersion,
        CancellationToken cancellationToken = default)
    {
        GetCalls++;
        return _inner.GetAsync(organizationId, packageId, zipSha256, analysisSchemaVersion, cancellationToken);
    }

    public Task<PersistedPackageImportAnalysis?> GetByAnalysisIdAsync(
        Guid organizationId, Guid analysisId, CancellationToken cancellationToken = default)
    {
        GetByAnalysisIdCalls++;
        return _inner.GetByAnalysisIdAsync(organizationId, analysisId, cancellationToken);
    }

    public Task<PersistedPackageImportAnalysis> InsertOrGetExistingAsync(
        PersistedPackageImportAnalysis analysis, CancellationToken cancellationToken = default)
    {
        InsertCalls++;
        return _inner.InsertOrGetExistingAsync(analysis, cancellationToken);
    }
}

internal sealed class DecisionCountingDecisionRepository : IPackageImportAnalysisDecisionRepository
{
    private readonly InMemoryPackageImportAnalysisDecisionRepository _inner = new();
    public int GetEffectiveCalls { get; private set; }
    public int ListHistoryCalls { get; private set; }
    public int AppendCalls { get; private set; }

    public void ResetCounters()
    {
        GetEffectiveCalls = 0;
        ListHistoryCalls = 0;
        AppendCalls = 0;
    }

    public Task<PackageImportAnalysisDecision?> GetEffectiveAsync(
        Guid analysisId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        GetEffectiveCalls++;
        return _inner.GetEffectiveAsync(analysisId, organizationId, cancellationToken);
    }

    public Task<IReadOnlyList<PackageImportAnalysisDecision>> ListHistoryAsync(
        Guid analysisId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        ListHistoryCalls++;
        return _inner.ListHistoryAsync(analysisId, organizationId, cancellationToken);
    }

    public Task AppendAsync(
        PackageImportAnalysisDecision decision, CancellationToken cancellationToken = default)
    {
        AppendCalls++;
        return _inner.AppendAsync(decision, cancellationToken);
    }
}
