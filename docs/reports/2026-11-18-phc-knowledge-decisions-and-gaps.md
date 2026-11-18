# PHC Knowledge — matriz de decisões, lacunas e riscos

| Campo | Valor |
|---|---|
| Data | 2026-11-18 |
| Base de análise | `main` e branches consultadas |
| Estado | Proposta de consolidação para validação do responsável do projeto |

Esta matriz separa decisões já registadas, decisões que precisam de confirmação e trabalho técnico por executar. Uma referência documental ou implementação parcial não equivale automaticamente a aprovação de produto.

## 1. Decisões arquiteturais a preservar

| Tema | Direção registada | Estado |
|---|---|---|
| Objetivo do produto | Reutilização de conhecimento técnico PHC; não é clone de ERP nem Digital Twin | Consolidado |
| Experiência | Portal-first, documentação e UI pt-PT | Consolidado |
| Scanner | Ferramenta independente que produz pacotes IKP | Consolidado |
| Fronteira | Knowledge importa; não incorpora runtime de coletores Scanner | Consolidado |
| Discovery/Fusion | Não recuperar como dependência do produto | Consolidado |
| Deployment | Plataforma central por organização; browser e cliente desktop como interfaces | Consolidado |
| Offline e sincronização peer-to-peer | Fora do âmbito inicial | Consolidado |
| Evidência | Preservar artefactos e observações originais, com proveniência | Consolidado como princípio |
| Identificadores | CaptureId distinto de InstallationId, PackageId e identificadores de origem | Consolidado no contrato |
| Fingerprints | DefinitionFingerprint e MetadataFingerprint têm semânticas distintas | Consolidado em P1.B |
| SourceObjectId | Referência de objeto dentro da captura, não identidade permanente | Consolidado em P1.B |
| RAG | Objetivo futuro, dependente de pesquisa/proveniência fiáveis | Direção estratégica |

## 2. Decisões que exigem confirmação explícita

| ID | Decisão | Opções/questão | Porque bloqueia |
|---|---|---|---|
| D-01 | Base relacional central | PostgreSQL ou SQL Server Express? A documentação de stack atual aponta SQL Server; decisões anteriores apontavam PostgreSQL. Confirmar uma única opção. | Modelo físico, migrations, FTS, deployment e operação |
| D-02 | Baseline de versão SQL | Se SQL Server for confirmado, qual a versão mínima e edição suportada? | Compatibilidade e deployment |
| D-03 | Modelo de domínio 0.2 | Aprovar os documentos/diagramas da branch `architecture/domain-model-0.2-review` ou indicar alterações | Bloqueia implementação de Core |
| D-04 | Identidade nativa SQL | Incorporar o esquema de identidade da branch `feature/sql-native-object-identities-20261108`? | Afeta matching entre capturas |
| D-05 | Object Index performance | Integrar a otimização da branch `feature/ikp-object-index-schema-perf` após testes de equivalência? | Desempenho de validação de pacotes |
| D-06 | Armazenamento de pacotes | Confirmar filesystem on-prem como primeira implementação e abstração para object storage futuro | Deployment, retenção e recuperação |
| D-07 | Scanner Desktop | Confirmar se a experiência Desktop faz parte da entrega Scanner atual ou fica como evolução separada | Evita misturar roadmap do Scanner com o produto Knowledge |

## 3. Lacunas técnicas priorizadas

### Críticas — antes de avançar para Core

**G-01 — Prova ponta a ponta com captura real**

- Exportar uma captura real através do Scanner atual.
- Validar o pacote no Import independente.
- Confirmar todos os artefactos, checksums, coverage, capabilities, object index e relações declaradas.
- Testar arquivo, receipt e repetição idempotente.
- Não inventar dados nem alterar coletores/SQLite para acomodar resultados antes de validar a captura real.

Critério de fecho: relatório de execução com pacote real, resultados, exceções e limitações.

**G-02 — Modelo de domínio aprovado**

- Rever Organization, Installation, Environment e localização da base de dados.
- Rever Package, PackageReceipt, Capture e Observation.
- Separar SourceObjectId de identidade lógica persistente.
- Definir estados e transições, incluindo captura parcial e associação pendente.
- Definir constraints, unicidade, chaves e relações.

Critério de fecho: modelo conceptual e físico aprovado, com diagramas coerentes e decisões registadas.

**G-03 — Escolha do motor relacional**

Critério de fecho: decisão explícita, justificação e atualização coerente de documentação e projetos. Não manter dois caminhos de persistência concorrentes sem requisito concreto.

**G-04 — Fronteira de materialização**

Definir claramente o que acontece depois de Validate/Archive:
1. aceitar pacote e registar receipt;
2. associar contexto organizacional/instalação;
3. materializar Capture e Observations;
4. ligar artefactos e relações;
5. publicar a captura para pesquisa;
6. preservar estados de erro/reprocessamento.

Critério de fecho: fluxo e contratos de aplicação definidos antes da implementação.

### Altas — necessárias para primeira versão utilizável

**G-05 — API e segurança**
- API ASP.NET Core;
- autenticação e autorização;
- isolamento multi-organização;
- auditoria de ações e autoria;
- gestão de utilizadores e permissões.

**G-06 — Identidade e histórico**
- identidade lógica separada da evidência;
- regras de matching por tipo de objeto;
- confiança/ambiguidade e revisão humana;
- presença, alteração, desaparecimento e reaparecimento entre capturas;
- nunca inferir identidade forte quando a origem não a suporta.

**G-07 — UI e workflows**
- organizações, instalações e ambientes;
- receção e estado das capturas;
- catálogo de objetos e artefactos;
- detalhe de proveniência;
- pesquisa e comparação.

**G-08 — Pesquisa**
- pesquisa textual determinística;
- filtros por organização, instalação, tipo, origem e data;
- pesquisa de código e referências cruzadas;
- resultados com ligações a artefactos e capturas.

### Médias — evolução controlada

**G-09 — Catálogo standard PHC**
- separar conhecimento standard curado de observações específicas;
- definir versionamento e processo de atualização;
- permitir que conhecimento standard seja reutilizado sem expor dados de clientes.

**G-10 — RAG**
- só depois de pesquisa e proveniência;
- indexar fragmentos com metadados e permissões;
- recuperação híbrida;
- respostas com citações e evidências;
- avaliação de qualidade e feedback.

## 4. Dívida documental

| Documento/tema | Problema | Ação |
|---|---|---|
| Avaliação tecnológica/import | Atualização já menciona baseline de 17/11, mas precisa de refletir conclusões desta auditoria | Referenciar esta baseline e clarificar pendências |
| README | Alguns estados de receção podem estar desatualizados face à PR #9 | Atualizar depois de confirmar o fluxo de recovery |
| Roadmap anterior | Mistura etapas concluídas, propostas e pendentes | Substituir a visão de execução pelo roadmap datado |
| Modelo 0.2 | Documentos adicionais estão numa branch de revisão | Decidir quais são normativos e consolidar |
| Branches antigas | Não há registo único de destino | Registar integrar, arquivar ou descartar com evidência |

## 5. Regras para branches

Nenhuma branch deve ser apagada apenas por ser antiga. Para cada branch:
1. comparar com `main`;
2. identificar commits exclusivos;
3. verificar PR associada e estado;
4. classificar alterações em integradas, úteis não integradas ou obsoletas;
5. preservar alterações úteis antes de arquivar;
6. só então propor eliminação.

Prioridade de revisão:
1. `architecture/domain-model-0.2-review`;
2. `feature/sql-native-object-identities-20261108`;
3. `feature/ikp-object-index-schema-perf`;
4. branches de Scanner Desktop;
5. restantes branches ancestrais/sobrepostas.

## 6. Critério de governação documental

- Uma decisão só passa a **Aprovada** quando explicitamente validada.
- Uma funcionalidade só passa a **Implementada** quando existe evidência de código e teste adequado.
- Uma funcionalidade só passa a **Validada operacionalmente** depois de execução num ambiente representativo.
- Documentação de proposta deve ser identificada como proposta.
- Relatórios de fase devem ser curtos: o que existe, o que foi validado, o que ficou pendente e próximo passo.

