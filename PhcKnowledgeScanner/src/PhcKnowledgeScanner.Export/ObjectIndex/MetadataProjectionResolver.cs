using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>
/// Projects descriptive metadata and the separate semantic input used by MetadataFingerprint.
/// Hashing/canonicalization is intentionally kept separate from this projection.
/// </summary>
public static class MetadataProjectionResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private static readonly IReadOnlyDictionary<string, PhcMetadataSpec> PhcSpecs =
        new Dictionary<string, PhcMetadataSpec>(StringComparer.Ordinal)
        {
            [ObjectKind.PhcUserRule] = new(["tabela"], "inactivo", ["tabela"]),
            [ObjectKind.PhcWebUserRule] = new(["tabela", "ecran"], "inactivo", ["tabela", "ecran"]),
            [ObjectKind.PhcHotkey] = new(["ecran", "teclas"], "inactivo", ["ecran", "teclas"]),
            [ObjectKind.PhcDefaultValue] = new(["tabela", "campo"], "inactivo", ["tabela", "campo"]),
            [ObjectKind.PhcWebDefaultValue] = new(["tabela", "campo", "ecran"], "inactivo", ["tabela", "campo", "ecran"]),
            [ObjectKind.PhcEvent] = new(["ecran", "qevento"], "inactivo", ["ecran", "qevento"]),
            [ObjectKind.PhcWebEvent] = new(["ecran", "qevento"], "inactivo", ["ecran", "qevento"]),
            [ObjectKind.PhcUserField] = new(
                ["schema", "table", "column", "type", "maxLength", "precision", "scale", "isNullable", "prefix"],
                null,
                ["schema", "table", "column", "type", "maxLength", "precision", "scale", "isNullable"]),
            [ObjectKind.PhcWebScript] = new(["codigo"], "inactivo", ["codigo"]),
            [ObjectKind.PhcUserJavascript] = new(["ecran"], "inativo", ["ecran"]),
            [ObjectKind.PhcWebCssJs] = new(["codigo", "tipo"], "inativo", ["codigo", "tipo"]),
            [ObjectKind.PhcHiddenGrid] = new(["ecran", "nome", "tag", "tvalobj"], null, ["ecran", "nome", "tag", "tvalobj"]),
            [ObjectKind.PhcCustomScreen] = new(["ecran", "nome", "tabela", "defeito"], "inactivo", ["ecran", "nome", "tabela", "defeito"]),
            [ObjectKind.PhcUserTsqlFunction] = new(["classif"], "inactivo", ["classif"])
        };

    public static MetadataProjectionResolution Resolve(TechnicalObjectRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var operation = ObjectOperationResolver.Resolve(record);
        if (!operation.IsResolved)
        {
            return MetadataProjectionResolution.Failure(operation.DiagnosticCode!, operation.Message!);
        }

        if (record.Kind is ObjectKind.View or ObjectKind.StoredProcedure
            && string.IsNullOrWhiteSpace(record.SummaryJson))
        {
            return MetadataProjectionResolution.Success(EmptyObject(), null);
        }

        if (string.IsNullOrWhiteSpace(record.SummaryJson))
        {
            return MetadataProjectionResolution.Failure(
                "metadata_summary_missing",
                $"Object kind '{record.Kind}' requires SummaryJson for its metadata projection.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(record.SummaryJson);
        }
        catch (JsonException)
        {
            return MetadataProjectionResolution.Failure(
                "metadata_summary_invalid",
                "SummaryJson is not valid JSON.");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return MetadataProjectionResolution.Failure(
                    "metadata_summary_invalid",
                    "SummaryJson must be a JSON object.");
            }

            var root = document.RootElement;
            if (record.Kind.StartsWith("phc.", StringComparison.Ordinal))
            {
                return ResolvePhc(record, root);
            }

            return ResolveSql(record, root);
        }
    }

    private static MetadataProjectionResolution ResolveSql(
        TechnicalObjectRecord record,
        JsonElement root)
    {
        var metadata = NewObject();
        var fingerprint = NewObject();

        switch (record.Kind)
        {
            case ObjectKind.Database:
                Copy(root, "versionProperty", metadata);
                return MetadataProjectionResolution.Success(ToJson(metadata), null);

            case ObjectKind.Table:
                Copy(root, "typeDesc", metadata);
                Copy(root, "isMsShipped", metadata);
                Copy(root, "classification", metadata);
                Copy(root, "namePrefixU", metadata);
                Copy(root, "createDate", metadata);
                Copy(root, "modifyDate", metadata);

                var tableFingerprintComplete =
                    Copy(root, "typeDesc", fingerprint)
                    && Copy(root, "isMsShipped", fingerprint)
                    && IsString(root, "typeDesc")
                    && IsBoolean(root, "isMsShipped");
                return CompleteOrDiagnostic(metadata, fingerprint, tableFingerprintComplete);

            case ObjectKind.PrimaryKey:
                if (!TryProjectColumns(root, "columns", ["name", "ordinal"], out var pkColumns))
                {
                    return MetadataProjectionResolution.Failure(
                        "metadata_columns_invalid",
                        "Primary-key SummaryJson must contain a non-empty ordered columns array with name and ordinal.");
                }

                metadata["columns"] = pkColumns;
                fingerprint["columns"] = pkColumns;
                return MetadataProjectionResolution.Success(ToJson(metadata), ToJson(fingerprint));

            case ObjectKind.ForeignKey:
                var fkComplete = true;
                foreach (var field in new[] { "referencedSchema", "referencedTable", "deleteReferentialAction", "updateReferentialAction" })
                {
                    var copied = Copy(root, field, metadata);
                    fkComplete &= copied && Copy(root, field, fingerprint);
                }

                if (!TryProjectColumns(root, "columns", ["parent", "referenced", "ordinal"], out var fkColumns))
                {
                    return MetadataProjectionResolution.Failure(
                        "metadata_columns_invalid",
                        "Foreign-key SummaryJson must contain a non-empty ordered columns array with parent, referenced and ordinal.");
                }

                metadata["columns"] = fkColumns;
                fingerprint["columns"] = fkColumns;
                return CompleteOrDiagnostic(metadata, fingerprint, fkComplete);

            case ObjectKind.Trigger:
                var triggerComplete = Copy(root, "isDisabled", metadata)
                    && Copy(root, "isDisabled", fingerprint)
                    && IsBoolean(root, "isDisabled");
                return CompleteOrDiagnostic(metadata, fingerprint, triggerComplete);

            case ObjectKind.View:
            case ObjectKind.StoredProcedure:
                return MetadataProjectionResolution.Success(EmptyObject(), null);

            default:
                return MetadataProjectionResolution.Failure(
                    "metadata_kind_unsupported",
                    $"No SQL metadata projection is defined for object kind '{record.Kind}'.");
        }
    }

    private static MetadataProjectionResolution ResolvePhc(
        TechnicalObjectRecord record,
        JsonElement root)
    {
        if (!PhcSpecs.TryGetValue(record.Kind, out var spec))
        {
            return MetadataProjectionResolution.Failure(
                "metadata_kind_unsupported",
                $"No PHC metadata projection is defined for object kind '{record.Kind}'.");
        }

        var source = ReadString(root, "source");
        var isInstalledFunction = record.Kind == ObjectKind.PhcUserTsqlFunction
            && string.Equals(source, "sys.objects", StringComparison.Ordinal);

        var metadata = NewObject();
        var fingerprint = NewObject();

        if (record.Kind == ObjectKind.PhcUserTsqlFunction)
        {
            metadata["sourceVariant"] = source!;
            if (isInstalledFunction)
            {
                var schemaComplete = Copy(root, "schema", metadata)
                    && Copy(root, "schema", fingerprint);
                return CompleteOrDiagnostic(metadata, fingerprint, schemaComplete);
            }
        }

        var complete = true;
        foreach (var field in spec.MetadataFields)
        {
            complete &= Copy(root, field, metadata);
        }

        if (spec.ActivityField is not null)
        {
            if (Copy(root, spec.ActivityField, metadata, "inactivoRaw"))
            {
                if (TryNormalizeActivity(root, spec.ActivityField, out var inactive))
                {
                    fingerprint["inactivo"] = inactive;
                }
                else
                {
                    complete = false;
                    return MetadataProjectionResolution.Incomplete(
                        ToJson(metadata),
                        "metadata_activity_unrecognized",
                        $"Activity value '{spec.ActivityField}' is missing or not recognized; no boolean fingerprint value was inferred.");
                }
            }
            else
            {
                complete = false;
            }
        }

        foreach (var field in spec.FingerprintFields)
        {
            complete &= Copy(root, field, fingerprint);
        }

        if (record.Kind == ObjectKind.PhcUserField)
        {
            complete &= IsNumber(root, "maxLength")
                && IsNumber(root, "precision")
                && IsNumber(root, "scale")
                && IsBoolean(root, "isNullable");
        }

        return CompleteOrDiagnostic(metadata, fingerprint, complete);
    }

    private static MetadataProjectionResolution CompleteOrDiagnostic(
        SortedDictionary<string, object?> metadata,
        SortedDictionary<string, object?> fingerprint,
        bool complete) =>
        complete && fingerprint.Count > 0
            ? MetadataProjectionResolution.Success(ToJson(metadata), ToJson(fingerprint))
            : MetadataProjectionResolution.Incomplete(
                ToJson(metadata),
                "metadata_fingerprint_incomplete",
                "The semantic metadata projection is incomplete; metadata is preserved but no fingerprint input is emitted.");

    private static bool TryProjectColumns(
        JsonElement root,
        string propertyName,
        IReadOnlyList<string> fields,
        out List<SortedDictionary<string, object?>> columns)
    {
        columns = [];
        if (!TryGetProperty(root, propertyName, out var array)
            || array.ValueKind != JsonValueKind.Array
            || array.GetArrayLength() == 0)
        {
            return false;
        }

        var previousOrdinal = 0;
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var projected = NewObject();
            foreach (var field in fields)
            {
                if (!Copy(item, field, projected))
                {
                    return false;
                }
            }

            if (!TryGetInt32(item, "ordinal", out var ordinal)
                || ordinal <= previousOrdinal)
            {
                return false;
            }

            previousOrdinal = ordinal;
            columns.Add(projected);
        }

        return true;
    }

    private static bool TryNormalizeActivity(JsonElement root, string propertyName, out bool inactive)
    {
        inactive = false;
        if (!TryGetProperty(root, propertyName, out var value))
        {
            return false;
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.True:
                inactive = true;
                return true;
            case JsonValueKind.False:
                inactive = false;
                return true;
            case JsonValueKind.Number:
                if (value.TryGetDecimal(out var number))
                {
                    inactive = number != 0;
                    return true;
                }

                return false;
            case JsonValueKind.String:
                var text = value.GetString()?.Trim();
                if (text is null)
                {
                    return false;
                }

                if (text.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || text.Equals(".t.", StringComparison.OrdinalIgnoreCase)
                    || text.Equals("1", StringComparison.Ordinal)
                    || text.Equals("yes", StringComparison.OrdinalIgnoreCase)
                    || text.Equals("sim", StringComparison.OrdinalIgnoreCase))
                {
                    inactive = true;
                    return true;
                }

                if (text.Equals("false", StringComparison.OrdinalIgnoreCase)
                    || text.Equals(".f.", StringComparison.OrdinalIgnoreCase)
                    || text.Equals("0", StringComparison.Ordinal)
                    || text.Equals("no", StringComparison.OrdinalIgnoreCase)
                    || text.Equals("nao", StringComparison.OrdinalIgnoreCase)
                    || text.Equals("não", StringComparison.OrdinalIgnoreCase))
                {
                    inactive = false;
                    return true;
                }

                return false;
            default:
                return false;
        }
    }

    private static bool Copy(
        JsonElement source,
        string sourceName,
        SortedDictionary<string, object?> destination,
        string? destinationName = null)
    {
        if (!TryGetProperty(source, sourceName, out var value))
        {
            return false;
        }

        destination[destinationName ?? sourceName] = value.Clone();
        return true;
    }

    private static bool IsString(JsonElement root, string name) =>
        TryGetProperty(root, name, out var value)
        && value.ValueKind == JsonValueKind.String;

    private static bool IsBoolean(JsonElement root, string name) =>
        TryGetProperty(root, name, out var value)
        && value.ValueKind is JsonValueKind.True or JsonValueKind.False;

    private static bool IsNumber(JsonElement root, string name) =>
        TryGetProperty(root, name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetDecimal(out _);

    private static bool TryGetInt32(JsonElement root, string name, out int value)
    {
        value = default;
        return TryGetProperty(root, name, out var element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out value);
    }

    private static string? ReadString(JsonElement root, string name) =>
        TryGetProperty(root, name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool TryGetProperty(JsonElement root, string name, out JsonElement value)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var found))
        {
            value = found;
            return true;
        }

        value = default;
        return false;
    }

    private static SortedDictionary<string, object?> NewObject() =>
        new(StringComparer.Ordinal);

    private static JsonElement EmptyObject() =>
        JsonSerializer.SerializeToElement(NewObject(), JsonOptions);

    private static JsonElement ToJson(SortedDictionary<string, object?> value) =>
        JsonSerializer.SerializeToElement(value, JsonOptions);

    private sealed record PhcMetadataSpec(
        IReadOnlyList<string> MetadataFields,
        string? ActivityField,
        IReadOnlyList<string> FingerprintFields);
}

/// <summary>Descriptive metadata and the distinct semantic projection used by metadata fingerprinting.</summary>
public sealed record MetadataProjectionResolution(
    JsonElement Metadata,
    JsonElement? FingerprintProjection,
    string? DiagnosticCode,
    string? Message)
{
    public bool HasFingerprintProjection => FingerprintProjection.HasValue;

    public static MetadataProjectionResolution Success(JsonElement metadata, JsonElement? fingerprintProjection) =>
        new(metadata, fingerprintProjection, null, null);

    public static MetadataProjectionResolution Incomplete(JsonElement metadata, string code, string message) =>
        new(metadata, null, code, message);

    public static MetadataProjectionResolution Failure(string code, string message) =>
        new(JsonSerializer.SerializeToElement(new Dictionary<string, object?>()), null, code, message);
}
