namespace PhcKnowledgeScanner.Collectors.Phc;

/// <summary>Normaliza flags PHC <c>inactivo</c>/<c>inativo</c> para rótulos Activo/Inactivo.</summary>
public static class PhcActivityState
{
    public static bool IsInactive(object? value)
    {
        if (value is null or DBNull)
        {
            return false;
        }

        return value switch
        {
            bool b => b,
            byte or short or int or long or float or double or decimal => Convert.ToDecimal(value) != 0,
            string s => ParseInactiveString(s),
            _ => ParseInactiveString(Convert.ToString(value))
        };
    }

    public static string ToEstado(object? value) => IsInactive(value) ? "Inactivo" : "Activo";

    public static string WithInactiveSuffix(string name, object? value)
    {
        if (!IsInactive(value) || string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (name.Contains("[Inactivo]", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Inactivo", StringComparison.OrdinalIgnoreCase))
        {
            return name;
        }

        return name.TrimEnd() + " [Inactivo]";
    }

    public static string? TryReadEstadoFromSummaryJson(string? summaryJson)
    {
        if (string.IsNullOrWhiteSpace(summaryJson))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(summaryJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("estado", out var estado) && estado.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var s = estado.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    return s;
                }
            }

            if (root.TryGetProperty("inactivo", out var inactivo))
            {
                return ToEstado(JsonValueToObject(inactivo));
            }

            if (root.TryGetProperty("inativo", out var inativo))
            {
                return ToEstado(JsonValueToObject(inativo));
            }
        }
        catch
        {
            // metadados opcionais
        }

        return null;
    }

    private static object? JsonValueToObject(System.Text.Json.JsonElement el) => el.ValueKind switch
    {
        System.Text.Json.JsonValueKind.True => true,
        System.Text.Json.JsonValueKind.False => false,
        System.Text.Json.JsonValueKind.Number => el.TryGetInt64(out var n) ? n : el.GetDouble(),
        System.Text.Json.JsonValueKind.String => el.GetString(),
        _ => el.ToString()
    };

    private static bool ParseInactiveString(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        s = s.Trim();
        if (s.Equals("true", StringComparison.OrdinalIgnoreCase)
            || s.Equals(".t.", StringComparison.OrdinalIgnoreCase)
            || s.Equals("1", StringComparison.Ordinal)
            || s.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || s.Equals("sim", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (s.Equals("false", StringComparison.OrdinalIgnoreCase)
            || s.Equals(".f.", StringComparison.OrdinalIgnoreCase)
            || s.Equals("0", StringComparison.Ordinal)
            || s.Equals("no", StringComparison.OrdinalIgnoreCase)
            || s.Equals("nao", StringComparison.OrdinalIgnoreCase)
            || s.Equals("não", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return false;
    }
}
