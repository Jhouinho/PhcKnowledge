# PHC Knowledge — Orientações do projeto

Documento central de orientação. Linguagem de documentação: Português de Portugal (pt-PT).

## Objetivo

O PHC Knowledge é uma plataforma de conhecimento técnico sobre PHC, destinada a recolher, organizar, relacionar, pesquisar e reutilizar conhecimento.

Não é apenas um repositório de ficheiros, uma coleção de código, um clone da aplicação PHC, nem uma simples ferramenta de pesquisa SQL.

Pretende-se uma base de conhecimento estruturada, com proveniência, evidências, relações, dependências e contexto técnico — integrando conhecimento PHC standard com conhecimento específico de instalações e desenvolvimentos personalizados.

## Decisões de produto

### Organização e utilização

- Existe uma única organização: a empresa utilizadora da plataforma.
- A plataforma é reservada aos colaboradores, com contas individuais.
- Cada cliente PHC tem um registo próprio e é contexto técnico, não tenant nem organização de autenticação.
- Cada base de dados PHC registada tem a sua própria instalação e `InstallationId`.
- Cada instalação pode ter várias capturas; cada captura pertence exclusivamente a uma instalação.
- Ambiente e localização são inicialmente atributos da instalação.
- Uma cópia de uma base de dados recebe nova identidade de instalação, com linhagem para a origem.
- Mudar de servidor não implica nova instalação se a continuidade da mesma base de dados puder ser confirmada.
- A plataforma é centralizada num Windows Server da empresa, acessível por LAN/VPN. A autenticação interna não deve depender de Internet.
- O Scanner é autónomo e a transferência para o Knowledge é manual através do Portal.

### Conhecimento

- Separar PHC Standard, observações de instalações, personalizações e interpretações derivadas.
- Personalizações de um cliente não são partilhadas automaticamente com outros.
- Toda a informação preserva origem, instalação, captura, cobertura e proveniência.
- A ausência só pode significar remoção quando a operação teve sucesso e a cobertura é completa.

## Arquitetura tecnológica

Decisões actuais: [Decisões arquitecturais aprovadas — 21-11-2026](docs/architecture/decisoes-aprovadas-2026-11-21.md). A avaliação tecnológica é fonte histórica de investigação e deve ser lida subordinada a essa referência.

| Área | Decisão |
| --- | --- |
| Utilização | Centralizada no Windows Server da empresa; acesso LAN/VPN; clientes PHC como contextos |
| Backend | .NET 10 + ASP.NET Core |
| Web | React + TypeScript |
| Desktop plataforma | Windows-first, mesma SPA via WebView2 (cliente do backend central) |
| BD central | Microsoft SQL Server central (edição/versão por decidir; não assumir Express) |
| SQLite | Apenas workspace/cache local do Scanner |
| Pesquisa inicial | Pesquisa inicial sobre SQL Server; edição/versão e disponibilidade de Full-Text Search por validar |
| Tempo real | SignalR (mantido na arquitetura; implementação conforme os casos de uso) |
| Scanner | Componente funcionalmente autónomo, versionado neste monorepo; Knowledge não o executa em runtime |
| Importação | IKP 0.1 emitido pelo Scanner; recepção/análise HTTP existentes; integração de upload com sessão do Portal e materialização ainda por fechar |

O Scanner já implementa o exportador IKP 0.1 e os comandos CLI `scan-ikp` e `export-ikp`. O núcleo PhcKnowledge.Import já implementa validação, recepção e persistência de receipts; o arquivo actual do ZIP é transitório e será removido após materialização verificada. O contrato deve evoluir sempre com schemas, fixtures e testes. Não ligar o Scanner ao backend em runtime.

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
- **A cada decisão aprovada**, actualizar imediatamente a referência canónica de decisões e todos os documentos activos afectados; corrigir instruções contraditórias e actualizar README/roadmap quando aplicável. Preservar relatórios históricos, identificando-os como fotografias anteriores. Não deixar decisões aprovadas apenas na conversa.
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

## Estado e próxima etapa — 2026-11-18

A PR #15 (fases 1.2 a 1.4.2.E — Identity/Portal backend) foi integrada na `main` em 2026-11-18. Merge commit: `6c619bfc6aa78ba1cd694c48f2d71c71c71aa4b8`.

A plataforma central já contém:
- Pipeline IKP de validação, ingestão, arquivo imutável, receipts/tentativas SQL, recovery, cleanup e monitorização operacional.
- API HTTP IKP com upload raw ZIP e consultas org-scoped, protegida por JWT Bearer/scopes quando ativada.
- Organização canónica e ASP.NET Identity com roles Administrator/Collaborator.
- Portal backend com cookie, login/logout, sessão, antiforgery, SecurityStamp e API de administração de utilizadores.
- CLI de instalação para scripts SQL, migrations Identity, organização, roles e bootstrap de administrador.

Validação registada:
- Harness local opt-in com o pacote real CSDefault202603: receção, idempotência, arquivo e recovery, conforme `docs/architecture/real-ikp-receipt-integration-harness-2026-11-18.md`.
- GitHub Actions no commit da PR #15: .NET workflow #253 e SQL Server integration workflow #39 com sucesso. No workflow SQL: 64 descobertos, 62 aprovados e 2 ignorados por dependerem do ZIP real não disponibilizado em CI.

Limites atuais:
- Não existe UI React; discutir a experiência do Portal antes de iniciar implementação.
- Análise prévia do ZIP, staging de análise e confirmação de importação ainda não implementados.
- Customer/Installation/Environment e associação/correção do contexto ainda não implementados.
- Observation, materialização de REL/DTL/artefactos, identidade lógica, diff, pesquisa contextual e RAG continuam futuros.
- Persistência/proteção de chaves Data Protection e reforço da garantia do bloqueio administrativo são hardening pendente antes de produção.

O roadmap visual é a referência para estados e sequência. Não iniciar a fase seguinte automaticamente: apresentar a proposta e validar com o responsável do projeto. No final de cada etapa, atualizar o roadmap e produzir apenas um relatório conciso de fase, evitando documentação duplicada.


## Decisões aprovadas — revisão 2026-11-21

A referência normativa é [Decisões arquitecturais aprovadas](docs/architecture/decisoes-aprovadas-2026-11-21.md). Inclui o modelo Cliente → Instalação → Captura, D-015 (manter recolha REL/DTL e comparar com catálogo standard), D-016 (captura completa no âmbito declarado e materialização incremental), ZIP exclusivamente temporário e armazenamento permanente dos artefactos/dados no SQL Server central. Esta orientação prevalece sobre texto histórico deste ficheiro.


## Estado vigente após revisão global — 23-11-2026

Esta secção prevalece sobre as fotografias de estado anteriores deste documento. A baseline de código auditada foi a `main` no commit `31deb2badb91a2b0453099290eaddaaa6afd944e`; a consolidação documental desta revisão ficou integrada na `main` pelo commit `bf7d1854d766eeb0e2ca902da3ac77ac7f7e4eee`.

- A base central é Microsoft SQL Server; PostgreSQL está excluído. Edição/versão SQL Server continua por decidir.
- O Scanner é autónomo, vive em `PhcKnowledgeScanner/` neste monorepo e não é executado pelo Knowledge em runtime.
- IKP 0.1 mantém-se como contrato global; Object Index v2 e PackageImportAnalysis 0.3 estão implementados e integrados pela PR #27 (`ad42f53fc3d4f7b654d61dea56f0f501af0905b8`).
- Receive / Validate / Archive, API HTTP, receipts/recovery e análise IKP A+B estão na `main`.
- Os workflows .NET Knowledge, .NET Scanner, validação IKP e integração SQL Server 2022 passaram no CI associado ao merge D-022. Não existe, por esse facto, validação funcional em instalação PHC real.
- A UI React e a confirmação do Incremento C não estão na `main`; a PR #20 está aberta, mas a sua branch está divergente da `main` actual (97 commits à frente / 89 atrás no compare consultado). Reconciliar e rever antes de qualquer integração.
- Materialização persistente de Capture/Observation/REL/DTL/artefactos no SQL Server, verificação pós-materialização e eliminação do ZIP continuam por implementar.
- A próxima actividade técnica de domínio é fechar a matriz de classificação por collector/ObjectKind com base na captura real, sem alterar collectors, workspace SQLite ou materialização antes da validação e autorização correspondentes.

A sequência e o estado detalhado encontram-se em `docs/architecture/roadmap-visual-phc-knowledge-2026-11-06.md`; a decisão normativa encontra-se em `docs/architecture/decisoes-aprovadas-2026-11-21.md`. O relatório da revisão global está em `docs/audits/revisao-global-main-2026-11-23.md`.

A revisão documental não executou build/testes localmente, não validou uma instalação PHC real e não alterou código, schemas executáveis, collectors, SQLite, migrations ou materialização SQL.
