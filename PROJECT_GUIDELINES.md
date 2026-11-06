# PHC Knowledge — Orientações do projeto

Documento central de orientação. Linguagem de documentação: Português de Portugal (pt-PT).

## Objetivo

O PHC Knowledge é uma plataforma de conhecimento técnico sobre PHC, destinada a recolher, organizar, relacionar, pesquisar e reutilizar conhecimento.

Não é apenas um repositório de ficheiros, uma coleção de código, um clone da aplicação PHC, nem uma simples ferramenta de pesquisa SQL.

Pretende-se uma base de conhecimento estruturada, com proveniência, evidências, relações, dependências e contexto técnico — integrando conhecimento PHC standard com conhecimento específico de instalações e desenvolvimentos personalizados.

## Decisões de produto

### Utilização

- Interface web (browser).
- Interface desktop (Windows-first, mesma SPA via WebView2).
- Instalação **centralizada** num servidor da organização (on-premises na rede local).
- Clientes ligam ao backend central; sem modo autónomo offline nem knowledge DB local independente.
- O mesmo modelo funcional pode, no futuro, ser alojado de forma central externa, sem obrigar a redesenho.

### Modelo de utilização

- Plataforma central partilhada por várias organizações.
- Cada organização terá os seus dados isolados.
- Um utilizador poderá pertencer a várias organizações.
- Vários consultores poderão trabalhar simultaneamente sobre a mesma instalação e o **mesmo** conhecimento partilhado no servidor.
- As alterações e descobertas deverão ficar disponíveis aos restantes utilizadores autorizados (sem sincronização entre bases autónomas).

### Conhecimento

- O conhecimento PHC standard deverá ser reutilizável entre organizações.
- O conhecimento específico de uma instalação deverá respeitar o isolamento da respetiva organização.
- Cada informação deverá preservar a sua proveniência.
- Distinguir factos confirmados, informação recolhida, inferências e conclusões ainda não validadas.

## Arquitetura tecnológica

Decisões consolidadas em [`docs/architecture/technology-and-import-contract-assessment.md`](docs/architecture/technology-and-import-contract-assessment.md) (§4–§5):

| Área | Decisão |
| --- | --- |
| Utilização | Centralizada no servidor da organização; clientes browser/desktop |
| Backend | .NET 10 + ASP.NET Core |
| Web | React + TypeScript |
| Desktop plataforma | Windows-first, mesma SPA via WebView2 (cliente do backend central) |
| BD central | PostgreSQL (inclui on-premises) |
| SQLite | Apenas workspace Scanner / cache auxiliar (não substitui PostgreSQL) |
| Pesquisa inicial | PostgreSQL Full-Text Search (sem OpenSearch nesta fase) |
| Tempo real | SignalR |
| Scanner | Componente funcionalmente autónomo, versionado neste monorepo; Knowledge não o executa em runtime |
| Importação | IKP a partir do Database Schema Snapshot — especificação 0.1 em proposta ([`ikp-0.1-specification.md`](docs/architecture/ikp-0.1-specification.md); não aprovada automaticamente) |

A implementação do Scanner e da plataforma deve manter limites claros. Alterações ao contrato IKP devem ser acompanhadas por schemas, fixtures e testes. Não ligar o Scanner ao backend em runtime.

## Projetos anteriores

### Incluído neste repositório

- `PhcKnowledgeScanner/` — componente de recolha autónomo, incluído e versionado neste monorepo. Mantém projetos, solução, testes e dependências próprios, mas o repositório canónico de desenvolvimento é `Jhouinho/PhcKnowledge`.

### Ainda externo

- `ConsoleApp` — exploração de conhecimento e componentes associados.

Regras:

- Analisar antes de reutilizar código ou arquitetura do Scanner no núcleo da plataforma.
- Não recuperar automaticamente mecanismos ou modelos de processamento já abandonados.
- Preservar conhecimento PHC standard já confirmado; não confundir com personalizações de clientes.
- Aproveitar apenas o que demonstrar valor real para a nova solução.

## Estrutura do repositório

```text
PhcKnowledge/
├── README.md
├── PhcKnowledge.sln          # solução agregadora na raiz
├── global.json
├── PROJECT_GUIDELINES.md
├── .gitignore
├── docs/
│   ├── architecture/   # decisões e princípios consolidados
│   └── sources/        # fontes brutas (proveniência; não canónicas)
├── src/
├── tests/
├── tools/
└── PhcKnowledgeScanner/   # componente Scanner autónomo, no mesmo repositório
```

Não criar pastas adicionais sem justificação.

Documentos de arquitetura relevantes:

- `docs/architecture/product-boundaries.md`
- `docs/architecture/knowledge-layers.md`
- `docs/architecture/extract-2026-11-conversation.md`
- `docs/architecture/technology-and-import-contract-assessment.md` (stack + utilização centralizada)
- `docs/architecture/ikp-0.1-specification.md` (contrato IKP 0.1 consolidado para revisão técnica)
- `docs/architecture/observation-identity-and-lineage-contract.md` (princípios validados de observação, identidade e linhagem)
- `docs/architecture/ikp-0.1-scanner-compatibility-matrix.md` (lacunas e trabalho necessário no Scanner atual)
- `docs/architecture/observation-identity-and-lineage-contract.md` (princípios validados de cobertura, identidade, observações, histórico e linhagem)

## Regras de trabalho

### Desenvolvimento controlado

- Não implementar funcionalidades sem uma direção previamente validada.
- Não assumir decisões arquiteturais que ainda estejam em aberto.
- Não adicionar tecnologias ou dependências sem justificação.
- Privilegiar simplicidade, modularidade e capacidade de evolução.

### Aproveitamento de conhecimento anterior

- Analisar antes de reutilizar.
- Não transportar automaticamente código ou arquitetura antiga.
- Preservar conhecimento PHC standard já confirmado.
- Não confundir conhecimento standard com personalizações de clientes.

### Documentação

- Evitar documentação excessiva.
- Não criar documentos intermédios sem valor efetivo.
- No final de cada etapa, consolidar num relatório conciso: o que foi feito, implementado, validado, problemas encontrados, estado atual e próximo passo.
- Evitar múltiplos documentos que descrevam o mesmo assunto.

### Reflexão obrigatória

No final de cada fase:

- Avaliar o estado real do projeto.
- Identificar o que existe e funciona.
- Identificar o que falta.
- Registar o que aprendemos.
- Avaliar se a arquitetura continua adequada.
- Discutir o próximo passo com o responsável do projeto.

Não avançar automaticamente para a fase seguinte sem validação conjunta.

## Próxima etapa prevista

O contrato IKP 0.1 já dispõe de schemas, fixture verificável, validador e testes positivos/negativos. A próxima etapa é implementar gradualmente no PhcKnowledgeScanner (neste monorepo) a identificação da captura, capabilities, cobertura por operação e índice de objetos. Depois, evoluir a exportação para IKP e preparar a importação na plataforma, preservando a separação funcional entre Scanner e Knowledge.
