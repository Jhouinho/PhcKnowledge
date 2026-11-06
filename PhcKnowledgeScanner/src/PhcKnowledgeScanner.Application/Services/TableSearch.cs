using PhcKnowledgeScanner.Abstractions.Inventory;

namespace PhcKnowledgeScanner.Application.Services;

/// <summary>
/// Pesquisa em memória de tabelas com prioridade:
/// exact name → prefix name → contains name → schema/objectId.
/// </summary>
public static class TableSearch
{
    public static IReadOnlyList<TechnicalObjectRecord> FilterAndRank(
        IReadOnlyList<TechnicalObjectRecord> tables,
        string? search)
    {
        ArgumentNullException.ThrowIfNull(tables);
        if (string.IsNullOrWhiteSpace(search))
        {
            return tables
                .OrderBy(t => t.SchemaName ?? "", StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var term = search.Trim();
        return tables
            .Select(t => (Table: t, Rank: Rank(t, term)))
            .Where(x => x.Rank > 0)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Table.SchemaName ?? "", StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Table.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Table)
            .ToList();
    }

    /// <summary>
    /// 1 exact, 2 prefix, 3 name contains, 4 schema/objectId; 0 = sem match.
    /// </summary>
    public static int Rank(TechnicalObjectRecord table, string term)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (string.IsNullOrWhiteSpace(term))
        {
            return 0;
        }

        var t = term.Trim();
        if (table.Name.Equals(t, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (table.Name.StartsWith(t, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (table.Name.Contains(t, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if ((table.SchemaName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false)
            || table.ObjectId.Contains(t, StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return 0;
    }
}
