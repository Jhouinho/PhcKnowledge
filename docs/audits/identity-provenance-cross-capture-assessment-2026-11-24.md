# Avaliação de identidade e proveniência entre capturas — 2026-11-24

**Repositório:** `Jhouinho/PhcKnowledge`  
**Branch:** `main`  
**Âmbito:** avaliação documental dos contratos existentes e dos resultados agregados da comparação local CS default / dfblock.  
**Natureza:** relatório de evidência e lacunas; não aprova alterações de modelo ou implementação.

## 1. Objectivo

Avaliar se os contratos actuais permitem reconhecer objectos entre capturas e instalações, distinguir identidade de correspondência e preservar a origem do conhecimento PHC.

A comparação local dos pacotes foi executada pelo responsável do projecto. Esta revisão utiliza apenas os resultados agregados comunicados; não acedeu aos ZIP, workspaces, SQLite ou ao relatório local completo. Nenhum conteúdo proprietário da dfblock foi copiado para o repositório.

## 2. Evidência comparativa

| Indicador | CS default | dfblock |
|---|---:|---:|
| Objectos no Object Index | 17 182 | 18 954 |
| Artefactos | 63 | 881 |
| Tabelas físicas | 8 294 | 9 601 |
| Campos físicos `u_*` | 0 | 598 |
| Personalizações PHC | Mínimas | Presentes em mais 10 kinds PHC |

As duas capturas:
- foram produzidas pela mesma build do Scanner;
- passaram as verificações de integridade, hashes e checksums;
- usam IKP 0.1 e Object Index v2;
- têm PackageId/CaptureId distintos e workspaces alinhados com o respectivo CaptureId.

A diferença de inventário demonstra que a captura distingue uma instalação próxima da baseline standard de outra com desenvolvimento significativo. Não prova, por si só, que todos os objectos exclusivos da dfblock sejam personalizações: versão PHC, âmbito, disponibilidade e cobertura também têm de ser considerados.

## 3. Resultado da identidade entre capturas

### 3.1 Stamps PHC

A comparação encontrou 12 correspondências cross-capture com evidência `phc.stamp`, nos kinds `formb` e `formc`.

Isto confirma utilidade prática do stamp dentro do scope PHC documentado para essas famílias. Não demonstra cobertura universal nem autoriza tratar qualquer stamp como global fora do respectivo tipo/tabela.

### 3.2 Identificadores SQL Server

`sqlserver.object_id` não identifica o mesmo objecto entre bases de dados distintas. O scope por base de dados, já previsto no contrato, impede que o valor seja interpretado como identidade global.

Foram observadas cerca de 7 933 tabelas comuns por nome/`SourceObjectId`. Esta é uma correspondência observacional útil, não prova de identidade persistente: nomes podem ser reutilizados, objectos podem ser recriados e a semântica pode mudar.

### 3.3 SourceObjectId

O resultado é coerente com o contrato existente: `SourceObjectId` referencia o objecto dentro da captura e não é, isoladamente, uma identidade lógica permanente.

A comparação por nome pode produzir candidatos de correspondência; não deve fundir automaticamente entidades.

## 4. DefinitionFingerprint e D-022

Os envelopes D-022 foram reportados como estruturalmente correctos nas duas capturas.

Não foi encontrado qualquer DefinitionFingerprint semântico disponível: as definições elegíveis ficaram com `Unavailable / NoValidatedPolicy`.

Consequências:
- a integridade dos artefactos foi validada;
- a disponibilidade das definições pode ser conhecida;
- a comparação semântica das definições não ficou demonstrada;
- não se pode concluir que definições com conteúdos diferentes são semanticamente diferentes, nem que conteúdos iguais são semanticamente equivalentes;
- MetadataFingerprint mantém-se uma dimensão separada e não substitui a comparação da definição.

D-022 comportou-se conforme a política aprovada; a ausência de políticas semânticas validadas é uma capacidade ainda por desenvolver, não uma falha do envelope.

## 5. Coverage e limitações

Procedures e triggers ficaram em estado Partial nas duas capturas.

Na dfblock, 540 triggers têm `definition_missing`, mas a causa não foi diagnosticada nesta tarefa. Não se assume que estejam encriptados apenas por analogia com a CS default.

A comparação de identidade não deve converter indisponibilidade de definição em ausência de objecto, nem uma operação Partial em prova de exclusividade ou remoção.

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

1. Não existe ainda evidência nativa cross-installation suficiente para todos os kinds.
2. A correspondência por nome/`SourceObjectId` não é uma identidade permanente.
3. A política de resolução de candidatos, desempate, confirmação, rejeição e histórico não está fechada em detalhe operacional.
4. A classificação PHC Standard / desenvolvimento / standard alterado / origem desconhecida não está implementada como processo completo. O catálogo standard versionado e a comparação são dependências.
5. Não há fingerprints semânticos disponíveis nas capturas; não foi possível demonstrar diff semântico de definições.
6. `EntityIdentity`, `IdentityMatch` e Observations continuam a ser conceitos de destino, sem materialização persistente no SQL Server.

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

A próxima fase de domínio é fechar, antes da materialização incremental, os contratos de:

1. **Identidade e correspondência:** evidência, scope, precedência, candidatos, estados, confirmação/rejeição e histórico.
2. **Proveniência e classificação:** origem standard, desenvolvimento específico, alteração ao standard e origem desconhecida, sempre sustentadas por evidência e pelo catálogo PHC Standard versionado.
3. **Comparação de definições:** separar integridade, comparação byte-a-byte e comparação semântica; definir políticas por kind apenas quando possam ser validadas.

Depois, validar os contratos com as duas capturas reais, sem modificar os pacotes originais. Só então decidir alterações ao Scanner, ao modelo de domínio e à persistência SQL Server.

## 10. Não realizado / não autorizado por esta avaliação

- Não foram alterados código, collectors, schemas executáveis, SQLite, migrations ou materialização.
- Não foram executadas consultas adicionais ao SQL Server.
- Não foram activadas políticas de fingerprint semântico.
- Não foi classificado individualmente qualquer objecto da dfblock.
- Não foi implementado matching, classificação automática, diff histórico ou RAG.
- Não foram copiados pacotes, payloads ou dados proprietários para o GitHub.

**Conclusão:** a arquitectura conceptual existente é uma boa base e deve ser preservada. A lacuna principal não é a ausência de identificadores no Scanner, mas a falta de um contrato operacional de correspondência e proveniência na plataforma, validado para o âmbito cross-capture e cross-installation.
