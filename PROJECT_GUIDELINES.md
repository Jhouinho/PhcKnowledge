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

Decisões consolidadas em [`docs/architecture/technology-and-import-contract-assessment.md`](docs/architecture/technology-and-import-contract-assessment.md) (§4–§5 e §11; revisão de 2026-11-10):

| Área | Decisão |
| --- | --- |
| Utilização | Centralizada no servidor da organização; clientes browser/desktop |
| Backend | .NET 10 + ASP.NET Core |
| Web | React + TypeScript |
| Desktop plataforma | Windows-first, mesma SPA via WebView2 (cliente do backend central) |
| BD central | SQL Server Express (central, incluindo instalação on-premises) |
| SQLite | Apenas workspace Scanner / cache auxiliar (não substitui SQL Server Express) |
| Pesquisa inicial | SQL Server 2025 Express inclui Full-Text Search; versão concreta ainda por fixar e validar se for anterior a 2025 |
| Tempo real | SignalR (mantido na arquitetura; implementação conforme os casos de uso) |
| Scanner | Componente funcionalmente autónomo, versionado neste monorepo; Knowledge não o executa em runtime |
| Importação | IKP 0.1 emitido pelo Scanner; primeira fatia Receive / Validate / Archive implementada no projeto Import, sem API HTTP/autenticação demonstradas |

O Scanner já implementa o exportador IKP 0.1 e os comandos CLI `scan-ikp` e `export-ikp`. O núcleo PhcKnowledge.Import já implementa validação, arquivo imutável e persistência de receipts. O contrato deve evoluir sempre com schemas, fixtures e testes. Não ligar o Scanner ao backend em runtime.

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
- `docs/architecture/observation-identity-and-lineage-contract.md` (contexto arquitetural; semântica normativa em P1 e IKP 0.1)
- `docs/architecture/ikp-0.1-scanner-compatibility-matrix.md` (lacunas e trabalho necessário no Scanner atual)


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

## Estado e próxima etapa — 2026-11-17

A primeira fatia de ingestão já existe no código:
- IkpPackageValidator: valida estrutura, schemas, checksums e referências do pacote;
- IkpPackageIngestionService: coordena validação, arquivo e receipt;
- FileSystemImmutablePackageStore: arquivo content-addressed global por SHA-256, sem overwrite;
- SqlServerPackageReceiptRepository: receipt por organização no SQL Server;
- migration 001_package_ingestion_receipts.sql;
- testes unitários e de integração sobre fixture IKP / ZIP sintético → Validator / Ingestion (aceitação do pacote válido, arquivo imutável idempotente, registo de receipt por organização, rejeição de path traversal e payload adulterado).

A implementação não demonstra ainda endpoint HTTP, autenticação/contexto de Organization, execução contra SQL Server real nem reconciliação automática de ficheiros arquivados sem receipt. A ordem actual é arquivo antes de receipt SQL; esta janela de falha deve ser tratada antes de ampliar a ingestão.

O modelo de destino 0.2 está em [knowledge-domain-model-0.2.md](docs/architecture/knowledge-domain-model-0.2.md). Distinguir sempre:
- **Implementado:** serviço Receive / Validate / Archive, arquivo imutável e receipt SQL.
- **Parcial / apenas no ZIP:** capabilities, coverage, issues, artefactos e REL/DTL; não materializados ainda em tabelas próprias.
- **Futuro:** API HTTP/autenticação, Core Organization/Installation/Environment, materialização de Observations, contexto, identidade, comparação e conhecimento.

Não alterar migrations globais antes de fechar os bloqueadores de domínio documentados no modelo 0.2. A próxima etapa é testar SQL Server real e desenhar reconciliação/retry e fronteira HTTP/autenticação.
