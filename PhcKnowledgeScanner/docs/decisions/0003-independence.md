# ADR 0003 — Independência do ConsoleApp / Scanner V1

- **Estado:** Aceite
- **Data:** 2026-10-30

## Contexto

Existe um Scanner V1 dentro de `ConsoleApp`. O pedido explícito é um projeto e repositório **totalmente independentes**.

## Decisão

- Pasta irmã: `PhcKnowledgeScanner/` (não dentro de `ConsoleApp/`)
- Solução, Git e GitHub próprios
- **Proibidas** project references, partilha de código por link e alterações ao `ConsoleApp` neste âmbito
- Consulta futura ao V1 apenas como referência documental/técnica, com justificação se algum padrão for reutilizado conscientemente

## Consequências

- Possível duplicação deliberada de ideias (não de binários)
- Evolução e versionamento desacoplados
