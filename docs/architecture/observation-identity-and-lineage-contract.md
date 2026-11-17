# Contrato de observação, identidade e linhagem — consolidação arquitetural

| Campo | Valor |
|---|---|
| Data | 2026-11-17 |
| Estado | **Documento de contexto arquitetural; semântica detalhada consolidada em P1** |
| Âmbito | Scanner independente, IKP e PHC Knowledge |
| Implementação | Documento conceptual de 2026-11-06; Scanner e exportador IKP evoluíram posteriormente |
| Relação | Complementa `ikp-0.1-specification.md` e `collector-coverage-investigation-2026-11-06.md` |

Este documento conserva o contexto arquitetural e as decisões de alto nível sobre cobertura, identidade, observações, compatibilidade entre capacidades e linhagem. A semântica normativa detalhada de identidade da origem, estados de definição e fingerprints está agora em [P1 — Contrato de identidade, proveniência e fingerprints](p1-identity-provenance-contract.md). A especificação IKP 0.1 define a serialização do pacote e referencia esse contrato.

## 1. Fronteiras de responsabilidade

- **Scanner independente:** recolhe factos, identifica objetos na origem, declara capacidades e cobertura da captura, preserva conteúdo original e evidências, produz o IKP.
- **IKP:** transporta uma observação imutável e autocontida da captura, incluindo capacidades, cobertura, índices de objetos, referências a artefactos e proveniência.
- **PHC Knowledge:** associa a captura a uma instalação, mantém entidades lógicas, histórico de observações e versões, compara capturas, reconhece correspondências de identidade e gere linhagem entre instalações.
- O PHC Knowledge não executa o Scanner em runtime.
- O Scanner não depende do catálogo central do PHC Knowledge.

## 2. Modelo de observação e cobertura

A cobertura é declarada por **operação lógica**, não apenas por collector nem obrigatoriamente por cada consulta SQL.

Cada operação deve ter:
- `categoryId`: categoria de conhecimento estável;
- `operationId`: operação lógica estável;
- `capabilityVersion`: versão da capacidade/semântica dessa operação;
- `collectorId`: componente do Scanner que a executa;
- resultado da execução;
- disponibilidade da estrutura de origem;
- disponibilidade agregada do conteúdo, quando aplicável;
- âmbito consultado, contagem observada e referências a issues.

As dimensões são independentes:

### Resultado da operação
`Succeeded`, `SucceededWithWarnings`, `Partial`, `Failed`, `Skipped`, `NotAttempted`.

### Disponibilidade da estrutura
`Present`, `NotPresent`, `NotApplicable`, `Unknown`.

### Disponibilidade do conteúdo
`Available`, `Partial`, `Unavailable`, `NotCollected`, `Empty`.

Regras:
- Zero resultados numa consulta concluída é diferente de estrutura ausente ou consulta falhada.
- Estrutura ausente não significa automaticamente não aplicável.
- Uma categoria não solicitada não equivale a uma categoria falhada.
- Um inventário pode estar disponível mesmo quando algumas definições não podem ser recuperadas.
- A cobertura agregada da categoria não substitui o estado individual de cada objeto.
- Detalhes de falhas e limitações ficam em issues associadas à operação.
- `Removed` nunca é produzido pelo Scanner nem declarado no IKP.

**Nota histórica (2026-11-06):** a frase anterior descrevia a situação observada nessa data. Desde então foram implementados no Scanner o catálogo de capabilities, coverage por operação e exportação IKP 0.1. A compatibilidade concreta e os limites remanescentes estão na [matriz atualizada](ikp-0.1-scanner-compatibility-matrix.md); não usar a descrição histórica como estado atual.

## 3. Capacidades e compatibilidade entre versões do Scanner

O pacote deve declarar as capacidades/operações que a versão do Scanner suporta, independentemente das operações efetivamente executadas na captura.

Isto permite distinguir:
- operação não suportada por aquela versão do Scanner;
- operação suportada, mas não solicitada;
- operação solicitada, mas não tentada/concluída;
- operação executada, com estrutura ausente ou não aplicável;
- operação executada com sucesso, parcial ou falhada.

A versão global do Scanner continua no manifesto. A compatibilidade histórica é determinada também por `operationId` e `capabilityVersion`, não apenas pela versão global.

O PHC Knowledge mantém regras explícitas de compatibilidade entre versões de capacidade. A existência do mesmo `operationId` não prova, por si só, que duas versões têm semântica comparável.

A evolução da capacidade de recolha não pode ser interpretada como alteração retroativa da instalação. Se uma categoria só passar a ser observável numa versão posterior, a primeira captura que a recolhe estabelece a primeira observação válida dessa categoria.

## 4. Identidade lógica, identidade da origem e observações

O modelo da plataforma distingue explicitamente organização, instalação, ambiente, localização técnica, observação e identidade lógica.

- **Organization:** fronteira de propriedade, administração e isolamento do conhecimento privado.
- **Installation:** unidade lógica PHC pertencente a uma organização. Pode conter vários ambientes.
- **Environment:** contexto operacional de uma instalação, por exemplo Production, Test ou Development.
- **DatabaseLocation:** localização técnica e histórica de uma base de dados num ambiente. Não é, por si só, a identidade da instalação nem do ambiente.
- **Observation:** ocorrência de um objeto identificada numa captura. Existe independentemente de a captura já estar associada a uma organização/instalação/ambiente e independentemente de ter identidade lógica confirmada.
- **EntityIdentity:** identidade lógica reconhecida pelo PHC Knowledge no âmbito de uma Installation.
- **SourceObjectId:** referência do objeto no índice de uma captura.
- **SourceIdentity:** evidência nativa da origem, com scheme e scope explícitos.
- **DefinitionFingerprint / MetadataFingerprint:** fingerprints de conteúdo e metadados; nunca são identificadores lógicos.

### 4.1 Organização, instalação e ambientes

Uma Organization pode possuir várias Installations. Cada Installation pode conter vários Environments, normalmente Production, Test e Development.

Uma base de dados de testes que seja cópia/restauro de Production pode representar outro Environment da mesma Installation, quando essa continuidade for confirmada. Uma base de dados pertencente a outra empresa representa outra Installation, ainda que tenha estrutura ou personalizações semelhantes.

A associação de uma base de dados a um ambiente é feita através de DatabaseLocation. Esta entidade regista a localização técnica observada ao longo do tempo, por exemplo DatabaseName e, quando conhecido, ServerName, com período de validade. Mudanças de servidor ou substituições técnicas podem originar novos registos de DatabaseLocation sem criarem automaticamente uma nova Installation.

A relação entre ambientes não é enviada pelo Scanner nem inferida como facto autoritativo a partir do nome da base de dados. É contexto gerido pelo PHC Knowledge.

### 4.2 Observation

Cada Observation representa um objeto efetivamente presente no Object Index de uma Capture.

- A plataforma atribui um ObservationId próprio, independente de InstallationId e de EntityIdentityId.
- A chave de unicidade funcional é (CaptureId, SourceObjectId), assumindo a unicidade de SourceObjectId garantida pelo contrato do Object Index.
- ObservationId não é acrescentado ao IKP nem ao Scanner.
- A Observation pode ser materializada sem contexto confirmado e sem identidade lógica.
- A Observation preserva a referência ao Capture, SourceObjectId, ObjectKind, SourceIdentity, metadados, fingerprints, estado da definição, proveniência e artefactos associados.
- A ausência de contexto ou de EntityIdentity não invalida nem elimina a Observation.

A materialização é uma responsabilidade posterior à receção, validação e arquivo do pacote. Não exige que a associação à Installation/Environment já esteja concluída.

### 4.3 EntityIdentity e correspondência

EntityIdentity pertence a uma única Installation. Pode ser partilhada por Observations associadas a diferentes Environments dessa mesma Installation, mas apenas após uma decisão de correspondência confirmada.

Uma EntityIdentity nunca é partilhada automaticamente entre Installations. A linhagem entre instalações pode fornecer evidência ou candidatos, mas não funde identidades.

A Observation não recebe uma EntityIdentity por defeito. Quando não existe correspondência confirmada:
- a Observation permanece sem EntityIdentity;
- podem ser registados vários candidatos em IdentityMatch;
- os candidatos podem ficar pendentes ou ser rejeitados;
- no máximo uma correspondência pode estar confirmada e ativa para cada Observation;
- a criação de uma nova EntityIdentity exige uma decisão explícita de que o objeto representa uma entidade lógica nova, não sendo criada apenas para preencher uma FK.

A decisão de correspondência regista evidência, confiança quando calibrada, autor, data, estado e justificação. As correspondências rejeitadas e substituídas permanecem no histórico.

### 4.4 Estado por ambiente e ciclo de vida

IdentityEnvironmentState representa o estado conhecido de uma EntityIdentity num Environment, não a identidade em si.

Estados conceptuais:
- Present: observada numa captura associada ao ambiente;
- NotObserved: não observada numa captura comparável, sem evidência suficiente para declarar remoção;
- PendingReview: a cobertura, compatibilidade ou associação exige revisão;
- Removed: remoção concluída pela plataforma com base em evidência e cobertura adequadas.

A ausência numa captura, isoladamente, nunca produz Removed. A plataforma tem de verificar compatibilidade das operações/capacidades, cobertura relevante completa e bem-sucedida, scope comparável e contexto confirmado.

Os eventos globais do ciclo de vida de EntityIdentity são separados dos estados por ambiente. EntityIdentityLifecycleEvent regista acontecimentos como criação, renomeação, desativação ou correção de identidade. Alterações de presença/ausência em ambientes pertencem ao histórico do estado ambiental. Um evento num ambiente não deve ser interpretado automaticamente como alteração global da identidade.

## 5. Identificadores nativos e reconhecimento de continuidade

Sempre que útil e disponível, o Scanner deve recolher identificadores nativos da origem como evidências complementares:
- SQL Server `object_id` para objetos;
- `column_id` para colunas;
- identificadores nativos equivalentes quando existam e sejam relevantes.

Estes identificadores são contextuais à base de dados e podem mudar após eliminação/recriação, migração ou reconstrução. Não são IDs globais nem substituem a identidade lógica da plataforma.

O reconhecimento deve distinguir identidade de correspondência:
1. identificador lógico estável, como stamp PHC;
2. identificador nativo contextual, com âmbito e origem;
3. atributos estruturais e relações como evidência de continuidade;
4. semelhança de nome ou conteúdo apenas como sugestão.

Correspondências sem evidência forte não devem fundir entidades automaticamente. O PHC Knowledge deve poder manter candidatos/correspondências pendentes, com evidências, confiança, decisão e proveniência.

Quando uma renomeação ou correspondência for confirmada, a plataforma preserva identificadores antigos e novos e o acontecimento de identidade; não reescreve observações históricas.

## 6. Observações, fingerprints e histórico

O Scanner produz observações da captura. O PHC Knowledge conserva o histórico.

Cada observação de objeto deve permitir representar:
- identidade e tipo;
- metadados recolhidos;
- estado de disponibilidade da definição;
- referência ao artefacto original, quando exista;
- fingerprints calculáveis;
- evidências e limitações da recolha;
- captura, operação/capacidade e proveniência.

Devem existir dois fingerprints independentes:

- **MetadataFingerprint:** calculado a partir dos metadados relevantes e normalizados do objeto.
- **DefinitionFingerprint:** calculado a partir do conteúdo original da definição técnica, quando disponível.

Dados voláteis (data da recolha, caminho temporário, ordem de consulta, etc.) não entram nos fingerprints. A definição deve inicialmente ser comparada pelo conteúdo exato; normalização semântica/formatting-aware é evolução futura e não deve ser confundida com o hash de integridade do artefacto.

O estado individual da definição deve distinguir pelo menos:
- `Available`;
- `Empty`;
- `Unavailable`;
- `NotCollected`.

A ausência de artefacto não pode ser interpretada automaticamente como definição vazia ou objeto removido.

O índice de objetos do IKP representa os objetos observados naquela captura. Não transporta o histórico inteiro. A plataforma cria e conserva observações históricas e versões de conteúdo ao importar capturas sucessivas.

## 7. Comparação de capturas

O PHC Knowledge compara observações documentadas, considerando cobertura e compatibilidade das capacidades.

Estados conceptuais:
- `Added`: entidade observada pela primeira vez numa operação comparável e com cobertura anterior adequada;
- `Modified`: metadados e/ou definição mudaram;
- `Unchanged`: não existem alterações relevantes;
- `NotObserved`: não há evidência suficiente para determinar presença/ausência;
- `Removed`: conclusão da plataforma após ausência numa captura compatível, com cobertura relevante completa e bem-sucedida.

A plataforma deve distinguir alterações de metadados e alterações de definição. Uma categoria que não era suportada, não foi solicitada, não se aplica ou falhou não pode produzir falsos `Added` ou `Removed`.

A confirmação de remoções pode, se necessário, ser reforçada por mais do que uma captura completa. Esta regra operacional ainda será avaliada no desenho do comparador.

## 8. Instalações, contextos e linhagem

Uma Installation é a unidade lógica de continuidade de uma organização PHC; não equivale necessariamente a uma única base de dados física. Os Environments representam contextos operacionais distintos dentro dessa instalação.

A linhagem é registada por acontecimentos explícitos entre Installations, por exemplo cópia, restauro, migração ou separação de uma instalação. Uma instalação pode ter várias origens e destinos ao longo do tempo; o modelo não impõe uma árvore rígida.

Cada Installation mantém identidades e histórico próprios. A linhagem:
- preserva origem, destino, tipo de acontecimento, data, evidência, método e estado de confirmação;
- pode gerar sugestões para resolução de identidade;
- não funde EntityIdentity nem históricos;
- não implica sincronização nem propagação de alterações;
- exige autorização administrativa e auditoria quando envolve transferência entre Organizations.

### 8.1 Receção, captura e pacote

Capture representa a recolha lógica identificada por CaptureId. PackageReceipt representa cada receção física de um pacote identificado por PackageId.

- Uma Capture pode ter vários PackageReceipts.
- Cada PackageReceipt pertence a uma Capture.
- O mesmo PackageId com o mesmo checksum do ZIP é uma receção repetida idempotente.
- O mesmo PackageId com checksum diferente é um conflito de integridade e nunca substitui o pacote anterior.
- PackageArtifact pertence ao PackageReceipt, preservando a proveniência física dos ficheiros recebidos.
- ObservationArtifact liga uma Observation a um ou mais PackageArtifacts que suportam a sua definição.
- Diagnósticos, cobertura e factos declarados mantêm a proveniência da captura/pacote, sem serem artificialmente convertidos em Observations de objetos.

Pacotes diferentes com o mesmo CaptureId representam diferentes representações/receções da mesma captura lógica, não capturas históricas independentes.

### 8.2 Associação do contexto

A receção, validação, arquivo e materialização de factos da captura não dependem de contexto organizacional confirmado.

CaptureContextAssignment regista a associação confirmada entre uma Capture e uma DatabaseLocation, permitindo resolver o respetivo Environment, Installation e Organization.

- Uma Capture pode não ter qualquer atribuição de contexto.
- O histórico admite várias atribuições ao longo do tempo, mas apenas uma atribuição ativa de cada vez.
- Uma correção não sobrescreve a atribuição anterior: revoga-a e cria uma nova atribuição, com autor, data, motivo e evidência.
- CaptureAssociationCandidate guarda sugestões não confirmadas e não constitui uma associação efetiva.
- A DatabaseLocation pode ter ServerName desconhecido; a ausência desse dado não deve ser preenchida por inferência.
- A reassociação não altera Capture, PackageReceipt, PackageArtifact ou Observation.

Uma alteração de contexto desencadeia reavaliação controlada apenas das correspondências de identidade e comparações dependentes desse contexto. A reavaliação preserva decisões anteriores, resultados, versões de contexto e motivo. Não implica reconstruir indiscriminadamente todo o histórico.

### 8.3 Sequência de processamento

1. Receber, validar e arquivar o pacote original.
2. Registar Capture e PackageReceipt, respeitando idempotência.
3. Numa fase posterior, materializar Observations, relações declaradas e referências a artefactos, mesmo que o contexto ainda esteja pendente.
4. Sugerir e confirmar o contexto através de CaptureContextAssignment.
5. Resolver IdentityMatch e associar EntityIdentity apenas após confirmação.
6. Executar comparações e atualizar estados por ambiente apenas quando o contexto e a cobertura o permitirem.

A primeira fatia de implementação do Knowledge continua limitada a Receive / Validate / Archive, conforme a matriz de compatibilidade. A materialização é uma fase distinta e não deve ser introduzida implicitamente nessa primeira fatia.

## 9. Responsabilidades de serialização no IKP

O IKP deve transportar:
- manifesto com PackageId, CaptureId, produtor e origem técnica;
- declaração de capacidades e versões;
- cobertura por operação lógica;
- índices estruturados de objetos observados;
- estados individuais de disponibilidade;
- referências e integridade de artefactos;
- issues/limitações e proveniência necessária para interpretar a captura.

O IKP não transporta:
- InstallationId interno atribuído pelo PHC Knowledge;
- histórico completo de observações da instalação;
- decisões históricas de correspondência/renomeação próprias da plataforma;
- linhagem global mantida pela plataforma, salvo hints de origem expressamente definidos e não autoritativos.

## 10. Sequência de trabalho original — histórico

A sequência abaixo regista o plano original de 2026-11-06 e está concluída/substituída pela especificação IKP 0.1, contratos P1 e matriz de compatibilidade atual.

1. Rever a especificação IKP 0.1 e incorporar os contratos deste documento, removendo contradições e identificando explicitamente o que é obrigatório/opcional.
2. Criar exemplos JSON/fixtures de referência para manifesto, capabilities, coverage, índice de objetos, identidade de origem e linhagem/hints.
3. Fazer uma matriz de compatibilidade entre o contrato IKP e os modelos atuais do Scanner, com alterações necessárias por componente.
4. Só depois, definir tarefas de implementação separadas:
   - Scanner: capacidades, cobertura granular, identidade de origem, índice estruturado e emissor IKP;
   - PHC Knowledge: ingestão, observações, fingerprints, histórico, comparador coverage-aware e gestão de linhagem.
5. **Concluído/substituído:** o Scanner já tem emissor IKP 0.1; a próxima implementação no Knowledge é Receive / Validate / Archive, conforme a matriz de compatibilidade.

## 11. Limites e assuntos ainda por especificar

Este documento fecha princípios, não todos os detalhes de serialização. Permanecem para a revisão do IKP:
- nomes e schemas JSON definitivos;
- regra canónica de cálculo dos fingerprints;
- matriz explícita de compatibilidade entre versões de capacidade;
- normalização de metadados por tipo de objeto;
- formato e política de resolução de correspondências de identidade;
- representação de acontecimentos de linhagem e respetiva autorização;
- política de confirmação de remoções;
- regras de privacidade/sanitização do conteúdo exportado.

Estes pontos devem ser resolvidos agrupados na revisão do contrato e nos exemplos de referência, evitando novas rondas de decisões isoladas sem artefactos concretos.


## Estado atual — 2026-11-10

A especificação IKP 0.1 e os contratos P1 são as referências atuais. O Scanner já implementa capabilities, coverage, Object Index e exportador IKP. A projeção do Object Index foi validada com uma captura PHC real; a validação end-to-end do ZIP do exportador com essa captura continua pendente. O PHC Knowledge ainda não tem API nem armazenamento central implementados. A primeira fatia é receção, validação e arquivo imutável do pacote, sem associação ou publicação.
