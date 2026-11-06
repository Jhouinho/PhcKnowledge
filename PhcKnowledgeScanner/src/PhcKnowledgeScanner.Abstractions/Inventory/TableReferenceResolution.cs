namespace PhcKnowledgeScanner.Abstractions.Inventory;

/// <summary>Estado da associação de um nome de tabela PHC a um objecto <c>table:*</c> do workspace.</summary>
public enum TableReferenceResolution
{
    /// <summary>Nenhuma tabela com esse nome no inventário.</summary>
    Unresolved = 0,

    /// <summary>Correspondência inequívoca a um objecto técnico.</summary>
    Resolved = 1,

    /// <summary>Várias tabelas com o mesmo nome em schemas diferentes — não se escolhe arbitrariamente.</summary>
    Ambiguous = 2
}
