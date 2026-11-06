using System.Reflection;

namespace PhcKnowledgeScanner.Engine;

public static class ScannerVersion
{
    public static string Current { get; } =
        typeof(ScanEngine).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ScanEngine).Assembly.GetName().Version?.ToString()
        ?? "0.0.0-dev";
}
