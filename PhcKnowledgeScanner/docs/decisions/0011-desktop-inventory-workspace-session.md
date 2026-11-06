# ADR 0011 — Desktop: inventário via ScanKnowledgeSession (Fase 1d.2.1)

## Contexto

A aplicação WPF precisava de abrir um workspace SQLite existente e explorar o inventário de tabelas sem recolha PHC nem HTTP.

## Decisão

- `WorkspaceSessionHost` (Application) possui o ciclo de vida da `ScanKnowledgeSession` activa: abertura serializada, falha preserva sessão anterior, `CloseSessionAsync` sem dispor o host, `DisposeAsync` no fecho da janela.
- Página **Inventário** no Desktop (code-behind existente): `OpenFileDialog` → `OpenAsync`, pesquisa em memória via `FindTables`, detalhe a partir de `TechnicalObjectRecord`.
- Sem ViewModel novo; sem singleton global; Explorer continua filtrado a `u_*`.

## Consequências

- Base pronta para 1d.2.2 (relações / FieldMappings) sobre a mesma sessão.
- Lógica testável no host; UI WPF fora da suite automatizada.
