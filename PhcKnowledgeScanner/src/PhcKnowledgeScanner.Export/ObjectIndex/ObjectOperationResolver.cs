using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>
/// Resolve a technical object's observing operation from the current Scanner emitter contract.
/// This is deliberately explicit: unknown kinds or PHC source variants are never guessed.
/// </summary>
public static class ObjectOperationResolver
{
    private const string SqlCollectorId = "sql.server.structure";
    private const string PhcCollectorId = "phc.customizations";

    private static readonly IReadOnlyDictionary<string, string> SqlOperations =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ObjectKind.Database] = KnownOperations.SqlDatabase,
            [ObjectKind.Table] = KnownOperations.SqlTablesInventory,
            [ObjectKind.PrimaryKey] = KnownOperations.SqlPrimaryKeys,
            [ObjectKind.ForeignKey] = KnownOperations.SqlForeignKeys,
            [ObjectKind.View] = KnownOperations.SqlViews,
            [ObjectKind.StoredProcedure] = KnownOperations.SqlProcedures,
            [ObjectKind.Trigger] = KnownOperations.SqlTriggers
        };

    private static readonly IReadOnlyDictionary<string, PhcOperationMapping> PhcOperations =
        new Dictionary<string, PhcOperationMapping>(StringComparer.Ordinal)
        {
            [ObjectKind.PhcUserRule] = new(KnownOperations.PhcBrule, ["brule"]),
            [ObjectKind.PhcWebUserRule] = new(KnownOperations.PhcEbrule, ["ebrule"]),
            [ObjectKind.PhcHotkey] = new(KnownOperations.PhcTeclas, ["ti"]),
            [ObjectKind.PhcDefaultValue] = new(KnownOperations.PhcUdefs, ["udefs"]),
            [ObjectKind.PhcWebDefaultValue] = new(KnownOperations.PhcEudefs, ["eudefs"]),
            [ObjectKind.PhcEvent] = new(KnownOperations.PhcEventos, ["eventos"]),
            [ObjectKind.PhcWebEvent] = new(KnownOperations.PhcEeventos, ["eeventos"]),
            [ObjectKind.PhcUserField] = new(KnownOperations.PhcUserFields, ["sys.columns"]),
            [ObjectKind.PhcWebScript] = new(KnownOperations.PhcEscr, ["escr"]),
            [ObjectKind.PhcUserJavascript] = new(KnownOperations.PhcJsu, ["jsu"]),
            [ObjectKind.PhcWebCssJs] = new(KnownOperations.PhcCssjs, ["cssjs"]),
            [ObjectKind.PhcHiddenGrid] = new(KnownOperations.PhcFormc, ["formc"]),
            [ObjectKind.PhcCustomScreen] = new(KnownOperations.PhcFormb, ["formb"]),
            [ObjectKind.PhcUserTsqlFunction] = new(KnownOperations.PhcFtu, ["ftu", "sys.objects"])
        };

    /// <summary>
    /// Resolves the operation that emitted <paramref name="record"/>.
    /// PHC objects require a valid SummaryJson.source discriminator.
    /// </summary>
    public static ObjectOperationResolution Resolve(TechnicalObjectRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (SqlOperations.TryGetValue(record.Kind, out var sqlOperation))
        {
            if (!string.Equals(record.CollectorId, SqlCollectorId, StringComparison.Ordinal))
            {
                return ObjectOperationResolution.Failure(
                    "object_collector_mismatch",
                    $"Object kind '{record.Kind}' is not emitted by collector '{record.CollectorId}'.");
            }

            return ObjectOperationResolution.Success(sqlOperation);
        }

        if (!PhcOperations.TryGetValue(record.Kind, out var phcMapping))
        {
            return ObjectOperationResolution.Failure(
                "object_kind_unsupported",
                $"No observing operation is mapped for object kind '{record.Kind}'.");
        }

        if (!string.Equals(record.CollectorId, PhcCollectorId, StringComparison.Ordinal))
        {
            return ObjectOperationResolution.Failure(
                "object_collector_mismatch",
                $"Object kind '{record.Kind}' is not emitted by collector '{record.CollectorId}'.");
        }

        var sourceResult = ReadSourceVariant(record.SummaryJson);
        if (!sourceResult.IsSuccess)
        {
            return ObjectOperationResolution.Failure(sourceResult.DiagnosticCode!, sourceResult.Message!);
        }

        if (!phcMapping.ExpectedSources.Contains(sourceResult.Source!, StringComparer.Ordinal))
        {
            return ObjectOperationResolution.Failure(
                "object_source_variant_unsupported",
                $"Source variant '{sourceResult.Source}' is not valid for object kind '{record.Kind}'.");
        }

        return ObjectOperationResolution.Success(phcMapping.OperationId);
    }

    private static SourceVariantResult ReadSourceVariant(string? summaryJson)
    {
        if (string.IsNullOrWhiteSpace(summaryJson))
        {
            return SourceVariantResult.Failure(
                "object_summary_missing",
                "PHC object has no SummaryJson; its observing operation cannot be resolved safely.");
        }

        try
        {
            using var document = JsonDocument.Parse(summaryJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return SourceVariantResult.Failure(
                    "object_summary_invalid",
                    "PHC object SummaryJson must be a JSON object.");
            }

            if (!document.RootElement.TryGetProperty("source", out var source)
                || source.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(source.GetString()))
            {
                return SourceVariantResult.Failure(
                    "object_source_missing",
                    "PHC object SummaryJson must contain a non-empty string property 'source'.");
            }

            return SourceVariantResult.Success(source.GetString()!);
        }
        catch (JsonException)
        {
            return SourceVariantResult.Failure(
                "object_summary_invalid",
                "PHC object SummaryJson is not valid JSON.");
        }
    }

    private sealed record PhcOperationMapping(
        string OperationId,
        IReadOnlyList<string> ExpectedSources);

    private sealed record SourceVariantResult(
        bool IsSuccess,
        string? Source,
        string? DiagnosticCode,
        string? Message)
    {
        public static SourceVariantResult Success(string source) =>
            new(true, source, null, null);

        public static SourceVariantResult Failure(string code, string message) =>
            new(false, null, code, message);
    }
}

/// <summary>Result of resolving an object's observing operation.</summary>
public sealed record ObjectOperationResolution(
    string? OperationId,
    string? DiagnosticCode,
    string? Message)
{
    public bool IsResolved => !string.IsNullOrWhiteSpace(OperationId);

    public static ObjectOperationResolution Success(string operationId) =>
        new(operationId, null, null);

    public static ObjectOperationResolution Failure(string diagnosticCode, string message) =>
        new(null, diagnosticCode, message);
}
