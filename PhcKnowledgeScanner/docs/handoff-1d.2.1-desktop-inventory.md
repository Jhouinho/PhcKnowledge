# Handoff — Fase 1d.2.1 (Desktop inventário)

## Integração

- Desktop (code-behind) → `WorkspaceSessionHost` → `ScanKnowledgeSession.OpenAsync` (RO, project-once).
- Página **Inventário** separada do Explorer (`u_*`).
- Ciclo de vida: host na `MainWindow`; `CloseSessionAsync` / `DisposeAsync` no fecho da janela.

## Validação run2 (cópia)

- ScanId: `9ece6cad736140de8f370987f3e83c2c`
- Tabelas: **8229**; hits `bo` / `cl` / `u_*`
- SHA-256: `80A2BF010AC1FA6C534C2B41B47C184BFDD896F658F1442D30C3DF9097AF6880` (antes = depois)

## Fora de âmbito (próximo)

KnowledgeRelation UI, FieldMappings, grafo, ConsoleApp.
