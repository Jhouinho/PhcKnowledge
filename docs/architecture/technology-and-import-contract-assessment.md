# Avaliação — Arquitetura tecnológica e contrato de importação

| Campo | Valor |
| --- | --- |
| Data | 2026-11-06 |
| Estado | Proposta para validação conjunta — **sem implementação** |
| Âmbito | Stack da plataforma PHC Knowledge + contrato de importação do Scanner independente |
| Origem do pedido | Etapa de arquitetura pós-inicialização do repositório |

Legenda de proveniência das conclusões:

| Marca | Significado |
| --- | --- |
| `[código]` | Confirmado em código/teste analisado |
| `[doc]` | Documento arquitetural existente |
| `[guideline]` | `PROJECT_GUIDELINES.md` / docs PHC Knowledge |
| `[inferência]` | Inferência a partir de evidências |
| `[proposta]` | Proposta ainda não validada |
| `[lacuna]` | Fonte pedida em falta |

---

## 1. Resumo executivo

Recomenda-se construir o PHC Knowledge como **plataforma .NET 10 / ASP.NET Core + React/TypeScript**, com **PostgreSQL** como base central multiempresa, **SQLite** apenas para workspace/cache local, e **SignalR** para atualizações em tempo real. A interface desktop deve **reutilizar a mesma SPA** (anfitrião WebView2 ou equivalente), evitando duas UIs divergentes.

O Scanner independente (`PhcKnowledgeScanner`) deve permanecer **fora do runtime** da plataforma. A ponte é um **contrato de pacote versionado**, ainda incompleto no Scanner atual: existem exports úteis (ZIP de personalizações, ZIP/manifesto de scan, **Database Schema Snapshot**), mas **não** um pacote de instalação unificado com identidade, cobertura e proveniência ao nível exigido pela plataforma.

**Fonte obrigatória em falta:** `ConsoleApp/docs/investigation/2026-11-05.md` **não existe** no disco (`docs/investigation/` inexistente). A análise usa, em alternativa documentada, o corpus de 05/11/2026 e fases relacionadas: sobretudo [`ADR-0049`](../../../ConsoleApp/docs/architecture/ADR-0049-Scanner-Sole-Installation-Acquisition-Boundary.md), Phase 7.8 / 7.10 / 7.11a / 7.12, e o código de identidade `EntityIdentityPolicy`. Os requisitos de identidade/cobertura/staging do pedido são tratados como **requisitos de produto confirmados pelo utilizador**, cruzados com esse corpus — não como citação direta do ficheiro em falta. `[lacuna]`

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

## 3. Descobertas relevantes (05/11 e património ConsoleApp)

### 3.1 Lacuna da investigação pedida

| Pedido | Estado |
| --- | --- |
| `ConsoleApp/docs/investigation/2026-11-05.md` | **Ausente** — pasta `docs/investigation/` não existe `[lacuna]` |

Substitutos consultados (mesma data / mesmo tema de aquisição e ingestão):

| Documento | Relevância |
| --- | --- |
| `ADR-0049-Scanner-Sole-Installation-Acquisition-Boundary.md` | Fronteira Scanner vs Knowledge; sem SQL direto na plataforma |
| `phase7-8-snapshot-contract-final-specification.md` | Manifesto Snapshot V1 histórico (IDs, checksums, compatibilidade) |
| `phase7-10-snapshot-platform-ingestion-boundary.md` | Receive → Validate → Archive; idempotência |
| `phase7-11a-snapshot-to-store-materialization-contract.md` | Snapshot ≠ Twin completo; perfis de completude; anti-ReplaceExisting cego |
| `phase7-12-candidate-readiness-activation-boundary.md` | Staging Candidate → Ready → Activation; publicação controlada |
| `PHC.Knowledge.Contracts/Identity/EntityIdentityPolicy.cs` | Chaves scoped por instalação; anti-merge silencioso |

### 3.2 Conclusões aproveitáveis (não reinvestigar sem necessidade)

1. **Dois percursos históricos de aquisição** — Discovery/Fusion (live SQL → Twin) vs Scanner/Snapshot (pacote → ingestão). A arquitetura alvo é só o segundo para dados de instalação. `[doc]` ADR-0049  
2. **Materialização ≠ ativação** — arquivar e materializar num Candidate não substitui o conhecimento publicado até haver readiness + publicação controlada. `[doc]` 7.11a / 7.12  
3. **Completude explícita** — um pacote parcial não pode fingir paridade com um Twin “completo”; perfis de completude são obrigatórios. `[doc]` 7.11a (`SqlCatalogPartial`)  
4. **Identidade com âmbito de instalação** — `EntityIdentityPolicy.BuildInstallationScopedKey` evita colisões entre instalações; merge só com chave completa. `[código]` `EntityIdentityPolicy.cs`  
5. **Fuzzy ≠ identidade** — comentário e API do mesmo ficheiro: matching fuzzy nunca confirma identidade. `[código]`  
6. **Idempotência de ingestão** — mesmo SnapshotId + mesmo checksum ⇒ duplicado idempotente; mesmo SnapshotId + checksum diferente ⇒ rejeição. `[doc]` 7.10  
7. **ReplaceExisting destrutivo** — rebuild que apaga DB partilhada perde conhecimento; preservação/staging são necessários. `[doc]` phase10-8 / knowledge-store-materialization  
8. **Explorer sticky root** — troca de revisão ativa exige reload consciente do consumidor, não só pointer em disco. `[doc]` 7.12 findings  

### 3.3 Aviso crítico

Os documentos Phase 7.* descrevem sobretudo o **Scanner V1 histórico** (`tools/PhcKnowledgeScannerV1`, ZIP `manifest.json` + `checksums.json` + `payload/`). **Não** descrevem integralmente o Scanner independente atual. `[doc]` ADR 0003 do Scanner + 7.11a §3.1

---

## 4. Arquitetura tecnológica proposta

### 4.1 Vista lógica `[proposta]`

```text
┌─────────────────────┐     pacote versionado      ┌──────────────────────────────┐
│ PhcKnowledgeScanner │ ─────────────────────────► │  PHC Knowledge Platform      │
│ (independente)      │   (sem runtime partilhado)  │                              │
│ Desktop WPF / CLI   │                            │  ASP.NET Core API            │
│ SQLite workspace    │                            │  Serviços de domínio         │
│ Export pacotes      │                            │  Importação / Staging / Pub. │
└─────────────────────┘                            │  PostgreSQL (central)        │
                                                   │  Blob/FS para artefactos     │
                                                   │  SignalR                     │
                                                   │         ▲                    │
                                                   │         │ mesma SPA          │
                                                   │  React+TS Web ◄── WebView2   │
                                                   │                 Desktop host │
                                                   └──────────────────────────────┘
```

### 4.2 Backend — .NET 10 / ASP.NET Core

| Aspeto | Recomendação |
| --- | --- |
| Runtime | .NET 10 (LTS), alinhado ao Scanner (`global.json` → SDK 10.0.403) `[código]` Scanner |
| API | ASP.NET Core Minimal APIs ou Controllers + camadas Application / Domain / Infrastructure |
| Partilha com Scanner | **Mesma família tecnológica**, **sem** project references ao Scanner (ADR 0003) `[doc]` |
| Local vs servidor | Mesmo host: perfil `Local` (Kestrel + PostgreSQL embutido ou remoto) e perfil `Server` |
| Autenticação | OpenID Connect / Identity; claims de organização; autorização por recurso |

**Vantagens:** alinhamento com Scanner; produtividade; SignalR nativo; um só ecossistema de testes.  
**Riscos:** tentação de referenciar assemblies do Scanner — proibir por política.

### 4.3 Interface web — React + TypeScript

Recomendado face a Blazor (usado no Explorer histórico do ConsoleApp):

| Critério | React+TS | Blazor (histórico) |
| --- | --- | --- |
| Ecossistema UI exploração / grafos / editores de código | Maduro | Mais limitado |
| Independência do backend | Alta | Acoplamento .NET |
| Reutilização no desktop via WebView | Natural | Possível mas menos padrão |
| Património ConsoleApp Explorer | Não reutiliza UI | Poderia copiar componentes |

**Proposta:** React + TypeScript + cliente OpenAPI gerado a partir da API. `[proposta]`  
Blazor do ConsoleApp fica património de referência UX, não stack obrigatória. `[inferência]`

### 4.4 Interface desktop

| Opção | Prós | Contras |
| --- | --- | --- |
| **WebView2 + mesma SPA** | Uma UI; coerência; custo baixo | Windows-first (WebView2); shell nativo mínimo |
| **WPF** | Já usado no Scanner | Segunda UI; só Windows; duplicação |
| **Avalonia** | Multiplataforma | Segunda UI; curva e ecossistema menores |

**Recomendação:** Desktop da plataforma = **anfitrião da SPA** (WebView2 no Windows; avaliar WebKit/outro depois se multi-OS for requisito duro). O Scanner mantém WPF próprio (recolha local). `[proposta]`

A desktop da plataforma **não** replica lógica de negócio: consome a mesma API. Ficheiros locais (abrir pacote ZIP) via diálogo nativo → upload à API.

### 4.5 Base de dados central — PostgreSQL

| Critério | PostgreSQL | SQL Server |
| --- | --- | --- |
| Multiempresa / RLS | Maduro (`ROW LEVEL SECURITY`) | Possível com schemas/filtering |
| JSON / jsonb | Excelente | Bom (JSON) |
| Full-text | Bom (`tsvector`) | Bom |
| Licenciamento | Sem custo de licença servidor | Custo/edição a gerir |
| Alinhamento PHC origem | Diferente do SQL Server das instalações | Mesmo motor das origens |

**Recomendação:** PostgreSQL como BD central. SQL Server das instalações fica **só** no Scanner. `[proposta]`  
Isolamento: `organization_id` em todas as tabelas de instalação + RLS (e testes de isolamento). Dados PHC Standard em schemas/tabelas **globais** sem `organization_id` de cliente (ou org sistema).

### 4.6 Armazenamento local — SQLite

| Uso | Papel |
| --- | --- |
| Workspace do Scanner | Já existe (`SqliteScanWorkspace`) `[código]` |
| Cache offline da plataforma | Opcional: réplica de revisão publicada + anotações em rascunho |
| BD central multiutilizador | **Não** |

Sincronização: pull da revisão publicada + push de anotações via API; conflitos resolvidos por revisão/autoria, não por merge cego de SQLite. `[proposta]`

### 4.7 Pesquisa

Ordem:

1. **PostgreSQL FTS** + índices B-tree/GIN em identificadores, nomes, stamps, tags.  
2. Pesquisa em código: FTS sobre artefactos indexados (texto extraído), não grep em blobs em tempo real.  
3. **OpenSearch** só se FTS + índices forem insuficientes (volume, facetas complexas, ranking).  

Distinguir: pesquisa **exata** (IDs/stamps), **textual**, **contextual** (grafo/relações), **semântica** (fase futura, com evidências). `[proposta]`

### 4.8 Tempo real

**ASP.NET Core SignalR**, grupos por `organizationId` + `installationId`. Eventos: anotação criada, revisão publicada, importação concluída. Alternativas (SSE, polling) só se SignalR for bloqueado por infra. `[proposta]`

---

## 5. Comparação das alternativas (síntese)

| Área | Escolha | Alternativa rejeitada / adiada | Motivo principal |
| --- | --- | --- | --- |
| Backend | .NET 10 + ASP.NET Core | Node / Java | Alinhamento Scanner; um só talento |
| Web | React + TS | Blazor | UI rica + partilha com desktop host |
| Desktop plataforma | SPA host | WPF/Avalonia full UI | Evitar duas plataformas |
| BD central | PostgreSQL | SQL Server / SQLite central | Multiempresa, custo, jsonb/FTS |
| Pesquisa | PG FTS primeiro | OpenSearch imediato | Simplicidade até haver prova de insuficiência |
| Realtime | SignalR | Polling | Nativo no stack |

---

## 6. Contrato de importação proposto

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

### 6.2 Pacote alvo (unificado) `[proposta]`

Nome conceptual: **Installation Knowledge Package (IKP)**.

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

## 9. Riscos e decisões em aberto

### Riscos

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| Continuar com 3 formatos de export divergentes | Importação frágil | Unificar IKP numa evolução controlada do Scanner (**depois** desta validação) |
| Comparar absences como Removed | Falsos negativos de “remoção” | Coverage no contrato + comparador honest |
| Importar ReplaceExisting na BD partilhada | Perda de anotações/histórico | Staging + revisões |
| Duas UIs (WPF plataforma + React) | Custo e divergência | SPA única + host |
| OpenSearch cedo demais | Complexidade operacional | PG FTS primeiro |
| Reintroduzir SQL direto na plataforma | Viola ADR-0049 | Gate de arquitetura / reviews |
| Usar Phase 7.* como se fosse o Scanner atual | Contrato errado | Basear no código do Scanner independente |

### Decisões que precisam de validação conjunta

1. Aceitar stack: .NET 10 + ASP.NET Core + React/TS + PostgreSQL + SignalR + desktop SPA-host?  
2. Aceitar IKP unificado como evolução do Schema Snapshot + customizations (em vez de adotar ZIP V1 do ConsoleApp)?  
3. `installationId` na plataforma: registo manual prévio vs auto-registo na primeira importação?  
4. Desktop multiplataforma é requisito imediato ou Windows-first basta?  
5. Onde vive o arquivo imutável de pacotes (FS local vs object storage)?  
6. Prioridade: evoluir export do Scanner **antes** de API, ou esboçar API contra fixtures do schema snapshot atual?

### Decisões consideradas suficientemente fundamentadas (para validação, não para implementar ainda)

- Scanner fora do runtime da plataforma (ADR-0049 + guidelines).  
- Não reativar Discovery/Fusion como caminho operacional.  
- Distinguir EntityIdentity / Observation / ContentFingerprint.  
- Staging antes de publicação; anti-ReplaceExisting.  
- Coverage-aware diffs.  
- PostgreSQL central; SQLite só local.  
- Não introduzir OpenSearch na primeira fatia.

---

## 10. Recomendação final

1. **Arquitetura:** plataforma .NET 10 / ASP.NET Core, React+TS, PostgreSQL, SignalR, desktop como host da mesma SPA; Scanner permanece independente.  
2. **Importação:** definir IKP versionado, partindo do **Database Schema Snapshot** + ZIP de personalizações + cobertura/evidências; **não** clonar o ZIP V1 histórico como SSOT.  
3. **Aprendizagens do património ConsoleApp:** fronteira de aquisição, ingestão idempotente, candidate/activation, identidade scoped, anti-merge — como **princípios**, não como runtime a portar.  
4. **Bloqueio documental:** recuperar ou recriar `ConsoleApp/docs/investigation/2026-11-05.md` se contiver conclusões adicionais não cobertas por ADR-0049 / Phase 7.*; até lá, esta avaliação marca a lacuna explicitamente.

---

## 11. Próximos passos sugeridos (não executar)

1. Validar consigo as decisões da secção 9.  
2. Se aprovado: redigir ADR curto da stack + esboço normativo do IKP (`exportContractVersion = 0.1-draft`) **sem código**.  
3. Inventariar gaps concretos no Scanner para emitir IKP 0.1 (lista de campos em falta) — ainda sem alterar runtime até ordem explícita.  
4. Definir fixture de importação (usar `samples/database-schema-snapshot/`) para testes futuros da plataforma.  
5. Localizar/restaurar a investigação 05/11 se existir noutro suporte e anexar diff de conclusões.

---

## Reflexão obrigatória

### O que existe e funciona

- Repositório PHC Knowledge com guidelines e fronteiras.  
- Scanner independente com recolha, workspace SQLite, relações/evidências, schema snapshot e exports parciais.  
- Património ConsoleApp rico em lições de ingestão/identidade (e em complexidade a não transportar).

### O que falta

- Ficheiro `docs/investigation/2026-11-05.md`.  
- Contrato de exportação unificado no Scanner.  
- Plataforma (API, BD, UI, importação).  
- Modelo multiempresa implementado.

### O que aprendemos

- O Scanner atual **já ultrapassa** o Snapshot V1 histórico em personalizações e relações, mas o **envelope de pacote** ainda é mais fraco que o desenho Phase 7.8.  
- O schema snapshot é o melhor embrião de contrato estrutural.  
- O risco maior não é escolher React vs Blazor — é importar sem cobertura/identidade/staging e repetir `ReplaceExisting`.

### Próximo passo

**Parar e aguardar validação** das recomendações tecnológicas e do rumo do contrato IKP antes de qualquer implementação ou alteração ao Scanner.
