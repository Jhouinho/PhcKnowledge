using Microsoft.Extensions.Configuration;

namespace PhcKnowledge.Host.Install;

/// <summary>
/// Loads install-time configuration. Only ConnectionStrings:Knowledge is required
/// (no JWT Authority/Audience, no storage paths).
/// </summary>
public static class InstallConfiguration
{
    public static IConfiguration Build(string? configPath = null)
    {
        var basePath = string.IsNullOrWhiteSpace(configPath)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(configPath);

        if (File.Exists(basePath) && basePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return new ConfigurationBuilder()
                .AddJsonFile(basePath, optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
        }

        var dir = Directory.Exists(basePath) ? basePath : Directory.GetCurrentDirectory();
        return new ConfigurationBuilder()
            .SetBasePath(dir)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    public static string RequireConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var connectionString = configuration.GetConnectionString("Knowledge");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "A configuração ConnectionStrings:Knowledge é obrigatória.");
        return connectionString;
    }
}
