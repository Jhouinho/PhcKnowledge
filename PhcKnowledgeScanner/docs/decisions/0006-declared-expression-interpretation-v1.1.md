# ADR 0006 — Interpretação de expressões declaradas (V1.1)

- **Estado:** Aceite
- **Data:** 2026-11-03
- **Parser:** `declared-expr-v1.1`

## Contexto

A V1 classificava incorrectamente como `Invalid` expressões PHC válidas com literais `´…´`, `+` e `LEFT(...)`. Faltavam diagnósticos estruturados para divergências de nomes e métricas qualified/unqualified.

## Decisão

1. Extender a gramática: literais tipográficos PHC (`´`), operador binário `+`, chamadas de função com argumentos, vírgulas.
2. AST: `BinaryExprNode`, `FunctionCallExprNode`; enums serializados como texto (leitura compatível com ints V1).
3. Recuperação parcial: construções desconhecidas → `PartiallyParsed` com nós preservados; `Invalid` só para malformação.
4. Diagnósticos: `MissingTable`, `NameMismatch`, `Ambiguous`, `UnqualifiedField` — sem fuzzy matching.
5. Métricas: total = qualified + unqualified; qualified = resolved + ambiguous + unresolved.
6. Reinterpretação por `parser_version` sem nova recolha PHC.

## Resultado interh (719)

| | V1 | V1.1 |
|---|---:|---:|
| Parsed | 717 | 719 |
| Invalid | 2 | 0 |
| High | 707 | 709 |
| Medium | 10 | 10 |
| Field refs | 1524 | 1530 |

RA→FO/FT passam a `Parsed`. Factos e expressões originais intactos.
