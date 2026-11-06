# ADR 0009 — Extração de FieldMapping (V1)

- **Estado:** Aceite
- **Data:** 2026-11-03
- **Fase:** 1c

## Contexto

As interpretações V1.1 já materializam AST + resolução de qualificadores. É necessário expor correspondências entre campos sem inferir chaves nem reexecutar o parser.

## Decisão

1. Extracção em memória (`FieldMappingExtractor`) a partir da AST persistida; origem obrigatória = `EvidenceId` REL/DTL.
2. Tipos: `DirectFieldEquality` (`=` + FieldRef/FieldRef); `ExpressionComparison` (`=` com função/binário); `NonFieldPredicate` (literais e operadores ≠ `=`).
3. Contexto: qualquer ancestral OR ⇒ `AlternativeUnderOr`; senão AND ou predicado-raiz ⇒ `RequiredUnderAnd`. AND **não** implica chave composta.
4. ID: `fm:{evidenceId}:{parserVersion}:{sha256(astPath)[0:8]}`.
5. Sem materialização SQLite; sem alteração ao parser V1.1.

## Consequências

- ConsoleApp consulta mappings sem parser.
- KnowledgeRelation consolidada pode agregar mappings de várias evidências sem deduplicar por nomes.
