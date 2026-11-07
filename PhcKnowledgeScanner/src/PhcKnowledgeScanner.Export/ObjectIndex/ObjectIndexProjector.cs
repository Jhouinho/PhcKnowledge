using System.Text.Json;
using System.Text.Json.Serialization;
using PhcKnowledgeScanner.Abstractions.Inventory;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>
/// Pure, deterministic Object Index projection. It consumes an immutable workspace input
/// and never accesses SQLite, files, clocks, or external services.
/// </summary>
public sealed class ObjectIndexProjector
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ObjectIndexProjectionResult Project(ObjectIndexProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var diagnostics = new List<ObjectIndexProjectionDiagnostic>(input.Diagnostics);

        if (!input.IsReady || input.Capture is null || string.IsNullOrWhiteSpace(input.CaptureId))
        {
            return ObjectIndexProjectionResult.Failure(
                diagnostics.Append(new ObjectIndexProjectionDiagnostic(
                    "object_index_input_not_ready",
                    "The workspace input is not ready for Object Index projection.")).ToArray());
        }

        if (!Guid.TryParse(input.CaptureId, out _))
        {
            diagnostics.Add(new ObjectIndexProjectionDiagnostic(
                "object_index_capture_id_invalid",
                "CaptureId must be a UUID to satisfy IKP 0.1."));
            return ObjectIndexProjectionResult.Failure(diagnostics);
        }

        var objects = input.Objects.ToArray();
        var duplicateIds = objects
            .GroupBy(x => x.ObjectId, StringComparer.Ordinal)
            .Where(x => string.IsNullOrWhiteSpace(x.Key) || x.Count() != 1)
            .Select(x => x.Key)
            .ToHashSet(StringComparer.Ordinal);

        if (duplicateIds.Count > 0)
        {
            diagnostics.Add(new ObjectIndexProjectionDiagnostic(
                "object_index_source_object_id_not_unique",
                $"SourceObjectId is empty or duplicated for: {string.Join(", ", duplicateIds.OrderBy(x => x, StringComparer.Ordinal))}."));
            return ObjectIndexProjectionResult.Failure(diagnostics);
        }

        var objectIds = objects.Select(x => x.ObjectId).ToHashSet(StringComparer.Ordinal);
        var operations = objects.ToDictionary(x => x.ObjectId, ObjectOperationResolver.Resolve, StringComparer.Ordinal);
        var unresolvedOperations = operations.Where(x => !x.Value.IsResolved).ToArray();
        foreach (var unresolved in unresolvedOperations)
        {
            diagnostics.Add(new ObjectIndexProjectionDiagnostic(
                unresolved.Value.DiagnosticCode ?? "object_operation_unresolved",
                $"Object '{unresolved.Key}': {unresolved.Value.Message ?? "operationId could not be resolved."}"));
        }

        // operationId is mandatory in IKP 0.1. Do not emit an approximate value.
        if (unresolvedOperations.Length > 0)
        {
            return ObjectIndexProjectionResult.Failure(diagnostics);
        }

        var identities = SourceIdentityResolver.ResolveAll(objects);
        var definitions = DefinitionProjectionResolver.ResolveAll(
            objects,
            input.DefinitionArtifacts,
            input.Issues);

        var projectedObjects = new List<ObjectIndexObject>(objects.Length);
        foreach (var record in objects.OrderBy(x => x.Kind, StringComparer.Ordinal)
                     .ThenBy(x => x.ObjectId, StringComparer.Ordinal))
        {
            var metadata = MetadataProjectionResolver.Resolve(record);
            if (metadata.DiagnosticCode is not null)
            {
                diagnostics.Add(new ObjectIndexProjectionDiagnostic(
                    metadata.DiagnosticCode,
                    $"Object '{record.ObjectId}': {metadata.Message}"));
            }

            var metadataFingerprint = MetadataFingerprintCalculator.Calculate(metadata);
            if (metadataFingerprint.DiagnosticCode is not null)
            {
                diagnostics.Add(new ObjectIndexProjectionDiagnostic(
                    metadataFingerprint.DiagnosticCode,
                    $"Object '{record.ObjectId}': {metadataFingerprint.Message}"));
            }

            identities.TryGetValue(record.ObjectId, out var identity);
            if (identity?.DiagnosticCode is not null)
            {
                diagnostics.Add(new ObjectIndexProjectionDiagnostic(
                    identity.DiagnosticCode,
                    $"Object '{record.ObjectId}': {identity.Message}"));
            }

            definitions.TryGetValue(record.ObjectId, out var definition);
            if (definition is null)
            {
                definition = DefinitionProjectionResolution.Unavailable(
                    "definition_projection_missing",
                    "No definition projection was produced for this object.");
            }

            if (definition.DiagnosticCode is not null)
            {
                diagnostics.Add(new ObjectIndexProjectionDiagnostic(
                    definition.DiagnosticCode,
                    $"Object '{record.ObjectId}': {definition.Message}"));
            }

            string? parentId = null;
            if (!string.IsNullOrWhiteSpace(record.ParentObjectId))
            {
                if (objectIds.Contains(record.ParentObjectId))
                {
                    parentId = record.ParentObjectId;
                }
                else
                {
                    diagnostics.Add(new ObjectIndexProjectionDiagnostic(
                        "object_index_parent_unresolved",
                        $"Object '{record.ObjectId}' references missing parent '{record.ParentObjectId}'."));
                }
            }

            projectedObjects.Add(new ObjectIndexObject(
                record.ObjectId,
                record.Kind,
                identity?.Identity is null
                    ? null
                    : new ObjectIndexSourceIdentity(
                        identity.Identity.Scheme,
                        identity.Identity.Value,
                        identity.Identity.Scope),
                record.Name,
                record.SchemaName,
                metadata.Metadata,
                metadataFingerprint.Fingerprint is null
                    ? null
                    : new ObjectIndexFingerprint(
                        metadataFingerprint.Fingerprint.Algorithm,
                        metadataFingerprint.Fingerprint.Value),
                new ObjectIndexDefinition(
                    definition.Status.ToString(),
                    definition.ArtifactIds.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                    definition.Fingerprint is null
                        ? null
                        : new ObjectIndexFingerprint(
                            definition.Fingerprint.Algorithm,
                            definition.Fingerprint.Value)),
                operations[record.ObjectId].OperationId!,
                parentId));
        }

        var document = new ObjectIndexDocument("1", input.CaptureId!, projectedObjects);
        var json = JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions);
        return ObjectIndexProjectionResult.Success(json, diagnostics);
    }
}

public sealed record ObjectIndexDocument(
    string SchemaVersion,
    string CaptureId,
    IReadOnlyList<ObjectIndexObject> Objects);

public sealed record ObjectIndexObject(
    string SourceObjectId,
    string ObjectKind,
    ObjectIndexSourceIdentity? SourceIdentity,
    string? Name,
    string? SchemaName,
    JsonElement Metadata,
    ObjectIndexFingerprint? MetadataFingerprint,
    ObjectIndexDefinition Definition,
    string OperationId,
    string? ParentSourceObjectId);

public sealed record ObjectIndexSourceIdentity(string Scheme, string Value, string Scope);

public sealed record ObjectIndexFingerprint(string Algorithm, string Value);

public sealed record ObjectIndexDefinition(
    string Status,
    IReadOnlyList<string> ArtifactIds,
    ObjectIndexFingerprint? DefinitionFingerprint);

public sealed record ObjectIndexProjectionResult(
    bool IsSuccess,
    byte[]? JsonBytes,
    IReadOnlyList<ObjectIndexProjectionDiagnostic> Diagnostics)
{
    public string? Json => JsonBytes is null ? null : System.Text.Encoding.UTF8.GetString(JsonBytes);

    public static ObjectIndexProjectionResult Success(
        byte[] jsonBytes,
        IReadOnlyList<ObjectIndexProjectionDiagnostic> diagnostics) =>
        new(true, jsonBytes, diagnostics);

    public static ObjectIndexProjectionResult Failure(
        IReadOnlyList<ObjectIndexProjectionDiagnostic> diagnostics) =>
        new(false, null, diagnostics);
}
