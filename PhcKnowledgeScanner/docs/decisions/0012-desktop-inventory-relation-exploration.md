# ADR 0012 — Desktop: exploração de relações (Fase 1d.2.2)

## Decisão

- `InventoryRelationExplorer` (Application) consulta `IRelationalKnowledgeQuery` (`GetKnowledgeRelations` / `GetProvenance`) e produz DTOs para a UI.
- Página Inventário: separadores saída/entrada, detalhe com evidências FK/REL/DTL vs interpretação, navegação só se `TableReferenceResolution.Resolved`.
- `TableSearch`: prioridade exact → prefix → contains(name) → schema/objectId.
- Sem FieldMappings na UI, sem grafo, sem reprojectar.

## Nota run2

Workspace de validação sem evidências `SqlForeignKey` (0); FK coberto por testes com fixtures.
