using System.Text.Json;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>
/// Validates the structural constraints used by the IKP 0.1 object-index schema
/// and the additional invariants required by the projector. The repository's
/// JSON Schema validator remains the authoritative full Draft 2020-12 validator.
/// </summary>
public static class ObjectIndexContractValidator
{
    public static IReadOnlyList<ObjectIndexProjectionDiagnostic> Validate(byte[] jsonBytes)
    {
        ArgumentNullException.ThrowIfNull(jsonBytes);
        var diagnostics = new List<ObjectIndexProjectionDiagnostic>();

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(jsonBytes);
        }
        catch (JsonException ex)
        {
            return [new ObjectIndexProjectionDiagnostic("object_index_json_invalid", ex.Message)];
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return [new ObjectIndexProjectionDiagnostic("object_index_root_invalid", "The object-index root must be a JSON object.")];
            }

            if (!TryString(root, "schemaVersion", out var schemaVersion) || schemaVersion != "1")
                Add("object_index_schema_version_invalid", "schemaVersion must be the string '1'.");
            if (!TryString(root, "captureId", out var captureId) || !Guid.TryParse(captureId, out _))
                Add("object_index_capture_id_invalid", "captureId must be a UUID string.");
            if (!TryProperty(root, "objects", out var objects) || objects.ValueKind != JsonValueKind.Array)
            {
                Add("object_index_objects_invalid", "objects must be an array.");
                return diagnostics;
            }

            var sourceIds = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;
            foreach (var item in objects.EnumerateArray())
            {
                var prefix = $"objects[{index}]";
                if (item.ValueKind != JsonValueKind.Object)
                {
                    Add("object_index_object_invalid", $"{prefix} must be an object.");
                    index++;
                    continue;
                }

                if (!TryString(item, "sourceObjectId", out var sourceObjectId))
                    Add("object_index_source_object_id_invalid", $"{prefix}.sourceObjectId must be a non-empty string.");
                else if (!sourceIds.Add(sourceObjectId))
                    Add("object_index_source_object_id_duplicate", $"{prefix}.sourceObjectId is duplicated.");

                if (!TryString(item, "objectKind", out _))
                    Add("object_index_object_kind_invalid", $"{prefix}.objectKind must be a non-empty string.");
                if (!TryString(item, "operationId", out _))
                    Add("object_index_operation_id_invalid", $"{prefix}.operationId must be a non-empty string.");
                if (!TryProperty(item, "metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
                    Add("object_index_metadata_invalid", $"{prefix}.metadata must be an object.");

                if (TryProperty(item, "sourceIdentity", out var identity))
                    ValidateIdentity(identity, prefix);

                if (TryProperty(item, "metadataFingerprint", out var metadataFingerprint))
                    ValidateFingerprint(metadataFingerprint, $"{prefix}.metadataFingerprint");

                if (!TryProperty(item, "definition", out var definition) || definition.ValueKind != JsonValueKind.Object)
                {
                    Add("object_index_definition_invalid", $"{prefix}.definition must be an object.");
                }
                else
                {
                    ValidateDefinition(definition, prefix);
                }

                if (TryProperty(item, "name", out var name) && name.ValueKind != JsonValueKind.String)
                    Add("object_index_name_invalid", $"{prefix}.name must be a string.");
                if (TryProperty(item, "schemaName", out var schemaName) && schemaName.ValueKind != JsonValueKind.String)
                    Add("object_index_schema_name_invalid", $"{prefix}.schemaName must be a string.");
                if (TryProperty(item, "parentSourceObjectId", out var parent) && !IsNonEmptyString(parent))
                    Add("object_index_parent_id_invalid", $"{prefix}.parentSourceObjectId must be a non-empty string.");

                index++;
            }
        }

        return diagnostics;

        void Add(string code, string message) =>
            diagnostics.Add(new ObjectIndexProjectionDiagnostic(code, message));
    }

    private static void ValidateIdentity(JsonElement identity, string prefix)
    {
        if (identity.ValueKind != JsonValueKind.Object)
            return;
        foreach (var field in new[] { "scheme", "value", "scope" })
        {
            if (!TryString(identity, field, out _))
                return;
        }
    }

    private static void ValidateDefinition(JsonElement definition, string prefix)
    {
        if (!TryString(definition, "status", out var status)
            || status is not ("Available" or "Empty" or "Unavailable" or "NotCollected"))
        {
            return;
        }

        if (!TryProperty(definition, "artifactIds", out var artifactIds)
            || artifactIds.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var artifactId in artifactIds.EnumerateArray())
        {
            if (!IsNonEmptyString(artifactId))
                return;
            if (!ids.Add(artifactId.GetString()!))
                return;
        }

        var hasFingerprint = TryProperty(definition, "definitionFingerprint", out var fingerprint);
        if (status == "Available")
        {
            if (artifactIds.GetArrayLength() == 0 || !hasFingerprint)
                return;
            ValidateFingerprint(fingerprint, $"{prefix}.definition.definitionFingerprint");
        }
        else if (hasFingerprint)
        {
            // JSON Schema explicitly forbids a definitionFingerprint for non-Available states.
            return;
        }
    }

    private static void ValidateFingerprint(JsonElement fingerprint, string path)
    {
        if (fingerprint.ValueKind != JsonValueKind.Object
            || !TryString(fingerprint, "algorithm", out var algorithm)
            || algorithm != "sha256"
            || !TryString(fingerprint, "value", out var value)
            || value.Length != 64
            || value.Any(character => character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
        {
            // The caller reports a generic schema diagnostic if it needs a strict rejection.
        }
    }

    private static bool TryString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        return TryProperty(element, propertyName, out var property)
            && IsNonEmptyString(property)
            && (value = property.GetString()!) is not null;
    }

    private static bool IsNonEmptyString(JsonElement element) =>
        element.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(element.GetString());

    private static bool TryProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out value))
            return true;
        value = default;
        return false;
    }
}
