/*
PHC Gestão — perfil funcional de SX/SXX
Data: 2026-11-27
Âmbito: CSDefault202603
Segurança: exclusivamente SELECT; sem DML, DDL ou execução de procedures.
Objectivo: caracterizar SXX e compará-la com SX sem devolver referências de artigos.
*/

SET NOCOUNT ON;

PRINT '01 - Contagens globais';
SELECT
    'SX' AS tabela,
    COUNT_BIG(*) AS total_linhas,
    SUM(CASE WHEN stock <> 0 THEN 1 ELSE 0 END) AS linhas_stock_nao_zero,
    SUM(CASE WHEN rescli <> 0 THEN 1 ELSE 0 END) AS linhas_rescli_nao_zero,
    SUM(CASE WHEN resfor <> 0 THEN 1 ELSE 0 END) AS linhas_resfor_nao_zero,
    SUM(CASE WHEN rescat <> 0 THEN 1 ELSE 0 END) AS linhas_rescat_nao_zero,
    SUM(CASE WHEN qttrec <> 0 THEN 1 ELSE 0 END) AS linhas_qttrec_nao_zero,
    SUM(CASE WHEN cor <> '' OR tam <> '' THEN 1 ELSE 0 END) AS linhas_com_grelha
FROM dbo.SX
UNION ALL
SELECT
    'SXX',
    COUNT_BIG(*),
    SUM(CASE WHEN stock <> 0 THEN 1 ELSE 0 END),
    SUM(CASE WHEN rescli <> 0 THEN 1 ELSE 0 END),
    SUM(CASE WHEN resfor <> 0 THEN 1 ELSE 0 END),
    SUM(CASE WHEN rescat <> 0 THEN 1 ELSE 0 END),
    SUM(CASE WHEN qttrec <> 0 THEN 1 ELSE 0 END),
    SUM(CASE WHEN cor <> '' OR tam <> '' THEN 1 ELSE 0 END)
FROM dbo.SXX;

PRINT '02 - Totais de quantidades e indicadores de preenchimento';
SELECT
    'SX' AS tabela,
    SUM(stock) AS stock_total,
    SUM(rescli) AS rescli_total,
    SUM(resfor) AS resfor_total,
    SUM(rescat) AS rescat_total,
    SUM(qttrec) AS qttrec_total,
    SUM(CASE WHEN codigo <> '' THEN 1 ELSE 0 END) AS linhas_com_codigo,
    SUM(CASE WHEN ststamp <> '' THEN 1 ELSE 0 END) AS linhas_com_ststamp
FROM dbo.SX
UNION ALL
SELECT
    'SXX',
    SUM(stock),
    SUM(rescli),
    SUM(resfor),
    SUM(rescat),
    SUM(qttrec),
    SUM(CASE WHEN codigo <> '' THEN 1 ELSE 0 END),
    CAST(NULL AS bigint)
FROM dbo.SXX;

PRINT '03 - Distribuição de SXX por armazém (sem referências)';
SELECT
    armazem,
    COUNT_BIG(*) AS linhas,
    COUNT(DISTINCT ref) AS artigos,
    SUM(stock) AS stock_total,
    SUM(rescli) AS rescli_total,
    SUM(resfor) AS resfor_total,
    SUM(rescat) AS rescat_total,
    SUM(qttrec) AS qttrec_total,
    SUM(CASE WHEN cor <> '' OR tam <> '' THEN 1 ELSE 0 END) AS linhas_com_grelha
FROM dbo.SXX
GROUP BY armazem
ORDER BY armazem;

PRINT '04 - Duplicados na chave funcional de SXX';
SELECT
    COUNT_BIG(*) AS combinacoes_duplicadas,
    COALESCE(SUM(x.quantidade_linhas - 1), 0) AS linhas_excedentes
FROM (
    SELECT COUNT_BIG(*) AS quantidade_linhas
    FROM dbo.SXX
    GROUP BY ref, armazem, cor, tam
    HAVING COUNT_BIG(*) > 1
) AS x;

PRINT '05 - Comparação agregada SX versus SXX pela combinação funcional';
;WITH sx AS (
    SELECT ref, armazem, cor, tam,
           COUNT_BIG(*) AS linhas_sx,
           SUM(stock) AS stock_sx,
           SUM(rescli) AS rescli_sx,
           SUM(resfor) AS resfor_sx,
           SUM(rescat) AS rescat_sx,
           SUM(qttrec) AS qttrec_sx
    FROM dbo.SX
    GROUP BY ref, armazem, cor, tam
),
sxx AS (
    SELECT ref, armazem, cor, tam,
           COUNT_BIG(*) AS linhas_sxx,
           SUM(stock) AS stock_sxx,
           SUM(rescli) AS rescli_sxx,
           SUM(resfor) AS resfor_sxx,
           SUM(rescat) AS rescat_sxx,
           SUM(qttrec) AS qttrec_sxx
    FROM dbo.SXX
    GROUP BY ref, armazem, cor, tam
),
comparacao AS (
    SELECT
        CASE WHEN sx.ref IS NULL THEN 0 ELSE 1 END AS existe_sx,
        CASE WHEN sxx.ref IS NULL THEN 0 ELSE 1 END AS existe_sxx,
        COALESCE(sx.linhas_sx, 0) AS linhas_sx,
        COALESCE(sxx.linhas_sxx, 0) AS linhas_sxx,
        COALESCE(sx.stock_sx, 0) AS stock_sx,
        COALESCE(sxx.stock_sxx, 0) AS stock_sxx,
        COALESCE(sx.rescli_sx, 0) AS rescli_sx,
        COALESCE(sxx.rescli_sxx, 0) AS rescli_sxx,
        COALESCE(sx.resfor_sx, 0) AS resfor_sx,
        COALESCE(sxx.resfor_sxx, 0) AS resfor_sxx,
        COALESCE(sx.rescat_sx, 0) AS rescat_sx,
        COALESCE(sxx.rescat_sxx, 0) AS rescat_sxx,
        COALESCE(sx.qttrec_sx, 0) AS qttrec_sx,
        COALESCE(sxx.qttrec_sxx, 0) AS qttrec_sxx
    FROM sx
    FULL OUTER JOIN sxx
      ON sxx.ref = sx.ref
     AND sxx.armazem = sx.armazem
     AND sxx.cor = sx.cor
     AND sxx.tam = sx.tam
)
SELECT
    COUNT_BIG(*) AS combinacoes_totais,
    SUM(CASE WHEN existe_sx = 1 AND existe_sxx = 1 THEN 1 ELSE 0 END) AS presentes_em_ambas,
    SUM(CASE WHEN existe_sx = 1 AND existe_sxx = 0 THEN 1 ELSE 0 END) AS apenas_sx,
    SUM(CASE WHEN existe_sx = 0 AND existe_sxx = 1 THEN 1 ELSE 0 END) AS apenas_sxx,
    SUM(CASE WHEN stock_sx <> stock_sxx THEN 1 ELSE 0 END) AS diferencas_stock,
    SUM(CASE WHEN rescli_sx <> rescli_sxx
               OR resfor_sx <> resfor_sxx
               OR rescat_sx <> rescat_sxx
               OR qttrec_sx <> qttrec_sxx THEN 1 ELSE 0 END) AS diferencas_compromissos,
    SUM(stock_sx) AS stock_sx_comparado,
    SUM(stock_sxx) AS stock_sxx_comparado
FROM comparacao;

PRINT '06 - Distribuição de preenchimento de campos distintivos';
SELECT
    'SX' AS tabela,
    SUM(CASE WHEN lordem <> 0 THEN 1 ELSE 0 END) AS linhas_lordem_nao_zero,
    SUM(CASE WHEN ststamp <> '' THEN 1 ELSE 0 END) AS linhas_ststamp_preenchido
FROM dbo.SX
UNION ALL
SELECT
    'SXX',
    CAST(NULL AS bigint),
    CAST(NULL AS bigint)
FROM dbo.SXX
HAVING COUNT_BIG(*) >= 0;
