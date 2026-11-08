using System.Globalization;
using System.Text;
using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;

namespace PhcKnowledgeScanner.Export.SchemaSnapshot;

/// <summary>
/// Apresentação textual do resultado de <see cref="DatabaseSchemaSnapshotComparer"/> para consola/CLI.
/// Não altera a lógica de comparação — apenas formata o modelo já produzido.
/// </summary>
public static class SchemaSnapshotComparisonReporter
{
    public static string Format(SchemaSnapshotComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        var sb = new StringBuilder();
        Write(comparison, sb);
        return sb.ToString();
    }

    public static void Write(SchemaSnapshotComparison comparison, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(Format(comparison));
    }

    private static void Write(SchemaSnapshotComparison comparison, StringBuilder sb)
    {
        sb.AppendLine("=== Schema Snapshot Comparison ===");
        sb.AppendLine("LeftHash:  " + NullToEmpty(comparison.LeftHash));
        sb.AppendLine("RightHash: " + NullToEmpty(comparison.RightHash));
        sb.AppendLine("LeftCollectedAtUtc:  " + FormatUtc(comparison.LeftCollectedAtUtc));
        sb.AppendLine("RightCollectedAtUtc: " + FormatUtc(comparison.RightCollectedAtUtc));
        sb.AppendLine();

        var tableIdentity = FilterPrefix(comparison.OtherObjectChanges, "~table:");
        var columnIdentity = FilterPrefix(comparison.OtherObjectChanges, "~column:");
        var columnIdIdentity = FilterPrefix(comparison.OtherObjectChanges, "~column_id:");
        var pkIdentity = FilterPrefix(comparison.OtherObjectChanges, "~primary_key:");
        var pkIndexIdentity = FilterPrefix(comparison.OtherObjectChanges, "~primary_key_index:");
        var views = FilterKinds(comparison.OtherObjectChanges, "view");
        var triggers = FilterKinds(comparison.OtherObjectChanges, "trigger");
        var procedures = FilterKinds(comparison.OtherObjectChanges, "procedure");
        var functions = FilterKinds(comparison.OtherObjectChanges, "function");
        var classified = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in tableIdentity
                     .Concat(columnIdentity)
                     .Concat(columnIdIdentity)
                     .Concat(pkIdentity)
                     .Concat(pkIndexIdentity)
                     .Concat(views)
                     .Concat(triggers)
                     .Concat(procedures)
                     .Concat(functions))
        {
            classified.Add(item);
        }

        var leftover = comparison.OtherObjectChanges
            .Where(x => !classified.Contains(x))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        WriteSection(sb, "Tables", () =>
        {
            foreach (var t in comparison.TablesAdded.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine("  + added: " + t);
            }

            foreach (var t in comparison.TablesRemoved.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine("  - removed: " + t);
            }

            foreach (var change in tableIdentity)
            {
                sb.AppendLine("  ~ native identity: " + FormatTildeNativeChange(change, "table"));
            }
        }, comparison.TablesAdded.Count + comparison.TablesRemoved.Count + tableIdentity.Count);

        WriteSection(sb, "Columns", () =>
        {
            foreach (var c in comparison.ColumnsAdded.OrderBy(x => x.TableKey, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.ColumnName, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"  + added: {c.TableKey}.{c.ColumnName}");
            }

            foreach (var c in comparison.ColumnsRemoved.OrderBy(x => x.TableKey, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.ColumnName, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"  - removed: {c.TableKey}.{c.ColumnName}");
            }

            foreach (var c in comparison.ColumnPropertyChanges.OrderBy(x => x.TableKey, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.ColumnName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.Property, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine(
                    $"  ~ property: {c.TableKey}.{c.ColumnName}.{c.Property}: {NullToEmpty(c.Before)} -> {NullToEmpty(c.After)}");
            }

            foreach (var change in columnIdentity)
            {
                sb.AppendLine("  ~ native identity (objectId): " + FormatTildeNativeChange(change, "column"));
            }

            foreach (var change in columnIdIdentity)
            {
                sb.AppendLine("  ~ native identity (columnId): " + FormatTildeNativeChange(change, "column_id"));
            }
        }, comparison.ColumnsAdded.Count
           + comparison.ColumnsRemoved.Count
           + comparison.ColumnPropertyChanges.Count
           + columnIdentity.Count
           + columnIdIdentity.Count);

        WriteSection(sb, "Primary Keys", () =>
        {
            foreach (var change in pkIdentity)
            {
                sb.AppendLine("  ~ constraint native identity: " + FormatTildeNativeChange(change, "primary_key"));
            }

            foreach (var change in pkIndexIdentity)
            {
                sb.AppendLine("  ~ supporting index native identity: "
                              + FormatTildeNativeChange(change, "primary_key_index"));
            }
        }, pkIdentity.Count + pkIndexIdentity.Count);

        WriteSection(sb, "Indexes", () =>
        {
            foreach (var change in comparison.IndexesAdded.OrderBy(x => x, StringComparer.Ordinal))
            {
                sb.AppendLine("  " + FormatSignedIdentityChange(change));
            }

            foreach (var change in comparison.IndexesRemoved.OrderBy(x => x, StringComparer.Ordinal))
            {
                sb.AppendLine("  " + FormatSignedIdentityChange(change));
            }
        }, comparison.IndexesAdded.Count + comparison.IndexesRemoved.Count);

        WriteSection(sb, "Foreign Keys", () =>
        {
            foreach (var change in comparison.RelationshipsAdded.OrderBy(x => x, StringComparer.Ordinal))
            {
                sb.AppendLine("  " + FormatSignedIdentityChange(change));
            }

            foreach (var change in comparison.RelationshipsRemoved.OrderBy(x => x, StringComparer.Ordinal))
            {
                sb.AppendLine("  " + FormatSignedIdentityChange(change));
            }
        }, comparison.RelationshipsAdded.Count + comparison.RelationshipsRemoved.Count);

        WriteKindSection(sb, "Views", views);
        WriteKindSection(sb, "Triggers", triggers);
        WriteKindSection(sb, "Procedures", procedures);
        WriteKindSection(sb, "Functions", functions);

        WriteSection(sb, "Unclassified OtherObjectChanges", () =>
        {
            foreach (var change in leftover)
            {
                sb.AppendLine("  " + change);
            }
        }, leftover.Count);

        var nativeIdentityCount = CountNativeIdentityChanges(
            comparison,
            tableIdentity,
            columnIdentity,
            columnIdIdentity,
            pkIdentity,
            pkIndexIdentity,
            views,
            triggers,
            procedures,
            functions,
            leftover);

        var total =
            comparison.TablesAdded.Count
            + comparison.TablesRemoved.Count
            + comparison.ColumnsAdded.Count
            + comparison.ColumnsRemoved.Count
            + comparison.ColumnPropertyChanges.Count
            + comparison.IndexesAdded.Count
            + comparison.IndexesRemoved.Count
            + comparison.RelationshipsAdded.Count
            + comparison.RelationshipsRemoved.Count
            + comparison.OtherObjectChanges.Count;

        sb.AppendLine("=== Summary ===");
        sb.AppendLine("Total changes: " + total.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(
            "Tables: +"
            + comparison.TablesAdded.Count.ToString(CultureInfo.InvariantCulture)
            + " -"
            + comparison.TablesRemoved.Count.ToString(CultureInfo.InvariantCulture)
            + " ~identity="
            + tableIdentity.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(
            "Columns: +"
            + comparison.ColumnsAdded.Count.ToString(CultureInfo.InvariantCulture)
            + " -"
            + comparison.ColumnsRemoved.Count.ToString(CultureInfo.InvariantCulture)
            + " ~property="
            + comparison.ColumnPropertyChanges.Count.ToString(CultureInfo.InvariantCulture)
            + " ~identity(objectId)="
            + columnIdentity.Count.ToString(CultureInfo.InvariantCulture)
            + " ~identity(columnId)="
            + columnIdIdentity.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(
            "Primary Keys: ~constraintIdentity="
            + pkIdentity.Count.ToString(CultureInfo.InvariantCulture)
            + " ~supportingIndexIdentity="
            + pkIndexIdentity.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(
            "Indexes: +"
            + comparison.IndexesAdded.Count.ToString(CultureInfo.InvariantCulture)
            + " -"
            + comparison.IndexesRemoved.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(
            "Foreign Keys: +"
            + comparison.RelationshipsAdded.Count.ToString(CultureInfo.InvariantCulture)
            + " -"
            + comparison.RelationshipsRemoved.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("Views: " + views.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("Triggers: " + triggers.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("Procedures: " + procedures.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("Functions: " + functions.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(
            "Unclassified OtherObjectChanges: " + leftover.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(
            "Native identity changes: " + nativeIdentityCount.ToString(CultureInfo.InvariantCulture));
        if (total == 0)
        {
            sb.AppendLine("No differences.");
        }
    }

    private static void WriteKindSection(StringBuilder sb, string title, IReadOnlyList<string> changes)
    {
        WriteSection(sb, title, () =>
        {
            foreach (var change in changes.OrderBy(x => x, StringComparer.Ordinal))
            {
                sb.AppendLine("  " + FormatSignedIdentityChange(change));
            }
        }, changes.Count);
    }

    private static void WriteSection(StringBuilder sb, string title, Action writeBody, int count)
    {
        if (count == 0)
        {
            return;
        }

        sb.AppendLine("--- " + title + " ---");
        writeBody();
        sb.AppendLine();
    }

    private static List<string> FilterPrefix(IEnumerable<string> items, string prefix) =>
        items.Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToList();

    private static List<string> FilterKinds(IEnumerable<string> items, string kind)
    {
        // Formatos: +kind:name | -kind:name | +/-kind:name:native_id=...
        var plus = "+" + kind + ":";
        var minus = "-" + kind + ":";
        return items.Where(x =>
                x.StartsWith(plus, StringComparison.Ordinal)
                || x.StartsWith(minus, StringComparison.Ordinal))
            .ToList();
    }

    private static string FormatTildeNativeChange(string raw, string kindPrefix)
    {
        // ~kind:name:native_id BEFORE -> AFTER
        var marker = "~" + kindPrefix + ":";
        if (!raw.StartsWith(marker, StringComparison.Ordinal))
        {
            return raw;
        }

        var rest = raw[marker.Length..];
        const string nativeMarker = ":native_id ";
        var idx = rest.IndexOf(nativeMarker, StringComparison.Ordinal);
        if (idx < 0)
        {
            return raw;
        }

        var name = rest[..idx];
        var ids = rest[(idx + nativeMarker.Length)..];
        var arrow = ids.IndexOf(" -> ", StringComparison.Ordinal);
        if (arrow < 0)
        {
            return name + " | native_id " + ids;
        }

        var before = ids[..arrow];
        var after = ids[(arrow + 4)..];
        return name + " | previous=" + before + " | new=" + after;
    }

    private static string FormatSignedIdentityChange(string raw)
    {
        // +kind:name | -kind:name | +/-kind:name:native_id=VALUE
        if (raw.Length < 2 || (raw[0] is not '+' and not '-'))
        {
            return raw;
        }

        var sign = raw[0] == '+' ? "+ added/replaced" : "- removed/replaced";
        var body = raw[1..];
        var nativeIdx = body.IndexOf(":native_id=", StringComparison.Ordinal);
        if (nativeIdx < 0)
        {
            return sign + ": " + body;
        }

        var name = body[..nativeIdx];
        var id = body[(nativeIdx + ":native_id=".Length)..];
        return sign + ": " + name + " | native_id=" + id;
    }

    private static int CountNativeIdentityChanges(
        SchemaSnapshotComparison comparison,
        IReadOnlyList<string> tableIdentity,
        IReadOnlyList<string> columnIdentity,
        IReadOnlyList<string> columnIdIdentity,
        IReadOnlyList<string> pkIdentity,
        IReadOnlyList<string> pkIndexIdentity,
        IReadOnlyList<string> views,
        IReadOnlyList<string> triggers,
        IReadOnlyList<string> procedures,
        IReadOnlyList<string> functions,
        IReadOnlyList<string> leftover)
    {
        static int CountNativeMarker(IEnumerable<string> items) =>
            items.Count(x => x.Contains("native_id", StringComparison.Ordinal));

        return tableIdentity.Count
               + columnIdentity.Count
               + columnIdIdentity.Count
               + pkIdentity.Count
               + pkIndexIdentity.Count
               + CountNativeMarker(comparison.IndexesAdded)
               + CountNativeMarker(comparison.IndexesRemoved)
               + CountNativeMarker(comparison.RelationshipsAdded)
               + CountNativeMarker(comparison.RelationshipsRemoved)
               + CountNativeMarker(views)
               + CountNativeMarker(triggers)
               + CountNativeMarker(procedures)
               + CountNativeMarker(functions)
               + CountNativeMarker(leftover);
    }

    private static string FormatUtc(DateTimeOffset? value) =>
        value is null ? "(none)" : value.Value.ToString("O", CultureInfo.InvariantCulture);

    private static string NullToEmpty(string? value) => value ?? "";
}
