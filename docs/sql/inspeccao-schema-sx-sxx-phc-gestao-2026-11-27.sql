/*
    PHC Gestão — Inspecção de SX / SXX
    Objectivo: recolher schema, defaults, índices, relações e referências SQL.
    Base prevista: CSDefault202603
    Segurança: exclusivamente leitura. Não contém DML, DDL, EXEC ou alterações de sessão persistentes.

    Executar na base de dados pretendida e exportar TODOS os conjuntos de resultados.
    Se SX/SXX não estiverem no schema dbo, o guião também as procura noutros schemas.
*/
SET NOCOUNT ON;

PRINT '01 - Objectos SX / SXX';
SELECT
    s.name AS schema_name,
    o.name AS object_name,
    o.type_desc AS object_type,
    o.create_date,
    o.modify_date,
    t.temporal_type_desc,
    t.is_memory_optimized
FROM sys.objects AS o
INNER JOIN sys.schemas AS s ON s.schema_id = o.schema_id
LEFT JOIN sys.tables AS t ON t.object_id = o.object_id
WHERE o.name IN (N'SX', N'SXX')
  AND o.is_ms_shipped = 0
ORDER BY o.name, s.name;

PRINT '02 - Colunas, tipos, nulabilidade, identidade, computed e defaults';
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    c.column_id,
    c.name AS column_name,
    ts.name AS type_schema,
    ty.name AS data_type,
    c.max_length AS max_length_bytes,
    c.precision,
    c.scale,
    c.is_nullable,
    c.is_identity,
    ic.seed_value AS identity_seed,
    ic.increment_value AS identity_increment,
    c.is_computed,
    cc.definition AS computed_definition,
    dc.name AS default_constraint_name,
    dc.definition AS default_definition,
    c.collation_name
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
INNER JOIN sys.columns AS c ON c.object_id = t.object_id
INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
INNER JOIN sys.schemas AS ts ON ts.schema_id = ty.schema_id
LEFT JOIN sys.identity_columns AS ic
    ON ic.object_id = c.object_id AND ic.column_id = c.column_id
LEFT JOIN sys.computed_columns AS cc
    ON cc.object_id = c.object_id AND cc.column_id = c.column_id
LEFT JOIN sys.default_constraints AS dc
    ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE t.name IN (N'SX', N'SXX')
ORDER BY t.name, s.name, c.column_id;

PRINT '03 - Índices, chaves primárias/únicas e ordem das colunas';
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    i.name AS index_name,
    i.type_desc AS index_type,
    i.is_primary_key,
    i.is_unique,
    i.is_unique_constraint,
    i.is_disabled,
    i.has_filter,
    i.filter_definition,
    ic.key_ordinal,
    ic.index_column_id,
    ic.is_included_column,
    c.name AS column_name,
    ic.is_descending_key
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
INNER JOIN sys.indexes AS i ON i.object_id = t.object_id
INNER JOIN sys.index_columns AS ic
    ON ic.object_id = i.object_id AND ic.index_id = i.index_id
LEFT JOIN sys.columns AS c
    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.name IN (N'SX', N'SXX')
  AND i.index_id > 0
ORDER BY t.name, s.name, i.index_id, ic.key_ordinal, ic.index_column_id;

PRINT '04 - Constraints CHECK e DEFAULT';
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    chk.name AS constraint_name,
    N'CHECK' AS constraint_type,
    chk.definition,
    chk.is_disabled,
    chk.is_not_trusted
FROM sys.check_constraints AS chk
INNER JOIN sys.tables AS t ON t.object_id = chk.parent_object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.name IN (N'SX', N'SXX')
UNION ALL
SELECT
    s.name,
    t.name,
    dc.name,
    N'DEFAULT',
    dc.definition,
    CONVERT(bit, 0),
    CONVERT(bit, 0)
FROM sys.default_constraints AS dc
INNER JOIN sys.tables AS t ON t.object_id = dc.parent_object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.name IN (N'SX', N'SXX')
ORDER BY table_name, constraint_type, constraint_name;

PRINT '05 - Chaves estrangeiras que entram ou saem de SX / SXX';
SELECT
    fk.name AS foreign_key_name,
    ps.name AS referencing_schema,
    pt.name AS referencing_table,
    pc.name AS referencing_column,
    rs.name AS referenced_schema,
    rt.name AS referenced_table,
    rc.name AS referenced_column,
    fkc.constraint_column_id AS column_ordinal,
    fk.delete_referential_action_desc,
    fk.update_referential_action_desc,
    fk.is_disabled,
    fk.is_not_trusted
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.tables AS pt ON pt.object_id = fkc.parent_object_id
INNER JOIN sys.schemas AS ps ON ps.schema_id = pt.schema_id
INNER JOIN sys.columns AS pc ON pc.object_id = pt.object_id AND pc.column_id = fkc.parent_column_id
INNER JOIN sys.tables AS rt ON rt.object_id = fkc.referenced_object_id
INNER JOIN sys.schemas AS rs ON rs.schema_id = rt.schema_id
INNER JOIN sys.columns AS rc ON rc.object_id = rt.object_id AND rc.column_id = fkc.referenced_column_id
WHERE pt.name IN (N'SX', N'SXX') OR rt.name IN (N'SX', N'SXX')
ORDER BY fk.name, fkc.constraint_column_id;

PRINT '06 - Triggers associados directamente a SX / SXX';
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    tr.name AS trigger_name,
    tr.is_disabled,
    tr.is_instead_of_trigger,
    tr.create_date,
    tr.modify_date,
    OBJECT_DEFINITION(tr.object_id) AS trigger_definition
FROM sys.triggers AS tr
INNER JOIN sys.tables AS t ON t.object_id = tr.parent_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.name IN (N'SX', N'SXX')
ORDER BY t.name, tr.name;

PRINT '07 - Dependências SQL declaradas que envolvem SX / SXX';
SELECT
    OBJECT_SCHEMA_NAME(d.referencing_id) AS referencing_schema,
    OBJECT_NAME(d.referencing_id) AS referencing_object,
    ro.type_desc AS referencing_type,
    d.referenced_server_name,
    d.referenced_database_name,
    d.referenced_schema_name,
    d.referenced_entity_name,
    d.is_schema_bound_reference,
    d.is_ambiguous
FROM sys.sql_expression_dependencies AS d
LEFT JOIN sys.objects AS ro ON ro.object_id = d.referencing_id
WHERE OBJECT_NAME(d.referencing_id) IN (N'SX', N'SXX')
   OR d.referenced_entity_name IN (N'SX', N'SXX')
ORDER BY referencing_schema, referencing_object, d.referenced_entity_name;

PRINT '08 - Definições SQL que mencionam SX ou SXX (pesquisa textual)';
SELECT
    s.name AS schema_name,
    o.name AS object_name,
    o.type_desc AS object_type,
    o.modify_date,
    CASE
        WHEN m.definition LIKE N'%SXX%' THEN N'SXX'
        WHEN m.definition LIKE N'%SX%' THEN N'SX'
        ELSE N''
    END AS matched_token,
    m.definition
FROM sys.sql_modules AS m
INNER JOIN sys.objects AS o ON o.object_id = m.object_id
INNER JOIN sys.schemas AS s ON s.schema_id = o.schema_id
WHERE m.definition LIKE N'%SX%'
   OR m.definition LIKE N'%SXX%'
ORDER BY o.type_desc, s.name, o.name;

/*
    Notas de interpretação:
    - max_length está em bytes; -1 indica MAX.
    - A pesquisa textual pode devolver falsos positivos (por exemplo, nomes que contêm SX).
    - sys.sql_expression_dependencies pode não resolver SQL dinâmico ou referências externas.
    - A ausência de uma FK declarada não exclui relações funcionais ou ligações por stamps.
    - Se algum resultado estiver vazio, isso é também um resultado a registar.
*/
