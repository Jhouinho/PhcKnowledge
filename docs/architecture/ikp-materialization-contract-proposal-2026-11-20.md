# IKP — Proposta de contrato de materialização

**Estado:** proposta técnica para discussão; não aprovada como decisão arquitetural.  
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

A eliminação deve ser uma fase separada, posterior a Verified. Antes de eliminar:
- confirmar que a análise está persistida;
- confirmar materialização Verified;
- confirmar que não há worker/lease ativo nem recovery pendente que dependa do pacote;
- confirmar que todos os artefactos necessários têm cópia durável;
- registar a intenção de eliminação.

Depois de eliminar:
- verificar que o ficheiro não existe;
- registar Deleted e instante da eliminação;
- manter Receipt, Analysis, Decision, Materialization e proveniência;
- não manter StorageKey como se o pacote ainda fosse recuperável.

A política para pacotes Rejected, análises Blocked, duplicados e períodos de retenção operacional precisa de decisão explícita. A regra aprovada de ZIP temporário não resolve por si só esses casos de exceção.

## 7. Questões que continuam em aberto

1. **Destino dos bytes dos artefactos:** SQL Server `varbinary(max)` ou armazenamento de conteúdo durável separado, com metadados/índice no SQL Server?
2. **Granularidade transacional:** batches e checkpoints ou transação única para subconjuntos? Definir com base no volume real de capturas.
3. **Política para Rejected/Blocked:** quando e como eliminar ZIP que nunca será materializado?
4. **Reanálise futura:** após eliminar o ZIP, a análise persistida é a evidência imutável suficiente? Se for necessário reanalisar, será necessária nova captura/pacote ou uma retenção excecional explícita.
5. **Retenção e auditoria:** durante quanto tempo manter Receipt, Analysis, Decision, itens de materialização e diagnóstico?
6. **Reversão/correção:** como corrigir uma materialização errada sem apagar a proveniência ou misturar capturas?

Estas questões não devem ser fechadas implicitamente durante a implementação.

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

Até aprovação explícita do contrato e resolução das questões de armazenamento/retensão:
- não criar migrations de materialização;
- não introduzir entidades ou serviços de materialização;
- não alterar o arquivo content-addressed;
- não adicionar eliminação de ZIP;
- não alterar o significado de Receipt Completed, Analysis ou Decision Confirmed.

**Próxima ação recomendada:** decidir primeiro o destino durável dos bytes dos artefactos e a política para pacotes rejeitados; depois fechar o modelo lógico e os estados, antes de implementar.
