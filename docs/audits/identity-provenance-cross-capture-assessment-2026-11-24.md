# Avaliação de identidade e proveniência entre capturas — 2026-11-24

**Repositório:** `Jhouinho/PhcKnowledge`  
**Branch:** `main`  
**Âmbito:** avaliação documental dos contratos existentes, comparação CS default / dfblock e validação temporal C1/C2 na instalação dfblock.  
**Natureza:** relatório de evidência e lacunas; não aprova alterações de modelo ou implementação.

## 1. Objectivo

Avaliar se os contratos actuais permitem reconhecer objectos entre capturas e instalações, distinguir identidade de correspondência e preservar a origem do conhecimento PHC.

A análise comparativa detalhada foi executada localmente pelo responsável do projecto sobre os dois pacotes originais e o respectivo relatório completo foi fornecido para revisão nesta etapa. Este documento consolida os resultados comunicados; não afirma uma execução independente dos ZIP, workspaces ou SQLite por esta revisão. O relatório técnico local e os agregados detalhados permanecem fora do repositório. Nenhum conteúdo proprietário, definição ou código personalizado da dfblock foi copiado para o GitHub.

## 2. Evidência comparativa

| Indicador | CS default | dfblock |
|---|---:|---:|
| Objectos no Object Index | 17 182 | 18 954 |
| Artefactos | 63 | 881 |
| Tabelas físicas | 8 294 | 9 601 |
| Campos físicos `u_*` | 0 | 598 |
| DefinitionFingerprint semântico Available | 0 | 0 |
| Definições Available | 60 | 280 |
| Triggers sem definição | 553 | 540 |

As duas capturas:
- foram produzidas pela mesma build `1.0.0+960eed18828bcbe60d28d072ef50bc0e3e4baf35` do Scanner;
- usam IKP 0.1 e Object Index v2;
- têm PackageId/CaptureId distintos;
- passaram as verificações de integridade reportadas; o ZIP da dfblock reconstruído a partir das partes coincide com o ZIP completo.

A comparação detalhada identificou 19 ObjectKinds na dfblock e 9 na CS default. A dfblock contém mais 10 kinds PHC e 598 campos físicos `u_*`. A diferença de inventário não classifica automaticamente os objectos exclusivos como personalizações: versão PHC, âmbito, disponibilidade, cobertura e catálogo standard continuam a ser necessários.

## 3. Resultado da identidade entre capturas

### 3.1 Stamps PHC

A análise por objecto confirmou **12 pares cross-capture com `phc.stamp`**:
- 1 par em `phc.custom_screen` (`formb`);
- 11 pares em `phc.hidden_grid` (`formc`).

Em cada captura, todos os stamps destes dois kinds são únicos no respectivo scope: zero valores repetidos. Nos 12 pares, a cardinalidade é 1:1 e coincidem `objectKind`, `operationId`, scope, scheme, nome, `SourceObjectId`, `MetadataFingerprint` SHA-256 e metadata projectada. As definições estão Available, mas o DefinitionFingerprint semântico está Unavailable / NoValidatedPolicy.

**Conclusão cross-installation:** existe correspondência observacional demonstrada para estes 12 pares e estas duas capturas. Não demonstra `SameLogicalEntity` persistente ou `SameStandardEntity`, nem cria automaticamente um registo persistente `Correspondence.Confirmed` no modelo D-023.

### 3.1.1 Estabilidade temporal C1/C2 — dfblock

Foram executadas duas capturas Full sucessivas da mesma instalação operacional `DESKTOP-GB9607Q\\SQLEXPRESS/dfblock`, utilizando a mesma build do Scanner `1.0.0+ba3bf7c0839acc634cb47da8c374f536e02d19f4`, IKP 0.1 e Object Index v2.

| Indicador | C1 | C2 | Resultado comparativo |
|---|---:|---:|---:|
| Objectos no índice | 18 955 | 18 955 | Igual |
| `formb` com stamp | 30 | 30 | 30/30 comuns |
| `formc` com stamp | 19 | 19 | 19/19 comuns |
| Duplicados de stamp por família | 0 | 0 | 0 |
| Drift observado nos atributos comparados | — | — | 0 |

- `CaptureId` C1: `113e35df-a4bb-45cc-b08a-2821b54389ae`.
- `CaptureId` C2: `a9f628d4-c906-40cd-937f-ef5161e642f1`.
- `InstallationId` de plataforma: ausente no IKP 0.1. A mesma instalação foi confirmada operacionalmente por servidor SQL, `origin.databaseName=dfblock` e `installation_label`.
- Para os 30 `formb` e 19 `formc`, as chaves `(scheme, scope, value)` foram comuns entre C1/C2, sem duplicados, desaparecimentos, novidades ou drift observado em SOID, nome, metadata/MetadataFingerprint ou disponibilidade da definição. As definições estavam Available nas duas capturas.
- O SOID é derivado do stamp e não constitui evidência independente.
- DefinitionFingerprint semântico continua indisponível (`NoValidatedPolicy`). A ausência de alterações reportadas nessa dimensão não demonstra estabilidade de definição semântica.
- Não houve alterações deliberadas a `formb`/`formc` entre capturas; actividade operacional inevitável não foi auditada independentemente.

**Conclusão temporal:** os stamps `formb` e `formc` demonstraram reprodutibilidade observacional entre duas capturas sucessivas, nesta instalação e janela, sem alterações deliberadas. Não demonstra comportamento sob mutações, generalização a outras instalações/versões, identidade persistente ou correspondência standard. Os hashes dos ZIPs e logical content digests diferem; isso não contradiz o resultado dos stamps, mas a composição exacta do logical content digest não foi auditada nesta avaliação.

### 3.1.2 Controlo T-A — T0/T1 na dfblock

Depois de C1/C2, foi executado T-A como controlo sem mutações deliberadas, na instalação de testes `dfblock`. Foram produzidas duas capturas Full (T0/T1), com **300,0 segundos** sem mutações deliberadas entre elas.

| Indicador | T0 | T1 |
|---|---|---|
| CaptureId | `6dab37bd-…` | `6fcd4606-…` |
| PackageId | `d78a2db7-…` | `ff4a0d16-…` |
| Objectos / payloads | 18 955 / 882 | 18 955 / 882 |

A1 (`Personalização 1`, `formb`) e A2 (`hide_calculos`, `formc`) mantiveram stamp único, `SourceObjectId`, nome/ecrã, `MetadataFingerprint` face à baseline, metadata, definição `Available` e bytes do artefacto. As contagens por `ObjectKind` permaneceram iguais.

Os hashes SHA-256 dos ZIPs e os logical content digests globais diferiram. A composição dos digests não foi auditada; não se conclui que a diferença seja irrelevante, nem se atribui qualquer alteração às duas âncoras.

**Resultado de T-A:** T-A foi aprovado como controlo sem mutações para estas duas âncoras, nesta instalação e janela. O resultado não valida detecção de mutações nem permite generalização a outras instalações/versões. À data do fecho de T-A, nenhuma mutação tinha sido executada e T-B…T-E ainda não tinham sido corridos. T-B foi executado posteriormente e está documentado na secção seguinte. O relatório detalhado de T-A é `artifacts/ikp-te-ta-control-report.md` e permanece fora do repositório.

### 3.1.3 Ensaio T-B — alteração controlada e reposição de definição

T-B foi executado depois de T-A, na mesma instalação de testes `dfblock`, com a build `1.0.0+ba3bf7c0839acc634cb47da8c374f536e02d19f4`, modo Full e IKP 0.1 / Object Index v2. O alvo foi uma única definição `phc.hidden_grid` em `dbo.formc`. As mutações temporárias foram feitas pela interface PHC; cada captura foi read-only.

A primeira captura identificada como T0 encontrou o marcador já presente e não foi aceite como baseline. A investigação apontou uma gravação PHC pelo utilizador `ADM` às 17:02:24 como hipótese mais suportada, mas não provada. O estado foi reposto manualmente e uma nova T0 foi capturada. A baseline recuperada produziu artefacto de 426 bytes, SHA-256 `ec217df6e0d2b11005e24fe56216fd3d0e833ad1f173f7acac7ac0ebe69e8811`, igual byte a byte ao artefacto de T-A. O campo SQL `prop` tinha 289 bytes devido a CRLF, sem diferença no artefacto normalizado.

| Indicador | T0 recuperada | T1 | T2 |
|---|---|---|---|
| CaptureId | `ec8be6c7-40aa-46de-a6ba-1f68b3ba0c6d` | `484bdb48-3bba-4cf2-8108-65c0162090e1` | `cabe1217-8af0-46ea-a73b-d2494862de13` |
| Artefacto | 426 bytes | 467 bytes | 426 bytes |
| SHA-256 do artefacto | `ec217df6…e69e8811` | `0fc66d80…44371469` | igual a T0 |
| `prop` SQL | 289 bytes | 331 bytes | 289 bytes, igual a T0 byte a byte |
| MetadataFingerprint | `4e48a43e…` | igual | igual |
| DefinitionFingerprint | Unavailable / NoValidatedPolicy | igual | igual |
| Definição | Available | Available | Available |

A única diferença de conteúdo T0→T1 foi a linha de comentário de teste acrescentada; a instrução executável permaneceu inalterada. SourceObjectId, SourceIdentity, metadata e MetadataFingerprint não mudaram. As 23 operações de coverage e as 18 955 contagens de objectos mantiveram-se; nenhum outro objecto mudou. O artefacto T1 tem SHA-256 `0fc66d8074dcb41eb225ad02feea57ac7b1bfca100dcf213fd4db56044371469`.

Na reposição, T2 recuperou os bytes originais de `prop` e o artefacto de 426 bytes, com o mesmo SHA-256 de T0. A linha de teste deixou de existir em `dbo.formc`. A única diferença residual reportada fora da definição foi `usrhora` (17:35:50→17:47:09, utilizador `ADM`), consistente com a gravação da reposição. O logical digest global diferiu porque CaptureId integra o pacote; igualando esse identificador, object-index, coverage e declared-relations foram iguais.

**Conclusão limitada:** T-B demonstrou detecção de alteração e reposição byte a byte do conteúdo capturado nesta âncora `formc`. Não demonstrou equivalência/diferença semântica: DefinitionFingerprint permaneceu `Unavailable / NoValidatedPolicy`. Também não demonstra identidade persistente, matching cross-installation, estabilidade universal, renomeação, eliminação, recriação ou generalização a outros ObjectKinds. O relatório completo está em [Ensaio T-B — alteração controlada e reposição](controlled-definition-change-tb-assessment-2026-11-24.md).

### 3.2 Identificadores SQL Server

`sqlserver.object_id` não coincide como triplo `scheme/value/scope` entre as duas bases, porque o scope inclui o nome da base de dados. Entre os 7 933 pares de tabelas candidatos, 6 179 têm o mesmo valor numérico de `object_id` quando o scope é ignorado e 1 754 têm valor diferente. A coincidência numérica não é identidade cross-installation.

Foram identificadas **7 933 tabelas candidatas** por `schemaName + name` (todas em `dbo`), sem duplicados locais: 361 existem apenas na CS e 1 668 apenas na dfblock. Os 7 933 pares têm `SourceObjectId` e `MetadataFingerprint` iguais.

Contudo, apenas **3 028** pares têm o objecto `metadata` completo igual. A investigação detalhada dos restantes **4 905** confirmou que diferem exclusivamente nas datas de catálogo SQL: **3 151** apenas em `modifyDate` e **1 754** em `createDate` + `modifyDate`. `typeDesc`, `isMsShipped`, `classification` e `namePrefixU` são iguais em todos os pares; não há qualquer caso de fingerprint igual com diferenças nos campos projectados. Estas diferenças de datas não demonstram alterações funcionais nem personalizações.

### 3.3 SourceObjectId

O resultado é coerente com o contrato existente: `SourceObjectId` referencia o objecto dentro da captura e não é, isoladamente, uma identidade lógica permanente.

A comparação por nome pode produzir candidatos de correspondência; não deve fundir automaticamente entidades.

## 4. DefinitionFingerprint e D-022

Os envelopes D-022 foram reportados como estruturalmente correctos nas duas capturas.

- CS: 60 definições Available; dfblock: 280.
- Em todos estes casos, o DefinitionFingerprint está Unavailable com `NoValidatedPolicy`.
- DefinitionFingerprints semânticos Available: zero em ambas as capturas.
- Não foi usado hash de bytes como substituto do fingerprint semântico.

Consequências:
- a integridade dos artefactos foi validada;
- a disponibilidade das definições pode ser conhecida;
- a comparação semântica das definições não ficou demonstrada;
- não se pode concluir que definições com conteúdos diferentes são semanticamente diferentes, nem que conteúdos iguais são semanticamente equivalentes;
- MetadataFingerprint mantém-se uma dimensão separada e não substitui a comparação da definição.

**Investigação concluída — MetadataFingerprint de tabelas SQL:** a projecção inclui apenas `typeDesc` e `isMsShipped`; a canonicalização ordena propriedades dos objectos e o fingerprint é SHA-256 da projecção canónica. A metadata descritiva inclui ainda `objectId`, `classification`, `namePrefixU`, `createDate` e `modifyDate`, que não entram no fingerprint. Os testes `Table_Fingerprint_Ignores_Internal_Ids_Dates_And_Derived_Classification` e `Table_Descriptive_Metadata_And_Fingerprint_Use_Different_Field_Sets` confirmam intencionalmente esta separação. A projecção actual mantém-se; não incluir datas ou IDs internos no fingerprint.

## 5. Coverage e limitações

Procedures e triggers ficaram em estado Partial nas duas capturas.

Na dfblock, 540 triggers estão sem definição; na CS, 553. A causa dos 540 triggers sem definição na dfblock não foi diagnosticada. Além disso, nove triggers com nome comum passam de definição Available na CS para Unavailable na dfblock. A causa desta transição também permanece desconhecida; não se assume encriptação, substituição ou outra explicação sem evidência.

A comparação de identidade não deve converter indisponibilidade de definição em ausência de objecto, nem uma operação Partial em prova de exclusividade ou remoção.

REL/DTL:
- 921 factos declarados na CS (626 DTL + 295 REL);
- 923 na dfblock (610 DTL + 313 REL);
- todos reportados como interpretados (`Parsed`);
- 13 interpretações em cada captura têm referências de tabela não resolvidas;
- 373 valores comuns por `(sourceType, sourceStamp)`, agregados entre REL e DTL.

Estes 373 casos são correspondências observacionais de factos declarados, não identidades de Object Index nem prova automática de `SameLogicalEntity` ou `SameStandardEntity`. REL/DTL requerem contrato próprio.

## 6. Contratos existentes e reutilizáveis

A auditoria documental da `main` confirmou que já existem princípios relevantes:

- `p1-identity-provenance-contract.md`: separa identidade lógica, observação, SourceObjectId, SourceIdentity, fingerprints e histórico.
- `p1b-source-identity-scope-matrix-2026-11-06.md`: define schemes e scopes por família; impede tratar nomes e hashes como identificadores nativos.
- `observation-identity-and-lineage-contract.md`: distingue identidade de correspondência; prevê candidatos, evidência, confiança e decisão; impede fusão automática entre instalações.
- `decisoes-aprovadas-2026-11-21.md`, D-018: separa origem do conteúdo, natureza semântica e proveniência.
- D-015: exige catálogo PHC Standard central e versionado; ausência de correspondência não classifica automaticamente um elemento como standard ou personalizado.
- D-019 a D-022: separam integridade, disponibilidade da definição, fingerprint semântico e MetadataFingerprint.

O modelo conceptual coloca `EntityIdentity` no âmbito de uma Installation. Instalações distintas mantêm identidades e históricos próprios; correspondências entre elas podem apoiar reutilização de conhecimento, mas não fundem automaticamente as entidades.

## 7. Lacunas demonstradas

1. A estabilidade temporal dos stamps `formb`/`formc` foi observada em C1/C2 na instalação `dfblock` (30/30 e 19/19). T-B demonstrou alteração e reposição de definição numa âncora `formc`; renomeação, eliminação, recriação e generalização a outras instalações/versões continuam por validar.
2. Não existe ainda evidência cross-installation suficiente para todas as famílias PHC; as restantes famílias com stamp não demonstraram correspondências comuns neste conjunto.
3. A correspondência de 7 933 tabelas por schema+nome continua a ser candidata estrutural, mas a lacuna sobre a MetadataFingerprint está fechada: as diferenças dos 4 905 pares são exclusivamente datas de catálogo SQL. Isto não confirma identidade nem correspondência cross-installation.
4. A política de resolução de candidatos, desempate, confirmação, rejeição e histórico não está fechada em detalhe operacional.
5. A classificação PHC Standard / desenvolvimento / standard alterado / origem desconhecida depende do catálogo standard versionado e da cobertura aplicável; nenhum objecto foi classificado individualmente.
6. Não há fingerprints semânticos disponíveis nas capturas; não foi possível demonstrar diff semântico de definições.
7. A causa dos 540 triggers sem definição na dfblock e das nove transições Available → Unavailable permanece desconhecida.
8. REL/DTL tem evidência de stamps comuns, mas falta contrato próprio de identidade e correspondência para factos declarados.
9. `phc.user_tsql_function` apresenta 6 registos em cada captura, sem `SourceIdentity` e com 6 nomes comuns; é necessário esclarecer a variante/origem dos registos e a diferença face à previsão P1.B para `dbo.ftu`.
10. `EntityIdentity`, `Correspondence` e Observations continuam a ser conceitos de destino, sem materialização persistente no SQL Server.

## 8. Distinções obrigatórias para o desenho seguinte

O contrato seguinte deve manter separadas estas situações:

- **Mesma instalação, capturas sucessivas:** continuidade da identidade lógica dentro da Installation.
- **Instalações diferentes:** correspondência/reutilização potencial, sem fusão de EntityIdentity ou históricos.
- **Identidade comprovada:** evidência nativa válida dentro do scope documentado.
- **Correspondência provável:** atributos ou nomes semelhantes, insuficientes para fusão automática.
- **Sem correspondência demonstrável:** manter em aberto; não classificar automaticamente como novo.
- **Objecto observado apenas na dfblock:** exclusivo desta comparação, não necessariamente personalizado até validar versão, coverage e catálogo PHC Standard.
- **Definição disponível sem fingerprint semântico:** conteúdo existente, comparação semântica ainda não suportada.

## 9. Próxima fase proposta

A próxima etapa documental/técnica deve seguir esta ordem:

1. **Completar a validação de mutações:** T-B já demonstrou alteração e reposição de definição numa âncora `formc`. Continuam por estudar renomeação, eliminação e recriação de `formb`/`formc`, além de outras âncoras, ObjectKinds, instalações e versões. Cada ensaio requer ambiente de testes e autorização explícita; não criar matching automático.
2. **Manter catálogo PHC Standard e classificação em aberto:** especificar versão/edição/cobertura antes de inferir StandardIntact, StandardModified ou InstallationDevelopment.
3. **Tratar REL/DTL separadamente:** propor contrato de identidade/proveniência dos factos declarados, sem os converter em ObjectKinds.
4. **Diagnosticar cobertura SQL:** investigar os triggers sem definição e as nove transições Available → Unavailable sem presumir causa.
5. Só depois apresentar proposta técnica para Scanner, IKP, domínio e persistência SQL Server, sujeita a aprovação explícita.

A matriz D-023 v0.5 consolida a evidência e as limitações por ObjectKind, incluindo T-B; continua draft, não é política operacional.

## 10. Não realizado / não autorizado por esta avaliação

- Não foram alterados código, collectors, schemas executáveis, SQLite, migrations ou materialização.
- Foram executadas leituras SQL para verificar a âncora e comparar os bytes de `prop`; as capturas T0/T1/T2 foram read-only.
- Não foram activadas políticas de fingerprint semântico.
- Não foi classificado individualmente qualquer objecto da dfblock.
- Não foi implementado matching, classificação automática, diff histórico ou RAG.
- Não foram copiados pacotes, payloads ou dados proprietários para o GitHub.
- T-B incluiu gravações temporárias e controladas da definição pela interface PHC, com reposição confirmada byte a byte. Não alterou código, collectors, contratos, SQLite, schemas executáveis, migrations, materialização ou RAG.

**Conclusão:** a arquitectura conceptual existente é uma boa base e deve ser preservada. A lacuna principal não é a ausência de identificadores no Scanner, mas a falta de um contrato operacional de correspondência e proveniência na plataforma, validado para o âmbito cross-capture e cross-installation.
