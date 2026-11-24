# D-023 — Identidade e correspondência cross-installation

| Campo | Valor |
|---|---|
| Decisão | D-023 |
| Data de aprovação | 24-11-2026 |
| Estado | **APROVADA — contrato conceptual e regras normativas** |
| Âmbito | PHC Knowledge, catálogo PHC Standard, identidades locais, correspondências e linhagem |
| Dependências | D-015 a D-022; P1.A/P1.B; contrato de observação, identidade e linhagem |
| Implementação | Não autorizada por esta decisão; requer matriz por `objectKind`, validação com capturas reais e decisão técnica posterior |

## 1. Objectivo

Definir como o PHC Knowledge reconhece que objectos observados em capturas ou instalações diferentes correspondem ao mesmo objecto conceptual, sem fundir identidades locais, históricos ou proveniências.

A decisão separa quatro dimensões que não podem ser confundidas:

1. identidade lógica local;
2. identidade de referência do PHC Standard;
3. correspondência entre identidades;
4. comparação/classificação das definições observadas.

## 2. Modelo de identidade

### 2.1 EntityIdentity — identidade local

- Cada objecto técnico reconhecido numa instalação tem uma `EntityIdentity` própria, no âmbito dessa instalação e do tipo de objecto.
- A identidade local não é partilhada nem fundida com identidades de outras instalações.
- Duas instalações mantêm identidades distintas mesmo quando os objectos têm o mesmo stamp, nome, definição, estrutura ou origem histórica aparente.
- Uma cópia/restauro registada como nova instalação recebe identidades locais próprias; a linhagem com a origem é representada separadamente.
- A identidade local e as observações históricas nunca são reescritas para simular continuidade entre instalações.

### 2.2 StandardEntityIdentity — referência central

- O PHC Standard é representado por um catálogo central, curado e versionado, associado à edição/versão PHC aplicável, conforme D-015.
- Cada entrada conceptual do catálogo tem identidade própria, independente das identidades locais das instalações.
- A identidade da referência standard não substitui nem se torna a `EntityIdentity` de um objecto local.
- A versão do catálogo e a cobertura aplicável são parte da evidência necessária para interpretar uma correspondência ou ausência de correspondência.

### 2.3 Correspondence — relação entre identidades

Uma correspondência é um registo autónomo que relaciona uma ou mais identidades, preservando as identidades e proveniências originais. Não é uma operação de merge.

O modelo não presume cardinalidade universal 1:1. Deve permitir representar relações um-para-um, um-para-muitos ou muitos-para-um quando a evidência e a semântica do tipo de relação o justificarem. Não se presume equivalência quando a relação representa derivação, substituição ou associação.

## 3. Tipos de relação

Os seguintes tipos conceptuais ficam aprovados. A representação física e os identificadores serializados serão definidos na especificação técnica posterior.

| Tipo conceptual | Significado | Implicação |
|---|---|---|
| `SameStandardEntity` | Uma identidade local corresponde a uma entrada conceptual do PHC Standard | Não implica definição intacta |
| `SameLogicalEntity` | Continuidade da mesma entidade lógica entre observações da mesma instalação | Preserva o histórico da identidade local |
| `RenamedFrom` / `RenamedTo` | Relação de renomeação sustentada por evidência | Preserva nomes/identificadores anteriores e posteriores |
| `DerivedFrom` | Um objecto foi derivado de outro | Não implica identidade nem equivalência |
| `Replaces` | Um objecto substitui funcionalmente outro | Não implica identidade nem equivalência |
| `RelatedTo` | Relação conhecida sem equivalência de identidade | Não permite fundir nem classificar como igual |

A cardinalidade e os tipos admissíveis dependem do tipo de objecto e da evidência. A matriz por `objectKind` deve declarar limitações e casos ainda não suportados.

## 4. Estado e ciclo de vida da correspondência

Estados conceptuais:

- `Candidate`: existem indícios, ainda insuficientes para confirmar.
- `Confirmed`: correspondência validada com evidência suficiente ou decisão humana fundamentada.
- `Rejected`: hipótese analisada e rejeitada.
- `Revoked`: correspondência anteriormente confirmada que deixou de ser válida.

Cada decisão de correspondência deve preservar, quando aplicável:

- identidades e tipo de relação;
- evidências observadas e respectivas fontes/proveniência;
- método de reconhecimento (automático ou decisão humana);
- motivo e responsável pela decisão humana;
- instante da decisão;
- versão da política e do catálogo standard usados;
- estado anterior, alteração e eventual revogação.

As decisões são auditáveis e reversíveis. Uma correspondência confirmada não é uma verdade imutável: pode ser revista quando surgirem novas evidências ou se alterar a interpretação da fonte. Não se introduzem scores numéricos de confiança sem política calibrada e validada.

## 5. Reconhecimento e critérios de evidência

A avaliação deve ser específica por `objectKind` e respeitar o âmbito dos identificadores:

1. validar tipo, instalação, versão/capability e âmbito da evidência;
2. avaliar identificadores nativos e o respectivo `scope`, conforme P1.A/P1.B;
3. consultar a versão aplicável e a cobertura conhecida do catálogo PHC Standard;
4. avaliar atributos estruturais, relações e definições apenas segundo políticas validadas para o tipo;
5. registar a relação e as evidências, ou manter o resultado como candidato/indeterminado.

Regras obrigatórias:

- Mesmo stamp ou mesmo `SourceIdentity` não é prova universal de identidade cross-installation. A semântica e unicidade do identificador têm de ser validadas no seu scope e para a relação em causa.
- Nome igual, nome qualificado igual, conteúdo semelhante ou fingerprint igual não fundem identidades.
- `SourceObjectId` continua a ser referência da captura, não identidade persistente.
- `SourceIdentity` é evidência contextual, não chave global.
- A ausência no catálogo só pode sustentar «sem correspondência standard identificada» quando a versão, edição, tipo e cobertura do catálogo forem comprovadamente suficientes.
- Se catálogo, cobertura, versão, identidade ou definição forem insuficientes, a classificação é `Unknown`/indeterminada, não «desenvolvimento da instalação».
- Correspondência com uma entidade standard não significa igualdade de definição.
- Correspondência confirmada não determina, por si só, reutilização ou publicação do conteúdo.

## 6. Classificação do objecto — separada da identidade

A classificação conceptual deve distinguir:

| Classificação | Condições mínimas |
|---|---|
| `StandardIntact` | Correspondência confirmada com a referência standard aplicável e definição compatível segundo política validada |
| `StandardModified` | Correspondência confirmada com a referência standard aplicável e divergência demonstrada por comparação válida |
| `InstallationDevelopment` | Sem correspondência standard, com catálogo e cobertura suficientes para sustentar essa conclusão e evidência de desenvolvimento da instalação |
| `Unknown` | Evidência, catálogo, cobertura, versão ou política de comparação insuficientes |

Os nomes são rótulos conceptuais; os valores finais do domínio e o schema serão especificados depois da matriz por tipo. Não classificar por nome isolado. Não confundir «não encontrado» com «não existe no standard».

A comparação de definições permanece subordinada a D-018 a D-022: original preservado, hash de integridade separado, DefinitionFingerprint semântico apenas com normalizador validado e indisponibilidade explícita quando não existir política segura.

## 7. Proveniência, reutilização e RAG

- Cada identidade, observação, artefacto e correspondência conserva a sua proveniência.
- A correspondência com PHC Standard não torna automaticamente o conteúdo local reutilizável, público ou standard.
- Conteúdo originado numa instalação mantém o seu âmbito de acesso e as suas restrições, mesmo quando corresponde a um objecto standard.
- A partilha ou promoção de conhecimento exige regras próprias de governação, validação e autorização; não decorre da correspondência.
- O futuro RAG deverá filtrar por origem, instalação/cliente, natureza do conhecimento, estado de validação, proveniência, permissões e contexto da consulta. Nunca deve expor código ou conteúdo proprietário de uma instalação a outra por defeito.

## 8. Matriz de reconhecimento por objectKind

A matriz documental v0.2, actualizada com a análise comparativa por objecto das capturas CS default e dfblock, está em [D-023 — Matriz de reconhecimento por ObjectKind (v0.2)](d-023-objectkind-recognition-matrix-2026-11-24.md). A análise confirmou 1 par `formb` e 11 pares `formc` com `phc.stamp`, únicos 1:1 nestas duas capturas, e 7 933 candidatos de tabelas SQL por schema+nome. A investigação da MetadataFingerprint confirmou que a projecção de tabelas usa apenas `typeDesc` e `isMsShipped`; os 4 905 casos em que a metadata completa diverge diferem exclusivamente em `createDate` e/ou `modifyDate`. A projecção actual é coerente com os testes do Scanner e mantém-se inalterada. A matriz continua draft: estes resultados não criam automaticamente `Correspondence.Confirmed`, não demonstram `SameLogicalEntity` persistente ou `SameStandardEntity`, não validam classificação e não autorizam implementação.

Antes de qualquer implementação, criar e validar uma matriz com, no mínimo:

| Campo | Conteúdo exigido |
|---|---|
| `objectKind` | Valor exacto do object-index |
| Âmbito | Instalação, tabela/pai, schema ou outro scope relevante |
| Evidência disponível | SourceIdentity, SourceObjectId, metadados, artefactos, relações |
| Força/limitação semântica | O que a evidência permite e não permite concluir; sem score arbitrário |
| Continuidade intra-instalação | Critérios e ambiguidades entre capturas |
| Correspondência standard | Critérios, versão/edição e cobertura necessária |
| Cardinalidade/tipos | Relações permitidas e limitações |
| Comparação de definição | Política validada, se existir; caso contrário indisponível |
| Classificação | Critérios para StandardIntact, StandardModified, InstallationDevelopment ou Unknown |
| Testes | Casos positivos, negativos, ambíguos e de cobertura insuficiente |
| Estado | Confirmado, parcial, ambíguo, não mapeado ou por validar |

Não preencher a matriz por inferência a partir do nome do tipo ou de uma captura parcial. A evidência real das capturas CS default e dfblock deve ser usada sem alterar os pacotes originais.

## 9. Limites e sequência de implementação

D-023 aprova o modelo conceptual e as regras de decisão. Não autoriza:

- alterações ao Scanner, collectors ou workspace SQLite;
- alterações aos schemas executáveis IKP/Object Index;
- migrations ou materialização SQL Server;
- classificação automática de famílias ainda não validadas;
- promoção/publicação de conhecimento de instalações;
- implementação de RAG, embeddings ou base vectorial.

Sequência obrigatória:

1. consolidar documentação e referências cruzadas;
2. produzir a matriz por `objectKind` com base nos contratos e evidência real;
3. validar identidade e correspondências nas capturas CS default e dfblock;
4. registar ambiguidades e lacunas sem inventar resultados;
5. apresentar proposta técnica de alterações ao Scanner, IKP, domínio e SQL Server;
6. obter aprovação antes de implementar.

## 10. Relação com decisões anteriores

D-023 complementa e não revoga D-015 a D-022. Em particular:

- D-015 continua a definir o catálogo PHC Standard e a sua governação;
- D-016 e D-017 continuam a reger captura, cobertura e ausência segura;
- D-018 a D-022 continuam a reger origem, natureza, proveniência, integridade e fingerprints;
- P1.A/P1.B continuam a reger SourceObjectId, SourceIdentity, scopes e fingerprints;
- o contrato de observação e linhagem continua a reger a identidade local e o histórico, agora clarificados por esta decisão.

Em caso de conflito sobre identidade cross-installation, prevalece D-023.
