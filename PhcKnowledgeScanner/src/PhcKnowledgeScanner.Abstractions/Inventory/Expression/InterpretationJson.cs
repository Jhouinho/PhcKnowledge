using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhcKnowledgeScanner.Abstractions.Inventory.Expression;

/// <summary>Opções JSON partilhadas para AST V1/V1.1 (enums textuais + leitura de ints V1).</summary>
public static class InterpretationJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var o = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        o.Converters.Add(new FlexibleTableResolutionConverter());
        return o;
    }

    /// <summary>Aceita enums V1 numéricos (0/1/2) e V1.1 textuais.</summary>
    private sealed class FlexibleTableResolutionConverter : JsonConverter<TableReferenceResolution>
    {
        public override TableReferenceResolution Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number
                && reader.TryGetInt32(out var n))
            {
                return n switch
                {
                    1 => TableReferenceResolution.Resolved,
                    2 => TableReferenceResolution.Ambiguous,
                    _ => TableReferenceResolution.Unresolved
                };
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (Enum.TryParse<TableReferenceResolution>(s, ignoreCase: true, out var e))
                {
                    return e;
                }

                return s?.ToLowerInvariant() switch
                {
                    "resolved" => TableReferenceResolution.Resolved,
                    "ambiguous" => TableReferenceResolution.Ambiguous,
                    _ => TableReferenceResolution.Unresolved
                };
            }

            throw new JsonException("Valor inválido para TableReferenceResolution.");
        }

        public override void Write(
            Utf8JsonWriter writer,
            TableReferenceResolution value,
            JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
