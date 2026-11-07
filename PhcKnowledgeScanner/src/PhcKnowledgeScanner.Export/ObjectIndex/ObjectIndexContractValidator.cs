using System.Text.Json;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>
/// Checks the structural constraints and conditional invariants of the IKP 0.1
/// object-index contract. The repository's JSON Schema validator remains the
/// authoritative full Draft 2020-12 validator.
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
                Add("object_index_root_invalid", "The object-index root must be a JSON object.");
                return diagnostics;
            }

            if (!TryString(root, "schemaVersion", out var schemaVersion) || schemaVersion != "1")
                Add("object_index_schema_version_invalid", "schemaVersion must be the string '1'.");
            if (!TryString(root, "captureId", out var captureId) || !Guid.TryParseExact(captureId, "D", out _))
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
                    ValidateIdentity(identity, prefix, Add);
                if (TryProperty(item, "metadataFingerprint", out var metadataFingerprint))
                    ValidateFingerprint(metadataFingerprint, $"{prefix}.metadataFingerprint", Add);

                if (!TryProperty(item, "definition", out var definition) || definition.ValueKind != JsonValueKind.Object)
                    Add("object_index_definition_invalid", $"{prefix}.definition must be an object.");
                else
                    ValidateDefinition(definition, prefix, Add);

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

    private static void ValidateIdentity(
        JsonElement identity,
        string prefix,
        Action<string, string> add)
    {
        if (identity.ValueKind != JsonValueKind.Object
            || !TryString(identity, "scheme", out _)
            || !TryString(identity, "value", out _)
            || !TryString(identity, "scope", out _))
        {
            add("object_index_source_identity_invalid", $"{prefix}.sourceIdentity must contain non-empty scheme, value and scope strings.");
        }
    }

    private static void ValidateDefinition(
        JsonElement definition,
        string prefix,
        Action<string, string> add)
    {
        if (!TryString(definition, "status", out var status)
            || status is not ("Available" or "Empty" or "Unavailable" or "NotCollected"))
        {
            add("object_index_definition_status_invalid", $"{prefix}.definition.status is not a supported IKP state.");
            return;
        }

        if (!TryProperty(definition, "artifactIds", out var artifactIds)
            || artifactIds.ValueKind != JsonValueKind.Array)
        {
            add("object_index_definition_artifacts_invalid", $"{prefix}.definition.artifactIds must be an array.");
            return;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var artifactId in artifactIds.EnumerateArray())
        {
            if (!IsNonEmptyString(artifactId) || !ids.Add(artifactId.GetString()!))
            {
                add("object_index_definition_artifacts_invalid", $"{prefix}.definition.artifactIds must contain unique non-empty strings.");
                return;
            }
        }

        var hasFingerprint = TryProperty(definition, "definitionFingerprint", out var fingerprint);
        if (status == "Available")
        {
            if (artifactIds.GetArrayLength() == 0 || !hasFingerprint)
            {
                add("object_index_definition_available_invalid", $"{prefix}.definition Available requires at least one artifact and a definitionFingerprint.");
                return;
            }

            ValidateFingerprint(fingerprint, $"{prefix}.definition.definitionFingerprint", add);
        }
        else if (hasFingerprint)
        {
            add("object_index_definition_fingerprint_forbidden", $"{prefix}.definitionFingerprint is only allowed when status is Available.");
        }
    }

    private static void ValidateFingerprint(
        JsonElement fingerprint,
        string path,
        Action<string, string> add)
    {
        if (fingerprint.ValueKind != JsonValueKind.Object
            || fingerprint.EnumerateObject().Count() != 2
            || !TryString(fingerprint, "algorithm", out var algorithm)
            || algorithm != "sha256"
            || !TryString(fingerprint, "value", out var value)
            || value.Length != 64
            || value.Any(character => character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
        {
            add("object_index_fingerprint_invalid", $"{path} must be a lowercase SHA-256 fingerprint.");
        }
    }

    private static bool TryString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!TryProperty(element, propertyName, out var property) || !IsNonEmptyString(property))
            return false;
        value = property.GetString()!;
        return true;
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
