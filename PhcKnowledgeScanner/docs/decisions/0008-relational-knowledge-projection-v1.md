# ADR 0008 — Projeção RelationEvidence / KnowledgeRelation (V1)

- **Estado:** Aceite
- **Data:** 2026-11-03
- **Fase:** 1b

## Contexto

Factos FK, REL/DTL e interpretações vivem em tabelas SQLite distintas. É necessária uma vista uniforme e consolidação conservadora sem materializar novas tabelas nem reexecutar o parser.

## Decisão

1. Projeção em memória (`RelationalKnowledgeProjector` → `RelationalKnowledgeSnapshot`).
2. Evidências: `SqlForeignKey`, `PhcRel`, `PhcDtl`, `ExpressionInterpretation` (esta última complementar ao facto declared).
3. Consolidação só com `CorrespondenceKind.Exact` persistido; Probable/None/FK → singleton; FK ≠ REL por endpoints.
4. IDs: `kr:exact:{correspondenceGroupId}`, `kr:declared:{relationId}`, `kr:fk:{fkObjectId}`; interp `interp:{relationId}:{parserVersion}`.
5. Exact KR id segue o group id (nomes+hash expressão) — mudança de resolução table:* não altera IDs Exact.
6. Diagnósticos: união por evidência, sem score global.

## Consequências

- ConsoleApp pode consultar sem parser/consolidação própria.
- Workspaces existentes sem schema novo.
- FieldMapping fica para Fase 1c.
