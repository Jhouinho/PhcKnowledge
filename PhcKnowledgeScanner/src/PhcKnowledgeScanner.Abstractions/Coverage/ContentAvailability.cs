namespace PhcKnowledgeScanner.Abstractions.Coverage;

/// <summary>Disponibilidade do conteúdo recolhido (IKP coverage.contentAvailability).</summary>
public enum ContentAvailability
{
    Available = 0,
    Partial = 1,
    Unavailable = 2,
    NotCollected = 3,
    Empty = 4
}
