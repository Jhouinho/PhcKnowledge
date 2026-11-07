using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>
/// Canonical JSON writer for the metadata projection value set.
/// Current P1.B fingerprint projections contain strings, booleans, nulls,
/// arrays/objects and safe integers only; non-integer numbers are rejected.
/// </summary>
public static class CanonicalJsonSerializer
{
    private const long MaxSafeInteger = 9_007_199_254_740_991L;

    public static byte[] Serialize(JsonElement value)
    {
        var builder = new StringBuilder();
        WriteValue(builder, value);
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static void WriteValue(StringBuilder builder, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(builder, value);
                break;
            case JsonValueKind.Array:
                WriteArray(builder, value);
                break;
            case JsonValueKind.String:
                WriteString(builder, value.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Number:
                WriteNumber(builder, value);
                break;
            case JsonValueKind.True:
                builder.Append("true");
                break;
            case JsonValueKind.False:
                builder.Append("false");
                break;
            case JsonValueKind.Null:
                builder.Append("null");
                break;
            default:
                throw new InvalidOperationException($"Unsupported JSON value kind '{value.ValueKind}'.");
        }
    }

    private static void WriteObject(StringBuilder builder, JsonElement value)
    {
        var properties = value.EnumerateObject()
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        for (var i = 1; i < properties.Length; i++)
        {
            if (string.Equals(properties[i - 1].Name, properties[i].Name, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Canonical JSON cannot contain duplicate property '{properties[i].Name}'.");
            }
        }

        builder.Append('{');
        for (var i = 0; i < properties.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            WriteString(builder, properties[i].Name);
            builder.Append(':');
            WriteValue(builder, properties[i].Value);
        }

        builder.Append('}');
    }

    private static void WriteArray(StringBuilder builder, JsonElement value)
    {
        builder.Append('[');
        var first = true;
        foreach (var item in value.EnumerateArray())
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            WriteValue(builder, item);
        }

        builder.Append(']');
    }

    private static void WriteNumber(StringBuilder builder, JsonElement value)
    {
        if (value.TryGetInt64(out var signed))
        {
            if (signed is < -MaxSafeInteger or > MaxSafeInteger)
            {
                throw new InvalidOperationException(
                    "Metadata fingerprint numbers must be within the IEEE-754 safe integer range.");
            }

            builder.Append(signed.ToString(CultureInfo.InvariantCulture));
            return;
        }

        if (value.TryGetUInt64(out var unsigned) && unsigned <= (ulong)MaxSafeInteger)
        {
            builder.Append(unsigned.ToString(CultureInfo.InvariantCulture));
            return;
        }

        throw new InvalidOperationException(
            "Metadata fingerprint projections currently support safe integer JSON numbers only.");
    }

    private static void WriteString(StringBuilder builder, string value)
    {
        builder.Append('"');
        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                default:
                    if (character < 0x20)
                    {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else if (char.IsHighSurrogate(character))
                    {
                        if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                        {
                            throw new InvalidOperationException("Canonical JSON strings cannot contain unpaired surrogates.");
                        }

                        builder.Append(character);
                        builder.Append(value[++i]);
                    }
                    else if (char.IsLowSurrogate(character))
                    {
                        throw new InvalidOperationException("Canonical JSON strings cannot contain unpaired surrogates.");
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
    }
}
