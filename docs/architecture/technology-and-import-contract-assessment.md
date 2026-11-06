# Avaliação — Arquitetura tecnológica e contrato de importação

| Campo | Valor |
| --- | --- |
| Data inicial | 2026-11-06 |
| Última consolidação | 2026-11-06 |
| Estado | **Decisões de stack e fronteiras consolidadas**; modelo de utilização local **pendente**; IKP 0.1 **ainda não especificado** |
| Âmbito | Stack da plataforma PHC Knowledge + bases do contrato de importação do Scanner independente |
| Implementação | Nenhuma nesta etapa |

Legenda:

| Marca | Significado |
| --- | --- |
| `[acordado]` | Decisão consolidada nesta documentação (com condições indicadas) |
| `[pendente]` | Exige validação explícita do responsável do projeto |
| `[código]` | Confirmado em código/teste analisado |
| `[doc]` | Documento arquitetural / investigação 05/11 (GitHub) |
| `[guideline]` | `PROJECT_GUIDELINES.md` / docs PHC Knowledge |
| `[inferência]` | Inferência a partir de evidências |
| `[proposta]` | Orientação técnica ainda não elevada a decisão |

---

## 1. Resumo executivo

**Stack acordada** `[acordado]`: .NET 10 + ASP.NET Core; React + TypeScript; PostgreSQL central; SQLite só auxiliar (workspace/cache/local); pesquisa inicial com PostgreSQL FTS; tempo real com SignalR; desktop Windows-first via WebView2 da mesma SPA. OpenSearch **fora** desta fase (evolução futura permitida, sem antecipar complexidade).

**Fronteiras** `[acordado]`: Scanner independente produz pacotes; PHC Knowledge importa e transforma; sem runtime do Scanner na plataforma; sem recuperar Discovery/Fusion do ConsoleApp.

**IKP** `[acordado]` (rumo; especificação detalhada = tarefa posterior): evoluir a partir do Database Schema Snapshot (não juntar ZIPs atuais nem recuperar Snapshot V1 histórico). Incluir estrutura, personalizações, relações, cobertura, evidências/proveniência, artefactos originais, manifesto e checksums.

**Instalações** `[acordado]`: IDs próprios na plataforma; associação guiada na 1.ª importação (existente / nova / pendente se ambígua); servidor/BD **não** são identificadores definitivos.

**Armazenamento de pacotes** `[acordado]`: abstração de armazenamento — filesystem (dev/local) e object storage (plataforma central); pacotes imutáveis; fornecedor concreto em aberto `[pendente]`.

**Utilização local / rede** `[pendente]`: dois cenários distintos (autónomo offline vs servidor de rede) — ver §5; não decidir aqui.

**Sequência acordada** `[acordado]`: (1) consolidar decisões → (2) clarificar modelo local → (3) especificar IKP 0.1 sem código → (4) comparar com exports reais do Scanner → (5) documentar lacunas → (6) só depois fixtures e esboço de API.

Fontes: este repositório (`product-boundaries.md`, `knowledge-layers.md`, `extract-2026-11-conversation.md`, `PROJECT_GUIDELINES.md`); Scanner incluído; investigação [`2026-11-05.md`](https://github.com/Jhouinho/ConsoleApp/blob/master/docs/investigation/2026-11-05.md) no GitHub `master`.
---

## 2. Requisitos considerados

Fontes: [`PROJECT_GUIDELINES.md`](../../PROJECT_GUIDELINES.md), [`product-boundaries.md`](product-boundaries.md), [`knowledge-layers.md`](knowledge-layers.md), pedido desta etapa.

### Interfaces e deployment

- Web + desktop com experiência coerente; não duas plataformas independentes. `[guideline]`
- Utilização local e em servidor; instalação central partilhada. `[guideline]`

### Multiempresa / multiutilizador

- Organizações isoladas; utilizadores em várias organizações; colaboração simultânea na mesma instalação; histórico, autoria, permissões e proveniência. `[guideline]`

### Conhecimento

1. PHC Standard (reutilizável).  
2. Conhecimento de instalação (isolado).  
3. Conhecimento produzido (interpretações, anotações, conclusões).  
Promoção a geral só com curadoria humana. `[guideline]` [`knowledge-layers.md`](knowledge-layers.md)

### Scanner

- Produto independente: recolhe, preserva originais, relações/evidências determinísticas, cobertura/erros, exporta.  
- PHC Knowledge **não** executa o Scanner em runtime. `[guideline]` [`product-boundaries.md`](product-boundaries.md)  
- Alinhado com ADR-0049 (Scanner = única fronteira de aquisição). `[doc]` ConsoleApp ADR-0049 (2026-11-05)

---

## 3. Descobertas relevantes (investigação 05/11 e património ConsoleApp)

### 3.1 Fonte principal

| Fonte | Estado |
| --- | --- |
| [`docs/investigation/2026-11-05.md`](https://github.com/Jhouinho/ConsoleApp/blob/master/docs/investigation/2026-11-05.md) | **Consultada** no GitHub (`master`, ~374 KB) `[doc]` |
| Cópia local `ConsoleApp/docs/investigation/` | Ausente nesta máquina (branch local `feat/adr0049-t1-scanner-dictionary` ≠ `master`) |

Complementos: ADR-0049, Phase 7.8 / 7.10 / 7.11a / 7.12, `EntityIdentityPolicy.cs`.

### 3.2 Conclusões da investigação (não reinvestigar sem necessidade)

De `docs/investigation/2026-11-05.md` (GitHub):

1. **Dois percursos distintos, não um único pipeline** — Twin operacional = Discovery/Evidence/Fusion/Store; percurso novo = Scanner → Snapshot → Ingestion → Candidate → Activation. `[doc]` §2  
2. **Materialização ≠ Active Twin** — Candidate isolado; Activation separada; Explorer precisa de reload de KnowledgeRoot. `[doc]` §2  
3. **Sistema antigo a descontinuar** — conhecimento Discovery/Fusion excluído do conjunto ativo; nova aquisição só via Scanner; quarentena lógica antes de apagar. `[doc]` §15  
4. **Candidate isolado não resolve identidade multi-origem** — `SqlCatalogPartialMapper` gera `knowledge:{type}:{id}` sem Install/Env/Snapshot; inadequado para catálogo unificado. `[doc]` §61  
5. **Separar StandardEntityId / ObservationId / DeviationId** — standard sem InstallationId; observação temporal; desvio vs revisão de referência. `[doc]` §61.4  
6. **EntityIdentity ≠ ObservationIdentity ≠ ContentFingerprint** — SnapshotId **não** entra na identidade estável. `[doc]` §65.4–65.5  
7. **Colisões estruturais** — FK só por nome é frágil; `ToLowerInvariant` pode colapsar nomes sob collation case-sensitive; preservar original + chave normalizada condicionada. `[doc]` §65.2–65.3  
8. **Três hashes distintos** — checksum de pacote ≠ fingerprint de artefacto ≠ ContentHash de pesquisa (truncado). `[doc]` §66.1  
9. **Comparador inseguro para remoções** — ausência ⇒ Removed; duplicados com `Last()`; teste `Detects_removed_artifacts` consagra o antipadrão; falta `NotObserved`. `[doc]` §66.2, §67.2  
10. **ReplaceExisting / atomicidade** — Builder apaga `knowledge.db` antes de reconstruir; falha a meio sem rollback da revisão anterior ⇒ staging + swap. `[doc]` §63  
11. **Completude explícita** — perfil `SqlCatalogPartial`; pacote parcial ≠ Twin completo. `[doc]` §2  

Idempotência de ingestão (mesmo SnapshotId + checksum) permanece válida via Phase 7.10. `[doc]`

### 3.3 Aviso crítico

A investigação analisa sobretudo o **ConsoleApp / Scanner V1 histórico**. As lições de identidade, cobertura e publicação aplicam-se ao PHC Knowledge; **não** descrevem o Scanner independente atual (`PhcKnowledgeScanner`, ADR 0003).

---

## 4. Decisões arquiteturais consolidadas

### 4.1 Vista lógica `[acordado]`

```text
┌─────────────────────┐     pacote IKP (futuro)    ┌──────────────────────────────┐
│ PhcKnowledgeScanner │ ─────────────────────────► │  PHC Knowledge Platform      │
│ (independente)      │   (sem runtime partilhado)  │                              │
│ Desktop WPF / CLI   │                            │  ASP.NET Core API (.NET 10)  │
│ SQLite workspace    │                            │  Serviços de domínio         │
│ Export → IKP        │                            │  Importação / Staging / Pub. │
└─────────────────────┘                            │  PostgreSQL (central)        │
                                                   │  Package store (abstração)   │
                                                   │  SignalR + PG FTS            │
                                                   │         ▲                    │
                                                   │         │ mesma SPA          │
                                                   │  React+TS Web ◄── WebView2   │
                                                   │           Desktop (Win-first)│
                                                   └──────────────────────────────┘
```

### 4.2 Stack tecnológica `[acordado]`

| Área | Decisão | Justificação (resumo) | Condições / limites |
| --- | --- | --- | --- |
| Backend | .NET 10 + ASP.NET Core | Alinhamento com Scanner (`global.json` 10.x) `[código]`; um ecossistema; SignalR nativo | Sem project references ao Scanner (ADR 0003) |
| Web | React + TypeScript | UI rica; partilha com desktop via WebView; Blazor do ConsoleApp fica referência UX, não stack | Cliente OpenAPI a partir da API `[proposta]` de detalhe |
| Desktop plataforma | Windows-first, SPA em WebView2 | Uma UI/lógica; evita WPF/Avalonia duplicados | Não impede evolução multiplataforma futura; Scanner mantém WPF próprio |
| BD central | PostgreSQL | Multiempresa, jsonb, FTS, licenciamento | SQL Server das instalações PHC fica só no Scanner |
| SQLite | Workspace / cache / auxiliar local | Já usado no Scanner `[código]`; não é BD multiutilizador | **Não** substitui PostgreSQL em cenário multiutilizador |
| Pesquisa | PostgreSQL FTS primeiro | Suficiente para começar; evita complexidade | OpenSearch **não** nesta fase; arquitetura deve permitir evolução sem antecipar |
| Tempo real | SignalR | Nativo no stack ASP.NET | Grupos por organização/instalação `[proposta]` de detalhe |

### 4.3 Scanner e PHC Knowledge `[acordado]`

| Regra | Estado |
| --- | --- |
| Scanner = produto independente (recolha + pacotes) | Acordado — alinhado a `product-boundaries.md` e ADR-0049 |
| PHC Knowledge = recebe, valida, importa, transforma em conhecimento | Acordado |
| PHC Knowledge **não** executa Scanner em runtime | Acordado |
| Não recuperar Discovery/Fusion como arquitetura operacional | Acordado — investigação §15; guidelines |

### 4.4 Contrato de importação (IKP) — rumo `[acordado]`

**Decisão:** evoluir o IKP a partir do **Database Schema Snapshot** do Scanner atual (ADR 0014), enriquecendo-o — **não** juntar simplesmente os ZIP existentes nem recuperar o formato histórico Scanner V1.

Elementos a considerar no contrato (especificação detalhada = **tarefa posterior**):

- Estrutura da base de dados  
- Personalizações e desenvolvimentos específicos  
- Relações entre objetos  
- Cobertura da recolha  
- Evidências e proveniência  
- Artefactos originais  
- Manifesto e checksums  

Princípios já assumidos na avaliação (não são ainda o IKP 0.1 normativo): EntityIdentity / ObservationIdentity / ContentFingerprint; coverage-aware diffs (`NotObserved` ≠ `Removed`); staging antes de publicação; anti-`ReplaceExisting` destrutivo. `[doc]` investigação §§61–67

### 4.5 Identificação de instalações `[acordado]`

- A plataforma possui **identificadores próprios** de instalação.  
- Na primeira importação: **associação guiada** — ligar a instalação existente, registar nova, ou deixar **pendente** se ambígua.  
- Nome do servidor ou da base de dados **não** é identificador definitivo (hoje o Scanner usa `$"{Server}/{Database}"` no manifesto de scan `[código]` `ScanAppService` — isto é lacuna a corrigir no rumo IKP, não política da plataforma).

### 4.6 Armazenamento dos pacotes `[acordado]`

- Abstração de armazenamento **sem** fornecedor concreto obrigatório.  
- Orientação inicial: **filesystem** (desenvolvimento / instalações locais); **object storage** (plataforma central).  
- Pacotes originais = artefactos **imutáveis**.  
- Escolha do fornecedor object storage: `[pendente]`.

### 4.7 Alternativas consideradas e rejeitadas / adiadas

| Área | Alternativa | Motivo |
| --- | --- | --- |
| Backend | Node / Java | Menos alinhamento com Scanner |
| Web | Blazor | Menos partilha natural com WebView; acoplamento UI-.NET |
| Desktop | WPF ou Avalonia full UI | Duplicaria interface e lógica |
| BD central | SQL Server ou SQLite multiutilizador | Custo / inadequação multiempresa |
| Pesquisa | OpenSearch já na 1.ª fatia | Complexidade sem prova de insuficiência do FTS |
| Import | ZIP V1 histórico / união ingénua dos 3 exports | Contrato errado / envelope frágil |

---

## 5. Utilização local e em rede — **decisão pendente** `[pendente]`

A arquitetura deve distinguir **dois cenários**. **Nenhuma escolha entre eles está acordada nesta consolidação.**

### Cenário A — Instalação autónoma num computador

Sem servidor dedicado nem Internet obrigatória; dados próprios; eventual sincronização posterior com uma plataforma central.

| Aspeto | Implicações |
| --- | --- |
| PostgreSQL | Exige instância local (serviço Windows, contentor, ou distribuição embutida). Mais pesado que SQLite, mas alinha o modelo de dados com o servidor. Embutir/operar PG num portátil é o principal custo deste cenário se se insistir na mesma BD. |
| Autenticação | Tipicamente utilizador local / conta de máquina; Identity “single-user” ou modo offline. Federação com IdP central só quando houver rede. |
| Armazenamento | Filesystem local para pacotes imutáveis e ficheiros. |
| Sincronização | Export/import de revisões ou pacotes quando voltar a haver conectividade; conflitos de anotações a resolver por política (ainda não definida). |
| Offline | Deve funcionar sem API remota; toda a stack (API + PG + SPA/WebView) no mesmo host. |
| Atualizações | Atualização do binário/app no posto; migrações PG locais. |

**Nota:** SQLite pode auxiliar cache/workspace, mas **não** está decidido — e **não** deve ser assumido — como substituto de PostgreSQL para multiutilizador. Para autónomo single-user, usar PG local vs perfil SQLite “compatível” continua `[pendente]`.

### Cenário B — Servidor na rede da empresa

Um servidor na LAN; restantes PCs acedem (browser e/ou desktop WebView2).

| Aspeto | Implicações |
| --- | --- |
| PostgreSQL | Instância no servidor (ou VM/contentor); clientes **não** instalarm PG. Perfil natural da stack acordada. |
| Autenticação | Contas partilhadas / AD / OIDC na rede; multiutilizador e permissões por organização. |
| Armazenamento | Filesystem no servidor ou object storage interno; pacotes imutáveis partilhados. |
| Sincronização | Clientes consomem API; sem “sync de base local” obrigatório. Desktop = cliente fino. |
| Offline | Postos sem rede perdem acesso (salvo cache read-only futuro — não acordado). |
| Atualizações | Atualizar o servidor; clientes web sem redeploy; desktop host com updates pontuais. |

### O que isto não decide

- Se o produto v1 privilegia A, B, ou ambos com o mesmo binário e perfis de deployment.  
- Se autónomo (A) embute PostgreSQL ou adota um perfil de persistência diferente (ainda sujeito a não violar a regra “SQLite ≠ multiutilizador”).  
- Política concreta de sincronização A → plataforma central.

**Estado:** aguarda validação do responsável do projeto antes da especificação do IKP 0.1 e do desenho de deployment.

---

## 6. Contrato de importação — estado da análise

### 6.1 O que o Scanner independente produz hoje `[código]`

Existem **três** mecanismos distintos — nenhum é ainda o contrato único da plataforma:

| Mecanismo | Classe / fluxo | Conteúdo | Lacunas face à plataforma |
| --- | --- | --- | --- |
| ZIP de scan | `ZipScanExporter` | `manifest.json` + `collectors/{id}/…` + README | Sem `SnapshotId`, `EnvironmentId`, `checksums.json`, package checksum; `InstallationId` = `$"{Server}/{Database}"` (`ScanAppService`) |
| ZIP personalizações | `WorkspaceExportService` | Pastas `brule`/`eventos`/… | Sem manifesto rico de cobertura; sem schema SQL |
| Schema snapshot | `DatabaseSchemaSnapshotWriter` + CLI `schema-snapshot` | Pasta/ZIP JSON + `manifest.json` com `schemaVersion`, `snapshotHash`, `completionStatus`, `categoryStatuses`, `originFingerprint` | Melhor base estrutural; não inclui personalizações nem projeção KR; comparador trata ausência como Removed |

Referências:  
- `PhcKnowledgeScanner/src/PhcKnowledgeScanner.Export/ZipScanExporter.cs`  
- `…/Application/Services/WorkspaceExportService.cs`  
- `…/Abstractions/SchemaSnapshot/DatabaseSchemaSnapshotModels.cs`  
- `…/Export/SchemaSnapshot/DatabaseSchemaSnapshotComparer.cs`  
- ADR 0014 / `docs/database-schema-snapshot.md`

### 6.2 Pacote alvo (unificado) — rumo `[acordado]`; detalhe normativo ainda não escrito

Nome conceptual: **Installation Knowledge Package (IKP)**.  
A especificação IKP 0.1 é a **próxima etapa documental** após clarificar o modelo de utilização local — **sem implementação**.

```text
{packageId}.zip
├── manifest.json          # contrato versionado
├── checksums.json         # SHA-256 por ficheiro do payload
├── coverage.json          # cobertura por capability/collector
└── payload/
    ├── schema/            # evolui a partir do Database Schema Snapshot
    ├── customizations/    # artefactos originais (pastas atuais do WorkspaceExport)
    ├── relations/         # factos REL/DTL + FK + interpretações (ou ponteiros)
    ├── knowledge/         # projeção opcional KR/FieldMapping (derivada, regenerável)
    └── diagnostics/       # issues, limitações
```

Transportes: ZIP (canónico) e envelope JSON base64 (como Phase 7.10) se útil. `[doc]` 7.10

### 6.3 Identificação do pacote

| Campo | Obrigatório | Notas |
| --- | --- | --- |
| `packageId` | sim | UUID do artefacto físico |
| `snapshotId` | sim | UUID da observação/recolha |
| `installationId` | sim | ID de registo na plataforma (**não** `server/db` cru) |
| `environmentId` | sim | ex. `production` / `test` |
| `organizationId` | na ingestão | Associado na plataforma; pode não ir no pacote se o Scanner for offline |
| `scannerVersion` | sim | SemVer build Scanner |
| `exportContractVersion` | sim | Versão do contrato IKP |
| `payloadSchemaVersion` | sim | Versão do schema dos JSON internos (≠ versão PHC) |
| `capturedAtUtc` | sim | |
| `phcVersion` / `phcBuild` | quando disponível | Schema snapshot já tem `PhcVersion?` / `PhcSignatureDetected?` `[código]` |
| `originFingerprint` | sim | Já no schema snapshot (anonimizado) `[código]` |

**Não confundir** `payloadSchemaVersion` com versão PHC. `[guideline]` pedido

### 6.4 Conteúdo e proveniência

Transportar e **não misturar**:

| Camada | Exemplos | Regra |
| --- | --- | --- |
| Originais | Texto brule/eventos, SQL definition se flag | Bytes + content SHA |
| Estrutura SQL | tables/columns/PK/FK/indexes… | Schema snapshot |
| Relações declaradas | REL/DTL | Factos; expressão original intacta |
| Interpretações | AST / diagnostics parser | Versionadas (`parserVersion`) |
| Projeções | KnowledgeRelation / FieldMapping | Marcadas como derivadas; regeneráveis |
| Cobertura | collector status, categoryStatuses | Obrigatório para diffs honestos |
| Issues | `CollectionIssue` | Severidade + código |

Modelo de evidências do Scanner (ADR 0008/0009) deve mapear para evidências da plataforma **sem** perder kind nem diagnostics. `[doc]` Scanner ADR 0008

### 6.5 Identidade e histórico

Separar explicitamente `[proposta]` + alinhado a `EntityIdentityPolicy` `[código]`:

| Conceito | Definição | Não incluir |
| --- | --- | --- |
| **EntityIdentity** | Identidade lógica estável no âmbito instalação (+ tipo + chave local) | `SnapshotId` |
| **ObservationIdentity** | Ocorrência da entidade **neste** `SnapshotId` | — |
| **ContentFingerprint** | Hash canónico do conteúdo estrutural observado (algoritmo versionado) | Hash de índice FTS |

Regras:

- Preservar nome e casing originais; normalização só em chaves derivadas, documentando collation/origem. `[proposta]`  
- Não assumir unicidade global de nomes de constraints. `[proposta]` / pedido  
- Colisões / ambiguidade ⇒ estado `Ambiguous`; **nunca** “último ganha”. `[código]` espírito `EntityIdentityPolicy.RequiresSeparateEntities` / `CanDeterministicallyMerge`

### 6.6 Fingerprints — distinção obrigatória

| Tipo | Uso |
| --- | --- |
| Checksum do pacote | Integridade do ZIP |
| Hash por ficheiro | `checksums.json` |
| Fingerprint de artefacto | SHA do conteúdo original (já em workspace `artifacts.sha256`) `[código]` `SqliteScanWorkspace` |
| Fingerprint estrutural canónico | Ex.: `SchemaSnapshotHasher.Compute` `[código]` |
| Hash de índice de pesquisa | Só sincronização FTS — **não** prova igualdade de conteúdo |

### 6.7 Cobertura e comparação

O comparador atual do schema snapshot classifica chaves só em left como **Removed**, sem consultar `completionStatus` / `categoryStatuses`. `[código]` `DatabaseSchemaSnapshotComparer`

O contrato e a plataforma devem distinguir:

`Added` | `Removed` | `Changed` | `Unchanged` | `Unknown` / `NotObserved` | `Ambiguous`

Regra: ausência ⇒ `Removed` **somente** se a capability relevante tiver cobertura completa e sucesso; senão `NotObserved`. `[proposta]` + requisito do pedido; coerente com `Partial` já existente no manifesto de schema `[código]`

### 6.8 Referência PHC Standard

| Dado | No Scanner atual | Onde obter |
| --- | --- | --- |
| Assinatura / versão PHC | Parcial (`PhcSignatureDetected`, `PhcVersion?`) `[código]` | Evoluir recolha no Scanner |
| Módulos instalados | Não comprovado no export atual `[inferência]` | Collector futuro / metadados PHC |
| Baseline Standard compatível | Não no pacote `[inferência]` | Catálogo Standard na plataforma + mapeamento por versão PHC |
| Desvio instalação vs Standard da **mesma** versão | Não | Diff na plataforma após ingestão |

### 6.9 Fluxo de importação / publicação `[proposta]`

Alinhado a 7.10 + 7.12, sem `ReplaceExisting` sobre a revisão ativa:

```text
1. Receção (upload / path) → ImportAttemptId
2. Validação estrutural (layout, contract version)
3. Integridade (checksums + package checksum)
4. Identidade (Installation/Environment/Org association)
5. Registo de proveniência (pacote imutável em archive)
6. Materialização em staging (revisão candidata)
7. Validação do resultado (contagens, coverage, ambiguidade)
8. Publicação atómica de KnowledgeRevision (pointer swap)
9. Notificação SignalR aos utilizadores autorizados
```

Regras:

- Idempotente: mesmo `snapshotId` + mesmo package checksum ⇒ sem duplicar.  
- Import inválido **não** substitui revisão publicada.  
- Staging isolado até publicação.  
- Reimportação ≠ apagar anotações humanas (ligadas a EntityIdentity).  

---

## 7. Modelo conceptual de dados `[proposta]`

Sem DDL nesta fase.

### Globais (partilhados / Standard)

- `User`
- `Organization` (metadados)
- `UserOrganizationMembership` (papel/permissões)
- `PhcStandardReference` (versão/baseline)
- `PhcStandardEntity` / relações standard curadas
- `KnowledgePromotion` (registo de promoção)

### Por organização (isolados)

- `Installation`, `Environment`
- `Snapshot` / `ImportAttempt` / `PackageArchive`
- `KnowledgeRevision` (candidata | publicada | superseded)
- `EntityIdentity`, `Observation`, `ContentFingerprint`
- `Evidence`, `Relation`, `FieldMapping` (ou projeção regenerável)
- `HumanAnnotation` (Draft/Validated/NeedsReview/Superseded)
- Artefactos blob (conteúdo original)

### Separação Standard vs instalação

| Dado | Âmbito |
| --- | --- |
| Catálogo PHC Standard curado | Global |
| Observações de schema/personalizações | Organização + Instalação |
| Anotações de consultores | Organização (+ opcionalmente promovidas) |
| Assets promovidos | Global só após `KnowledgePromotion` humana |

---

## 8. Matriz de reutilização do património

| Capacidade | Estado | Evidência |
| --- | --- | --- |
| Recolha SQL estrutural + personalizações PHC | **Já no Scanner atual** | Collectors + testes |
| REL/DTL + interpretação V1.1 + KR/FieldMapping | **Já no Scanner atual** | ADR 0005–0009; `RelationalKnowledgeProjector` |
| Schema snapshot + hash canónico + Partial | **Já no Scanner atual** | ADR 0014; `SchemaSnapshotHasher` |
| Comparação schema coverage-aware (`NotObserved`) | **Parcial** — compara, mas Remove sem coverage | `DatabaseSchemaSnapshotComparer` |
| ZIP scan com manifesto mínimo | **Parcial** — exporta, contrato insuficiente | `ZipScanExporter` |
| Pacote unificado IKP (schema+custom+relations+coverage) | **Ainda não existe** | — |
| Ingestão Receive/Validate/Archive | **Existe no ConsoleApp (histórico)** — adaptar, não copiar cego | Phase 7.10 `SnapshotIngestionService` |
| Candidate / Activation | **Existe como desenho/histórico** | Phase 7.12 |
| EntityIdentity scoped | **Existe no ConsoleApp** — adaptar conceitos | `EntityIdentityPolicy` |
| Discovery → Fusion → Store live | **Não reutilizar como arquitetura operacional** | ADR-0049; guidelines |
| Explorer Blazor / FTS Store V1 | **Património UX/pesquisa** — adaptar ideias, não o pipeline | ConsoleApp |
| Multiempresa + RLS + SignalR + React | **Ainda não existe** no produto alvo | — |
| Anotações humanas / promoção Standard | **Modelo conceptual** (docs) / Domain D histórico separado | `knowledge-layers.md`; IK piloto ConsoleApp |

---

## 9. Riscos, acordado e pendente

### Riscos (permanecem válidos)

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| Continuar com 3 formatos de export divergentes | Importação frágil | IKP evolutivo a partir do schema snapshot (após especificação) |
| Ausência classificada como Removed | Falsos “removidos” | Coverage + `NotObserved` no contrato |
| ReplaceExisting na BD partilhada | Perda de histórico/anotações | Staging + publicação atómica |
| Reintroduzir SQL direto na plataforma | Viola fronteira Scanner | Gate de arquitetura |
| Antecipar OpenSearch / IKP detalhado / API | Complexidade precoce | Seguir sequência §10 |
| Assumir deployment local sem validação | Stack desalinhada com uso real | §5 permanece `[pendente]` |

### Decisões acordadas (consolidadas nesta revisão)

- Stack: .NET 10, ASP.NET Core, React+TS, PostgreSQL, SQLite auxiliar, PG FTS, SignalR.  
- Desktop Windows-first WebView2 (mesma SPA); evolução multiplataforma não bloqueada.  
- Scanner independente; Knowledge sem runtime Scanner; sem Discovery/Fusion operacional.  
- IKP a partir do Database Schema Snapshot (não ZIP V1; não união ingénua dos exports).  
- Associação guiada de instalações; IDs próprios; servidor/BD ≠ identidade definitiva.  
- Abstração de package store (FS + object storage); pacotes imutáveis.  
- Sequência de trabalho da §10.

### Ainda pendente de validação

1. **Modelo de utilização local vs servidor de rede** (§5) — autónomo offline e/ou servidor LAN; implicações PG/auth/sync.  
2. **Fornecedor concreto de object storage** (S3-compatível, Azure Blob, MinIO, outro).  
3. **Detalhe normativo IKP 0.1** — tarefa posterior; não iniciada aqui.  
4. **Política de sincronização** entre autónomo e plataforma central (se o cenário A for adotado).  
5. Opcional: sincronizar `docs/investigation/` no clone local do ConsoleApp.

### Princípios técnicos assumidos (não são ADRs de implementação)

- EntityIdentity / ObservationIdentity / ContentFingerprint.  
- Coverage-aware diffs; anti-merge silencioso.  
- Staging antes de publicação.

---

## 10. Sequência de trabalho acordada `[acordado]`

1. **Consolidar** as decisões arquiteturais *(esta tarefa)*.  
2. **Clarificar** o modelo de utilização local (§5) — validação do utilizador.  
3. **Especificar** o IKP 0.1 **sem implementação**.  
4. **Comparar** o IKP proposto com as capacidades reais dos exports do Scanner atual.  
5. **Identificar e documentar** as lacunas.  
6. **Só depois** preparar fixtures e esboçar a API de importação.

Não antecipar etapas. Não implementar código nesta fase.

---

## 11. Estado documental e próximo passo

### Coerência

- Decisões de stack e fronteiras passaram de “proposta” a **acordadas** (§4).  
- Utilização local permanece explicitamente **pendente** (§5).  
- Análise do Scanner/exports (§6–8) mantém-se como base factual para o futuro IKP 0.1.  
- `PROJECT_GUIDELINES.md` / `README.md` devem apontar para este estado (atualizados em conjunto).

### Lacunas / contradições documentais encontradas

| Item | Nota |
| --- | --- |
| `InstallationId` no Scanner = `server/database` | Contradiz a política acordada de IDs próprios — a corrigir no rumo IKP, não na plataforma a aceitar como definitivo |
| Três exports Scanner vs um IKP | Documentado; unificação ainda não especificada |
| Guidelines antigas (“tecnologias candidatas”) | Desatualizadas face a §4 — alinhar no mesmo commit documental |
| Investigação 05/11 | No GitHub `master`; pode faltar no clone local |

### Próximo passo recomendado

**Clarificar o modelo de utilização local** (cenário A, B, ou ambos) com o responsável do projeto.  
Só depois iniciar a especificação documental do **IKP 0.1** (sem código).
