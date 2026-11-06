namespace PhcKnowledgeScanner.Abstractions.Inventory;

/// <summary>
/// Classificação conservadora de tabelas físicas. Não afirma origem PHC standard
/// apenas por ausência do prefixo <c>u_</c>.
/// </summary>
public static class TableInventoryClassification
{
    public const string SqlSystem = "sql_system";
    public const string CandidateCustom = "candidate_custom";
    public const string ApplicationOrUnknown = "application_or_unknown";

    public static string Classify(bool isMsShipped, string tableName)
    {
        if (isMsShipped)
        {
            return SqlSystem;
        }

        if (!string.IsNullOrEmpty(tableName)
            && tableName.StartsWith("u_", StringComparison.OrdinalIgnoreCase))
        {
            return CandidateCustom;
        }

        return ApplicationOrUnknown;
    }

    public static string BuildObjectId(string schema, string tableName) =>
        $"table:{schema}.{tableName}";

    public static bool IsCandidateCustomName(string tableName) =>
        !string.IsNullOrEmpty(tableName)
        && tableName.StartsWith("u_", StringComparison.OrdinalIgnoreCase);
}
