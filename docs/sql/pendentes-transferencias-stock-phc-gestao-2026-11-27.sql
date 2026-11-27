/*
  PHC Gestão — verificações pendentes do circuito de transferências
  Base de referência: CSDefault202603
  Estado: preparado para executar quando houver acesso ao SQL Server.

  IMPORTANTE:
  - Script exclusivamente de leitura (SELECT / metadados).
  - Não cria, altera nem elimina dados ou objectos.
  - Não assumir nomes de colunas ainda não confirmados: consultar primeiro o esquema.
  - Guardar/exportar os resultados para fechar a análise.
*/

SET NOCOUNT ON;

PRINT '1) ESQUEMA REAL — TS, TS2 e CM2';
SELECT
    SCHEMA_NAME(t.schema_id) AS esquema,
    t.name AS tabela,
    c.column_id,
    c.name AS coluna,
    ty.name AS tipo,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable
FROM sys.tables AS t
INNER JOIN sys.columns AS c ON c.object_id = t.object_id
INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
WHERE SCHEMA_NAME(t.schema_id) = 'dbo'
  AND t.name IN ('TS', 'TS2', 'CM2')
ORDER BY t.name, c.column_id;

PRINT '2) CONFIGURAÇÃO DOS DOSSIERS — TS.ndos = 5';
SELECT *
FROM dbo.TS
WHERE ndos = 5;

PRINT '3) CONFIGURAÇÃO ASSOCIADA — TS2';
-- A ligação TS.tsstamp = TS2.ts2stamp já foi observada na recolha anterior.
-- TS.* e TS2.* evitam presumir nomes de campos ainda por validar.
SELECT ts.*, ts2.*
FROM dbo.TS AS ts
LEFT JOIN dbo.TS2 AS ts2
    ON ts.tsstamp = ts2.ts2stamp
WHERE ts.ndos = 5;

PRINT '4) CONFIGURAÇÃO CM2 — TODOS OS REGISTOS';
-- Devolve a tabela de configuração para localizar os códigos 98 e 48
-- sem presumir o nome da coluna que identifica o código.
SELECT *
FROM dbo.CM2
ORDER BY 1;
