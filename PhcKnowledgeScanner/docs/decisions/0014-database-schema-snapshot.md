# ADR 0014 — Database Schema Snapshot

- **Estado:** Aceite
- **Data:** 2026-11-04

## Contexto

O collector `sql.server.structure` materializa inventário leve no workspace SQLite (tabelas, PK/FK, views/procs/triggers com definições SQL). Isso serve o explorador Desktop, mas:

1. não produz um pacote JSON estável para versionar no GitHub;
2. omite colunas/índices em massa e funções/UDT/synonyms;
3. exporta por omissão código SQL de módulos (inadequado a partilha pública/segura).

## Decisão

1. Criar um **fluxo dedicado** `schema-snapshot` (CLI + Desktop), independente do scan completo e do ZIP de personalizações.
2. Extrair metadados via catálogo SQL (`sys.*`) para o formato `database-schema/` (JSON + `manifest.json`).
3. **Modo seguro por omissão:** sem definições SQL completas; validação de privacidade bloqueia publicação automática se forem detetadas.
4. Reutilizar classificação conservadora (`sql_system` / `candidate_custom` / `unknown`) e a ligação SQL existente.
5. Não alterar o comportamento do ZIP de personalizações nem do collector estrutural actual.

## Consequências

- Schema PHC pode ser preservado offline no GitHub sem dados comerciais.
- Comparação entre snapshots fica preparada (`DatabaseSchemaSnapshotComparer`).
- Definições SQL completas continuam disponíveis apenas com flag explícita.
