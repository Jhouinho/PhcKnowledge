namespace PhcKnowledgeScanner.Abstractions.Inventory;

/// <summary>
/// Relaciona factos <c>rel</c> e <c>dtl</c> sem eliminar registos de origem.
/// </summary>
public static class DeclaredRelationCorrespondence
{
    public static IReadOnlyList<DeclaredRelationRecord> Apply(
        IReadOnlyList<DeclaredRelationRecord> records)
    {
        var list = records.ToList();
        var rel = list.Where(r => string.Equals(r.SourceType, DeclaredRelationSourceTypes.PhcRel, StringComparison.Ordinal))
            .ToList();
        var dtl = list.Where(r => string.Equals(r.SourceType, DeclaredRelationSourceTypes.PhcDtl, StringComparison.Ordinal))
            .ToList();

        var usedDtl = new HashSet<string>(StringComparer.Ordinal);
        var updates = new Dictionary<string, (DeclaredRelationCorrespondenceKind Kind, string GroupId)>(StringComparer.Ordinal);

        foreach (var r in rel)
        {
            DeclaredRelationRecord? exact = null;
            DeclaredRelationRecord? probable = null;
            foreach (var d in dtl)
            {
                if (usedDtl.Contains(d.RelationId))
                {
                    continue;
                }

                if (!TablesEqual(r, d))
                {
                    continue;
                }

                if (ExpressionsEqual(r.OriginalExpression, d.OriginalExpression))
                {
                    exact = d;
                    break;
                }

                probable ??= d;
            }

            if (exact is not null)
            {
                var groupId = $"corr:exact:{NormalizeTable(r.FromTable)}:{NormalizeTable(r.ToTable)}:{HashExpr(r.OriginalExpression)}";
                updates[r.RelationId] = (DeclaredRelationCorrespondenceKind.Exact, groupId);
                updates[exact.RelationId] = (DeclaredRelationCorrespondenceKind.Exact, groupId);
                usedDtl.Add(exact.RelationId);
            }
            else if (probable is not null)
            {
                var groupId = $"corr:probable:{NormalizeTable(r.FromTable)}:{NormalizeTable(r.ToTable)}:{r.SourceRecordId ?? r.RelationId}";
                updates[r.RelationId] = (DeclaredRelationCorrespondenceKind.Probable, groupId);
                updates[probable.RelationId] = (DeclaredRelationCorrespondenceKind.Probable, groupId);
                usedDtl.Add(probable.RelationId);
            }
        }

        return list.Select(rec =>
        {
            if (!updates.TryGetValue(rec.RelationId, out var u))
            {
                return rec with
                {
                    CorrespondenceKind = DeclaredRelationCorrespondenceKind.None,
                    CorrespondenceGroupId = null
                };
            }

            return rec with
            {
                CorrespondenceKind = u.Kind,
                CorrespondenceGroupId = u.GroupId
            };
        }).ToList();
    }

    private static bool TablesEqual(DeclaredRelationRecord a, DeclaredRelationRecord b) =>
        string.Equals(NormalizeTable(a.FromTable), NormalizeTable(b.FromTable), StringComparison.OrdinalIgnoreCase)
        && string.Equals(NormalizeTable(a.ToTable), NormalizeTable(b.ToTable), StringComparison.OrdinalIgnoreCase);

    private static bool ExpressionsEqual(string a, string b) =>
        string.Equals(NormalizeExpression(a), NormalizeExpression(b), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeTable(string table) =>
        table.Trim().Replace("dbo.", "", StringComparison.OrdinalIgnoreCase).Trim();

    private static string NormalizeExpression(string expression) =>
        string.Join(' ', expression.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string HashExpr(string expression)
    {
        var n = NormalizeExpression(expression).ToLowerInvariant();
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(n));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}
