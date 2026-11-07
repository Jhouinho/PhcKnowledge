using System.Globalization;
using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>
/// Projects native source identifiers already present in a Scanner capture.
/// It never manufactures an identity from names, hashes, or synthetic object IDs.
/// </summary>
public static class SourceIdentityResolver
{
    private const string SqlCollectorId = "sql.server.structure";
    private const string PhcCollectorId = "phc.customizations";

    private static readonly IReadOnlyDictionary<string, PhcStampMapping> PhcStampMappings =
        new Dictionary<string, PhcStampMapping>(StringComparer.Ordinal)
        {
            [ObjectKind.PhcUserRule] = new("brule", "phc.table:dbo.brule"),
            [ObjectKind.PhcWebUserRule] = new("ebrule", "phc.table:dbo.ebrule"),
            [ObjectKind.PhcHotkey] = new("ti", "phc.table:dbo.ti"),
            [ObjectKind.PhcDefaultValue] = new("udefs", "phc.table:dbo.udefs"),
            [ObjectKind.PhcWebDefaultValue] = new("eudefs", "phc.table:dbo.eudefs"),
            [ObjectKind.PhcEvent] = new("eventos", "phc.table:dbo.eventos"),
            [ObjectKind.PhcWebEvent] = new("eeventos", "phc.table:dbo.eeventos"),
            [ObjectKind.PhcWebScript] = new("escr", "phc.table:dbo.escr"),
            [ObjectKind.PhcUserJavascript] = new("jsu", "phc.table:dbo.jsu"),
            [ObjectKind.PhcWebCssJs] = new("cssjs", "phc.table:dbo.cssjs"),
            [ObjectKind.PhcHiddenGrid] = new("formc", "phc.table:dbo.formc"),
            [ObjectKind.PhcCustomScreen] = new("formb", "phc.table:dbo.formb"),
            [ObjectKind.PhcUserTsqlFunction] = new("ftu", "phc.table:dbo.ftu")
        };

    /// <summary>
    /// Resolves identities for a complete capture inventory so SQL table/column scopes
    /// can be grounded in the database and parent table records from that same capture.
    /// Results are keyed by the unchanged TechnicalObjectRecord.ObjectId.
    /// </summary>
    public static IReadOnlyDictionary<string, SourceIdentityResolution> ResolveAll(
        IEnumerable<TechnicalObjectRecord> sourceObjects)
    {
        ArgumentNullException.ThrowIfNull(sourceObjects);

        var objects = sourceObjects.ToArray();
        var groups = objects
            .GroupBy(x => x.ObjectId, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);

        var databaseObjects = objects
            .Where(x => x.Kind == ObjectKind.Database
                && x.CollectorId == SqlCollectorId
                && !string.IsNullOrWhiteSpace(x.Name))
            .ToArray();

        var databaseName = databaseObjects.Length == 1 ? databaseObjects[0].Name : null;
        (string Code, string Message)? databaseDiagnostic = databaseObjects.Length switch
        {
            0 => ("source_identity_database_missing", "The capture has no unique SQL database object to establish the identity scope."),
            > 1 => ("source_identity_database_ambiguous", "The capture contains more than one SQL database object; the identity scope is ambiguous."),
            _ => null
        };

        var results = new Dictionary<string, SourceIdentityResolution>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            if (group.Value.Length != 1)
            {
                results[group.Key] = SourceIdentityResolution.Failure(
                    "source_identity_object_id_duplicate",
                    "The capture contains more than one object with the same SourceObjectId.");
                continue;
            }

            var record = group.Value[0];
            results[group.Key] = ResolveOne(record, groups, databaseName, databaseDiagnostic);
        }

        return results;
    }

    private static SourceIdentityResolution ResolveOne(
        TechnicalObjectRecord record,
        IReadOnlyDictionary<string, TechnicalObjectRecord[]> objectsById,
        string? databaseName,
        (string Code, string Message)? databaseDiagnostic)
    {
        if (record.Kind == ObjectKind.Table)
        {
            if (record.CollectorId != SqlCollectorId)
            {
                return SourceIdentityResolution.Failure(
                    "source_identity_collector_mismatch",
                    "SQL table identity was requested for an object emitted by a different collector.");
            }

            if (databaseName is null)
            {
                return SourceIdentityResolution.Failure(
                    databaseDiagnostic!.Value.Code,
                    databaseDiagnostic.Value.Message);
            }

            if (!TryReadInt64(record.SummaryJson, "objectId", out var objectId))
            {
                return SourceIdentityResolution.Failure(
                    "source_identity_sql_object_id_missing",
                    "SQL table SummaryJson does not contain a valid numeric objectId.");
            }

            return SourceIdentityResolution.Success(
                new SourceIdentityValue(
                    "sqlserver.object_id",
                    objectId.ToString(CultureInfo.InvariantCulture),
                    $"database:{databaseName}"));
        }

        if (record.Kind == ObjectKind.PhcUserField)
        {
            if (record.CollectorId != PhcCollectorId)
            {
                return SourceIdentityResolution.Failure(
                    "source_identity_collector_mismatch",
                    "PHC user-field identity was requested for an object emitted by a different collector.");
            }

            var operation = ObjectOperationResolver.Resolve(record);
            if (!operation.IsResolved)
            {
                return SourceIdentityResolution.Failure(operation.DiagnosticCode!, operation.Message!);
            }

            if (!TryReadInt64(record.SummaryJson, "columnId", out var columnId))
            {
                return SourceIdentityResolution.Failure(
                    "source_identity_sql_column_id_missing",
                    "PHC user-field SummaryJson does not contain a valid numeric columnId.");
            }

            if (databaseName is null)
            {
                return SourceIdentityResolution.Failure(
                    databaseDiagnostic!.Value.Code,
                    databaseDiagnostic.Value.Message);
            }

            if (string.IsNullOrWhiteSpace(record.ParentObjectId)
                || !objectsById.TryGetValue(record.ParentObjectId, out var parents)
                || parents.Length != 1
                || parents[0].Kind != ObjectKind.Table
                || parents[0].CollectorId != SqlCollectorId)
            {
                return SourceIdentityResolution.Failure(
                    "source_identity_parent_table_unresolved",
                    "PHC user-field parent does not resolve uniquely to a SQL table object in this capture.");
            }

            if (!TryReadInt64(parents[0].SummaryJson, "objectId", out var parentObjectId))
            {
                return SourceIdentityResolution.Failure(
                    "source_identity_parent_object_id_missing",
                    "Resolved parent table does not contain a valid numeric objectId.");
            }

            return SourceIdentityResolution.Success(
                new SourceIdentityValue(
                    "sqlserver.column_id",
                    columnId.ToString(CultureInfo.InvariantCulture),
                    $"database:{databaseName}/object:{parentObjectId.ToString(CultureInfo.InvariantCulture)}"));
        }

        if (PhcStampMappings.TryGetValue(record.Kind, out var stampMapping))
        {
            if (record.CollectorId != PhcCollectorId)
            {
                return SourceIdentityResolution.Failure(
                    "source_identity_collector_mismatch",
                    "PHC stamp identity was requested for an object emitted by a different collector.");
            }

            var operation = ObjectOperationResolver.Resolve(record);
            if (!operation.IsResolved)
            {
                return SourceIdentityResolution.Failure(operation.DiagnosticCode!, operation.Message!);
            }

            if (record.Kind == ObjectKind.PhcUserTsqlFunction
                && string.Equals(ReadSource(record.SummaryJson), "sys.objects", StringComparison.Ordinal))
            {
                // Installed functions have no native identity in the current SELECT:
                // schema + name are descriptive, not a native object identifier.
                return SourceIdentityResolution.None();
            }

            if (!string.Equals(ReadSource(record.SummaryJson), stampMapping.ExpectedSource, StringComparison.Ordinal))
            {
                return SourceIdentityResolution.Failure(
                    "source_identity_source_mismatch",
                    $"PHC object source does not match the expected stamp table '{stampMapping.ExpectedSource}'.");
            }

            if (!TryReadString(record.SummaryJson, "stamp", out var stamp))
            {
                return SourceIdentityResolution.Failure(
                    "source_identity_stamp_missing",
                    "PHC object SummaryJson does not contain a non-empty string stamp.");
            }

            return SourceIdentityResolution.Success(
                new SourceIdentityValue("phc.stamp", stamp, stampMapping.Scope));
        }

        // sys.objects functions, SQL constraints/modules and database objects have no
        // native identity with a sufficiently grounded scope in the current capture model.
        return SourceIdentityResolution.None();
    }

    private static bool TryReadInt64(string? json, string propertyName, out long value)
    {
        value = default;
        if (!TryGetProperty(json, propertyName, out var element)
            || element.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        return element.TryGetInt64(out value);
    }

    private static bool TryReadString(string? json, string propertyName, out string value)
    {
        value = string.Empty;
        if (!TryGetProperty(json, propertyName, out var element)
            || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var candidate = element.GetString();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        value = candidate;
        return true;
    }

    private static string? ReadSource(string? json) =>
        TryReadString(json, "source", out var source) ? source : null;

    private static bool TryGetProperty(string? json, string propertyName, out JsonElement element)
    {
        element = default;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(propertyName, out var found))
            {
                return false;
            }

            // Clone because the owning JsonDocument is disposed on return.
            element = found.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record PhcStampMapping(string ExpectedSource, string Scope);
}

/// <summary>Outcome of projecting an optional native SourceIdentity.</summary>
public sealed record SourceIdentityResolution(
    SourceIdentityValue? Identity,
    string? DiagnosticCode,
    string? Message)
{
    public bool HasIdentity => Identity is not null;

    public static SourceIdentityResolution Success(SourceIdentityValue identity) =>
        new(identity, null, null);

    public static SourceIdentityResolution None() =>
        new(null, null, null);

    public static SourceIdentityResolution Failure(string diagnosticCode, string message) =>
        new(null, diagnosticCode, message);
}

/// <summary>Native source identifier and the scope required to interpret it.</summary>
public sealed record SourceIdentityValue(string Scheme, string Value, string Scope);
