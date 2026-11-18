using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PhcKnowledge.Host;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Tests.Http;

namespace PhcKnowledge.Import.Tests.Identity;

public sealed class IdentityPersistenceRegistrationTests
    : IClassFixture<IdentityPersistenceRegistrationFactory>
{
    private readonly IdentityPersistenceRegistrationFactory _factory;

    public IdentityPersistenceRegistrationTests(IdentityPersistenceRegistrationFactory factory) =>
        _factory = factory;

    [Fact]
    public void Identity_services_are_registered()
    {
        using var scope = _factory.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<ApplicationDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetService<UserManager<ApplicationUser>>());
        Assert.NotNull(scope.ServiceProvider.GetService<RoleManager<IdentityRole<Guid>>>());
        Assert.NotNull(scope.ServiceProvider.GetService<ApplicationUserFactory>());
        Assert.NotNull(scope.ServiceProvider.GetService<IdentityRoleSeeder>());
        Assert.NotNull(scope.ServiceProvider.GetService<IIdentityMigrator>());
        Assert.NotNull(scope.ServiceProvider.GetService<UserActivationService>());
        Assert.NotNull(scope.ServiceProvider.GetService<IPortalCookiePrincipalValidator>());
        Assert.NotNull(scope.ServiceProvider.GetService<IPortalUserSessionLookup>());
        Assert.NotNull(scope.ServiceProvider.GetService<PortalCookieValidationDiagnostics>());
    }

    [Fact]
    public async Task PortalCookie_is_registered_without_identity_application_scheme()
    {
        var schemes = _factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var all = await schemes.GetAllSchemesAsync();
        Assert.Contains(all, s => s.Name == PhcAuthenticationSchemes.PortalCookie);
        Assert.Contains(all, s => s.Name == JwtBearerDefaults.AuthenticationScheme);
        Assert.DoesNotContain(all, s => s.Name == IdentityConstants.ApplicationScheme);
        Assert.DoesNotContain(all, s => s.Name == IdentityConstants.ExternalScheme);
    }

    [Fact]
    public async Task No_login_endpoint_is_exposed()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class IdentityPersistenceRegistrationFactory : WebApplicationFactory<Program>
{
    private readonly string _archive = Path.Combine(
        Path.GetTempPath(), "ikp-identity-reg", Guid.NewGuid().ToString("N"), "archive");
    private readonly string _staging = Path.Combine(
        Path.GetTempPath(), "ikp-identity-reg", Guid.NewGuid().ToString("N"), "staging");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_archive);
        Directory.CreateDirectory(_staging);
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Knowledge",
            "Server=(local);Database=PhcKnowledgeIdentityRegTests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Storage:ArchiveDirectory", _archive);
        builder.UseSetting("Storage:StagingDirectory", _staging);
        builder.UseSetting("Authentication:Jwt:Enabled", "true");
        builder.UseSetting("Authentication:Authority", "https://idp.example.test");
        builder.UseSetting("Authentication:Audience", "phcknowledge-ikp-api");
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
        catch
        {
            /* best effort */
        }
    }
}
