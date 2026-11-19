using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Deterministic JSON serialization and semantic fingerprint for PackageImportAnalysis.
/// </summary>
public static class PackageImportAnalysisSerialization
{
    public static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static string Serialize(PackageImportAnalysis analysis) =>
        JsonSerializer.Serialize(analysis, JsonOptions);

    public static PackageImportAnalysis Deserialize(string json)
    {
        var analysis = JsonSerializer.Deserialize<PackageImportAnalysis>(json, JsonOptions);
        if (analysis is null)
            throw new InvalidOperationException("AnalysisJson inválido ou vazio.");
        return analysis;
    }

    /// <summary>
    /// Explicit semantic projection: every analysis field except provenance/time metadata.
    /// Excluded: <see cref="PackageImportAnalysis.ReceiptId"/>, <see cref="PackageImportAnalysis.AnalyzedAtUtc"/>.
    /// Included: identity (org/package/capture), integrity, capabilities, coverage, object index,
    /// artifacts, relations, issues, limitations, readiness, unavailable objects, digests,
    /// AnalysisSchemaVersion, and related interpretation results.
    /// Distinct from ZipSha256, LogicalContentDigest, DefinitionFingerprint, MetadataFingerprint.
    /// </summary>
    public static PackageImportAnalysisSemanticProjection ToSemanticProjection(
        PackageImportAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        return new PackageImportAnalysisSemanticProjection(
            analysis.OrganizationId,
            analysis.PackageId,
            analysis.CaptureId,
            analysis.ContractVersion,
            analysis.ProducerProductId,
            analysis.ProducerProductVersion,
            analysis.OriginDatabaseName,
            analysis.OriginEngine,
            analysis.OriginServerLabel,
            analysis.OriginPhcVersion,
            analysis.CaptureMode,
            analysis.MetadataStatus,
            analysis.ScannerVersion,
            analysis.ZipSha256,
            analysis.LogicalContentDigest,
            analysis.AnalysisSchemaVersion,
            analysis.Integrity,
            analysis.Capabilities,
            analysis.Coverage,
            analysis.ObjectIndex,
            analysis.Artifacts,
            analysis.Relations,
            analysis.Issues,
            analysis.Limitations,
            analysis.Readiness,
            analysis.UnavailableObjects);
    }

    /// <summary>
    /// SHA-256 (hex lower) over the canonical JSON of <see cref="ToSemanticProjection"/>.
    /// </summary>
    public static string ComputeSemanticFingerprint(PackageImportAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var json = JsonSerializer.Serialize(ToSemanticProjection(analysis), JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Canonical semantic payload for fingerprinting. Intentionally omits ReceiptId and AnalyzedAtUtc.
/// </summary>
public sealed record PackageImportAnalysisSemanticProjection(
    Guid OrganizationId,
    Guid PackageId,
    Guid CaptureId,
    string ContractVersion,
    string ProducerProductId,
    string ProducerProductVersion,
    string? OriginDatabaseName,
    string OriginEngine,
    string? OriginServerLabel,
    string? OriginPhcVersion,
    string? CaptureMode,
    string? MetadataStatus,
    string? ScannerVersion,
    string ZipSha256,
    string LogicalContentDigest,
    string AnalysisSchemaVersion,
    PackageImportIntegrityAnalysis Integrity,
    IReadOnlyList<IkpInspectedCapabilityOperation> Capabilities,
    IReadOnlyList<IkpInspectedCoverageOperation> Coverage,
    PackageImportObjectIndexAnalysis ObjectIndex,
    PackageImportArtifactsAnalysis Artifacts,
    PackageImportRelationsAnalysis Relations,
    IReadOnlyList<IkpInspectedIssue> Issues,
    IReadOnlyList<string> Limitations,
    PackageImportReadinessAnalysis Readiness,
    IReadOnlyList<PackageImportUnavailableObject> UnavailableObjects);
