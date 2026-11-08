using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;

namespace PhcKnowledgeScanner.Export.SchemaSnapshot;

/// <summary>Compara dois snapshots estruturais (formato estável para evolução futura de UI).</summary>
public sealed class DatabaseSchemaSnapshotComparer
{
    public SchemaSnapshotComparison Compare(DatabaseSchemaSnapshot left, DatabaseSchemaSnapshot right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var comparison = new SchemaSnapshotComparison
        {
            LeftHash = left.Manifest.SnapshotHash,
            RightHash = right.Manifest.SnapshotHash,
            LeftCollectedAtUtc = left.Manifest.CollectedAtUtc,
            RightCollectedAtUtc = right.Manifest.CollectedAtUtc
        };

        var leftTables = left.Tables.ToDictionary(TableKey, StringComparer.OrdinalIgnoreCase);
        var rightTables = right.Tables.ToDictionary(TableKey, StringComparer.OrdinalIgnoreCase);

        foreach (var key in rightTables.Keys.Except(leftTables.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
        {
            comparison.TablesAdded.Add(key);
        }

        foreach (var key in leftTables.Keys.Except(rightTables.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
        {
            comparison.TablesRemoved.Add(key);
        }

        foreach (var key in leftTables.Keys.Intersect(rightTables.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var l = leftTables[key];
            var r = rightTables[key];
            var leftCols = l.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
            var rightCols = r.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var col in rightCols.Keys.Except(leftCols.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
            {
                comparison.ColumnsAdded.Add(new SchemaFieldChange { TableKey = key, ColumnName = col });
            }

            foreach (var col in leftCols.Keys.Except(rightCols.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
            {
                comparison.ColumnsRemoved.Add(new SchemaFieldChange { TableKey = key, ColumnName = col });
            }

            foreach (var col in leftCols.Keys.Intersect(rightCols.Keys, StringComparer.OrdinalIgnoreCase))
            {
                var lc = leftCols[col];
                var rc = rightCols[col];
                CompareProp(comparison, key, col, "columnId", lc.ColumnId?.ToString(), rc.ColumnId?.ToString());
                CompareProp(comparison, key, col, "sqlType", lc.SqlType, rc.SqlType);
                CompareProp(comparison, key, col, "maxLength", lc.MaxLength?.ToString(), rc.MaxLength?.ToString());
                CompareProp(comparison, key, col, "precision", lc.Precision?.ToString(), rc.Precision?.ToString());
                CompareProp(comparison, key, col, "scale", lc.Scale?.ToString(), rc.Scale?.ToString());
                CompareProp(comparison, key, col, "isNullable", lc.IsNullable.ToString(), rc.IsNullable.ToString());
                CompareProp(comparison, key, col, "defaultDefinition", lc.DefaultDefinition, rc.DefaultDefinition);
                CompareProp(comparison, key, col, "isComputed", lc.IsComputed.ToString(), rc.IsComputed.ToString());
                CompareProp(comparison, key, col, "computedDefinition", lc.ComputedDefinition, rc.ComputedDefinition);
            }
        }

        var leftFks = left.Relationships.Select(FkKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightFks = right.Relationships.Select(FkKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        comparison.RelationshipsAdded.AddRange(rightFks.Except(leftFks, StringComparer.OrdinalIgnoreCase).OrderBy(x => x));
        comparison.RelationshipsRemoved.AddRange(leftFks.Except(rightFks, StringComparer.OrdinalIgnoreCase).OrderBy(x => x));

        var leftIdx = left.Indexes.Select(IndexKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightIdx = right.Indexes.Select(IndexKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        comparison.IndexesAdded.AddRange(rightIdx.Except(leftIdx, StringComparer.OrdinalIgnoreCase).OrderBy(x => x));
        comparison.IndexesRemoved.AddRange(leftIdx.Except(rightIdx, StringComparer.OrdinalIgnoreCase).OrderBy(x => x));

        CompareObjectSet(comparison, "view", left.Views.Select(v => $"{v.SchemaName}.{v.Name}:object_id={v.ObjectId}"), right.Views.Select(v => $"{v.SchemaName}.{v.Name}:object_id={v.ObjectId}"));
        CompareObjectSet(comparison, "procedure", left.Procedures.Select(p => $"{p.SchemaName}.{p.Name}:object_id={p.ObjectId}"), right.Procedures.Select(p => $"{p.SchemaName}.{p.Name}:object_id={p.ObjectId}"));
        CompareObjectSet(comparison, "function", left.Functions.Select(f => $"{f.SchemaName}.{f.Name}:object_id={f.ObjectId}"), right.Functions.Select(f => $"{f.SchemaName}.{f.Name}:object_id={f.ObjectId}"));
        CompareObjectSet(comparison, "trigger", left.Triggers.Select(t => $"{t.SchemaName}.{t.Name}:object_id={t.ObjectId}"), right.Triggers.Select(t => $"{t.SchemaName}.{t.Name}:object_id={t.ObjectId}"));

        return comparison;
    }

    private static void CompareObjectSet(
        SchemaSnapshotComparison comparison,
        string kind,
        IEnumerable<string> left,
        IEnumerable<string> right)
    {
        var l = left.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var r = right.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var added in r.Except(l, StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
        {
            comparison.OtherObjectChanges.Add($"+{kind}:{added}");
        }

        foreach (var removed in l.Except(r, StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
        {
            comparison.OtherObjectChanges.Add($"-{kind}:{removed}");
        }
    }

    private static void CompareProp(
        SchemaSnapshotComparison comparison,
        string tableKey,
        string column,
        string property,
        string? before,
        string? after)
    {
        if (string.Equals(before ?? "", after ?? "", StringComparison.Ordinal))
        {
            return;
        }

        comparison.ColumnPropertyChanges.Add(new SchemaFieldChange
        {
            TableKey = tableKey,
            ColumnName = column,
            Property = property,
            Before = before,
            After = after
        });
    }

    private static string TableKey(SchemaTableSnapshot t) => $"{t.SchemaName}.{t.Name}";

    private static string FkKey(SchemaForeignKeySnapshot fk) =>
        $"{fk.ParentSchema}.{fk.ParentTable}.{fk.ConstraintName}";

    private static string IndexKey(SchemaIndexSnapshot i) =>
        $"{i.SchemaName}.{i.TableName}.{i.IndexName}";
}
