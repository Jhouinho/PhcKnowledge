using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PhcKnowledge.Host;
using PhcKnowledge.Import.Ikp.Analysis;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Tests.Http;

namespace PhcKnowledge.Import.Tests.Ikp.Analysis;

/// <summary>
/// Confirms Host DI registers the SQL decision repository and domain service (Increment C Phase 2 host wiring).
/// Does not open SQL connections or run decisions.
/// </summary>
public sealed class PackageImportAnalysisDecisionHostRegistrationTests
    : IClassFixture<PackageImportAnalysisDecisionHostRegistrationFactory>
{
    private readonly PackageImportAnalysisDecisionHostRegistrationFactory _factory;

    public PackageImportAnalysisDecisionHostRegistrationTests(
        PackageImportAnalysisDecisionHostRegistrationFactory factory) =>
        _factory = factory;

    [Fact]
    public void Decision_repository_resolves_to_sql_implementation()
    {
        var repo = _factory.Services.GetRequiredService<IPackageImportAnalysisDecisionRepository>();
        Assert.IsType<SqlServerPackageImportAnalysisDecisionRepository>(repo);
    }

    [Fact]
    public void Decision_service_resolves_with_sql_repository()
    {
        var service = _factory.Services.GetRequiredService<PackageImportAnalysisDecisionService>();
        Assert.NotNull(service);

        var repo = _factory.Services.GetRequiredService<IPackageImportAnalysisDecisionRepository>();
        Assert.IsType<SqlServerPackageImportAnalysisDecisionRepository>(repo);
    }

    [Fact]
    public void Analysis_repository_remains_sql_and_uses_central_connection_string()
    {
        var analyses = _factory.Services.GetRequiredService<IPackageImportAnalysisRepository>();
        Assert.IsType<SqlServerPackageImportAnalysisRepository>(analyses);

        var settings = _factory.Services.GetRequiredService<ReceiptHostSettings>();
        Assert.Contains("PhcKnowledgeDecisionHostRegTests", settings.ConnectionString, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class PackageImportAnalysisDecisionHostRegistrationFactory : WebApplicationFactory<Program>
{
    private readonly string _archive = Path.Combine(
        Path.GetTempPath(), "ikp-decision-host-reg", Guid.NewGuid().ToString("N"), "archive");
    private readonly string _staging = Path.Combine(
        Path.GetTempPath(), "ikp-decision-host-reg", Guid.NewGuid().ToString("N"), "staging");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_archive);
        Directory.CreateDirectory(_staging);
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Knowledge",
            "Server=(local);Database=PhcKnowledgeDecisionHostRegTests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Storage:ArchiveDirectory", _archive);
        builder.UseSetting("Storage:StagingDirectory", _staging);
        builder.UseSetting("Authentication:Jwt:Enabled", "false");
        builder.UseSetting("Upload:MaxConcurrentUploads", "1");
        builder.UseSetting("Upload:MaxUploadBytes", UploadHostSettings.DefaultMaxUploadBytes.ToString());

        builder.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToArray())
                services.Remove(descriptor);

            services.RemoveAll<HostReadinessState>();
            var readiness = new HostReadinessState();
            readiness.MarkSchemaReady();
            services.AddSingleton(readiness);
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
