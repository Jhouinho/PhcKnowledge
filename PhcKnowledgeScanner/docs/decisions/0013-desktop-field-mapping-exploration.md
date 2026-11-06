# ADR 0013 — Desktop: FieldMappings no detalhe da relação (Fase 1d.2.3)

## Decisão

- `InventoryRelationExplorer.BuildDetail` / `BuildFieldMappings` obtém mappings via `GetFieldMappings(knowledgeRelationId)`.
- DTOs `FieldMappingView` preservam Kind, LogicalContext, operandos, resolução e proveniência REL/DTL.
- UI: secção «Mapeamentos de campos» no detalhe da relação (lista + detalhe).
- Sem inferência de chaves, sem grafo, sem reprojectar.
