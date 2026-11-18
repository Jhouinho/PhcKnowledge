using Microsoft.Extensions.Configuration;

namespace PhcKnowledge.Host;

/// <summary>
/// HTTP upload limits aligned with IkpPackageIngestionService / IkpPackageValidator defaults.
/// </summary>
public sealed class UploadHostSettings
{
    public const long DefaultMaxUploadBytes = 250L * 1024 * 1024;

    public long MaxUploadBytes { get; init; } = DefaultMaxUploadBytes;
    public int MaxConcurrentUploads { get; init; } = 1;

    public static UploadHostSettings Load(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var maxUploadBytes = ReadInt64(
            configuration, "Upload:MaxUploadBytes", DefaultMaxUploadBytes);
        if (maxUploadBytes is < 1 or > DefaultMaxUploadBytes)
            throw new InvalidOperationException(
                $"Upload:MaxUploadBytes tem de estar entre 1 e {DefaultMaxUploadBytes}.");

        var maxConcurrent = ReadInt(configuration, "Upload:MaxConcurrentUploads", 1);
        if (maxConcurrent is < 1 or > 32)
            throw new InvalidOperationException(
                "Upload:MaxConcurrentUploads tem de estar entre 1 e 32.");

        return new UploadHostSettings
        {
            MaxUploadBytes = maxUploadBytes,
            MaxConcurrentUploads = maxConcurrent
        };
    }

    private static int ReadInt(IConfiguration configuration, string key, int defaultValue)
    {
        var raw = configuration[key];
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;
        if (!int.TryParse(raw, out var value))
            throw new InvalidOperationException($"A configuração {key} tem de ser um número inteiro.");
        return value;
    }

    private static long ReadInt64(IConfiguration configuration, string key, long defaultValue)
    {
        var raw = configuration[key];
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;
        if (!long.TryParse(raw, out var value))
            throw new InvalidOperationException($"A configuração {key} tem de ser um número inteiro.");
        return value;
    }
}
