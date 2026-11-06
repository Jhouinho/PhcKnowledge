# ADR 0005 — Interpretação de expressões declaradas (V1)

- **Estado:** Aceite
- **Data:** 2026-11-03

## Contexto

Os factos `rel`/`dtl` preservam `tliga`/`expliga` em texto. É necessário pesquisar e analisar a estrutura lógica sem alterar os factos nem executar SQL das expressões.

## Decisão

1. Camada independente: facto (`DeclaredRelationRecord`) ≠ interpretação (`DeclaredRelationInterpretationRecord` + AST) ≠ resolução de tabelas (reutiliza `TableReferenceResolver`).
2. Parser determinístico `declared-expr-v1` (tokenizer + descent); sem regex como mecanismo principal.
3. AST preserva `AND`/`OR`/parênteses; não achata `OR` em `AND`.
4. Persistência em `declared_relation_interpretations` (upsert por `relation_id`); reinterpretação sem nova recolha PHC.
5. Confiança distinta do estado de parsing; existência física de colunas **não** validada nesta V1.

## Consequências

- Workspaces antigos ganham a tabela no `EnsureSchema` sem perder factos.
- Expressões com funções SQL, operadores `+`, aspas tipográficas, etc. ficam Invalid/Unsupported/Partial até V2.
- Próximas fases: inventário de colunas, validação semântica, grafo.
