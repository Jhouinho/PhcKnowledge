# ADR 0002 — Estrutura inicial de projetos

- **Estado:** Aceite (fase 1) — sujeita a revisão após o primeiro collector real
- **Data:** 2026-10-30

## Contexto

O Scanner V1 no `ConsoleApp` fragmentou-se em muitos projetos `Sources.*`. O novo scanner precisa de extensibilidade sem over-engineering inicial.

## Decisão

Criar quatro projetos de produção + um de testes:

1. `PhcKnowledgeScanner.Abstractions`
2. `PhcKnowledgeScanner.Engine`
3. `PhcKnowledgeScanner.Export`
4. `PhcKnowledgeScanner.Cli`
5. `PhcKnowledgeScanner.Engine.Tests`

Collectors PHC **não** têm projetos dedicados nesta fase.

## Justificação

- Cada projeto tem responsabilidade concreta e testável.
- Evita copiar a granularidade do V1 sem necessidade demonstrada.
- Permite acrescentar `Collectors.<Área>` mais tarde sem reescrever contratos.

## Consequências

- Novos módulos de recolha devem começar por implementar `ICollector`.
- Extração para assemblies separados só quando houver dependências ou ownership claros.
