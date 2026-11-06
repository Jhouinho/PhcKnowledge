namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>Estado agregado dos dois endpoints de uma relação de conhecimento.</summary>
public enum EndpointResolutionState
{
    /// <summary>Ambos os lados com resolução inequívoca a <c>table:*</c>.</summary>
    BothResolved = 0,

    /// <summary>Exactamente um lado Resolved; o outro Unresolved/Ambiguous.</summary>
    Partial = 1,

    /// <summary>Nenhum lado Resolved.</summary>
    Unresolved = 2
}
