# ADR 0007 — Inventário completo de PK e FK

- **Estado:** Aceite
- **Data:** 2026-11-03
- **Fase:** 1a (Camada de Conhecimento Relacional)

## Contexto

O inventário de tabelas já cobre todas as tabelas físicas (`ADR 0004`). PK/FK em `sql.server.structure` continuavam filtradas por `LIKE N'u[_]%'`, o que produzia inventário incompleto das relações físicas (ex.: 0 FK em `interh` apesar de constraints existirem).

## Decisão

1. Remover o filtro `u_%` **apenas** das consultas de PK e FK.
2. Manter o filtro `u_%` em triggers (volume/ruído; fora do âmbito 1a).
3. IDs estáveis inalterados: `pk:{schema}.{table}.{name}`, `fk:{schema}.{table}.{name}`, `rel:fk:{fkObjectId}`.
4. Enriquecer `summary_json` com colunas + ordinal (PK/FK) e acções referenciais (FK); sem novas tabelas SQLite.
5. 0 FKs na BD continua a ser sucesso (não é erro).

## Consequências

- Factos físicos completos para a projeção de conhecimento (Fase 1b).
- Workspaces antigos abrem; nova recolha actualiza por upsert sem duplicar.
- Triggers de tabelas standard continuam fora do inventário detalhado.
