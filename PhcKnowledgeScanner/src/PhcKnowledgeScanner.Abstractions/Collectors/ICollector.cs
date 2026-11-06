namespace PhcKnowledgeScanner.Abstractions.Collectors;

/// <summary>
/// Contrato de um módulo de recolha. Cada collector é responsável por uma categoria
/// de informação técnica e deve reportar sucesso parcial, falhas e omissões.
/// </summary>
public interface ICollector
{
    /// <summary>Identificador estável (ex.: "sql.schema", "phc.dictionary").</summary>
    string Id { get; }

    /// <summary>Nome legível para CLI e manifests.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Indica se o collector pode executar no contexto atual (versão, permissões, objetos disponíveis).
    /// Não deve alterar a instalação.
    /// </summary>
    Task<CollectorCapability> ProbeAsync(ScanContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Executa a recolha apenas de leitura e produz artefactos preservando conteúdo original.
    /// </summary>
    Task<CollectorResult> CollectAsync(ScanContext context, CancellationToken cancellationToken);
}
