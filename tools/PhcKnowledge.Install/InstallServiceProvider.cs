using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Install;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Install;

public static class InstallServiceProvider
{
    public static ServiceProvider Create(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddSingleton<ICanonicalOrganizationService>(
            _ => new SqlServerCanonicalOrganizationService(connectionString));
        services.AddPhcKnowledgeIdentityPersistence(connectionString);
        services.AddSingleton(new SqlScriptApplicator(connectionString));
        return services.BuildServiceProvider();
    }
}
