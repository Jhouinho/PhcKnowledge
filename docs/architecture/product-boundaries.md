# Fronteiras de produto — Scanner, PHC Knowledge e investigação


> **Actualização de decisões — 21-11-2026:** a separação Scanner/Knowledge mantém-se. O modelo actual é uma organização interna, clientes como contextos, uma instalação por base de dados PHC registada e várias capturas por instalação. O ZIP é transporte temporário; o SQL Server central guarda dados e artefactos permanentes. D-015/D-016 e regras completas em [Decisões aprovadas](decisoes-aprovadas-2026-11-21.md).

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
- Recebe, valida e importa pacotes IKP 0.1; mantém o conhecimento central, histórico, identidade e proveniência.
- Não depende de correr o Scanner em runtime nem acede ao workspace SQLite.

### Motor de investigação / IA (futuro)

- Recebe perguntas, cruza informação, formula explicações.
- Apresenta evidências e marca o que não está confirmado.
- Não substitui factos nem inventa proveniência.

## Decisões de produto já expressas

1. O Scanner usado para extrair instalações é o `PhcKnowledgeScanner`.
2. O Scanner é autónomo na execução e na responsabilidade de aquisição; o PHC Knowledge trabalha sobre os pacotes exportados. Ambos são versionados neste monorepo.
3. Exportação deve servir importação no PHC Knowledge (além do ZIP atual).
4. Ferramenta de recolha deve ser simples de transportar (ligação, progresso, exportação).
5. Evolução começa por conhecimento estruturado, pesquisável e rastreável — não por chatbot.
6. O conhecimento deve compreender dependências funcionais e transitivas entre objetos, distinguindo referências detetadas, dependências confirmadas e inferências; deve permitir avaliar o que falta ou precisa de adaptação ao reutilizar um objeto noutra instalação.

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


## Estado de implementação — 2026-11-10

- **Scanner:** componente autónomo existente; exportador IKP 0.1 integrado na CLI.
- **IKP:** contrato, schemas, fixtures, validador e testes existentes. A projeção do Object Index foi validada com uma captura PHC real; o ciclo completo do ZIP do exportador com captura real ainda carece de confirmação.
- **PHC Knowledge:** a plataforma central ainda não tem projetos próprios em `src/` nem testes em `tests/`. A primeira fatia aprovada para implementação é Receive / Validate / Archive.
- **Fora da primeira fatia:** associação de instalação, materialização de observações, comparação histórica, dependências funcionais e publicação.


## Direcção aprovada — 21-11-2026

O Scanner recolhe uma captura completa dentro do âmbito declarado, sem consultar o histórico do Knowledge, e transporta evidência, conteúdo original disponível, metadados, fingerprints e cobertura. Mantém a recolha de REL/DTL. O Knowledge compara com o catálogo PHC Standard e com o histórico da mesma instalação, materializa incrementalmente e só conclui remoções com cobertura comprovadamente completa. O ZIP é eliminado depois da materialização SQL verificada.
