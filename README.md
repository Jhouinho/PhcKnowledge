# PHC Knowledge

Plataforma de conhecimento técnico sobre PHC, destinada a recolher, organizar, relacionar, pesquisar e reutilizar conhecimento com proveniência, evidências e contexto.

O repositório reúne a plataforma PHC Knowledge e o componente de recolha **PhcKnowledgeScanner**. O Scanner mantém autonomia funcional e executável, mas o seu código é versionado neste mesmo repositório.

## Estrutura

```text
PhcKnowledge/
├── PhcKnowledge.sln
├── global.json
├── src/                       # plataforma PHC Knowledge (em desenvolvimento)
├── tests/                     # testes da plataforma
├── tools/                     # ferramentas e validação do contrato IKP
├── PhcKnowledgeScanner/       # aplicação de recolha PHC autónoma
│   ├── src/
│   ├── tests/
│   ├── tools/
│   └── docs/
└── docs/                      # arquitetura e contrato de importação
```

## Limites entre componentes

- **PhcKnowledgeScanner:** liga-se às instalações PHC em modo de leitura, recolhe metadados e personalizações, mantém o workspace local e produz artefactos/pacotes.
- **PHC Knowledge:** recebe, valida e importa pacotes IKP; mantém o conhecimento central, histórico, identidade e proveniência.
- A plataforma não executa o Scanner em runtime nem acede ao workspace SQLite do Scanner.
- Ambos evoluem no mesmo repositório, mas mantêm projetos, dependências, testes e responsabilidades separados.

## Desenvolvimento

Requisito: .NET SDK 10.0.403 ou posterior compatível com `global.json`.

A partir da raiz do repositório:

```bash
dotnet restore PhcKnowledge.sln
dotnet build PhcKnowledge.sln --no-restore
dotnet test PhcKnowledge.sln --no-build
```

Executar a aplicação desktop (Windows):

```bash
dotnet run --project PhcKnowledgeScanner/src/PhcKnowledgeScanner.Desktop
```

Executar um teste de ligação pela CLI (definir a password em `PHC_SCANNER_SQL_PASSWORD`, sem a colocar nos argumentos):

```bash
dotnet run --project PhcKnowledgeScanner/src/PhcKnowledgeScanner.Cli -- test-connection --server HOST\INSTANCE --database BD --auth sql --user UTILIZADOR
```

Também se mantém a solução específica do Scanner em `PhcKnowledgeScanner/PhcKnowledgeScanner.sln` para desenvolvimento isolado do componente.

## Estado da plataforma — 2026-11-18

A base de ingestão e o backend de acesso à plataforma estão implementados na branch `main`. A PR #15 foi integrada em 2026-11-18 pelo merge commit `6c619bfc`.

**Implementado no núcleo central:**
- Validação IKP 0.1, receção por serviço, arquivo imutável global content-addressed por SHA-256 e receipts/tentativas SQL org-scoped.
- Recovery de tentativas, limpeza de staging, monitorização operacional e preflight do schema no Host.
- API HTTP IKP: upload raw ZIP e consulta org-scoped de tentativas/pacotes, protegida por JWT Bearer e scopes quando ativada.
- Organização canónica e ASP.NET Identity com roles `Administrator` e `Collaborator`.
- Portal backend: login/logout, sessão, antiforgery, validação contínua de SecurityStamp e API administrativa de utilizadores.
- CLI `PhcKnowledge.Install` para scripts SQL, migrations Identity, organização, roles e bootstrap do primeiro administrador.

**Validação registada:**
- Harness local opt-in com o pacote real `CSDefault202603`: receção, idempotência, arquivo e recuperação após interrupção simulada, com resultados e tempos em [Harness IKP real](docs/architecture/real-ikp-receipt-integration-harness-2026-11-18.md).
- GitHub Actions no commit da PR #15: workflow .NET #253 e workflow SQL Server integration #39 concluídos com sucesso.
- No workflow SQL Server: 64 testes descobertos, 62 aprovados e 2 ignorados por dependerem do ZIP real, que não é disponibilizado em CI.

**Limites atuais — não confundir backend com produto completo:**
- Ainda não existe UI React do Portal. A experiência e navegação serão discutidas antes de iniciar a implementação.
- A análise prévia do ZIP, staging de análise e confirmação explícita de importação ainda não estão implementados.
- A organização canónica e o isolamento dos receipts existem; gestão de Customers, Installations, Environments e associação/correção de contexto ainda não estão implementadas.
- Capabilities, coverage, issues, artefactos, Object Index e REL/DTL são validados/preservados no pacote, mas ainda não estão materializados integralmente em entidades próprias do domínio 0.2.
- Observation, identidade lógica entre capturas, diff, pesquisa contextual e RAG continuam por construir.
- Antes de produção, falta fechar a persistência/proteção das chaves Data Protection e reforçar a garantia de que chamadas internas ao serviço de ativação não conseguem contornar o bloqueio administrativo.

O modelo de destino 0.2 continua em [Modelo de domínio 0.2](docs/architecture/knowledge-domain-model-0.2.md). O estado faseado, as dependências e os próximos marcos estão no [roadmap visual](docs/architecture/roadmap-visual-phc-knowledge-2026-11-06.md). O fecho da etapa Identity/Portal está registado em [Relatório de fase — Identity e Portal](docs/architecture/phase-report-identity-portal-2026-11-18.md).

A próxima decisão de produto é discutir a experiência do Portal antes de iniciar React. Em paralelo, devem ser tratados os pontos de hardening operacional e de segurança identificados; não antecipar materialização nem RAG.
