# P1.A — Contrato de identidade, proveniência e fingerprints

| Campo | Valor |
|---|---|
| Data | 2026-11-06 |
| Estado | **Contrato técnico aprovado e aplicado ao Object Index / IKP 0.1** |
| Fase | P1.A/P1.B — contrato aplicado no Scanner; validação de cobertura completa por kind ainda parcial |
| Âmbito | Scanner PHC, IKP 0.1 e PHC Knowledge |
| Base | investigação P1, especificação IKP 0.1, schemas e contrato de observação/linhagem |

Este documento é a referência normativa para identidade, proveniência e fingerprints no Object Index / IKP 0.1. As decisões foram aplicadas no Scanner. A validação com captura real confirmou a projeção para os kinds efetivamente presentes, mas não cobre todos os 21 kinds nem a estabilidade de identidade entre capturas. Não autoriza recolha indiscriminada nem inferências de identidade fora do âmbito definido.

## 1. Decisões fundamentais

1. **Identidade, observação, proveniência e conteúdo são conceitos distintos.**
2. `SourceObjectId` identifica a referência do objeto na captura. Não é, por si só, prova de identidade lógica persistente.
3. `SourceIdentity` transporta evidência nativa da origem, sempre com esquema e âmbito explícitos.
4. `EntityIdentity`, histórico de observações, decisões de correspondência e linhagem pertencem à plataforma.
5. `CaptureId` identifica a captura; `PackageId` identifica o pacote. Nenhum entra na identidade lógica do objeto.
6. Hashes de conteúdo nunca são promovidos a identidade do objeto.
7. Ausência de definição, ausência de artefacto e ausência do objeto são situações diferentes.
8. O Scanner não declara `Removed`; essa conclusão pertence à plataforma e depende de cobertura compatível.
9. Nesta fase, reutilizam-se os dados já recolhidos. Só se propõem novas recolhas quando uma lacuna concreta impedir uma interpretação correta.

## 2. Identificadores e evidência de origem

### 2.1 SourceObjectId

`sourceObjectId` é uma chave de referência única dentro do índice da captura. Deve manter a estratégia existente por família sempre que seja determinística e não colida dentro do índice.

| Família | Estratégia atual observada | Qualidade da evidência | Interpretação permitida |
|---|---|---|---|
| Personalizações PHC com stamp | ID derivado do stamp PHC | Forte, dentro do âmbito/tabela PHC correspondente | Candidato forte a continuidade; validar unicidade e semântica do stamp |
| Teclas com `tistamp` | ID derivado do stamp | Forte, no âmbito da entidade PHC | Mesmo critério dos stamps PHC |
| Objetos SQL estruturais | Nome qualificado (schema + nome + tipo) | Sintética, dependente do nome | Referência estável enquanto nome e âmbito não mudarem; não prova continuidade após rename |
| Constraints/índices/colunas | Nome/tipo/parent conforme estratégia atual | Sintética e hierárquica | Interpretar sempre com o pai e o âmbito |
| REL/DTL com stamp | ID derivado do stamp | Forte se o stamp for identificador nativo válido | Candidato forte, sujeito ao âmbito da tabela |
| REL/DTL sem stamp | ID atual derivado de hash/atributos de conteúdo | Fraca como identidade | Preservar como referência sintética legada; nunca apresentar o hash como identidade nativa |

Não alterar retroativamente os IDs existentes apenas para os tornar mais elegantes. A eventual substituição de IDs hash-based de REL/DTL por IDs opacos é uma migração futura, com compatibilidade e referências a validar.

### 2.2 SourceIdentity

A propriedade opcional `sourceIdentity` representa uma evidência nativa, não a chave interna da plataforma.

Contrato lógico:

| Campo | Obrigatório | Semântica |
|---|---:|---|
| `scheme` | Sim | Namespace estável do tipo de identificador, por exemplo `phc.stamp`, `sqlserver.object_id` ou `sqlserver.column_id` |
| `value` | Sim | Valor original serializado como string, sem perda de precisão |
| `scope` | Sim | Âmbito mínimo que torna o valor interpretável, por exemplo tabela PHC, base de dados ou objeto-pai |

Regras:
- Não inventar um `scheme` se o identificador não for nativo ou a sua semântica não estiver confirmada.
- Stamp PHC: scope deve identificar a entidade/tabela PHC onde o stamp é único.
- SQL Server `object_id`: scope é a base de dados de origem; não é global nem permanente após drop/recreate.
- SQL Server `column_id`: scope deve identificar o objeto/tabela SQL que contém a coluna; o valor isolado não identifica uma coluna.
- Nome qualificado SQL pode ser transportado em `SourceObjectId) ou metadata, mas não deve ser rotulado como identificador nativo.
- REL/DTL cujo ID atual deriva de hash não recebe `SourceIdentity` de scheme nativo. O hash permanece evidência de conteúdo/derivação, não identidade.
- Não incluir hostnames, credenciais ou outros dados sensíveis no scope. Usar a referência técnica menos identificável que mantenha o âmbito inequívoco.

O schema atual de object-index já admite `scheme`, `value` e `scope`. Não é necessário criar uma nova estrutura nem adicionar `confidence` nesta fase. A qualidade da evidência é determinada pela semântica documentada do scheme e da família, sem fingir uma pontuação quantitativa inexistente.

## 3. Proveniência da observação e operação

Cada objeto do índice tem de apontar para a operação lógica que sustenta a sua presença no inventário.

- `operationId` no objeto significa **operação que observou/inventariou o objeto**.
- A operação que recuperou a definição pode ser diferente da operação de inventário. Quando isso acontece, a proveniência da definição deve ser representada dentro de `definition` através de `collectedByOperationId` (opcional).
- Não atribuir uma operação por inferência ambígua baseada apenas no nome da tabela ou no collector.
- O mapeamento `objectKind → operationId` deve ser explícito e versionado na matriz de compatibilidade. Se um kind puder ser produzido por operações distintas, o mapeamento é resolvido no momento da recolha e guardado no registo; não se escolhe arbitrariamente uma.
- Se não for possível determinar a operação que observou o objeto, o objeto não pode ser usado para sustentar conclusões de cobertura/remoção até a lacuna ser resolvida. Não inventar um operationId genérico.
- Não adicionar `ObservationId` ao Scanner. A plataforma pode derivá-lo de `InstallationId + CaptureId + SourceObjectId` (e `objectKind` se necessário), mantendo a sua chave persistente própria.

A proposta de `collectedByOperationId` é uma extensão opcional do objeto `definition`; antes de ser adotada no schema JSON, confirmar que a distinção inventário/recuperação de definição existe para a categoria concreta. Não tornar o campo obrigatório para todos os objetos.

## 4. Estado individual da definição

O estado da definição responde a uma pergunta específica: **qual é a disponibilidade do conteúdo da definição deste objeto nesta captura?** Não descreve a existência do objeto nem o sucesso global do collector.

| Estado | Significado exato | Evidência mínima |
|---|---|---|
| `Available` | Definição recuperada e conteúdo disponível | Um ou mais artefactos válidos referenciados; fingerprint calculável |
| `Empty` | A definição foi consultada e existe, mas o conteúdo é genuinamente vazio segundo a semântica da categoria | Operação concluída e evidência explícita de valor vazio; não basta faltar artefacto |
| `Unavailable` | A definição era aplicável e foi tentada, mas não foi possível obtê-la ou validá-la | Tentativa registada e issue/resultado que explique a indisponibilidade |
| `NotCollected` | A definição não foi solicitada, foi excluída por configuração, ou não faz parte do âmbito desta captura | Capability/coverage/configuração demonstram que não houve tentativa |

Regras de decisão:
1. Se há artefacto válido com conteúdo, usar `Available`.
2. Se a recolha concluiu e a fonte confirma um conteúdo vazio legítimo, usar `Empty`.
3. Se houve tentativa de recolha e falhou, ou a fonte estava inacessível, usar `Unavailable`.
4. Se não houve tentativa porque a operação não foi solicitada, foi excluída, ou a capacidade não foi executada, usar `NotCollected`.
5. Se a recolha ficou interrompida sem resultado conclusivo, não inventar `Unavailable`: usar `NotCollected` para a definição não observada e preservar a operação como `NotAttempted`/falhada na coverage, conforme o resultado real.
6. A ausência de artefacto, por si só, nunca determina o estado.
7. `Empty` não significa zero objetos no inventário. `observedCount=0` pertence à coverage da operação de inventário.
8. `Unavailable` e `NotCollected` não podem ser colapsados: o primeiro representa tentativa sem sucesso; o segundo ausência de tentativa.
9. Um objeto pode estar presente no índice com definição `NotCollected` ou `Unavailable`.

### 4.1 Relação com coverage

A coverage mantém as três dimensões existentes: resultado da operação, disponibilidade da estrutura e disponibilidade agregada do conteúdo. O estado individual da definição complementa-as e não as substitui.

- `contentAvailability=Empty` na operação de inventário pode significar zero conteúdo/objetos no âmbito consultado; não determina `definition.status=Empty` de um objeto.
- `contentAvailability=NotCollected` na operação de definição é compatível com objetos inventariados cuja definição está `NotCollected`.
- `contentAvailability=Unavailable` é compatível com definições individuais `Unavailable`, mas a coverage agregada não obriga todos os objetos a terem o mesmo estado.
- Falha da operação de inventário impede concluir que objetos ausentes foram removidos.
- A coverage da operação que inventaria o objeto é a referência primária para interpretar presença/ausência; a operação de definição só governa a disponibilidade do conteúdo.

Não acrescentar um quinto estado de definição para `Failed`. Falha é resultado de operação e/ou issue; `Unavailable` é o estado do conteúdo depois de uma tentativa falhada.

## 5. Fingerprints

Todos os fingerprints definidos neste contrato usam SHA-256, representado por 64 caracteres hexadecimais minúsculos. O algoritmo é identificado no objeto serializado.

### 5.1 DefinitionFingerprint

- O hash SHA-256 dos bytes exatos do conteúdo original do artefacto é preservado como hash de integridade do artefacto; muda perante qualquer diferença de bytes, incluindo encoding, espaços ou quebras de linha.
- O DefinitionFingerprint é distinto desse hash de integridade: representa a definição semântica através de SHA-256 sobre uma representação canónica normalizada, específica do objectKind, determinística e versionada.
- Só se ignoram diferenças de apresentação quando uma regra validada demonstra que a normalização preserva o significado. Não existe normalização universal.
- Sem regra segura e validada, a comparação é conservadora: usa-se comparação exacta do original com modo explicitamente identificado, ou não se emite fingerprint semântico. Nunca se declara equivalência semântica por suposição.
- O formato, encoding e hash de integridade do original continuam descritos pelo manifesto do artefacto. A representação canónica nunca substitui o original.
- Se a definição for composta por vários artefactos, calcular o fingerprint composto sobre uma lista determinística ordenada por `artifactId`; cada entrada usa `artifactId + LF + componentDefinitionFingerprint + LF`, codificada em UTF-8, incluindo a versão da política de normalização no envelope canónico. O resultado é novamente hashed com SHA-256. Os hashes de integridade dos artefactos componentes continuam preservados separadamente.
- Só existe para `definition.status=Available`.
- Não calcular hash de string vazia para representar `Empty`; ausência de fingerprint não equivale a hash de conteúdo vazio.
- O SHA-256 do artefacto pode ser reutilizado como hash de integridade, mas não como DefinitionFingerprint semântico, salvo no modo conservador de comparação exacta explicitamente identificado.

### 5.2 MetadataFingerprint

- Hash dos bytes UTF-8 de `metadata` serializado segundo JSON Canonicalization Scheme (RFC 8785).
- O objeto `metadata` tem de ser previamente normalizado segundo uma política explícita por `objectKind`; canonicalizar JSON não substitui a normalização semântica.
- Excluir sempre: timestamps de recolha, duração, ordem de consulta, paths temporários, IDs de execução, CaptureId, PackageId, host/servidor de origem, InstallationId e valores de ambiente que não descrevam o objeto.
- Excluir inicialmente `create_date`/`modify_date` SQL e equivalentes: são metadados observados úteis, mas voláteis e não representam, por si, alteração estrutural/semântica do objeto. Permanecem no metadata quando recolhidos, mas fora da projeção usada para fingerprint.
- Incluir apenas atributos semanticamente relevantes para a categoria. Não incluir valores que sejam apenas contagens, datas de recolha ou diagnósticos.
- Normalização de strings, casing, números, booleanos, nulos, arrays e ordenação deve ser definida por categoria antes da implementação. Não aplicar lower-case universal a nomes SQL ou valores PHC.
- Se ainda não existir política canónica para um kind, omitir `metadataFingerprint` para esse objeto; não gerar um hash aparentemente estável de metadata arbitrário.

A matriz de normalização por `objectKind` é uma dependência de P1.B: deve ser criada para os kinds efetivamente incluídos no primeiro object-index. Não se deve inventar uma lista universal de campos para todos os tipos.

### 5.3 SnapshotHash e StructuralFingerprint

- `SnapshotHash` mantém exatamente o contrato atual do Schema Snapshot, incluindo a influência de `OriginFingerprint` e datas atualmente incorporadas.
- Não alterar `SchemaSnapshotHasher`, nem reinterpretar hashes históricos.
- Não chamar ao SnapshotHash atual `StructuralFingerprint`.
- `StructuralFingerprint` é um conceito futuro e separado: fingerprint de estrutura normalizada, independente da origem e de datas voláteis, com versão e regras próprias. Não é necessário para P1.A nem para o primeiro object-index.
- Os hashes de integridade do ZIP/artefactos também são distintos dos fingerprints semânticos do objeto.

## 6. Política de inclusão no object-index

1. O índice contém objetos efetivamente observados, não linhas artificiais para objetos ausentes.
2. O inventário standard PHC não deve ser repetido apenas para preencher o índice. Reutilizar o snapshot/schema e os dados já recolhidos quando forem suficientes.
3. Só recolher metadados adicionais se uma lacuna demonstrada impedir identidade, proveniência ou comparação correta.
4. Objetos derivados/projetados devem ser identificados como derivados, com versão do projetor e referências às evidências de origem; não os confundir com factos nativos.
5. Relações REL/DTL sem stamp podem aparecer como objetos/relações com SourceObjectId sintético e fingerprint de conteúdo, mas sem alegar identidade nativa forte.
6. A ausência de uma família no índice é interpretada com capabilities e coverage, nunca isoladamente.

## 7. Compatibilidade com IKP 0.1

O contrato IKP 0.1 já prevê:
- `sourceObjectId`, `objectKind`, `sourceIdentity`, `metadata`, fingerprints, `definition.status`, `operationId` e `parentSourceObjectId`;
- os quatro estados `Available`, `Empty`, `Unavailable`, `NotCollected`;
- fingerprints SHA-256;
- separação entre identidade da origem, identidade lógica da plataforma e observação.

Assim, as decisões centrais são compatíveis com IKP 0.1 e não exigem novo identificador persistente no Scanner. Há, contudo, duas lacunas de schema/documentação a resolver antes de P1.B:

1. O schema atual permite `sourceIdentity` como objeto genérico e não descreve os schemes nem o significado do scope. A semântica fica neste contrato; uma enumeração fechada de schemes não deve ser introduzida antes de o inventário real de identificadores estar completo.
2. O schema não representa a operação que recuperou a definição quando difere da operação de inventário. A extensão opcional `definition.collectedByOperationId` deve ser adicionada apenas se a matriz real confirmar esse cenário; o `operationId` de topo continua a significar operação de inventário.

A estrutura dos fingerprints atual (`algorithm`, `value`) é suficiente. Não adicionar um campo `confidence` numérico sem modelo calibrado. A qualidade é semântica e documentada por família.

## 8. Mapeamento ObjectKind → operationId

O mapeamento não deve ser deduzido de forma genérica. A fonte normativa será uma matriz única, versionada com a capability, com estas colunas:

| Campo | Regra |
|---|---|
| objectKind | Valor exato serializado no object-index |
| observedByOperationId | Operação que cria a observação do objeto |
| definitionByOperationId | Operação que recolhe a definição, se separada |
| capabilityVersion | Versão da semântica aplicável |
| collectorId | Collector executor |
| source | Facto, snapshot ou projeção derivada |
| mappingStatus | Confirmed / Ambiguous / NotMapped |
| notes | Limitações e âmbito |

Não preencher os valores da matriz por suposição. A matriz deve ser construída a partir do catálogo P0 efetivamente declarado e dos pontos de emissão reais dos collectors. Mapeamentos ambíguos ficam identificados como tal e bloqueiam o uso do objeto para conclusões coverage-aware até serem resolvidos.

## 9. Responsabilidades da plataforma

A plataforma é responsável por:
- associar CaptureId a uma instalação;
- derivar e persistir a identidade da observação;
- manter EntityIdentity e histórico;
- resolver correspondências e renomeações com evidência e decisões auditáveis;
- aplicar compatibilidade de capabilities e coverage antes de declarar Added/Removed;
- manter linhagem entre instalações sem fundir os respetivos históricos.

O Scanner não recebe InstallationId como autoridade e não mantém histórico multi-captura no SQLite.

## 10. Gaps e plano de fecho — estado atualizado em 2026-11-10

| Gap | Decisão | Fase |
|---|---|---|
| Qualidade dos IDs por família | Validada para os kinds presentes na captura real; cobertura não exaustiva dos 21 kinds | Parcial |
| Schemes e scopes concretos de SourceIdentity | Aplicados no projector; validar famílias/kinds não presentes na captura real | Parcial |
| Estado individual da definição | Implementado no Object Index; validação real limitada aos kinds observados | Parcial |
| MetadataFingerprint por kind | Projeções canónicas implementadas para os kinds suportados pelo projector; validar cobertura completa | Parcial |
| operationId por objeto | Mapping explícito implementado; confirmar famílias ainda não observadas em captura real | Parcial |
| Operação de recolha de definição separada | Confirmar caso real; só então adicionar campo opcional ao schema | P1.B |
| REL/DTL sem stamp | Preservar ID atual como referência sintética; não o tratar como identidade | P1.D |
| StructuralFingerprint | Não necessário; manter separado e futuro | Posterior |

## 11. Restrições originais da fase P1.A — histórico

- Não alterar código funcional ou schemas SQLite.
- Não alterar hashes existentes nem `SchemaSnapshotHasher`.
- Não criar EntityIdentity ou ObservationId no Scanner.
- O exportador IKP foi implementado posteriormente; o importador da plataforma continua por implementar.
- Não recolher tabelas/campos/relações standard PHC por rotina.
- Não inferir remoções, renomeações ou linhagem no Scanner.
- P1.A/P1.B estão aplicadas no código para os kinds suportados; não declarar validação exaustiva enquanto os 21 kinds e a identidade entre capturas não estiverem comprovados.

## 12. Próximo passo — atualizado em 2026-11-10

A etapa P1.B de inspeção e projeção foi implementada. O trabalho pendente é alargar a validação aos kinds não presentes na captura real, testar identidade entre capturas e confirmar o pacote ZIP completo emitido pelo exportador. Não alterar SQLite/collectors sem evidência concreta de lacuna.
