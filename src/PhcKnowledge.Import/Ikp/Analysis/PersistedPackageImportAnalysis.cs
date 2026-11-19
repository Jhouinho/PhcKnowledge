namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Stored row for a package import analysis (Increment B).
/// </summary>
public sealed record PersistedPackageImportAnalysis(
    Guid AnalysisId,
    Guid OrganizationId,
    Guid ReceiptId,
    Guid PackageId,
    Guid CaptureId,
    string ZipSha256,
    long ZipByteLength,
    string? LogicalContentDigest,
    string AnalysisSchemaVersion,
    string ContractVersion,
    string ReadinessStatus,
    string SemanticFingerprint,
    string AnalysisJson,
    int ObjectCount,
    int ArtifactCount,
    int IssueCount,
    int UnavailableObjectCount,
    DateTimeOffset AnalyzedAtUtc,
    DateTimeOffset CreatedAtUtc)
{
    public PackageImportAnalysis ToAnalysis() =>
        PackageImportAnalysisSerialization.Deserialize(AnalysisJson);

    public static PersistedPackageImportAnalysis FromAnalysis(
        Guid analysisId,
        PackageImportAnalysis analysis,
        long zipByteLength,
        DateTimeOffset createdAtUtc)
    {
        var json = PackageImportAnalysisSerialization.Serialize(analysis);
        var fingerprint = PackageImportAnalysisSerialization.ComputeSemanticFingerprint(analysis);
        return new PersistedPackageImportAnalysis(
            analysisId,
            analysis.OrganizationId,
            analysis.ReceiptId,
            analysis.PackageId,
            analysis.CaptureId,
            analysis.ZipSha256,
            zipByteLength,
            string.IsNullOrWhiteSpace(analysis.LogicalContentDigest) ? null : analysis.LogicalContentDigest,
            analysis.AnalysisSchemaVersion,
            analysis.ContractVersion,
            analysis.Readiness.Status.ToString(),
            fingerprint,
            json,
            analysis.ObjectIndex.TotalObjects,
            analysis.Artifacts.TotalArtifacts,
            analysis.Issues.Count,
            analysis.UnavailableObjects.Count,
            analysis.AnalyzedAtUtc,
            createdAtUtc);
    }
}
