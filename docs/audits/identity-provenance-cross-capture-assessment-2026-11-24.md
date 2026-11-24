# Avaliação de identidade e proveniência entre capturas — 2026-11-24

**Repositório:** `Jhouinho/PhcKnowledge`  
**Branch:** `main`  
**Âmbito:** avaliação documental dos contratos existentes e dos resultados agregados da comparação local CS default / dfblock.  
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

**Conclusão:** existe correspondência observacional demonstrada para estes 12 pares e estas duas capturas. Não demonstra estabilidade intra-installation, generalização a outras instalações/versões, `SameLogicalEntity` persistente ou `SameStandardEntity`. Também não cria automaticamente um registo persistente `Correspondence.Confirmed` no modelo D-023.

### 3.2 Identificadores SQL Server

`sqlserver.object_id` não coincide como triplo `scheme/value/scope` entre as duas bases, porque o scope inclui o nome da base de dados. Entre os 7 933 pares de tabelas candidatos, 6 179 têm o mesmo valor numérico de `object_id` quando o scope é ignorado e 1 754 têm valor diferente. A coincidência numérica não é identidade cross-installation.

Foram identificadas **7 933 tabelas candidatas** por `schemaName + name` (todas em `dbo`), sem duplicados locais: 361 existem apenas na CS e 1 668 apenas na dfblock. Os 7 933 pares têm `SourceObjectId` e `MetadataFingerprint` iguais.

Contudo, apenas **3 028** pares têm o objecto `metadata` completo igual; **4 905** diferem em campos adicionais ou fora da projecção do fingerprint. O fingerprint coincide, mas a semântica da sua projecção ainda não foi investigada. Assim, os pares permanecem candidatos estruturais, não identidades nem equivalências confirmadas.

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

**Ponto adicional por investigar:** nas tabelas SQL, 7 933 candidatos têm MetadataFingerprint igual, mas apenas 3 028 têm metadata completa igual. É necessário documentar a projecção exacta do MetadataFingerprint (campos incluídos/excluídos e canonicalização) antes de o usar como critério de correspondência.

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

1. A unicidade dos stamps `formb`/`formc` foi observada apenas nas duas capturas analisadas; estabilidade intra-installation e generalização continuam por validar.
2. Não existe ainda evidência cross-installation suficiente para todas as famílias PHC; as restantes famílias com stamp não demonstraram correspondências comuns neste conjunto.
3. A correspondência de 7 933 tabelas por schema+nome é candidata. A projecção de MetadataFingerprint carece de investigação porque 4 905 pares diferem na metadata completa.
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

1. **Investigar MetadataFingerprint SQL:** documentar campos incluídos/excluídos, canonicalização e intenção semântica da projecção; explicar os 4 905 pares com metadata completa diferente.
2. **Validar estabilidade formb/formc:** comparar capturas sucessivas da mesma Installation e recolher evidência noutras instalações/versões, sem criar matching automático nesta fase.
3. **Manter catálogo PHC Standard e classificação em aberto:** especificar versão/edição/cobertura antes de inferir StandardIntact, StandardModified ou InstallationDevelopment.
4. **Tratar REL/DTL separadamente:** propor contrato de identidade/proveniência dos factos declarados, sem os converter em ObjectKinds.
5. **Diagnosticar cobertura SQL:** investigar os triggers sem definição e as nove transições Available → Unavailable sem presumir causa.
6. Só depois apresentar proposta técnica para Scanner, IKP, domínio e persistência SQL Server, sujeita a aprovação explícita.

A matriz D-023 v0.2 consolida a evidência e as limitações por ObjectKind; continua draft, não é política operacional.

## 10. Não realizado / não autorizado por esta avaliação

- Não foram alterados código, collectors, schemas executáveis, SQLite, migrations ou materialização.
- Não foram executadas consultas adicionais ao SQL Server.
- Não foram activadas políticas de fingerprint semântico.
- Não foi classificado individualmente qualquer objecto da dfblock.
- Não foi implementado matching, classificação automática, diff histórico ou RAG.
- Não foram copiados pacotes, payloads ou dados proprietários para o GitHub.

**Conclusão:** a arquitectura conceptual existente é uma boa base e deve ser preservada. A lacuna principal não é a ausência de identificadores no Scanner, mas a falta de um contrato operacional de correspondência e proveniência na plataforma, validado para o âmbito cross-capture e cross-installation.
