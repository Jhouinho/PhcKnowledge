namespace PhcKnowledgeScanner.Abstractions.Inventory;

/// <summary>
/// Resolve referências de tabelas declaradas (rel/dtl) contra o inventário de objectos <c>table:*</c>.
/// Estratégia conservadora: só resolve quando a correspondência é inequívoca.
/// </summary>
public static class TableReferenceResolver
{
    public readonly record struct Result(string? ObjectId, TableReferenceResolution Resolution);

    /// <summary>
    /// Constrói índice nome → lista de object ids a partir dos objectos de tabela do workspace.
    /// </summary>
    public static Dictionary<string, List<string>> BuildNameIndex(
        IEnumerable<TechnicalObjectRecord> tables)
    {
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tables)
        {
            if (!string.Equals(t.Kind, ObjectKind.Table, StringComparison.Ordinal))
            {
                continue;
            }

            if (!index.TryGetValue(t.Name, out var list))
            {
                list = [];
                index[t.Name] = list;
            }

            list.Add(t.ObjectId);
        }

        return index;
    }

    /// <summary>
    /// Resolve uma referência <paramref name="tableRef"/> (ex.: <c>bo</c>, <c>dbo.bo</c>, <c>sales.orders</c>).
    /// </summary>
    public static Result Resolve(
        string? tableRef,
        IReadOnlyDictionary<string, List<string>> nameIndex,
        IReadOnlySet<string>? knownObjectIds = null)
    {
        if (string.IsNullOrWhiteSpace(tableRef))
        {
            return new Result(null, TableReferenceResolution.Unresolved);
        }

        var raw = tableRef.Trim();
        if (TrySplitSchemaAndName(raw, out var schema, out var name))
        {
            var objectId = TableInventoryClassification.BuildObjectId(schema, name);
            var exists = knownObjectIds is not null
                ? knownObjectIds.Contains(objectId)
                : nameIndex.TryGetValue(name, out var candidates)
                  && candidates.Any(id => string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));

            return exists
                ? new Result(objectId, TableReferenceResolution.Resolved)
                : new Result(null, TableReferenceResolution.Unresolved);
        }

        if (!nameIndex.TryGetValue(raw, out var matches) || matches.Count == 0)
        {
            return new Result(null, TableReferenceResolution.Unresolved);
        }

        if (matches.Count == 1)
        {
            return new Result(matches[0], TableReferenceResolution.Resolved);
        }

        return new Result(null, TableReferenceResolution.Ambiguous);
    }

    /// <summary>
    /// Tenta extrair schema e nome. Aceita <c>schema.name</c> com ou sem brackets.
    /// Nomes sem ponto são tratados como nome simples (sem schema).
    /// </summary>
    public static bool TrySplitSchemaAndName(string input, out string schema, out string name)
    {
        schema = string.Empty;
        name = string.Empty;
        var text = input.Trim();
        if (text.Length == 0)
        {
            return false;
        }

        // Remover brackets simples [schema].[name] / [name]
        text = text.Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal);

        var dot = text.IndexOf('.');
        if (dot <= 0 || dot >= text.Length - 1)
        {
            return false;
        }

        // Apenas o primeiro segmento como schema (ex.: dbo.bo); não parsear expressões.
        if (text.IndexOf('.', dot + 1) >= 0)
        {
            // Mais do que um ponto — tratar como referência não qualificada conservadora.
            return false;
        }

        schema = text[..dot].Trim();
        name = text[(dot + 1)..].Trim();
        return schema.Length > 0 && name.Length > 0;
    }
}
