using System.Security.Cryptography;
using System.Text;

namespace PhcKnowledgeScanner.Analysis.Knowledge;

/// <summary>
/// IDs determinísticos de FieldMapping.
/// Formato: <c>fm:{evidenceId}:{parserVersion}:{pathHash8}</c> — independente da ordem de leitura global.
/// </summary>
public static class FieldMappingIds
{
    public const string ExtractionVersion = "fm-v1";

    public static string Build(string declaredEvidenceId, string parserVersion, string astPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(declaredEvidenceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(parserVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(astPath);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(astPath));
        var hex = Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
        return $"fm:{declaredEvidenceId}:{parserVersion}:{hex}";
    }
}
