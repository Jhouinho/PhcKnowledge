# Database Schema Snapshot

Extração de **metadados estruturais** de uma base PHC / SQL Server para um pacote JSON versionável, adequado a consulta offline e comparação entre versões.

## Como gerar

### CLI

```bash
# Modo seguro (por omissão) — sem definições SQL completas
dotnet run --project src/PhcKnowledgeScanner.Cli -- schema-snapshot ^
  --server HOST\INSTANCE --database BD --auth sql --user sa ^
  --output .\database-schema

# ZIP
dotnet run --project src/PhcKnowledgeScanner.Cli -- schema-snapshot ^
  --server HOST\INSTANCE --database BD --zip .\database-schema.zip

# Opcional e NÃO partilhável: incluir CREATE VIEW/PROC/TRIGGER/...
dotnet run --project src/PhcKnowledgeScanner.Cli -- schema-snapshot ^
  --server HOST\INSTANCE --database BD --output .\database-schema ^
  --include-sql-definitions
```

Password: preferir `PHC_SCANNER_SQL_PASSWORD`.

### Desktop

Página **Exportação** → **Gerar schema snapshot…**  
Usa a ligação configurada. A opção “Incluir definições SQL” desactiva a partilha segura.

### Comparar dois snapshots

```bash
dotnet run --project src/PhcKnowledgeScanner.Cli -- compare-schema-snapshot ^
  --left .\schema-a --right .\schema-b
```

## Como funciona a extracção

1. Abre ligação SQL com `ApplicationIntent=ReadOnly` (mesmo builder do Scanner).
2. Consulta **apenas** catálogos `sys.*` (e `SERVERPROPERTY` / `OBJECT_ID` para assinatura PHC).
3. Não executa `SELECT` sobre tabelas de negócio.
4. Produz um `DatabaseSchemaSnapshot` em memória, valida privacidade e escreve a pasta/ZIP.

Não depende de um scan completo do workspace SQLite.

## Informação recolhida

| Categoria | Conteúdo |
| --- | --- |
| Tabelas | schema, nome, tipo, descrição MS_Description, ms_shipped, classificação |
| Campos | tipo, tamanho, precisão, escala, nullable, identity, default, ordem, collation, computed |
| PK / FK | constraints, colunas compostas, ON DELETE/UPDATE, disabled / not trusted |
| Índices | tipo, unique/PK, key + included, filtro, disabled |
| Views / Triggers / Procedures / Functions | metadados; eventos de trigger; parâmetros |
| Tipos UDT / Synonyms | metadados |
| Manifesto | versões, hash, contagens, avisos, limitações, fingerprint anonimizado |

## Informação deliberadamente excluída

- Registos / dados comerciais
- Credenciais e connection strings
- Nome real do cliente / empresa
- Endereços IP e utilizadores SQL
- Código SQL completo de módulos (views, procs, functions, triggers) **no modo seguro**

Defaults e expressões de colunas calculadas fazem parte da estrutura da coluna e são incluídos.

## Manifesto

Campos principais:

- `schemaVersion` — versão do formato (`1.0.0`)
- `scannerVersion` — build do Scanner
- `originFingerprint` — SHA-256 truncado de servidor+BD (não reversível para nome legível)
- `snapshotHash` — SHA-256 canónico do conteúdo estrutural
- `completionStatus` — `Succeeded` / `SucceededWithWarnings` / `Partial` / `Failed`
- `includesSqlDefinitions` / `safeForPublicShare`
- `classification` — contagens + política conservadora
- `warnings` / `limitations` / `categoryStatuses`

## Classificação de objectos

Reutiliza a política conservadora do inventário:

| Classe | Critério |
| --- | --- |
| `sql_system` | `is_ms_shipped = 1` |
| `candidate_custom` | prefixo de nome `u_` |
| `unknown` | restante (não se afirma “PHC standard”) |

Cada objecto guarda `classification`, `criterion` e `source`.

## Comparação

O comparador identifica:

- tabelas / campos / relações / índices adicionados ou removidos
- alterações de tipo, tamanho, nullable, default e computed

Não há UI visual nesta fase; o CLI `compare-schema-snapshot` imprime o resumo.

## Permissões SQL insuficientes

Se faltar `VIEW DEFINITION` ou acesso a `sys.*`, a categoria falha, fica registada em `categoryStatuses` e o manifesto passa a `Partial` (se houver outras categorias com dados). O snapshot parcial continua utilizável; as limitações ficam documentadas.

## Exemplo de fixture

`samples/database-schema-snapshot/` — snapshot sintético gerado pelos testes, sem dados de cliente.
