# Fronteiras de produto — Scanner, PHC Knowledge e investigação

Fonte consolidada a partir de [`../sources/analisar-base-phc-2026-11.txt`](../sources/analisar-base-phc-2026-11.txt).  
Estado: orientação de produto aceite. Stack e modelo de utilização centralizado: ver [`technology-and-import-contract-assessment.md`](technology-and-import-contract-assessment.md).

## Objetivo do PHC Knowledge

Plataforma de conhecimento técnico reutilizável, contextualizado por instalação e sustentado em evidências verificáveis.

Capacidades desejadas (visão):

- Encontrar implementações semelhantes realizadas anteriormente.
- Compreender como funcionalidades e comportamentos PHC são produzidos.
- Identificar dependências e impactos antes de alterações.
- Relacionar documentação oficial PHC com a realidade de cada instalação.
- Recuperar problemas, causas, soluções e decisões técnicas anteriores.
- Apoiar consultores e programadores com informação fundamentada.

Não é: clone do PHC, explorador SQL genérico, repositório de ficheiros, nem chatbot com documentação solta.

## Separação de responsabilidades

### Scanner (`PhcKnowledgeScanner`)

- Recolhe informação técnica de instalações.
- Interpreta estruturas e expressões só com regras determinísticas.
- Preserva evidências, proveniência e incertezas.
- Não inventa significado funcional.
- Pode permanecer como produto independente, transportável para servidores de clientes.
- Exportação (ZIP e/ou JSON) é a ponte para o PHC Knowledge.

### PHC Knowledge (esta plataforma)

- Relaciona informação de várias fontes.
- Representa implementações, comportamentos, dependências e conhecimento validado.
- Reutiliza conhecimento standard entre organizações; isola conhecimento de instalação.
- Consome artefactos exportados; não depende de correr o Scanner em runtime.

### Motor de investigação / IA (futuro)

- Recebe perguntas, cruza informação, formula explicações.
- Apresenta evidências e marca o que não está confirmado.
- Não substitui factos nem inventa proveniência.

## Decisões de produto já expressas

1. O Scanner usado para extrair instalações é o `PhcKnowledgeScanner`.
2. Preferência por Scanner independente; o PHC Knowledge trabalha sobre dados exportados.
3. Exportação deve servir importação no PHC Knowledge (além do ZIP atual).
4. Ferramenta de recolha deve ser simples de transportar (ligação, progresso, exportação).
5. Evolução começa por conhecimento estruturado, pesquisável e rastreável — não por chatbot.

## Domínios de conhecimento

| Domínio | Pergunta | Exemplos |
| --- | --- | --- |
| Estrutural | Como está organizada a instalação? | Tabelas, campos, relações, chaves, estruturas `u_*` |
| Comportamental | Como funciona uma operação? | Eventos, regras, procedures, udefs, configs |
| Implementação | Como foi feita uma solução? | Objetivo, componentes, decisões, limites, validação |
| Experiência | O que aprendemos antes? | Problemas, causas, correções, versões afetadas |

## Casos de utilização de referência

Usados para avaliar arquitetura (não como backlog imediato):

1. Encontrar implementações semelhantes entre clientes.
2. Avaliar impacto de alterar um evento / componente.
3. Explicar um comportamento PHC (standard + personalizações + configs).
