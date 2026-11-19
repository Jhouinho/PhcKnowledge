using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PhcKnowledge.Host;
using PhcKnowledge.Host.Http;
using PhcKnowledge.Import.Receipts;

namespace PhcKnowledge.Import.Tests.Http;

public sealed class IkpJwtDisabledApiTests : IClassFixture<IkpJwtDisabledApiFactory>
{
    private readonly IkpJwtDisabledApiFactory _factory;

    public IkpJwtDisabledApiTests(IkpJwtDisabledApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Ikp_post_returns_503_when_jwt_disabled()
    {
        var client = _factory.CreateClient();
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.TryAddWithoutValidation("Content-Type", "application/zip");

        var response = await client.PostAsync(IkpApiEndpoints.PackagesPath, content);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await AssertDisabledProblemAsync(response);
    }

    [Fact]
    public async Task Ikp_get_attempt_returns_503_when_jwt_disabled_without_auth()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/ikp/attempts/{Guid.NewGuid():D}");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await AssertDisabledProblemAsync(response);
    }

    [Fact]
    public async Task Ikp_get_package_returns_503_when_jwt_disabled_without_auth()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/ikp/packages/{Guid.NewGuid():D}");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await AssertDisabledProblemAsync(response);
    }

    [Fact]
    public async Task Ikp_get_package_analysis_returns_503_when_jwt_disabled_without_auth()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/ikp/packages/{Guid.NewGuid():D}/analysis");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await AssertDisabledProblemAsync(response);
    }

    [Fact]
    public async Task Ikp_post_package_analysis_returns_503_when_jwt_disabled_without_auth()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/v1/ikp/packages/{Guid.NewGuid():D}/analysis", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await AssertDisabledProblemAsync(response);
    }

    [Fact]
    public async Task Health_live_remains_available_when_jwt_disabled()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task AssertDisabledProblemAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(IkpPublicErrorCodes.IkpApiDisabled, doc.RootElement.GetProperty("code").GetString());
    }
}

public sealed class IkpJwtDisabledApiFactory : WebApplicationFactory<Program>
{
    private readonly string _archive = Path.Combine(
        Path.GetTempPath(), "ikp-jwt-disabled", Guid.NewGuid().ToString("N"), "archive");
    private readonly string _staging = Path.Combine(
        Path.GetTempPath(), "ikp-jwt-disabled", Guid.NewGuid().ToString("N"), "staging");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_archive);
        Directory.CreateDirectory(_staging);

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Knowledge",
            "Server=(local);Database=PhcKnowledgeJwtDisabledTests;Integrated Security=true;TrustServerCertificate=true");
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
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (Directory.Exists(_archive))
                Directory.Delete(Path.GetDirectoryName(_archive)!, recursive: true);
        }
        catch { /* best effort */ }
    }
}
