using System.Security.Cryptography;
using PhcKnowledgeScanner.Export.ObjectIndex;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>Calculates the IKP SHA-256 fingerprint from the approved semantic metadata projection.</summary>
public static class MetadataFingerprintCalculator
{
    public static MetadataFingerprintCalculation Calculate(MetadataProjectionResolution projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        if (!projection.HasFingerprintProjection)
        {
            return projection.DiagnosticCode is null
                ? MetadataFingerprintCalculation.None()
                : MetadataFingerprintCalculation.Failure(projection.DiagnosticCode, projection.Message!);
        }

        try
        {
            var canonicalBytes = CanonicalJsonSerializer.Serialize(projection.FingerprintProjection!.Value);
            var digest = SHA256.HashData(canonicalBytes);
            var value = Convert.ToHexString(digest).ToLowerInvariant();

            return MetadataFingerprintCalculation.Success(new MetadataFingerprintValue("sha256", value));
        }
        catch (InvalidOperationException ex)
        {
            return MetadataFingerprintCalculation.Failure(
                "metadata_fingerprint_canonicalization_failed",
                ex.Message);
        }
    }
}

/// <summary>IKP fingerprint value.</summary>
public sealed record MetadataFingerprintValue(string Algorithm, string Value);

/// <summary>Outcome of calculating an optional metadata fingerprint.</summary>
public sealed record MetadataFingerprintCalculation(
    MetadataFingerprintValue? Fingerprint,
    string? DiagnosticCode,
    string? Message)
{
    public bool HasFingerprint => Fingerprint is not null;

    public static MetadataFingerprintCalculation Success(MetadataFingerprintValue fingerprint) =>
        new(fingerprint, null, null);

    public static MetadataFingerprintCalculation None() =>
        new(null, null, null);

    public static MetadataFingerprintCalculation Failure(string code, string message) =>
        new(null, code, message);
}
