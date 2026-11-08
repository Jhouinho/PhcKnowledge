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
            CompareNativeIdentity(comparison, "table", key, l.ObjectId, r.ObjectId);
            ComparePrimaryKeyNativeIdentity(comparison, key, l.PrimaryKey, r.PrimaryKey);
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
                CompareNativeIdentity(comparison, "column", $"{key}.{col}", lc.ObjectId, rc.ObjectId);
                CompareNativeIdentity(comparison, "column_id", $"{key}.{col}", lc.ColumnId, rc.ColumnId);
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

        CompareIdentitySet(comparison, "foreign_key", left.Relationships, right.Relationships,
            fk => $"{fk.ParentSchema}.{fk.ParentTable}.{fk.ConstraintName}", fk => fk.ObjectId is null ? null : (long)fk.ObjectId.Value,
            comparison.RelationshipsAdded, comparison.RelationshipsRemoved);
        CompareIdentitySet(comparison, "index", left.Indexes, right.Indexes,
            i => $"{i.SchemaName}.{i.TableName}.{i.IndexName}", i => i.ObjectId is null || i.IndexId is null
                ? null : ((long)i.ObjectId.Value << 32) | (uint)i.IndexId.Value,
            comparison.IndexesAdded, comparison.IndexesRemoved);
        CompareIdentitySet(comparison, "view", left.Views, right.Views,
            v => $"{v.SchemaName}.{v.Name}", v => v.ObjectId is null ? null : (long)v.ObjectId.Value, null, null);
        CompareIdentitySet(comparison, "procedure", left.Procedures, right.Procedures,
            p => $"{p.SchemaName}.{p.Name}", p => p.ObjectId is null ? null : (long)p.ObjectId.Value, null, null);
        CompareIdentitySet(comparison, "function", left.Functions, right.Functions,
            f => $"{f.SchemaName}.{f.Name}", f => f.ObjectId is null ? null : (long)f.ObjectId.Value, null, null);
        CompareIdentitySet(comparison, "trigger", left.Triggers, right.Triggers,
            t => $"{t.SchemaName}.{t.Name}", t => t.ObjectId is null ? null : (long)t.ObjectId.Value, null, null);

        return comparison;
    }

    private static void CompareIdentitySet<T>(
        SchemaSnapshotComparison comparison,
        string kind,
        IEnumerable<T> left,
        IEnumerable<T> right,
        Func<T, string> key,
        Func<T, long?> identity,
        List<string>? added,
        List<string>? removed)
    {
        var l = left.ToDictionary(key, StringComparer.OrdinalIgnoreCase);
        var r = right.ToDictionary(key, StringComparer.OrdinalIgnoreCase);
        foreach (var name in r.Keys.Except(l.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
        {
            var value = $"+{kind}:{name}";
            if (added is not null) added.Add(value);
            else comparison.OtherObjectChanges.Add(value);
        }
        foreach (var name in l.Keys.Except(r.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
        {
            var value = $"-{kind}:{name}";
            if (removed is not null) removed.Add(value);
            else comparison.OtherObjectChanges.Add(value);
        }
        foreach (var name in l.Keys.Intersect(r.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var before = identity(l[name]);
            var after = identity(r[name]);
            if (before is null || after is null || before == after) continue;
            var oldValue = $"-{kind}:{name}:native_id={before}";
            var newValue = $"+{kind}:{name}:native_id={after}";
            if (removed is not null) removed.Add(oldValue);
            else comparison.OtherObjectChanges.Add(oldValue);
            if (added is not null) added.Add(newValue);
            else comparison.OtherObjectChanges.Add(newValue);
        }
    }

    private static void ComparePrimaryKeyNativeIdentity(
        SchemaSnapshotComparison comparison,
        string tableKey,
        SchemaPrimaryKeySnapshot? left,
        SchemaPrimaryKeySnapshot? right)
    {
        if (left is null || right is null)
        {
            return;
        }

        var name = $"{tableKey}.{left.ConstraintName}";
        CompareNativeIdentity(comparison, "primary_key", name, left.ObjectId, right.ObjectId);
        CompareNativeIdentity(comparison, "primary_key_index", name, left.IndexId, right.IndexId);
    }

    private static void CompareNativeIdentity(
        SchemaSnapshotComparison comparison,
        string kind,
        string name,
        int? before,
        int? after)
    {
        // Null significa snapshot legado sem identidade nativa; não prova substituição.
        if (before is null || after is null || before == after) return;
        comparison.OtherObjectChanges.Add($"~{kind}:{name}:native_id {before} -> {after}");
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

}
