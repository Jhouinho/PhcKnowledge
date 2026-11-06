namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>Critério determinístico que originou a <see cref="KnowledgeRelation"/>.</summary>
public enum KnowledgeConsolidationBasis
{
    /// <summary>Uma única evidência de ligação (FK, REL ou DTL).</summary>
    Singleton = 0,

    /// <summary>Membros do mesmo <c>CorrespondenceKind.Exact</c> (lógica já persistida).</summary>
    DeclaredExactCorrespondence = 1
}
