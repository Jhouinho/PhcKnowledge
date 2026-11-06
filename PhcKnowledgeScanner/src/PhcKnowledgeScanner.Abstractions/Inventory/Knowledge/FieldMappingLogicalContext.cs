namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>
/// Contexto lógico do predicado. OR tem precedência: qualquer ancestral OR ⇒ alternativa.
/// </summary>
public enum FieldMappingLogicalContext
{
    /// <summary>Sob AND (sem ancestral OR), ou predicado único na raiz da expressão.</summary>
    RequiredUnderAnd = 0,

    /// <summary>Sob OR (em qualquer nível) — nunca tratar como ligação obrigatória.</summary>
    AlternativeUnderOr = 1,

    /// <summary>Contexto indeterminado com segurança.</summary>
    Unknown = 2
}
