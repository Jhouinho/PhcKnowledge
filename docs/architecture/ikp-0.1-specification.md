# IKP 0.1 — Installation Knowledge Package (especificação)

| Campo | Valor |
| --- | --- |
| Estado | **Proposta para revisão conjunta** — não aprovada só por existir |
| Data | 2026-11-06 |
| Contrato | `ikpContractVersion = 0.1` |
| Content-Type | `phc.installation.knowledge.package` |
| Transporte canónico | ZIP |
| Implementação | Nenhuma nesta etapa |
| Documento-mãe | [`technology-and-import-contract-assessment.md`](technology-and-import-contract-assessment.md) |

Legenda: `[código]` Scanner atual · `[acordado]` decisões de arquitetura · `[proposta]` IKP 0.1 · `[pendente]` validação conjunta · `[lacuna]` gap Scanner ↔ IKP

---

## 1. Resumo das capacidades reais do Scanner `[código]`

O Scanner independente (`PhcKnowledgeScanner/`) **não produz ainda um IKP**. Produz **três** exports distintos:

| Export | Produtor | Layout / manifesto | Identidade | Integridade | Cobertura |
| --- | --- | --- | --- | --- | --- |
| **Database Schema Snapshot** | `DatabaseSchemaSnapshotWriter` + CLI `schema-snapshot` | Pasta ou ZIP: `manifest.json`, `tables/*.json`, `relationships/`, `indexes/`, `views/`, `triggers/`, `procedures/`, `functions/`, `types/`, `synonyms/`, `documentation/overview.md` | `originFingerprint` (anonimizado); **sem** `packageId` / `snapshotId` UUID / `environmentId`; sem ID de instalação da plataforma | `snapshotHash` estrutural canónico (`SchemaSnapshotHasher`); **sem** `checksums.json` por ficheiro nem checksum do ZIP | `completionStatus`, `categoryStatuses[]`, `warnings`, `limitations`; classificação conservadora |
| **ZIP de personalizações** | `WorkspaceExportService` | Pastas `brule`, `ebrule`, `udefs`, `eudefs`, `eventos`, `eeventos`, `teclas`, `campos`, `formc`, `formb`, `ftu` | Nomes de ficheiro derivados de schema/name; **sem manifesto** | Sem checksums no ZIP | Implícita (só o que está no workspace) |
| **ZIP de scan** | `ZipScanExporter` | `manifest.json` + `collectors/{collectorId}/…` + `README.txt` | `InstallationId = "{Server}/{Database}"` (`ScanAppService`) | Sem checksums; manifesto lista `byteLength` sem hash | `CollectorStatus` por collector (`Succeeded` / `SucceededWithWarnings` / `Partial` / `Failed` / `Skipped`) + `Issues` |

**Melhor embrião para IKP 0.1:** Database Schema Snapshot (ADR 0014, `samples/database-schema-snapshot/`).

**Workspace SQLite** (não é pacote de importação): persiste objetos, `artifacts.sha256`, relações declaradas, interpretações; alimenta exports e Desktop. Projeção KR/FieldMapping é em memória (`RelationalKnowledgeProjector`) — **não** exportada como pacote unificado hoje.

Não confundir com `PhcKnowledgeScannerV1` / Snapshot ZIP histórico do ConsoleApp.

---

## 2. Objetivo do IKP 0.1 `[proposta]`

Formato **normalizado de entrada** no PHC Knowledge (plataforma centralizada, PostgreSQL). O Scanner permanece independente; a plataforma **não** o executa em runtime. `[acordado]`

IKP 0.1 define o contrato que a plataforma **aceita**. A capacidade do Scanner de **emitir** IKP 0.1 é uma evolução posterior — esta especificação documenta o alvo e as lacunas.

Princípios:

- Partir do Database Schema Snapshot; **não** unir ingénuamente os três ZIPs; **não** recuperar Snapshot V1 histórico. `[acordado]`
- Conteúdo parcial permitido (categorias opcionais).
- Cobertura explícita; `NotObserved` ≠ `Removed`. `[acordado]`
- Identidade de instalação da plataforma ≠ identidade técnica de origem do Scanner. `[acordado]`
- Staging + publicação controlada; anti-`ReplaceExisting`. `[acordado]`

---

## 3. Estrutura do pacote

### 3.1 Layout ZIP `[proposta]`

```text
{packageId}.zip
├── manifest.json                 # obrigatório — contrato IKP
├── checksums.json                # obrigatório — SHA-256 por entrada do payload (+ ficheiros de topo listados)
├── coverage.json                 # obrigatório — cobertura por categoria/capability
└── payload/
    ├── schema/                   # recomendado em 0.1 se a recolha estrutural existir
    │   ├── manifest.json         # manifesto legado/compatível do schema snapshot (opcional se fundido no IKP)
    │   ├── tables/
    │   ├── relationships/
    │   ├── indexes/
    │   ├── views/
    │   ├── triggers/
    │   ├── procedures/
    │   ├── functions/
    │   ├── types/
    │   ├── synonyms/
    │   └── documentation/
    ├── customizations/           # opcional — artefactos originais (pastas tipo WorkspaceExport)
    ├── relations/                # opcional — REL/DTL/FK factos + interpretações
    ├── knowledge/                # opcional — projeções derivadas (KR/FieldMapping); regeneráveis
    └── diagnostics/              # opcional — issues agregados, limitações narrativas
```

Ficheiros fora deste inventário → rejeição ou quarentena segundo política §7 (recomendação 0.1: **rejeitar** entradas não declaradas no manifesto).

### 3.2 Perfis de conteúdo `[proposta]`

| Perfil | Significado | Obrigatório em 0.1? |
| --- | --- | --- |
| `schema` | Metadados estruturais SQL (base do schema snapshot) | Não — mas se declarado em coverage como `Succeeded*`, o payload deve existir |
| `customizations` | Personalizações PHC | Não |
| `relations` | Relações declaradas / FK / interpretações | Não |
| `knowledge` | Projeções derivadas | Não |
| `diagnostics` | Diagnósticos extra | Não |

Um pacote válido pode conter só `schema`, só `customizations`, ou combinações. O manifesto e `coverage.json` devem declarar o que foi tentado e o resultado.

---

## 4. Identidade do pacote e da instalação

### 4.1 Identidades a distinguir `[proposta]` + `[acordado]`

| Conceito | Onde vive | Notas |
| --- | --- | --- |
| **PackageId** | `manifest.packageId` | UUID do artefacto físico IKP |
| **CaptureId** | `manifest.capture.captureId` | UUID da recolha/observação (substitui o papel de “snapshotId” de captura; **não** é EntityIdentity) |
| **Producer** | `manifest.producer` | `productId`, `productVersion` (Scanner) |
| **OriginIdentity** | `manifest.origin` | Identidade técnica da origem no Scanner (servidor/BD/fingerprint) — **hint**, não ID de plataforma |
| **InstallationId (plataforma)** | Atribuído/confirmado **na ingestão** | ID próprio do PHC Knowledge; **nunca** = `server/database` |
| **EnvironmentId (plataforma)** | Na ingestão / manifesto hint | Ex. `production`, `test` |
| **EntityIdentity** | Por objeto no âmbito instalação+tipo+chave local | **Sem** CaptureId / PackageId |
| **ObservationIdentity** | Entidade + CaptureId (+ capability) | Ocorrência temporal |
| **ContentFingerprint** | Hash canónico do conteúdo observado | ≠ checksum ZIP ≠ hash FTS |

### 4.2 Campos do manifesto — identidade e produção `[proposta]`

| Campo | Obrig. 0.1 | Descrição |
| --- | --- | --- |
| `ikpContractVersion` | sim | `"0.1"` |
| `contentType` | sim | `"phc.installation.knowledge.package"` |
| `packageId` | sim | UUID |
| `producedAtUtc` | sim | Instantâneo de empacotamento |
| `producer.productId` | sim | Ex. `"PhcKnowledgeScanner"` |
| `producer.productVersion` | sim | Versão do Scanner |
| `producer.exportTool` | não | CLI/Desktop/host |
| `capture.captureId` | sim | UUID da recolha |
| `capture.startedAtUtc` / `finishedAtUtc` | sim | Janela da recolha |
| `capture.mode` | não | Ex. `schema-snapshot`, `customizations`, `full` |
| `origin.engine` | sim | Ex. `"sqlserver"` |
| `origin.databaseName` | não | Técnico; **não** ID de instalação |
| `origin.serverLabel` | não | Etiqueta não confidencial preferível a FQDN |
| `origin.originFingerprint` | sim se schema | Como no schema snapshot `[código]` |
| `origin.phcVersion` / `phcSignatureDetected` | não | Quando disponível `[código]` |
| `hints.suggestedInstallationKey` | não | Sugestão para UI de associação (nunca autoritativa) |
| `hints.suggestedEnvironmentId` | não | |
| `compatibility.minPlatformIkp` / `maxPlatformIkp` | não | Reservado; 0.1 usa só `ikpContractVersion` |

### 4.3 Associação à instalação na importação `[acordado]` + `[proposta]`

1. Utilizador autenticado escolhe organização.  
2. Sistema apresenta: instalação existente (match por `originFingerprint` / hints / histórico) **ou** criar nova **ou** deixar **pendente** se ambíguo.  
3. `InstallationId` / `EnvironmentId` da plataforma são gravados no registo de importação — **não** reescritos no ZIP (pacote imutável).  
4. Reimportação do mesmo `packageId` + mesmo package checksum ⇒ idempotente.  

---

## 5. Manifesto de artefactos `[proposta]`

Além da identidade, `manifest.json` inclui `artifacts[]`:

| Campo | Obrig. | Descrição |
| --- | --- | --- |
| `artifactId` | sim | Estável dentro do pacote (ex. path normalizado) |
| `path` | sim | Caminho relativo no ZIP (`payload/...`) |
| `category` | sim | `schema` \| `customizations` \| `relations` \| `knowledge` \| `diagnostics` \| `meta` |
| `format` | sim | Ex. `application/json`, `text/plain`, `text/x-prg` |
| `formatVersion` | não | Versão do schema desse artefacto |
| `byteLength` | sim | |
| `sha256` | sim | Deve coincidir com `checksums.json` |
| `required` | sim | Se `true`, ausência ⇒ validação falha |
| `role` | não | Ex. `tables`, `relationships`, `brule-source` |

`checksums.json`: mapa `path → sha256` (hex lowercase) de **todas** as entradas listadas em `artifacts` e dos ficheiros de contrato (`manifest.json` pode autoexcluir-se do próprio hash de pacote — ver §7).

### Distinção de hashes `[proposta]` + `[código]`

| Tipo | Uso no IKP 0.1 |
| --- | --- |
| Package checksum | SHA-256 do ZIP (ou conjunto canónico de entradas) — integridade do transporte |
| Artifact SHA-256 | Por ficheiro em `checksums.json` |
| Structural fingerprint | Ex. `snapshotHash` do schema — igualdade de conteúdo estrutural |
| Search index hash | **Fora do IKP** — só plataforma/FTS |

---

## 6. Conteúdo `[proposta]`

### 6.1 `payload/schema/` (base = schema snapshot atual)

Reutilizar a forma JSON já produzida `[código]`:

- Tabelas/colunas/PK (ficheiro por tabela).  
- FK, índices, views, triggers, procedures, functions, UDTs, synonyms.  
- Classificação `sql_system` / `candidate_custom` / `unknown` — **sem** afirmar PHC Standard.  
- `includesSqlDefinitions` / `safeForPublicShare`.  

EntityIdentity sugerida (plataforma, na ingestão):  
`table:{schema}.{name}`, `column:{schema}.{table}.{column}`, etc., preservando casing original nas propriedades e documentando regra de normalização.

### 6.2 `payload/customizations/` (opcional)

Espelhar pastas do `WorkspaceExportService` `[código]`. Cada ficheiro = artefacto original. Metadados mínimos recomendados (ficheiro índice JSON opcional em 0.1+): `objectKind`, `name`, `schemaName`, `sourceStamp` se conhecido.

Kinds já existentes no Scanner (`ObjectKind`): eventos, regras, udefs, campos `u_*`, formc/formb, ftu, etc.

### 6.3 `payload/relations/` (opcional)

Factos REL/DTL/FK e interpretações versionadas (`parserVersion`). Não misturar com projeções KR.

### 6.4 `payload/knowledge/` (opcional)

KR / FieldMapping marcados como `derived: true`, regeneráveis. Ausência não implica ausência de factos.

### 6.5 Evolução

Categorias novas = entradas opcionais + `coverage` + `artifacts`. Consumidores 0.1 **ignoram** propriedades JSON desconhecidas nos artefactos conhecidos; **rejeitam** `ikpContractVersion` major desconhecida.

---

## 7. Cobertura (`coverage.json`) `[proposta]`

### 7.1 Modelo

```json
{
  "captureId": "...",
  "categories": [
    {
      "categoryId": "schema.tables",
      "status": "Succeeded",
      "scope": "all_user_tables",
      "observedCount": 8229,
      "detail": null
    }
  ],
  "limitations": [ "..." ]
}
```

`status` alinhado ao Scanner onde fizer sentido `[código]` `CollectorStatus` / `SchemaSnapshotCompletionStatus`:  
`Succeeded` | `SucceededWithWarnings` | `Partial` | `Failed` | `Skipped` | `NotAttempted`

### 7.2 Observed / NotObserved / Removed

| Termo | Significado no IKP / ingestão |
| --- | --- |
| **Observed** | Objeto presente no payload com cobertura da categoria bem-sucedida (ou parcial **e** o objeto incluído) |
| **NotObserved** | Objeto não aparece; categoria incompleta, falhada, skipped, ou fora de âmbito — **não** é remoção |
| **Removed** | **Não** é um estado do pacote isolado. Só pode ser concluído pela plataforma ao comparar duas capturas com cobertura relevante **completa** e bem-sucedida em ambas |

O comparador atual do schema snapshot (`DatabaseSchemaSnapshotComparer`) trata ausência como Removed — **incompatível** com esta regra `[lacuna]` / `[código]`.

---

## 8. Proveniência `[proposta]`

Por observação materializada na plataforma (não necessariamente serializado por linha no ZIP 0.1):

- `producer.productId` / `productVersion`  
- `origin.*`  
- `captureId`, timestamps  
- `packageId`, path do artefacto, `sha256`  
- `EntityIdentity` + `ObservationIdentity` + `ContentFingerprint` (quando calculável)

CaptureId **não** entra em EntityIdentity. `[acordado]`

---

## 9. Integridade e segurança `[proposta]`

Requisitos mínimos na receção:

1. ZIP legível; entradas sem `..` nem caminhos absolutos (já há padrão em `ZipScanExporter.NormalizeEntryPath` `[código]`).  
2. `manifest.json` + `checksums.json` + `coverage.json` presentes.  
3. `ikpContractVersion` = `0.1` (ou compatível segundo §11).  
4. Cada `artifacts[].path` existe; SHA-256 confere; `byteLength` confere.  
5. Entradas no ZIP não declaradas → **rejeitar** (0.1).  
6. Artefactos `required: true` em falta → rejeitar.  
7. Limites configuráveis: tamanho ZIP, nº de entradas, tamanho por ficheiro.  
8. Pacote **não** é confiável só por `producer.productId`; validar sempre.  
9. Duplicados de `artifactId` ou path → rejeitar.  
10. JSON inválido / encoding não UTF-8 onde exigido → rejeitar.

Falha em qualquer passo de validação ⇒ **não** avança para staging de conhecimento; archive opcional do blob rejeitado para auditoria.

---

## 10. Processo conceptual de importação `[proposta]` + `[acordado]`

```text
1. Receção          → ImportAttemptId; armazenar blob imutável
2. Validação estrutural (layout + manifesto)
3. Integridade (checksums + package checksum)
4. Associação instalação/ambiente (guiada; pode ficar Pending)
5. Interpretação do manifesto + coverage
6. Compatibilidade (ikpContractVersion, perfis)
7. Staging          → KnowledgeRevision candidata (não publicada)
8. Duplicados / diff vs revisão publicada (coverage-aware)
9. Preparação da publicação (relatório Ready / bloqueios)
10. Publicação atómica (pointer swap; revisão anterior intacta até sucesso)
11. Registo resultado + proveniência + notificação (SignalR)
```

| Estado | Significado |
| --- | --- |
| Received / Archived | Blob guardado; **ainda não** é conhecimento publicado |
| Validated | Contrato OK |
| AssociationPending | Falta decisão humana de instalação |
| Staged | Candidato materializado |
| Published | Revisão ativa |
| Rejected / Failed | Sem substituir revisão publicada |

Receber ou arquivar um ZIP **≠** conhecimento importado. Sem `ReplaceExisting` sobre a revisão ativa.

---

## 11. Versionamento e evolução `[proposta]`

| Aspeto | Regra 0.1 |
| --- | --- |
| Significado de `0.1` | Primeiro contrato documentado; pode mudar com breaking changes até `1.0` |
| Campos obrigatórios | Os marcados “sim” nesta especificação |
| Campos opcionais | Extensíveis; desconhecidos no manifesto de topo → rejeitar se críticos à identidade; em JSON de payload conhecido → ignorar com aviso |
| Categorias novas | Opcionais + coverage |
| Scanner antigo | Continua a emitir exports atuais até implementar emissor IKP; plataforma pode ter **adaptadores de ingestão** (fora do contrato IKP) `[pendente]` se útil |
| Compatibilidade | Consumidor declara versões aceites; `0.1` não promete forward-compat estável |

---

## 12. Lacunas e incompatibilidades Scanner ↔ IKP 0.1

| Lacuna | Impacto |
| --- | --- |
| Nenhum export é IKP | Scanner precisa de emissor futuro ou adaptador na plataforma |
| Schema snapshot sem `packageId`/`captureId` UUID / `checksums.json` | Envelope incompleto |
| ZIP personalizações sem manifesto/coverage/checksums | Não importável como IKP sem adaptação |
| ZIP scan usa `server/database` como InstallationId | Contradiz política de IDs `[acordado]` |
| KR/FieldMapping não exportados | Perfil `knowledge` vazio inicialmente |
| REL/DTL só no workspace SQLite | Perfil `relations` exige export novo |
| Comparador schema = Removed sem coverage | Não usar como motor de diff da plataforma tal como está |
| Sem package checksum no schema ZIP | Integridade de transporte a acrescentar |

---

## 13. Decisões, recomendações e pendentes

### Fundamentadas / alinhadas a decisões já acordadas

- IKP a partir do schema snapshot; Scanner fora do runtime; instalação centralizada.  
- InstallationId de plataforma ≠ origin técnica.  
- Staging + publicação; NotObserved ≠ Removed.  
- Separação Entity / Observation / ContentFingerprint.

### Recomendações específicas do IKP 0.1 (para validar)

1. Nome `captureId` (em vez de sobrecarregar `snapshotId`) para a recolha.  
2. Rejeitar ficheiros ZIP não declarados no manifesto.  
3. Três ficheiros de topo obrigatórios: `manifest.json`, `checksums.json`, `coverage.json`.  
4. Perfis de conteúdo todos opcionais no pacote, mas coerentes com coverage.  
5. Adaptadores temporários Scanner→IKP na plataforma **vs** emissor nativo no Scanner — preferir emissor nativo a médio prazo; adaptador só se desbloquear importações piloto `[pendente]`.

### Questões que precisam de validação conjunta `[pendente]`

| # | Questão | Alternativas |
| --- | --- | --- |
| Q1 | O Scanner deve emitir IKP nativamente na próxima evolução, ou a plataforma adapta schema snapshot + ZIP custom no curto prazo? | Emissor nativo (mais limpo) vs adaptador (mais rápido para protótipo) |
| Q2 | `captureId` gerado pelo Scanner ou pela plataforma na receção se ausente? | Scanner (rastreio ponta-a-ponta) vs plataforma (compatibilidade com exports atuais) |
| Q3 | Incluir `payload/schema/manifest.json` duplicado do snapshot ou fundir tudo no IKP `manifest.json`? | Duplicar (menos transformação) vs fundir (um só sítio) |
| Q4 | Política de privacidade (`safeForPublicShare`) — a plataforma rejeita pacotes com SQL definitions ou só etiqueta? | Rejeitar em orgs sem permissão vs aceitar com flag |
| Q5 | Algoritmo exato do package checksum (ZIP bytes vs canonical entry set) | ZIP bytes (simples) vs conjunto canónico (estável a recompressão) |

---

## 14. Próximos passos (após aprovação desta especificação)

1. Validação conjunta deste documento (não está aprovado automaticamente).  
2. Comparação campo-a-campo com exports reais + amostras (`samples/database-schema-snapshot/`) — refinar lacunas.  
3. Decidir Q1–Q5.  
4. Só depois: fixtures sintéticas IKP e esboço de API de importação (**sem** implementação até ordem explícita).

---

## Referências

- Scanner: `DatabaseSchemaSnapshotWriter`, `SchemaSnapshotHasher`, `WorkspaceExportService`, `ZipScanExporter`, `ScanAppService`, `ObjectKind`, ADR 0014.  
- Arquitetura: `technology-and-import-contract-assessment.md`, `product-boundaries.md`, `knowledge-layers.md`, `PROJECT_GUIDELINES.md`.  
- Investigação 05/11 (GitHub): identidade, coverage, anti-ReplaceExisting.
