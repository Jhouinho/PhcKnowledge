> **Clarificação de ciclo de vida — 21-11-2026:** as referências neste relatório ao ZIP como fonte imutável dos bytes descrevem a análise/reabertura durante a ingestão. Não estabelecem arquivo permanente. Depois da materialização verificada, os bytes do ZIP são eliminados; os artefactos originais necessários são persistidos no SQL Server. Ver [decisões aprovadas](decisoes-aprovadas-2026-11-21.md).

# Relatório de fase — Incrementos A e B (análise IKP e persistência)

| Campo | Valor |
|---|---|
| Data | 2026-11-19 |
| Branch de origem | `feature/ikp-package-analysis-a` |
| Âmbito A | Reopen + Reader + Analysis — **fechado** |
| Âmbito B | Persistência e auditoria de `PackageImportAnalysis` — **implementado e tecnicamente validado** |
| Migrations | `004_package_import_analyses.sql` (após 001–003); incluída no Install e no helper de bootstrap |
| Publicação Git | **Integrado na `main`** — PR #19 (`PHC Knowledge — IKP Package Analysis (Increments A+B)`); merge `98a0b200cf99098bc37eddf754c3d0bb360ca309` |

> Este relatório fecha os Incrementos **A e B**. Separação mantida: receção/receipt ≠ análise ≠ confirmação ≠ materialização. O Incremento C (confirmação explícita) tem desenho aprovado em documento separado e **não** está implementado.

---

## Parte 1 — Incremento A (análise)

### 1.1 Implementado (facto)

| Capacidade | Evidência |
|---|---|
| Reopen read-only do arquivo | `IRecoverableImmutablePackageStore.OpenArchivedPackageReadAsync`; SHA-256 + length no mesmo stream, depois `Position = 0` |
| Engine partilhado Validator/Reader | `IkpPackageInspectionEngine`; `IkpPackageValidator` e `IkpPackageReader` delegam na mesma passagem |
| Análise tipada | `PackageImportAnalysis` + `IkpPackageAnalysisBuilder` + `MaterializationReadinessEvaluator` |
| Serviço org-scoped | `IkpPackageAnalysisService` via `GetCompletedReceiptByPackageIdAsync` |
| HTTP (contrato final A+B) | Ver §2.6 — GET/POST de análise com `IkpRead` |

### 1.2 Validação com captura real CSDefault202603 (facto)

Executada com harness efémero **fora do repositório**, usando as APIs reais. Receipt em memória (não SQL de produção). ZIP fonte não alterado.

| Métrica | Observado |
|---|---:|
| Object Index | 16 618 |
| SourceIdentity | 8 307 |
| MetadataFingerprint | 16 581 |
| Definições Available / com artefactos | 49 |
| Artefactos totais | 52 |
| declaredRelations / interpretations | 921 / 921 |
| REL / DTL (`sourceType`) | 295 / 626 |
| Interpretações órfãs | 0 |
| Issues | 6 (Warning, `sql.procedure.definition_missing`) |
| Readiness | **ReadyWithWarnings** |

PackageId `b0b7e61f-8ea4-4cf0-a57d-99d37ab220ea`; CaptureId `49826eeb-0388-48bb-a495-2031f30a58a4`; ZipSha256 `dc06320f…266cd5`; LogicalContentDigest `4dfb38fb…1ef190`.

As métricas coincidem com a referência documental anterior. Não foram codificadas como constantes de teste.

### 1.3 Limitações do Incremento A (histórico; parcialmente resolvidas em B)

1. Os seis issues reais **não incluem** `sourceObjectId` — ausente no pacote; o analisador não inventa referências (**mantém-se**).
2. Em A, a análise agregava `Unavailable` sobretudo por contagem; em **B** o modelo inclui lista individual `UnavailableObjects` (**resolvido em B**).
3. Percurso SQL receipt real + HTTP com ZIP real CSDefault202603 **em conjunto** não foi o foco desta fase (harness efémero + testes sintéticos HTTP; SQL de análise em BD de teste — ver §3).
4. Em A a análise era efémera; em **B** passou a ser persistida (**resolvido em B**).
5. Confirmação de importação e materialização **continuam fora** de A e B.

### 1.4 Fora de âmbito de A (na altura)

Persistência (depois entregue em B), confirmação, materialização, Identity Matching, associação Installation/Environment, Scanner, schemas IKP 0.1, formato ZIP.

### 1.5 Testes na implementação A (execução local 2026-11-19)

Filtro Import.Tests (analysis, store open, validator, HTTP analysis): **22 passed**.
Suite Import.Tests na implementação A: **214 passed**.
IKP Integration (export→validate): **3 passed**.

(Evidências posteriores de A+B: ver §3.)

### 1.6 Nota sobre documentação anterior

O relatório de 2026-11-17 ainda descrevia “API HTTP não demonstradas”. Esse estado ficou desatualizado com a superfície IKP HTTP e com a análise A/B.

---

## Parte 2 — Incremento B (persistência da análise)

**Estado:** implementado e integrado na `main` (PR #19); validação unitária, HTTP e SQL Server de teste concluídas (ver §3 e §6).

### 2.1 Objetivo

Persistir e auditar o resultado de `PackageImportAnalysis` para um pacote já recebido, mantendo:

```
Ingestão / Receipt  →  Análise (A+B)  →  Confirmação (C, futuro)  →  Materialização (futuro)
```

A entidade persistida **não** é Capture, Observation nem conhecimento materializado. É o registo da análise de um pacote/receipt.

### 2.2 Modelo de persistência (implementado)

Tabela física `dbo.PackageImportAnalyses` (`004_package_import_analyses.sql`):

| Campo | Papel |
|---|---|
| `AnalysisId` | PK |
| `OrganizationId` | Isolamento |
| `ReceiptId` | Proveniência **first-writer** (FK para `PackageIngestionReceipts`); **não** faz parte da chave única |
| `PackageId` / `CaptureId` | Identidade do pacote/captura |
| `ZipSha256` / `ZipByteLength` | Âncora de integridade do blob |
| `LogicalContentDigest` | Digest lógico do conteúdo IKP |
| `AnalysisSchemaVersion` | Versão do modelo de análise — **`"0.2"`**, **independente** de `ContractVersion` IKP |
| `ContractVersion` | Versão IKP observada |
| `ReadinessStatus` | Ready / ReadyWithWarnings / Blocked |
| `AnalyzedAtUtc` / `CreatedAtUtc` | Metadados temporais |
| `SemanticFingerprint` | Hash da projeção semântica (ver §2.11) |
| `AnalysisJson` | Serialização completa do resultado (inclui `ReceiptId` e `AnalyzedAtUtc` para auditoria) |
| Contagens índice | `ObjectCount`, `ArtifactCount`, `IssueCount`, `UnavailableObjectCount` |

**Chave canónica (UNIQUE):**

`(OrganizationId, PackageId, ZipSha256, AnalysisSchemaVersion)`

Justificação: digest+resumo sozinho não basta para revisão humana nem confirmação futura sem reabrir o ZIP. O ZIP continua a fonte imutável dos bytes; a análise persistida é a fotografia auditável da interpretação.

Não foram criadas tabelas de Observation / PackageArtifact / REL materializados.

### 2.3 Associação, ReceiptId e invalidação

Regras implementadas:

- Só criar/reutilizar análise se existir receipt **Completed** da mesma org com o mesmo `PackageId` (e SHA do receipt na resolução).
- Chave única **omite** `ReceiptId`. `ReceiptId` na linha é a proveniência da **primeira** persistência (first-writer) e **não** é reescrita.
- Pedidos posteriores com a mesma chave canónica **reutilizam** a análise compatível mesmo que a receipt resolvida no pedido seja conceptualmente outra instância — no modelo actual de receipts a receção é idempotente por org+PackageId, pelo que a receipt Completed é estável.
- Nova `AnalysisSchemaVersion` ⇒ nova linha; a confirmação futura deve referir `AnalysisId` concreto (desenho C).

### 2.4 Idempotência e repetição (implementado)

| Situação | Comportamento |
|---|---|
| Primeira análise (POST) | Computar → persistir → devolver |
| POST com análise compatível já persistida | Devolver existente **sem** reabrir o ZIP |
| GET | Só leitura; **não** abre ZIP; **não** faz parse; 404 se ausente |
| Conflito de unicidade (2601/2627) | Ler vencedor (`InsertOrGetExistingAsync`) |
| Deadlock SQL 1205 | Retry limitado (política `SqlServerDeadlockRetry`: até 4 tentativas, delays 25/50/100 ms) |
| Nova `AnalysisSchemaVersion` | Linha separada |

**Não** reanalisar automaticamente no upload. **Não** há refresh forçado nesta fase.

### 2.5 Segurança

- `OrganizationId` só do principal (`OrganizationIdResolver`).
- Política `IkpRead` (`ikp.read`) em GET e POST de análise.
- 404 genérico cross-org / inexistente.

### 2.6 API implementada

| Método | Caminho | Comportamento |
|---|---|---|
| `POST` | `/api/v1/ikp/packages/{packageId}/analysis` | Executa ou reutiliza análise compatível e persiste se necessário |
| `GET` | `/api/v1/ikp/packages/{packageId}/analysis` | Lê a persistida; sem ZIP/parsing; 404 se ausente |

Autorização: `IkpRead`. Sem `analysis:refresh`.

`AnalysisSchemaVersion` persistida: **`0.2`** (inclui `UnavailableObjects`; distinta de ContractVersion IKP).

### 2.7 Findings e revisão humana

No JSON / DTO de análise:

- lista `UnavailableObjects` (sourceObjectId, objectKind, operationId, …);
- issues tal como no pacote (incluindo ausência de `sourceObjectId`);
- coverage, limitations e readiness findings.

Não se inventa `sourceObjectId` em issues que o IKP não fornece. Associação a issue em Unavailable só com ligação explícita quando existir.

### 2.8 Install, preflight e bootstrap

- `PhcKnowledge.Install`: `SqlScriptApplicator.ScriptNames` e Content incluem `004_package_import_analyses.sql` após 003; mensagens “001–004”.
- Host: `SqlServerSchemaPreflightService` exige colunas de `PackageImportAnalyses`.
- Testes: `SqlServerInstallBootstrapTests.EnsureSqlScriptsAsync` aplica **001–004** pela ordem correcta (correção do helper após lacuna 001–003).

### 2.9 Decisões aplicadas na implementação B

1. POST executa/persiste vs GET só lê.
2. Unicidade `(OrganizationId, PackageId, ZipSha256, AnalysisSchemaVersion)`; conflito → vencedor.
3. `ReceiptId` first-writer; fora da chave única e da igualdade/fingerprint semânticos.
4. `AnalysisJson` completo + colunas índice; sem satélites de issues.
5. Versões de schema preservadas; sem refresh forçado.
6. `UnavailableObjects` individuais.
7. Retry limitado 1205; tratamento 2601/2627.

### 2.10 Explicitamente fora do Incremento B

Confirmação de importação (Incremento C — desenho em `increment-c-package-import-analysis-confirmation-design-2026-11-19.md`, **não implementado**); materialização; Identity Matching; CaptureContextAssignment; alterações Scanner/IKP schemas; alteração do arquivo imutável.

### 2.11 SemanticFingerprint e SemanticEquals

- **Projeção semântica explícita** (`PackageImportAnalysisSemanticProjection` / `ToSemanticProjection`).
- **Excluem** apenas: `ReceiptId`, `AnalyzedAtUtc`.
- **Mantêm-se** no `AnalysisJson` completo para auditoria: `ReceiptId` e `AnalyzedAtUtc`.
- Fingerprint = SHA-256 hex do JSON canónico da projeção.
- Distinto de ZipSha256, LogicalContentDigest, DefinitionFingerprint e MetadataFingerprint.

---

## 3. Evidências de validação (antes da publicação)

Estas contagens referem-se a execuções **anteriores** à publicação Git; preservadas como histórico.

| Suite / evidência | Resultado |
|---|---|
| `PhcKnowledge.Import.Tests` (suite completa, consolidação A+B) | **224 passed** |
| `SqlServerPackageImportAnalysisTests` | **3/3 passed**, em **duas** execuções |
| `SqlServerInstallBootstrapTests` (helper 001–004) | **9/9 passed**, em **duas** execuções |
| Migration 004 | Aplicada e reaplicada com sucesso (`IF OBJECT_ID … IS NULL`) na BD `PhcKnowledgeReceiptTests` |
| `git diff --check` (validações anteriores / auditoria) | **exit 0** |

Base de dados de teste: exclusivamente `PhcKnowledgeReceiptTests`. Sem aplicação de migrations em produção.

---

## 4. Limitações de cobertura (trabalho futuro — não falhas dos testes já executados)

1. Não foi forçado um deadlock SQL Server **1205** num teste de integração real (existe teste unitário da política de retry).
2. Não foi executada a instalação E2E através do `SqlScriptApplicator` do Install (o bootstrap aplica 004 via helper espelhado).
3. Não foi validada a recuperação/rearranque do Host após recriação do serviço.
4. A suite SQL Server de integração **completa** não foi executada na estabilização pós-merge; apenas as suites relevantes A+B (análise + bootstrap).

Não afirmar que estes cenários foram testados.

Avisos NuGet observados no build Release (trabalho pendente, fora desta fase):
- Pacote `System.Security.Cryptography.Pkcs` **9.0.4**;
- Avisos **NU1902** / **NU1903**;
- Avaliação e eventual atualização ficam como follow-up, sem impacto nos resultados dos testes já aprovados.

---

## 5. Próximos passos

1. ~~Publicação Git (commit/PR) dos Incrementos A+B~~ — **concluído** (PR #19 na `main`).
2. Incremento C — confirmação explícita: desenho em `increment-c-package-import-analysis-confirmation-design-2026-11-19.md`; **não implementado** (documento local untracked; fora da `main`).
3. Melhorias de cobertura listadas no §4, quando priorizadas.
4. Opt-in ZIP real + SQL + HTTP em conjunto permanece desejável, sem fixtures de cliente no repo.

---

## 6. Estado final pós-merge (estabilização 2026-11-19)

| Item | Resultado |
|---|---|
| PR | **#19** — `PHC Knowledge — IKP Package Analysis (Increments A+B)` |
| Merge commit | `98a0b200cf99098bc37eddf754c3d0bb360ca309` |
| Branch integrada | `main` |
| Build Release | **sucesso**, **0 erros** |
| `PhcKnowledge.Import.Tests` | **224 passed** |
| SQL relevantes | **12 passed** (`SqlServerPackageImportAnalysisTests` 3 + `SqlServerInstallBootstrapTests` 9) |
| Migration 004 | Presente na instalação na ordem **001 → 002 → 003 → 004**; Host preflight cobre `PackageImportAnalyses` |
| Migrations em produção | **Não** aplicadas nesta estabilização |

Build e testes desta secção foram executados na estabilização pós-merge; **não** foram reexecutados nesta atualização documental.
