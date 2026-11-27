/*
  PHC Gestão — verificações pendentes do circuito de transferências
  Base de referência: CSDefault202603
  Estado: preparado para executar quando houver acesso ao SQL Server.

  IMPORTANTE:
  - Script exclusivamente de leitura (SELECT / metadados).
  - Não cria, altera nem elimina dados ou objectos.
  - Não assumir que os campos CM2 têm nomes específicos: confirmar no 1.º resultado.
  - Guardar/exportar os resultados dos 3 blocos para fechar a análise.
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
SELECT
    ts.ndos,
    ts.tsstamp,
    ts.stocks,
    ts.trfa,
    ts.producao,
    ts.cmstocks,
    ts.cm2stocks,
    ts2.*
FROM dbo.TS AS ts
LEFT JOIN dbo.TS2 AS ts2
    ON ts.tsstamp = ts2.ts2stamp
WHERE ts.ndos = 5;

PRINT '4) CONFIGURAÇÃO CM2 — TODOS OS REGISTOS';
-- CM2 é uma tabela de configuração; devolver os registos permite localizar
-- os códigos 98 e 48 sem presumir o nome da coluna que identifica o código.
SELECT *
FROM dbo.CM2
ORDER BY 1;
