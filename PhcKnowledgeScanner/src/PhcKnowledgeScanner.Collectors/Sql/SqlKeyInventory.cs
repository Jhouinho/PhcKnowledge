using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;

namespace PhcKnowledgeScanner.Collectors.Sql;

/// <summary>
/// Materialização determinística de objectos PK/FK a partir do catálogo SQL.
/// Factos físicos — sem inferência nem correspondência a <c>dbo.rel</c>/<c>dbo.dtl</c>.
/// </summary>
public static class SqlKeyInventory
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    /// <summary>Consulta PK: todas as tabelas físicas; colunas ordenadas por <c>key_ordinal</c>.</summary>
    public const string PrimaryKeysSql =
        """
        SELECT
            s.name AS schema_name,
            t.name AS table_name,
            kc.name AS pk_name,
            kc.unique_index_id,
            c.name AS column_name,
            ic.key_ordinal
        FROM sys.key_constraints kc
        INNER JOIN sys.tables t ON t.object_id = kc.parent_object_id
        INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
        INNER JOIN sys.index_columns ic
            ON ic.object_id = kc.parent_object_id
           AND ic.index_id = kc.unique_index_id
        INNER JOIN sys.columns c
            ON c.object_id = ic.object_id
           AND c.column_id = ic.column_id
        WHERE kc.type = N'PK'
        ORDER BY s.name, t.name, kc.name, ic.key_ordinal;
        """;

    /// <summary>Consulta FK: todas as tabelas físicas pai; colunas por <c>constraint_column_id</c>.</summary>
    public const string ForeignKeysSql =
        """
        SELECT
            sch.name AS schema_name,
            tab.name AS table_name,
            fk.name AS fk_name,
            refsch.name AS ref_schema,
            reftab.name AS ref_table,
            pc.name AS parent_column,
            rc.name AS ref_column,
            fkc.constraint_column_id,
            fk.delete_referential_action_desc,
            fk.update_referential_action_desc
        FROM sys.foreign_keys fk
        INNER JOIN sys.tables tab ON tab.object_id = fk.parent_object_id
        INNER JOIN sys.schemas sch ON sch.schema_id = tab.schema_id
        INNER JOIN sys.tables reftab ON reftab.object_id = fk.referenced_object_id
        INNER JOIN sys.schemas refsch ON refsch.schema_id = reftab.schema_id
        INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        INNER JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
        INNER JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
        ORDER BY sch.name, tab.name, fk.name, fkc.constraint_column_id;
        """;

    public static string BuildPrimaryKeyObjectId(string schema, string table, string pkName) =>
        $"pk:{schema}.{table}.{pkName}";

    public static string BuildForeignKeyObjectId(string schema, string table, string fkName) =>
        $"fk:{schema}.{table}.{fkName}";

    public static string BuildDerivedForeignKeyRelationId(string fkObjectId) =>
        $"rel:fk:{fkObjectId}";

    public static TechnicalObjectRecord CreatePrimaryKey(
        string schema,
        string table,
        string pkName,
        int uniqueIndexId,
        IReadOnlyList<(string Name, int Ordinal)> columns,
        DateTimeOffset collectedAt,
        string collectorId = SqlServerStructureCollector.CollectorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(table);
        ArgumentException.ThrowIfNullOrWhiteSpace(pkName);
        ArgumentNullException.ThrowIfNull(columns);

        var summary = JsonSerializer.Serialize(new
        {
            uniqueIndexId,
            columns = columns.Select(c => new { name = c.Name, ordinal = c.Ordinal })
        }, JsonOptions);

        return new TechnicalObjectRecord(
            BuildPrimaryKeyObjectId(schema, table, pkName),
            ObjectKind.PrimaryKey,
            pkName,
            schema,
            TableInventoryClassification.BuildObjectId(schema, table),
            collectorId,
            null,
            summary,
            collectedAt);
    }

    public static TechnicalObjectRecord CreateForeignKey(
        string schema,
        string table,
        string fkName,
        string referencedSchema,
        string referencedTable,
        IReadOnlyList<(string Parent, string Referenced, int Ordinal)> columns,
        DateTimeOffset collectedAt,
        string? deleteReferentialAction = null,
        string? updateReferentialAction = null,
        string collectorId = SqlServerStructureCollector.CollectorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(table);
        ArgumentException.ThrowIfNullOrWhiteSpace(fkName);
        ArgumentException.ThrowIfNullOrWhiteSpace(referencedSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(referencedTable);
        ArgumentNullException.ThrowIfNull(columns);

        var summary = JsonSerializer.Serialize(new
        {
            referencedSchema,
            referencedTable,
            deleteReferentialAction,
            updateReferentialAction,
            columns = columns.Select(c => new
            {
                parent = c.Parent,
                referenced = c.Referenced,
                ordinal = c.Ordinal
            })
        }, JsonOptions);

        return new TechnicalObjectRecord(
            BuildForeignKeyObjectId(schema, table, fkName),
            ObjectKind.ForeignKey,
            fkName,
            schema,
            TableInventoryClassification.BuildObjectId(schema, table),
            collectorId,
            null,
            summary,
            collectedAt);
    }
}
