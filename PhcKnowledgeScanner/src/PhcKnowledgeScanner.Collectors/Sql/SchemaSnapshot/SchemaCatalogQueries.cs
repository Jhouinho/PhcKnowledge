namespace PhcKnowledgeScanner.Collectors.Sql.SchemaSnapshot;

/// <summary>Consultas ao catálogo SQL Server — apenas metadados, sem SELECT a tabelas de negócio.</summary>
internal static class SchemaCatalogQueries
{
    public const string ServerInfo =
        """
        SELECT
            CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128)),
            CAST(@@VERSION AS nvarchar(400));
        """;

    public const string PhcSignature =
        """
        SELECT
            CASE WHEN OBJECT_ID(N'dbo.bo', N'U') IS NOT NULL THEN 1 ELSE 0 END,
            CASE WHEN OBJECT_ID(N'dbo.cl', N'U') IS NOT NULL THEN 1 ELSE 0 END,
            CASE WHEN OBJECT_ID(N'dbo.st', N'U') IS NOT NULL THEN 1 ELSE 0 END,
            CASE WHEN OBJECT_ID(N'dbo.ft', N'U') IS NOT NULL THEN 1 ELSE 0 END;
        """;

    public const string Tables =
        """
        SELECT
            s.name AS schema_name,
            t.name AS table_name,
            t.type_desc,
            t.is_ms_shipped,
            t.create_date,
            t.modify_date,
            CONVERT(nvarchar(4000), ep.value)
        FROM sys.tables t
        INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
        LEFT JOIN sys.extended_properties ep
            ON ep.major_id = t.object_id
           AND ep.minor_id = 0
           AND ep.name = N'MS_Description'
           AND ep.class = 1
        ORDER BY s.name, t.name;
        """;

    public const string Columns =
        """
        SELECT
            s.name AS schema_name,
            t.name AS table_name,
            c.name AS column_name,
            c.column_id,
            ty.name AS type_name,
            c.max_length,
            c.precision,
            c.scale,
            c.is_nullable,
            c.is_identity,
            CAST(ic.seed_value AS bigint) AS identity_seed,
            CAST(ic.increment_value AS bigint) AS identity_incr,
            dc.definition AS default_definition,
            c.collation_name,
            c.is_computed,
            cc.definition AS computed_definition
        FROM sys.tables t
        INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
        INNER JOIN sys.columns c ON c.object_id = t.object_id
        INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        LEFT JOIN sys.identity_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
        LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
        ORDER BY s.name, t.name, c.column_id;
        """;

    public const string PrimaryKeys =
        """
        SELECT
            s.name AS schema_name,
            t.name AS table_name,
            kc.name AS pk_name,
            c.name AS column_name,
            ic.key_ordinal,
            kc.is_disabled
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

    public const string ForeignKeys =
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
            fk.update_referential_action_desc,
            fk.is_disabled,
            fk.is_not_trusted
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

    public const string Indexes =
        """
        SELECT
            s.name AS schema_name,
            t.name AS table_name,
            i.name AS index_name,
            i.type_desc,
            i.is_unique,
            i.is_primary_key,
            i.is_unique_constraint,
            i.is_disabled,
            i.filter_definition,
            c.name AS column_name,
            ic.key_ordinal,
            ic.is_descending_key,
            ic.is_included_column
        FROM sys.indexes i
        INNER JOIN sys.tables t ON t.object_id = i.object_id
        INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.name IS NOT NULL
          AND i.type > 0
        ORDER BY s.name, t.name, i.name, ic.is_included_column, ic.key_ordinal, c.name;
        """;

    public const string Views =
        """
        SELECT
            s.name,
            v.name,
            v.is_ms_shipped,
            v.create_date,
            v.modify_date,
            CASE WHEN m.definition IS NULL THEN 0 ELSE 1 END AS has_definition,
            CASE WHEN @includeDefinitions = 1 THEN m.definition ELSE NULL END AS definition
        FROM sys.views v
        INNER JOIN sys.schemas s ON s.schema_id = v.schema_id
        LEFT JOIN sys.sql_modules m ON m.object_id = v.object_id
        ORDER BY s.name, v.name;
        """;

    public const string Triggers =
        """
        SELECT
            OBJECT_SCHEMA_NAME(tr.parent_id) AS parent_schema,
            OBJECT_NAME(tr.parent_id) AS parent_name,
            OBJECT_SCHEMA_NAME(tr.object_id) AS trigger_schema,
            tr.name AS trigger_name,
            o.type_desc AS parent_type,
            tr.is_disabled,
            tr.is_instead_of_trigger,
            CASE WHEN m.definition IS NULL THEN 0 ELSE 1 END AS has_definition,
            CASE WHEN @includeDefinitions = 1 THEN m.definition ELSE NULL END AS definition
        FROM sys.triggers tr
        INNER JOIN sys.objects o ON o.object_id = tr.parent_id
        LEFT JOIN sys.sql_modules m ON m.object_id = tr.object_id
        WHERE tr.parent_class_desc = N'OBJECT_OR_COLUMN'
        ORDER BY parent_schema, parent_name, tr.name;
        """;

    public const string TriggerEvents =
        """
        SELECT
            OBJECT_SCHEMA_NAME(tr.object_id) AS trigger_schema,
            tr.name AS trigger_name,
            te.type_desc
        FROM sys.triggers tr
        INNER JOIN sys.trigger_events te ON te.object_id = tr.object_id
        WHERE tr.parent_class_desc = N'OBJECT_OR_COLUMN'
        ORDER BY trigger_schema, tr.name, te.type_desc;
        """;

    public const string Procedures =
        """
        SELECT
            s.name,
            p.name,
            p.is_ms_shipped,
            p.create_date,
            p.modify_date,
            CASE WHEN m.definition IS NULL THEN 0 ELSE 1 END AS has_definition,
            CASE WHEN @includeDefinitions = 1 THEN m.definition ELSE NULL END AS definition
        FROM sys.procedures p
        INNER JOIN sys.schemas s ON s.schema_id = p.schema_id
        LEFT JOIN sys.sql_modules m ON m.object_id = p.object_id
        ORDER BY s.name, p.name;
        """;

    public const string Functions =
        """
        SELECT
            s.name,
            o.name,
            o.type_desc,
            o.is_ms_shipped,
            o.create_date,
            o.modify_date,
            CASE WHEN m.definition IS NULL THEN 0 ELSE 1 END AS has_definition,
            CASE WHEN @includeDefinitions = 1 THEN m.definition ELSE NULL END AS definition
        FROM sys.objects o
        INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
        LEFT JOIN sys.sql_modules m ON m.object_id = o.object_id
        WHERE o.type IN (N'FN', N'IF', N'TF', N'FS', N'FT')
        ORDER BY s.name, o.name;
        """;

    public const string Parameters =
        """
        SELECT
            s.name AS schema_name,
            o.name AS object_name,
            o.type AS object_type,
            p.name AS parameter_name,
            p.parameter_id,
            ty.name AS type_name,
            p.max_length,
            p.precision,
            p.scale,
            p.is_output,
            p.is_readonly,
            p.has_default_value
        FROM sys.parameters p
        INNER JOIN sys.objects o ON o.object_id = p.object_id
        INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
        INNER JOIN sys.types ty ON ty.user_type_id = p.user_type_id
        WHERE o.type IN (N'P', N'PC', N'FN', N'IF', N'TF', N'FS', N'FT')
        ORDER BY s.name, o.name, p.parameter_id;
        """;

    public const string UserTypes =
        """
        SELECT
            s.name AS schema_name,
            t.name AS type_name,
            bas.name AS system_type_name,
            t.max_length,
            t.precision,
            t.scale,
            t.is_nullable,
            t.is_table_type
        FROM sys.types t
        INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
        LEFT JOIN sys.types bas ON bas.user_type_id = t.system_type_id
        WHERE t.is_user_defined = 1
        ORDER BY s.name, t.name;
        """;

    public const string Synonyms =
        """
        SELECT
            s.name AS schema_name,
            syn.name AS synonym_name,
            syn.base_object_name
        FROM sys.synonyms syn
        INNER JOIN sys.schemas s ON s.schema_id = syn.schema_id
        ORDER BY s.name, syn.name;
        """;
}
