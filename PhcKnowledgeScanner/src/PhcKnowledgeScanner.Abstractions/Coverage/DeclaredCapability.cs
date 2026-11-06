namespace PhcKnowledgeScanner.Abstractions.Coverage;

/// <summary>
/// Declaração de capacidade do Scanner (catálogo), independente dos resultados de uma captura.
/// Não confundir com <see cref="Collectors.CollectorCapability"/> (resultado de probe).
/// </summary>
public sealed record DeclaredCapability(
    string CategoryId,
    string OperationId,
    string CapabilityVersion,
    string CollectorId,
    bool Supported,
    string? Description = null);
