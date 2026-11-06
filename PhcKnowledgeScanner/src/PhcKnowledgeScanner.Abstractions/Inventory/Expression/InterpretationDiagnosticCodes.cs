namespace PhcKnowledgeScanner.Abstractions.Inventory.Expression;

/// <summary>Códigos estáveis de diagnóstico de resolução (V1.1+).</summary>
public static class InterpretationDiagnosticCodes
{
    public const string Resolved = "Resolved";
    public const string MissingTable = "MissingTable";
    public const string NameMismatch = "NameMismatch";
    public const string Ambiguous = "Ambiguous";
    public const string UnqualifiedField = "UnqualifiedField";
    public const string ParseNote = "ParseNote";
}
