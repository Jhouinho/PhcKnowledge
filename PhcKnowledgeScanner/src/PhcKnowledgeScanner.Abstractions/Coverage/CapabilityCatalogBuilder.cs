namespace PhcKnowledgeScanner.Abstractions.Coverage;

/// <summary>
/// Agrega declarações de capability num catálogo estável.
/// Duplicados rigorosamente idênticos (categoria, operationId, versão, collector, suporte)
/// são colapsados para a primeira ocorrência. Declarações incompatíveis são rejeitadas.
/// </summary>
public static class CapabilityCatalogBuilder
{
    public static IReadOnlyList<DeclaredCapability> Build(IEnumerable<DeclaredCapability> declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);

        var ordered = new List<DeclaredCapability>();
        var byOperation = new Dictionary<string, DeclaredCapability>(StringComparer.OrdinalIgnoreCase);

        foreach (var declaration in declarations)
        {
            if (string.IsNullOrWhiteSpace(declaration.OperationId))
            {
                throw new InvalidOperationException(
                    "Declaração de capability inválida: operationId vazio.");
            }

            if (!byOperation.TryGetValue(declaration.OperationId, out var existing))
            {
                byOperation[declaration.OperationId] = declaration;
                ordered.Add(declaration);
                continue;
            }

            if (AreSemanticallyIdentical(existing, declaration))
            {
                // Regra explícita: duplicados idênticos são intencionais e colapsam na primeira ocorrência.
                continue;
            }

            throw new InvalidOperationException(
                "Conflito no catálogo de capabilities para operationId '"
                + declaration.OperationId
                + "': declarações incompatíveis. "
                + Format(existing)
                + " vs "
                + Format(declaration)
                + ".");
        }

        return ordered;
    }

    /// <summary>
    /// Identidade semântica usada para colapsar duplicados: categoria, operationId, versão,
    /// collector e suporte. A descrição não participa (pode variar sem alterar a capability).
    /// </summary>
    public static bool AreSemanticallyIdentical(DeclaredCapability left, DeclaredCapability right)
        => string.Equals(left.OperationId, right.OperationId, StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.CategoryId, right.CategoryId, StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.CapabilityVersion, right.CapabilityVersion, StringComparison.Ordinal)
           && string.Equals(left.CollectorId, right.CollectorId, StringComparison.OrdinalIgnoreCase)
           && left.Supported == right.Supported;

    private static string Format(DeclaredCapability capability)
        => $"(category={capability.CategoryId}, version={capability.CapabilityVersion}, "
           + $"collector={capability.CollectorId}, supported={capability.Supported})";
}
