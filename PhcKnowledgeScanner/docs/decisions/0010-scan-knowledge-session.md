# ADR 0010 — ScanKnowledgeSession (leitura de workspace)

- **Estado:** Aceite
- **Data:** 2026-11-03
- **Fase:** 1d.1

## Contexto

A projeção relacional existe (`RelationalKnowledgeProjector`), mas faltava um ciclo de vida explícito para abrir um `workspace.db` existente sem recolha nem escrita.

## Decisão

1. `ScanKnowledgeSession.OpenAsync` abre SQLite **read-only** (`SqliteOpenMode.ReadOnly`, `Pooling=false`, `PRAGMA query_only=ON`, sem `EnsureSchema`).
2. Carrega metadados + tabelas + projecta **uma vez**; fecha a ligação SQLite antes de devolver a sessão.
3. Consultas usam só memória (`IRelationalKnowledgeQuery` + inventário); após `DisposeAsync` → `ObjectDisposedException`.
4. Sem estado estático; cada abertura é uma sessão independente.
5. Sem UI, ConsoleApp, NuGet ou materialização SQLite nesta fase.

## Consequências

- Workspaces de validação (`run2`, `run3-pkfk`) podem ser explorados sem alterar o ficheiro.
- Desktop/ConsoleApp futuros consomem a sessão sem reabrir o projector por query.
