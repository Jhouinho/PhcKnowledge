# IKP — Proposta de contrato de materialização

**Estado:** proposta técnica em atualização; armazenamento de artefactos aprovado para V1; retenção inicial de rejeitados aprovada em 30 dias, configurável.  
**Data:** 2026-11-20  
**Branch auditada:** `feature/increment-c-package-import-analysis-confirmation`  
**Commit auditado:** `b22c6fb1e9f954a14d02cfe5b0c903c956d0cff3`  
**Âmbito:** desenho do contrato entre análise/decisão humana, materialização do conhecimento e eliminação do pacote IKP.  
**Alterações de código/testes nesta etapa:** nenhuma.

## 1. Conclusão executiva

A branch já persiste três conceitos distintos:

1. **Receipt:** prova durável de receção de um pacote, com organização, PackageId, CaptureId, SHA-256, tamanho, StorageKey e resumo de validação.
2. **PackageImportAnalysis:** interpretação versionada do pacote, persistida em JSON, com readiness e SemanticFingerprint.
3. **PackageImportAnalysisDecision:** histórico append-only de confirmação/rejeição humana, ligado à análise e ao SemanticFingerprint que foi apresentado ao decisor.

Não foi identificada no percurso de importação inspecionado uma quarta camada que materialize os objetos, metadados, definições, artefactos e relações numa base de conhecimento pesquisável. Também não foi identificada uma operação de eliminação do pacote arquivado.

**Conclusão:** Receipt Completed, Analysis persisted e Decision Confirmed são três factos diferentes. Nenhum deles, isoladamente ou em conjunto, prova que os dados de conhecimento estão materializados e recuperáveis sem o ZIP.

A proposta abaixo cria esse quarto conceito sem alterar retroativamente o significado dos três existentes.

## 2. Evidência na implementação atual

### 2.1 Receipt e tentativa de receção

- `PackageIngestionReceipts` guarda ReceiptId, OrganizationId, PackageId, CaptureId, versão do contrato/produtor, SHA-256, tamanho, StorageKey e ValidationSummaryJson.
- `PackageReceiptAttempts` é um mecanismo operacional de recovery da fronteira entre filesystem e SQL Server. Os estados atuais são PendingArchive, Completed e NeedsAttention.
- A conclusão da tentativa arquiva o ZIP e regista o Receipt na mesma transação SQL que marca a tentativa Completed.
- A migração `002_package_receipt_attempts.sql` descreve explicitamente o ZIP validado como imutável e permanente; isto diverge da decisão de produto de ZIP temporário.

### 2.2 Análise

- `PackageImportAnalyses` persiste AnalysisJson, ReadinessStatus, SemanticFingerprint, contagens e identidade do pacote/captura.
- A análise exclui ReceiptId e AnalyzedAtUtc da igualdade semântica; o fingerprint representa o conteúdo semântico analisado.
- `IkpPackageAnalysisService` devolve uma análise persistida compatível quando existe. Quando não existe, abre o ZIP arquivado, revalida-o e calcula a análise.
- O próprio serviço declara que não materializa conhecimento nem confirma importação.

### 2.3 Decisão humana

- `PackageImportAnalysisDecisions` é append-only, com DecisionSequence, ator, issuer, justificação e SemanticFingerprintSnapshot.
- A API Portal permite consultar decisão efetiva/histórico e submeter Confirmed ou Rejected.
- O ator e a organização são resolvidos no servidor a partir da sessão autenticada; não são aceites do body.
- Ready e ReadyWithWarnings permitem Confirmed ou Rejected. Blocked só permite Rejected.
- A decisão confirma/rejeita a análise específica; não existe, no fluxo inspecionado, uma chamada subsequente de materialização.

### 2.4 Readiness

`MaterializationReadinessEvaluator` avalia integridade, compatibilidade, issues e coverage. O nome indica prontidão para materializar, não prova de materialização. Ready significa que a análise não encontrou blockers segundo as regras atuais; não significa que todos os dados tenham sido escritos na base central.


### 2.5 Armazenamento real, leitura e recuperação do ZIP — auditoria de código (2026-11-20)

A inspeção de `PhcKnowledge.Import`, `PhcKnowledge.Host` e da migration `002_package_receipt_attempts.sql` confirmou o seguinte percurso:

**Receção e arquivo**
- `IkpPackageIngestionService` recebe o upload para um ficheiro temporário em `Storage:StagingDirectory`, calcula SHA-256/tamanho e valida o pacote.
- Antes de arquivar, cria/recupera uma tentativa durável em SQL Server (`PackageReceiptAttempts`), incluindo OrganizationId, PackageId, CaptureId, SHA-256, tamanho, StorageKey e StagingKey.
- `FileSystemImmutablePackageStore` guarda o ZIP em `Storage:ArchiveDirectory`, numa estrutura content-addressed `sha256/{prefix}/{hash}.ikp.zip`.
- O arquivo valida novamente o hash e tamanho antes da cópia e depois da cópia temporária. Não substitui um objeto existente; se já existir, volta a verificar a integridade.
- Após criar o objeto, aplica o atributo de só de leitura. A chave de arquivo é determinística e derivada do SHA-256.
- A conclusão da tentativa e a criação do Receipt são feitas numa transação SQL Server, com validação de lease. O filesystem e o SQL Server não partilham uma transação distribuída; a recuperação explícita cobre essa fronteira.

**Recuperação operacional**
- `PackageReceiptRecoveryService` tenta concluir tentativas pendentes usando o staging preservado ou, se este já não existir, verificando se o objeto content-addressed já foi arquivado.
- As leases são renovadas durante operações longas. Falhas transitórias são reagendadas; falhas permanentes passam a `NeedsAttention`.
- O host executa o worker de recuperação periodicamente (configuração por omissão: 60 segundos; lease: 300 segundos).
- `PackageStagingCleanupService` remove apenas ficheiros temporários antigos não referenciados por tentativas `PendingArchive` ou `NeedsAttention`. A retenção por omissão é 24 horas.
- Esta limpeza não percorre nem elimina o arquivo content-addressed.

**Leitura para análise**
- `IkpPackageAnalysisService` exige Receipt concluído e usa `IRecoverableImmutablePackageStore.OpenArchivedPackageReadAsync`.
- A leitura verifica SHA-256 e tamanho no stream e reposiciona-o no início antes de o entregar ao leitor IKP.
- A análise persistida é reutilizada quando existe uma linha compatível; nessa situação não é necessário reabrir o ZIP. Se ainda não existir análise, o serviço depende do ZIP arquivado para a calcular.
- O GET da análise lê a análise persistida e não abre o ZIP, mas continua a exigir o Receipt correspondente.

**Ausências confirmadas no âmbito inspecionado**
- Não foi encontrada operação de eliminação do arquivo content-addressed, serviço de retenção de ZIP arquivados ou worker de expiração de pacotes.
- Não foi encontrada capacidade de assinalar um ZIP como eliminado/indisponível no Receipt.
- Não foi encontrada camada de materialização de conhecimento.
- A migration `002_package_receipt_attempts.sql` afirma expressamente que o ZIP validado permanece imutável e permanente. Esta afirmação já não corresponde à direção aprovada de ZIP temporário e terá de ser corrigida numa futura alteração de contrato/migration, sem reescrever migrations históricas aplicadas.

**Implicações para o desenho**
1. A recuperação atual resolve falhas da receção (staging → arquivo → Receipt); não resolve falhas da materialização nem a eliminação segura.
2. O diretório de arquivo é uma dependência operacional separada do SQL Server. Backups/restauros têm de preservar coerência entre a base de dados e o diretório; restaurar apenas um dos lados pode deixar Receipts sem bytes recuperáveis ou bytes órfãos.
3. A eliminação futura terá de remover o atributo ReadOnly antes de apagar o ficheiro e tratar concorrência com leituras/recuperação. Não basta executar `File.Delete` sem controlo de estado e verificação posterior.
4. **Violação de isolamento organizacional identificada:** a chave content-addressed atual é global ao diretório e deriva apenas do SHA-256. Assim, dois pacotes pertencentes a organizações diferentes podem acabar a apontar para o mesmo ficheiro físico. Isto não corresponde ao domínio: cada pacote IKP pertence a uma única organização e não pode ser partilhado nem ter o seu ciclo de vida condicionado por outra organização. A correção aprovada é eliminar a partilha física entre organizações, usando uma localização de arquivo própria por organização e pacote. O SHA-256 continua a validar integridade e a identificar igualdade de conteúdo, mas não determina sozinho a identidade/localização física nem a propriedade do ficheiro.
5. O Receipt é histórico e contém atualmente StorageKey não anulável. A futura disposição do ZIP deve distinguir a chave histórica/proveniência do estado de disponibilidade física; a interface e os serviços não podem apresentar um StorageKey histórico como prova de que o conteúdo ainda existe.
6. A política de 30 dias para Rejected é distinta da retenção de staging (24 horas) e da futura eliminação pós-materialização Verified. São três políticas diferentes e não devem partilhar o mesmo worker/regra sem estados e critérios explícitos.

## 3. Separação de conceitos a preservar

| Conceito | O que prova | O que não prova |
|---|---|---|
| Receipt Completed | Pacote validado e receipt registado | Análise concluída ou conhecimento persistido |
| Analysis persisted | Resultado de análise persistido e recuperável em SQL Server | Conteúdo/bytes dos artefactos materializados |
| Decision Confirmed | Uma pessoa autorizada confirmou aquela análise/fingerprint | Escrita integral, consistência ou recuperabilidade do conhecimento |
| Materialization Verified | Todos os elementos abrangidos pelo contrato foram persistidos e verificados | Eliminação física do ZIP |
| Package Deleted | O pacote e temporários elegíveis foram eliminados com sucesso | Por si só, validade semântica dos dados |

Não reutilizar o estado `Completed` da tentativa de receipt como estado de importação/materialização.

## 4. Contrato proposto

### 4.1 Identidade e vínculo de proveniência

Cada materialização deve ficar vinculada, no mínimo, a:

- OrganizationId;
- ReceiptId;
- AnalysisId;
- PackageId;
- CaptureId;
- ZipSha256 e ZipByteLength;
- LogicalContentDigest, quando presente;
- AnalysisSchemaVersion e ContractVersion;
- SemanticFingerprint da análise;
- DecisionId e DecisionSequence da decisão Confirmed;
- instante de início e de conclusão.

A materialização só pode começar se:
1. O Receipt e a Analysis pertencem à mesma OrganizationId, PackageId e CaptureId.
2. A análise persistida corresponde ao ZIP/SHA-256 do Receipt.
3. A decisão efetiva é Confirmed.
4. SemanticFingerprintSnapshot da decisão coincide exatamente com SemanticFingerprint da análise.
5. A readiness não é Blocked.

A verificação deve voltar a confirmar estes vínculos no servidor no momento de iniciar a materialização; não confiar em IDs ou estados enviados pelo Portal.

### 4.2 Entidades lógicas propostas

Os nomes são indicativos, não decisões de nomenclatura.

**PackageMaterializations** — agregado de execução:
- MaterializationId (PK);
- OrganizationId, ReceiptId, AnalysisId, PackageId, CaptureId;
- ZipSha256, LogicalContentDigest, AnalysisSchemaVersion;
- DecisionId, DecisionSequence, SemanticFingerprint;
- Status, StartedAtUtc, UpdatedAtUtc, VerifiedAtUtc;
- LastErrorCode, LastErrorMessage;
- RowVersion/concorrência otimista.

**PackageMaterializationItems** — resultado por objeto/facto:
- MaterializationId;
- referência ao Object Index/SourceObjectId dentro da captura;
- ObjectKind;
- SourceIdentity, apenas quando existe identidade forte;
- MetadataFingerprint e DefinitionFingerprint, quando aplicáveis;
- estado de materialização e referência persistida;
- chave única idempotente dentro da materialização.

**PackageMaterializedArtifacts** — conteúdo referenciado:
- MaterializationId e referência ao objeto/captura;
- categoria, ContentType, tamanho e SHA-256;
- referência de armazenamento durável;
- distinção entre bytes originais e representações derivadas;
- estado de verificação.

A estrutura física final deve respeitar o modelo de domínio já aprovado e ser desenhada antes de criar migrations. Não criar uma segunda identidade permanente a partir de SourceObjectId: este é uma referência local à captura. Quando não existe SourceIdentity forte, a identidade deve permanecer limitada à captura.

### 4.3 Conteúdo abrangido

O contrato deve cobrir, de acordo com o conteúdo efetivamente presente e validado no pacote:

- entradas do Object Index e os respetivos metadados;
- definições e artefactos binários/textuais efetivamente referenciados;
- fingerprints de metadados e de definição, sem os confundir;
- relações declaradas REL/DTL e respetivas interpretações, preservadas como factos/evidência distintos das relações derivadas;
- issues, limitações e cobertura relevantes para explicar ausência ou indisponibilidade.

Não transformar automaticamente ausência numa captura em eliminação da realidade anterior. Não inferir que um objeto foi removido apenas porque não aparece num pacote posterior. Não promover customizações de uma instalação a PHC Standard. Não tratar metadados FORMB como código executável nem representações formatadas de FORMC como bytes originais.

### 4.4 Estados propostos

Estados da materialização:
- **Pending:** criada e elegível, ainda sem processamento.
- **Running:** existe um worker/lease que detém a execução.
- **RetryableFailure:** falha transitória; pode ser retomada de forma idempotente.
- **NeedsAttention:** falha permanente, inconsistência ou intervenção necessária.
- **Verified:** todos os itens e artefactos exigidos foram persistidos e passaram as verificações de integridade.

Estados de disposição do pacote, separados dos estados de materialização:
- **Retained:** ZIP ainda necessário para análise/materialização ou recuperação.
- **EligibleForDeletion:** materialização Verified e nenhuma operação pendente depende do ZIP.
- **Deleted:** ZIP e temporários associados eliminados e ausência confirmada.
- **DeletionFailed:** materialização permanece Verified, mas a eliminação falhou; a limpeza pode ser repetida.

A separação permite que uma falha de filesystem depois da materialização não invalide o conhecimento persistido nem faça parecer que a materialização falhou.

### 4.5 Execução, atomicidade e recuperação

A materialização deve ser idempotente por MaterializationId e por chave estável de item. Repetir um pedido não pode duplicar objetos, observações ou conteúdo.

Para capturas potencialmente grandes, não assumir uma única transação SQL gigantesca. Proposta:
1. Criar o agregado de materialização e validar os vínculos.
2. Processar itens em unidades/batches duráveis, cada uma com resultado e contagens persistidas.
3. Persistir bytes dos artefactos num destino durável e verificável. O destino concreto (SQL Server `varbinary(max)` ou armazenamento de conteúdo associado) continua por decidir; não pode depender do ZIP após a eliminação.
4. Executar uma verificação final que reconcilie o conjunto esperado da análise com o conjunto persistido: itens, artefactos, referências, tamanhos, hashes e proveniência.
5. Marcar Verified apenas depois de todas as verificações passarem.
6. Marcar EligibleForDeletion e executar eliminação idempotente do ZIP/temporários.
7. Confirmar ausência física e marcar Deleted.

A recuperação deve distinguir:
- falha antes de qualquer escrita;
- falha a meio de um batch;
- falha depois de persistir tudo mas antes de marcar Verified;
- falha depois de Verified mas antes de eliminar;
- falha depois de eliminar mas antes de registar Deleted.

Em todos os casos, a repetição deve reconciliar o estado existente e nunca apagar conteúdo persistido válido. O ZIP só pode ser removido depois de Verified. A limpeza não pode depender exclusivamente de um processo em memória.

## 5. Critérios mínimos para considerar a materialização concluída

A materialização só pode transitar para Verified quando:

1. A análise, receipt e decisão Confirmed correspondem à mesma organização, pacote, captura e fingerprint.
2. Todos os objetos/factos abrangidos pelo contrato foram contabilizados, incluindo estados explicitamente não materializáveis e respetiva justificação.
3. Todos os artefactos referenciados que o contrato classifica como obrigatórios estão persistidos fora do ZIP.
4. Cada conteúdo persistido passa a verificação SHA-256 e tamanho contra o conteúdo de origem.
5. As referências entre objetos e artefactos não ficam órfãs.
6. A proveniência mantém a ligação à instalação/captura e à evidência de origem.
7. A reconciliação final é persistida e pode ser consultada sem abrir o ZIP.
8. Um teste de leitura independente consegue recuperar o conteúdo materializado sem recorrer ao pacote.

Contagens iguais, sem validação de referências e hashes, não são prova suficiente.

## 6. Eliminação segura do ZIP

A eliminação deve ser uma fase separada, posterior à elegibilidade definida para aquele pacote. Antes de eliminar:
- confirmar que a análise está persistida, quando aplicável;
- confirmar materialização Verified, no caso de pacote confirmado;
- no caso de pacote Rejected, confirmar que terminou o prazo de retenção aprovado;
- confirmar que não há worker/lease ativo nem recovery pendente que dependa do pacote;
- confirmar que não existe bloqueio de preservação;
- registar a intenção de eliminação.

Depois de eliminar:
- remover apenas o artefacto físico pertencente àquele pacote;
- verificar que o ficheiro não existe;
- registar Deleted e instante da eliminação;
- manter Receipt, Analysis, Decision e auditoria/proveniência;
- preservar a StorageKey como referência histórica, mas expor separadamente que o conteúdo já não está disponível.

A eliminação é sempre delimitada ao pacote e à organização proprietária. Não se consulta nem se altera o ciclo de vida de pacotes de outras organizações para decidir a eliminação. A política de retenção de cada pacote é independente.

## 7. Questões que continuam em aberto

1. **Granularidade transacional:** batches e checkpoints ou transação única para subconjuntos? Definir com base no volume real de capturas.
2. **Prazo de retenção de rejeitados:** 30 dias de calendário após a decisão efetiva Rejected, aprovado; configurável para redução futura.
3. **Reanálise futura:** após eliminar o ZIP, a análise persistida é a evidência imutável suficiente? Se for necessário reanalisar, será necessária nova captura/pacote ou uma retenção excecional explícita.
4. **Retenção e auditoria:** durante quanto tempo manter Receipt, Analysis, Decision, itens de materialização e diagnóstico?
5. **Reversão/correção:** como corrigir uma materialização errada sem apagar a proveniência ou misturar capturas?

A decisão de armazenamento dos bytes foi fechada para a V1. O prazo de 30 dias é uma recomendação para discussão, não uma decisão já aprovada. As restantes questões não devem ser fechadas implicitamente durante a implementação.


## 7.1 Decisão de armazenamento e proposta de retenção de rejeitados (2026-11-20)

### Armazenamento de artefactos — decisão aprovada

Na primeira versão, os bytes originais dos artefactos serão armazenados diretamente em SQL Server, usando `varbinary(max)`. Não será introduzido nesta fase um serviço externo de objetos ou armazenamento de ficheiros.

Os bytes não devem ser colocados nas tabelas de Receipt ou Analysis. O modelo lógico deve manter uma entidade/relação própria para artefactos, com pelo menos:
- identidade do artefacto e vínculo à organização, Receipt, Package, Capture e Analysis;
- tipo/categoria e nome original, quando aplicável;
- conteúdo binário original;
- tamanho em bytes e SHA-256;
- data de armazenamento e estado de verificação.

A entidade e os nomes físicos continuam por desenhar. O SHA-256 e o tamanho devem ser verificados após persistência e numa leitura independente. Os bytes originais são imutáveis; transformações ou representações derivadas devem ser identificadas separadamente.

A implementação deverá considerar explicitamente a capacidade da edição de SQL Server escolhida, o limite de tamanho da base de dados, o crescimento acumulado, backups/restauros e alertas de capacidade. `varbinary(max)` é uma escolha de tipo de coluna, não uma garantia de capacidade ilimitada. A evolução futura para armazenamento externo poderá ser considerada sem alterar o contrato funcional, mas não faz parte da V1.

### Pacotes rejeitados — política proposta

Um pacote com decisão humana efetiva `Rejected` nunca será materializado. Propõe-se:
1. Manter o ZIP durante **30 dias de calendário após a decisão Rejected**, para permitir revisão operacional ou investigação.
2. No fim do prazo, torná-lo elegível para eliminação automática, desde que não exista bloqueio de retenção.
3. Suspender a eliminação quando exista incidente, investigação, pedido de suporte/revisão ou obrigação de preservação registada.
4. Executar a eliminação como operação idempotente e recuperável, registando intenção, resultado, timestamp e confirmação de ausência física.
5. Preservar os registos de Receipt, Analysis, Decision e auditoria da eliminação, sem conservar uma referência que sugira que o ZIP continua disponível.
6. Se for necessária nova análise depois da eliminação, exigir novo pacote/captura; não reconstruir nem fingir acesso aos bytes eliminados.

O prazo inicial de 30 dias foi aprovado em 2026-11-20 e deve ser configurável, para poder ser reduzido futuramente sem alteração de código. A alteração de configuração aplica-se segundo uma regra explícita e auditável, sem apagar histórico nem contornar bloqueios de preservação. A política não autoriza eliminar pacotes sem decisão Rejected, nem resolve ainda a retenção de análises Blocked sem decisão, pacotes pendentes ou duplicados. Esses casos permanecem fora do âmbito desta regra.



## 7.2 Decisão aprovada — propriedade e isolamento físico dos ZIP (2026-11-20)

### Regra de domínio

Um pacote IKP pertence a uma única organização. Não existe pacote partilhado entre organizações. A propriedade é determinada pelo vínculo persistido do pacote/Receipt à respetiva OrganizationId, e não pelo conteúdo binário.

Dois ZIP podem ter exatamente os mesmos bytes e o mesmo SHA-256 sem serem o mesmo pacote. São duas receções independentes, com identidades, Receipts, decisões, retenções, materializações e ciclos de vida próprios.

### Decisão aprovada

**Cada pacote terá um artefacto físico independente, isolado por organização e por pacote.** O arquivo não pode reutilizar o mesmo ficheiro físico entre organizações, mesmo quando o SHA-256 coincide.

A localização física deverá incluir, de forma segura e não ambígua, a identidade da organização e a identidade do pacote/Receipt (ou uma chave física exclusiva derivada dessas identidades). Exemplo conceptual:

```text
Archive/
  {OrganizationId}/
    {PackageId ou ReceiptId}/
      package.ikp.zip
```

O formato exato da StorageKey e a escolha entre PackageId e ReceiptId como componente do caminho ficam para o desenho técnico, respeitando unicidade e validação de pertença. Não usar o SHA-256 como identificador exclusivo do caminho físico.

### Papel do SHA-256

O SHA-256 mantém-se obrigatório para:
- validar que os bytes recebidos e arquivados são os mesmos;
- verificar integridade em leituras e operações de recuperação;
- detetar igualdade de conteúdo, se isso for útil para diagnóstico ou otimização futura.

O SHA-256 **não** confere propriedade, não funde pacotes e não autoriza partilha de armazenamento físico. Igualdade de conteúdo não significa identidade de pacote.

### Consequências para retenção e eliminação

- A decisão Rejected da Organização A afeta apenas o ZIP pertencente ao pacote da Organização A.
- A eliminação desse ZIP não pode remover, invalidar ou bloquear o ZIP da Organização B, mesmo que os hashes sejam iguais.
- A materialização Verified de um pacote só permite avaliar a eliminação do ZIP desse mesmo pacote.
- Deixam de ser necessárias contagens globais de referências entre organizações para autorizar a eliminação de um ZIP.
- Continuam a ser obrigatórios os controlos locais do próprio pacote: leases, leituras concorrentes, recovery pendente, bloqueios de preservação, confirmação de ausência física e registo auditável.
- A autorização e o caminho de eliminação têm de validar OrganizationId e identidade do pacote no servidor; nunca aceitar um caminho físico arbitrário fornecido pelo cliente.

### Migração do mecanismo atual

O armazenamento atual usa caminhos content-addressed globais `sha256/{prefix}/{hash}.ikp.zip`. Esta implementação não satisfaz a decisão acima e deverá ser substituída por armazenamento isolado por organização/pacote numa alteração futura.

Não alterar migrations históricas já aplicadas. Antes de mudar o armazenamento em código, será necessário definir uma transição segura para pacotes já arquivados: inventariar Receipts e tentativas, copiar/verificar cada pacote para a sua localização isolada, confirmar hashes e tamanhos, atualizar referências de forma recuperável e só depois tratar os ficheiros antigos. Não eliminar nem mover ficheiros existentes nesta etapa documental.

Esta decisão altera apenas o contrato e a direção futura. Não autoriza ainda alterações de código, migrations, workers ou eliminação de ZIP.



## 7.3 Auditoria e plano de transição do arquivo físico (2026-11-20)

### 7.3.1 Estado real confirmado no código

A inspeção da implementação e dos testes na branch confirmou:

- `FileSystemImmutablePackageStore.GetStorageKey(validation)` gera atualmente `sha256/{prefix}/{hash}.ikp.zip`; OrganizationId e PackageId não participam da chave.
- `IExpectedStorageKeyProvider.GetStorageKey` recebe apenas o resultado de validação, pelo que não consegue calcular uma chave isolada por organização.
- `IkpPackageIngestionService` calcula a StorageKey antes de criar a tentativa SQL durável. A tentativa guarda OrganizationId, PackageId, SHA-256, tamanho, StorageKey e StagingKey.
- `PackageReceiptRecoveryService` arquiva novamente usando apenas staging + validação. A implementação atual do store volta a calcular a chave global em vez de receber/usar explicitamente a StorageKey persistida na tentativa.
- A recuperação sem staging verifica o arquivo usando a StorageKey guardada na tentativa.
- `PackageReceiptAttempts` e `PackageIngestionReceipts` guardam StorageKey em `nvarchar(512)`. A conclusão da tentativa verifica que a chave devolvida pelo arquivo coincide com a chave persistida e grava o Receipt e o estado Completed na mesma transação SQL Server.
- O serviço de análise abre o ZIP usando a StorageKey do Receipt, SHA-256 e tamanho. Por isso, a nova resolução de caminho também tem de validar a identidade organizacional/do pacote no percurso de leitura.
- As migrations 001 e 002 têm índices únicos por (OrganizationId, PackageId) e (OrganizationId, SHA-256). Estes índices já isolam a unicidade lógica na base de dados, mas não isolam o caminho físico.
- Os testes de armazenamento e integração assumem atualmente chaves content-addressed globais e terão de acompanhar o novo contrato.

Não foi encontrada, no âmbito inspecionado, uma rotina de migração dos arquivos existentes nem uma rotina de eliminação do arquivo antigo.

### 7.3.2 Contrato técnico necessário para a nova chave

A nova chave física deve ser determinística para a identidade do pacote e incluir, no mínimo, OrganizationId e PackageId. Exemplo conceptual:

```text
organizations/{OrganizationId}/packages/{PackageId}/{ZipSha256}.ikp.zip
```

O formato é ilustrativo; a implementação deve normalizar os GUIDs, validar cada segmento e garantir que o caminho resolvido permanece dentro do diretório raiz.

O SHA-256 permanece no nome como verificação/diagnóstico, mas não é o identificador exclusivo do caminho. Dois pacotes de organizações diferentes terão destinos físicos diferentes, mesmo com o mesmo hash.

A API interna do armazenamento terá de deixar de depender apenas de `IkpPackageValidationResult`:
- a geração da chave esperada precisa de receber a OrganizationId autenticada e a identidade do pacote;
- o arquivo deve receber a identidade e/ou a StorageKey esperada já persistida na tentativa, validando que corresponde à OrganizationId, PackageId e SHA-256;
- a recuperação deve arquivar no destino exato persistido na tentativa, sem recalcular uma chave com regras potencialmente diferentes;
- Verify/Open devem validar também a identidade organizacional e PackageId, além do SHA-256 e tamanho;
- a leitura de análise deve fornecer ao store a identidade retirada do Receipt, nunca uma identidade indicada pelo cliente.

A OrganizationId tem de vir do contexto autenticado no percurso de receção. A validação do caminho não pode depender de uma StorageKey arbitrária enviada pelo Portal.

### 7.3.3 Transição dos arquivos já existentes

A transição deve ser uma operação controlada, repetível e sem perda de dados. Sequência proposta:

1. **Preparação e inventário:** identificar todos os Receipts e todas as tentativas, incluindo PendingArchive e NeedsAttention, com OrganizationId, PackageId, SHA-256, tamanho e StorageKey atuais. Identificar também os arquivos físicos existentes e validar a correspondência hash/tamanho.
2. **Janela de manutenção:** parar a receção e os workers de recuperação/limpeza antigos. Confirmar que não existem operações de arquivo em curso nem leases ativas. Fazer backup consistente da base de dados e do diretório de arquivo/staging.
3. **Plano de cópia:** para cada identidade distinta (OrganizationId, PackageId), calcular o destino novo. Se vários registos de organizações diferentes apontarem para o mesmo caminho antigo, criar uma cópia física independente para cada destino.
4. **Cópia sem destruição:** copiar o ficheiro antigo para o destino novo sem mover nem apagar o original. Remover o atributo ReadOnly apenas no destino novo se necessário para a escrita; o original permanece intacto.
5. **Verificação independente:** calcular SHA-256 e tamanho no destino novo e comparar com os valores persistidos em SQL. Não atualizar referências se qualquer verificação falhar.
6. **Atualização SQL por pacote:** numa transação SQL Server, atualizar a StorageKey da tentativa e do Receipt correspondente, sempre condicionando a atualização por OrganizationId, PackageId e SHA-256. Verificar que ambos ficam com a mesma nova chave e que nenhuma linha esperada ficou por atualizar.
7. **Reconciliação global:** confirmar que cada tentativa/Receipt tem um destino novo existente e íntegro; confirmar que os destinos novos são distintos entre organizações; confirmar que não há PendingArchive/NeedsAttention a depender de staging ou de chaves antigas sem tratamento explícito.
8. **Ativação:** só depois da reconciliação, ativar a versão do host que gera, arquiva, recupera, verifica e lê exclusivamente pelo novo contrato isolado.
9. **Retenção do arquivo antigo:** manter o arquivo antigo intacto durante um período de observação/rollback definido operacionalmente. A remoção posterior dos caminhos antigos é uma operação separada, depois de confirmar que nenhuma referência SQL ou processo ativo os utiliza e de validar o backup.

A migração deve ser idempotente: se for repetida, reconhece destinos novos já existentes apenas depois de verificar hash e tamanho; não sobrescreve ficheiros divergentes. Uma falha depois da cópia e antes da atualização SQL deixa, no máximo, uma cópia nova órfã e recuperável, nunca perde o original. Uma falha depois da atualização SQL mantém o destino novo já verificado e o original preservado.

### 7.3.4 Tratamento das tentativas incompletas

- **Completed:** migrar a cópia e atualizar em conjunto as StorageKeys da tentativa e do Receipt.
- **PendingArchive com staging existente:** preservar o staging e migrar a tentativa para o novo destino; a recuperação nova deve continuar a partir da StorageKey persistida.
- **PendingArchive sem staging, mas com arquivo antigo íntegro:** copiar o arquivo antigo para o destino novo, verificar e só depois concluir/atualizar o estado de forma coerente.
- **NeedsAttention:** não descartar nem presumir que o arquivo existe. Inventariar individualmente; recuperar se staging/arquivo forem íntegros ou manter em intervenção manual. Não marcar como concluída apenas por ter sido copiada uma chave.
- **Sem staging e sem arquivo antigo íntegro:** manter NeedsAttention e preservar diagnóstico. Não fabricar um Receipt nem declarar a migração concluída.

### 7.3.5 Regras de segurança e rollback

- Não executar a migração com versões antigas do host ainda a receber ou recuperar pacotes.
- Não apagar nem mover o arquivo antigo durante a cópia ou atualização SQL.
- Não fazer fallback silencioso do novo caminho para o caminho global antigo na operação normal depois do cutover; isso esconderia referências não migradas e reintroduziria dependência do arquivo partilhado.
- Se a reconciliação falhar antes do cutover, manter o host antigo parado e corrigir/repetir a migração; o arquivo antigo continua a ser a fonte recuperável.
- Se for necessário rollback depois do cutover, parar novamente os workers e reverter as StorageKeys de forma controlada, apenas enquanto o arquivo antigo estiver preservado e validado.
- Registar contagens de linhas, ficheiros copiados, hashes verificados, falhas, referências atualizadas e resultado final. O relatório de migração deve permitir provar que nenhum Receipt/tentativa ficou sem arquivo íntegro.

### 7.3.6 Testes obrigatórios antes do cutover

1. Mesmo ZIP/hash recebido por duas organizações cria dois caminhos físicos distintos.
2. Eliminar/expirar o pacote da Organização A não altera nem remove o ficheiro da Organização B.
3. A mesma tentativa recuperada após reinício escreve no destino persistido, sem recalcular uma chave diferente.
4. Recuperação com staging presente e recuperação apenas a partir do arquivo antigo.
5. Cópia repetida para destino novo já existente e íntegro é idempotente.
6. Destino novo existente com hash/tamanho divergente é rejeitado e nunca sobrescrito.
7. Falha entre cópia, verificação e atualização SQL é recuperável sem perda do original.
8. Atualização SQL mantém StorageKey igual em Attempt e Receipt.
9. Análise abre o ZIP pelo Receipt e valida organização, pacote, hash e tamanho.
10. Chaves com traversal, OrganizationId ou PackageId trocados são rejeitadas.
11. Pacotes PendingArchive e NeedsAttention não são ignorados pela migração.
12. A limpeza de staging continua a proteger tentativas pendentes e não toca no arquivo.

**Estado:** auditoria e sequência de transição documentadas. A implementação da nova chave, a migração de dados/ficheiros, a alteração das APIs internas e os testes continuam por fazer. Não foi alterado código, migration ou arquivo físico nesta etapa.

## 8. Plano de validação antes de implementar

- Testes de elegibilidade e vínculo de fingerprint/decision.
- Testes de isolamento por OrganizationId.
- Testes de idempotência em repetição e concorrência.
- Falha simulada após cada fronteira de batch/transação.
- Verificação de recuperação depois de reinício do processo.
- Integridade de bytes, hashes, tamanhos e referências.
- Teste de leitura integral do conhecimento após eliminação do ZIP.
- Teste de limpeza repetida e de falha de filesystem.
- Teste de que ausência numa captura não elimina observações anteriores.
- Teste de que PHC Standard e observações de instalações permanecem separados.

## 9. Não iniciar ainda

Até aprovação explícita do contrato de materialização e resolução das questões ainda abertas (incluindo granularidade de execução):
- não criar migrations de materialização;
- não introduzir entidades ou serviços de materialização;
- não alterar o arquivo content-addressed;
- não adicionar eliminação de ZIP;
- não alterar o significado de Receipt Completed, Analysis ou Decision Confirmed.

**Próxima ação recomendada:** fechar a granularidade de batches/checkpoints e rever o plano de transição do arquivo isolado com os cenários de recuperação; só depois preparar a implementação e a migração controlada.
