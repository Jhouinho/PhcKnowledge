using System.Security.Cryptography;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Inventory;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>
/// Resolves individual definition availability from explicit object/artifact evidence.
/// It never infers Empty from operation coverage or from an absent artifact.
/// </summary>
public static class DefinitionProjectionResolver
{
    private static readonly IReadOnlyDictionary<string, string> DefinitionIssueCodes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ObjectKind.View] = "sql.view.definition_missing",
            [ObjectKind.StoredProcedure] = "sql.procedure.definition_missing",
            [ObjectKind.Trigger] = "sql.trigger.definition_missing"
        };

    private static readonly HashSet<string> NotCollectedKinds =
    [
        ObjectKind.Database,
        ObjectKind.Table,
        ObjectKind.PrimaryKey,
        ObjectKind.ForeignKey,
        ObjectKind.PhcUserField
    ];

    public static IReadOnlyDictionary<string, DefinitionProjectionResolution> ResolveAll(
        IEnumerable<TechnicalObjectRecord> sourceObjects,
        IEnumerable<DefinitionArtifactSnapshot> artifacts,
        IEnumerable<CollectionIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(sourceObjects);
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(issues);

        var objects = sourceObjects.ToArray();
        var artifactGroups = artifacts
            .GroupBy(x => x.ArtifactId, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);
        var issueList = issues.ToArray();
        var objectGroups = objects
            .GroupBy(x => x.ObjectId, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);

        var results = new Dictionary<string, DefinitionProjectionResolution>(StringComparer.Ordinal);
        foreach (var group in objectGroups)
        {
            if (group.Value.Length != 1)
            {
                results[group.Key] = DefinitionProjectionResolution.Failure(
                    "definition_source_object_id_duplicate",
                    "The capture contains more than one object with the same SourceObjectId.");
                continue;
            }

            results[group.Key] = ResolveOne(group.Value[0], artifactGroups, issueList);
        }

        return results;
    }

    private static DefinitionProjectionResolution ResolveOne(
        TechnicalObjectRecord record,
        IReadOnlyDictionary<string, DefinitionArtifactSnapshot[]> artifactsById,
        IReadOnlyList<CollectionIssue> issues)
    {
        if (NotCollectedKinds.Contains(record.Kind))
        {
            if (record.Kind == ObjectKind.PhcUserField && !string.IsNullOrWhiteSpace(record.DefinitionArtifactId))
            {
                return DefinitionProjectionResolution.NotCollected(
                    "definition_descriptive_artifact_ignored",
                    "The user-field artifact is a generated metadata description, not the original column definition.");
            }

            return DefinitionProjectionResolution.NotCollected();
        }

        if (record.Kind is not (ObjectKind.View or ObjectKind.StoredProcedure or ObjectKind.Trigger)
            && !record.Kind.StartsWith("phc.", StringComparison.Ordinal))
        {
            return DefinitionProjectionResolution.Failure(
                "definition_kind_unsupported",
                $"No definition-state rule is defined for object kind '{record.Kind}'.");
        }

        var operation = ObjectOperationResolver.Resolve(record);
        if (!operation.IsResolved)
        {
            return DefinitionProjectionResolution.Unavailable(operation.DiagnosticCode!, operation.Message!);
        }

        var objectIssue = FindDefinitionIssue(record, operation.OperationId!, issues);
        if (objectIssue is not null && !string.IsNullOrWhiteSpace(record.DefinitionArtifactId))
        {
            return DefinitionProjectionResolution.Unavailable(
                "definition_artifact_issue_conflict",
                $"Object has both a definition artifact reference and a definition-unavailable issue '{objectIssue.Code}'.");
        }

        if (string.IsNullOrWhiteSpace(record.DefinitionArtifactId))
        {
            if (objectIssue is not null)
            {
                return DefinitionProjectionResolution.Unavailable(
                    "definition_unavailable_issue",
                    $"The collector reported '{objectIssue.Code}' for this object.");
            }

            return DefinitionProjectionResolution.Unavailable(
                "definition_artifact_missing",
                "The object has no definition artifact reference and no matching object-specific definition issue.");
        }

        if (!artifactsById.TryGetValue(record.DefinitionArtifactId, out var artifactMatches))
        {
            return DefinitionProjectionResolution.Unavailable(
                "definition_artifact_reference_missing",
                $"Definition artifact '{record.DefinitionArtifactId}' is not present in the artifact inventory.");
        }

        if (artifactMatches.Length != 1)
        {
            return DefinitionProjectionResolution.Unavailable(
                "definition_artifact_reference_ambiguous",
                $"Definition artifact id '{record.DefinitionArtifactId}' is not unique in the artifact inventory.");
        }

        var artifact = artifactMatches[0];
        if (artifact.Content is null)
        {
            return DefinitionProjectionResolution.Unavailable(
                "definition_artifact_bytes_missing",
                $"Definition artifact '{artifact.ArtifactId}' has no readable bytes.");
        }

        if (artifact.ExpectedByteLength is null
            || artifact.ExpectedByteLength.Value != artifact.Content.LongLength)
        {
            return DefinitionProjectionResolution.Unavailable(
                "definition_artifact_length_mismatch",
                $"Definition artifact '{artifact.ArtifactId}' does not match its recorded byte length.");
        }

        if (string.IsNullOrWhiteSpace(artifact.ExpectedSha256))
        {
            return DefinitionProjectionResolution.Unavailable(
                "definition_artifact_hash_missing",
                $"Definition artifact '{artifact.ArtifactId}' has no recorded SHA-256.");
        }

        var digest = Convert.ToHexString(SHA256.HashData(artifact.Content)).ToLowerInvariant();
        if (!string.Equals(digest, artifact.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            return DefinitionProjectionResolution.Unavailable(
                "definition_artifact_hash_mismatch",
                $"Definition artifact '{artifact.ArtifactId}' does not match its recorded SHA-256.");
        }

        if (artifact.Content.Length == 0)
        {
            return DefinitionProjectionResolution.Empty(artifact.ArtifactId);
        }

        // Integrity has been verified above. No semantic normalization policy is
        // approved for production yet, so the definition remains Available while
        // its semantic fingerprint is explicitly Unavailable.
        return DefinitionProjectionResolution.Available(
            artifact.ArtifactId,
            fingerprintUnavailableReasonCode: "NoValidatedPolicy");
    }

    private static CollectionIssue? FindDefinitionIssue(
        TechnicalObjectRecord record,
        string operationId,
        IReadOnlyList<CollectionIssue> issues)
    {
        if (!DefinitionIssueCodes.TryGetValue(record.Kind, out var expectedCode))
        {
            return null;
        }

        var expectedObjectName = BuildIssueObjectName(record);
        if (expectedObjectName is null)
        {
            return null;
        }

        return issues.FirstOrDefault(issue =>
            issue.Severity == CollectionIssueSeverity.Warning
            && string.Equals(issue.Code, expectedCode, StringComparison.Ordinal)
            && string.Equals(issue.OperationId, operationId, StringComparison.Ordinal)
            && string.Equals(issue.ObjectName, expectedObjectName, StringComparison.Ordinal));
    }

    private static string? BuildIssueObjectName(TechnicalObjectRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Name) || string.IsNullOrWhiteSpace(record.SchemaName))
        {
            return null;
        }

        if (record.Kind is ObjectKind.View or ObjectKind.StoredProcedure)
        {
            return $"{record.SchemaName}.{record.Name}";
        }

        if (record.Kind == ObjectKind.Trigger
            && !string.IsNullOrWhiteSpace(record.ParentObjectId)
            && record.ParentObjectId.StartsWith("table:", StringComparison.Ordinal))
        {
            return $"{record.ParentObjectId["table:".Length..]}.{record.Name}";
        }

        return null;
    }
}

/// <summary>Artifact metadata and the exact bytes read from the corresponding workspace artifact file.</summary>
public sealed record DefinitionArtifactSnapshot(
    string ArtifactId,
    byte[]? Content,
    string? ExpectedSha256,
    long? ExpectedByteLength);

public enum DefinitionStatus
{
    Available,
    Empty,
    Unavailable,
    NotCollected
}

/// <summary>IKP definition projection for one technical object.</summary>
public sealed record DefinitionProjectionResolution(
    DefinitionStatus Status,
    IReadOnlyList<string> ArtifactIds,
    DefinitionFingerprintValue? Fingerprint,
    string? FingerprintUnavailableReasonCode,
    string? DiagnosticCode,
    string? Message)
{
    public bool HasFingerprint => Fingerprint is not null;

    public static DefinitionProjectionResolution Available(
        string artifactId,
        string? fingerprintUnavailableReasonCode = null) =>
        new(DefinitionStatus.Available, [artifactId], null,
            fingerprintUnavailableReasonCode ?? "NoValidatedPolicy", null, null);

    public static DefinitionProjectionResolution Available(
        string artifactId,
        DefinitionFingerprintValue fingerprint) =>
        new(DefinitionStatus.Available, [artifactId], fingerprint, null, null, null);

    public static DefinitionProjectionResolution Empty(string artifactId) =>
        new(DefinitionStatus.Empty, [artifactId], null, null, null, null);

    public static DefinitionProjectionResolution NotCollected(
        string? code = null,
        string? message = null) =>
        new(DefinitionStatus.NotCollected, Array.Empty<string>(), null, null, code, message);

    public static DefinitionProjectionResolution Unavailable(string code, string message) =>
        new(DefinitionStatus.Unavailable, Array.Empty<string>(), null, null, code, message);

    public static DefinitionProjectionResolution Failure(string code, string message) =>
        Unavailable(code, message);
}

/// <summary>Semantic fingerprint value produced only by a validated, versioned policy.</summary>
public sealed record DefinitionFingerprintValue(
    string Algorithm,
    string Value,
    string PolicyId,
    string PolicyVersion);
