# ADR 0004 — Inventário completo de tabelas físicas

- **Estado:** Aceite
- **Data:** 2026-11-03

## Contexto

`sql.server.structure` materializava apenas tabelas `u_*`. As relações declaradas (`dbo.rel` / `dbo.dtl`) apontam sobretudo para tabelas PHC standard, ficando por resolver no workspace.

## Decisão

1. Inventário leve de **todas** as tabelas via `sys.tables` + `sys.schemas` (sem colunas/índices das standard nesta fase).
2. Identidade estável `table:{schema}.{name}`; upsert sem duplicar.
3. Classificação conservadora em `summary_json`: `sql_system` | `candidate_custom` | `application_or_unknown`.
4. Resolução de refs declaradas: inequívoca → Resolved; nomes repetidos em schemas → Ambiguous; em falta → Unresolved. Expressão original preservada.
5. ~~PK/FK/triggers de detalhe continuam limitados a `u_*` até fase posterior.~~ **Actualizado:** PK/FK completas em `ADR 0007`; triggers de detalhe mantêm filtro `u_*`.

## Consequências

- Tabelas standard passam a existir no workspace e desbloqueiam resolução de `phc.declared-relations`.
- Próximas fases: parser de `tliga`/`expliga`, grafo, recolha detalhada de colunas standard.
