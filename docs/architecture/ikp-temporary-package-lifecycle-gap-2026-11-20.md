# Divergência entre ZIP IKP temporário e arquivo atual

- Data: 2026-11-20
- Branch auditada: `feature/increment-c-package-import-analysis-confirmation`
- Commit de referência da inspeção: `4124fe2b43a9949e54fe9b4b028666059627f3f0`
- Tipo: auditoria read-only de implementação e decisão
- Estado: divergência confirmada; sem alteração de código

## 1. Decisão vigente

A decisão D-007 e a baseline do produto definem o ZIP IKP como transporte temporário. Depois de receção, validação, análise, decisão humana e materialização, o conteúdo necessário deve estar persistido no SQL Server central e a integridade dessa materialização confirmada. Só então o ZIP e os temporários podem ser eliminados.

A decisão não autoriza eliminar o ZIP apenas por existir um receipt, nem antes de a informação necessária estar materializada.

## 2. Comportamento observado no código

### Receção e staging

Em `src/PhcKnowledge.Import/Ikp/IkpPackageIngestionService.cs`:

1. O upload é copiado para um ficheiro temporário `receive-{guid}.tmp` no staging.
2. É calculado SHA-256 e tamanho; o ZIP é validado.
3. Com persistência de tentativas ativa, é criada/reutilizada uma tentativa durável com `StorageKey` e `StagingKey`.
4. O processamento é delegado a `PackageReceiptRecoveryService`.
5. O staging só é removido depois de a tentativa concluir; se ficar pendente, é preservado para recovery.

### Arquivo content-addressed

Em `src/PhcKnowledge.Import/Storage/FileSystemImmutablePackageStore.cs`:

- O ZIP é copiado para `sha256/{prefix}/{hash}.ikp.zip`.
- O ficheiro arquivado é marcado read-only.
- O store verifica e abre o pacote arquivado para leitura.
- A interface `IImmutablePackageStore` só expõe arquivo.
- A interface `IRecoverableImmutablePackageStore` expõe verificação e leitura; não expõe eliminação.

### Dependência da análise

Em `src/PhcKnowledge.Import/Ikp/Analysis/IkpPackageAnalysisService.cs`:

- A análise exige um receipt concluído.
- Quando ainda não existe análise persistida compatível, `ComputeAsync` abre o ZIP arquivado através de `OpenArchivedPackageReadAsync`, volta a ler o pacote e valida PackageId, CaptureId e SHA-256.
- Portanto, a análise atual depende da disponibilidade do ZIP arquivado para a primeira execução/reexecução.

### Limpeza

Em `PackageStagingCleanupService.cs`:

- Só elimina ficheiros antigos na pasta de staging que não estejam protegidos por tentativas.
- Não percorre nem elimina o arquivo content-addressed.
- A retenção de staging por defeito é 24 horas; isto não é política de retenção do ZIP arquivado.

### Materialização

`MaterializationReadinessEvaluator` avalia bloqueios e avisos de prontidão. Não materializa entidades de conhecimento nem prova persistência de observações, definições, relações ou artefactos no modelo central.

## 3. Conclusão da auditoria

<badge> Divergência confirmada </badge>

O comportamento atual é, na prática:

`Upload → Staging → Validação → Tentativa → Arquivo permanente → Receipt → Análise a partir do arquivo`

A decisão aprovada exige:

`Upload → Staging → Validação → Análise → Decisão humana → Materialização → Verificação de persistência → Eliminação do ZIP e temporários`

Não existe ainda evidência de uma implementação completa do segundo percurso. O receipt e a análise persistida não equivalem à materialização integral do conhecimento. A eliminação do arquivo neste momento quebraria a capacidade atual de executar uma primeira análise quando ainda não existe uma análise persistida.

## 4. Consequências

1. Não eliminar manualmente ficheiros do arquivo atual.
2. Não alterar o store para permitir eliminação até estar definido o momento transacional/lógico de conclusão da materialização e o comportamento de recovery.
3. Não tratar `Completed` do receipt como sinónimo de importação/materialização concluída.
4. Preservar o hash, tamanho, identidade do pacote e proveniência no modelo central, mesmo depois de o ZIP ser eliminado.
5. Garantir que análises já persistidas continuam consultáveis sem ZIP; definir o que acontece a uma análise ainda não calculada quando o ZIP foi eliminado.
6. Definir idempotência, concorrência, falha parcial, retries, recuperação após restart e eliminação segura.

## 5. Decisões necessárias antes de implementar

- Qual o evento de domínio que declara a materialização concluída e quem o confirma?
- Que dados IKP têm de ser materializados integralmente para que o ZIP possa ser eliminado?
- Como verificar a integridade da materialização: contagens, hashes por artefacto, relações e referências?
- A análise deve ser obrigatoriamente concluída e persistida antes da eliminação?
- Como suportar reanálise após alteração do schema da análise, sem o ZIP original?
- Como garantir que duas tentativas/processos não eliminam o ZIP enquanto outro ainda o está a ler?
- Como tratar retenção temporária, falhas de cleanup e ficheiros órfãos?
- Que metadados mínimos ficam retidos para auditoria e reprodutibilidade?

## 6. Critérios mínimos de aceitação futuros

- Não eliminar ZIP enquanto existir uma tentativa/receipt em estado que ainda dependa do pacote.
- Materialização deve ser idempotente e recuperável após falha em qualquer etapa.
- Confirmar que todos os objetos, definições, relações e artefactos abrangidos pelo contrato foram persistidos ou explicitamente classificados como não materializados.
- Validar hashes/tamanhos dos artefactos materializados e integridade das referências.
- Só marcar materialização como concluída depois da confirmação persistente no SQL Server.
- Eliminação do ZIP deve ser idempotente, auditável e segura perante concorrência.
- A consulta da análise já persistida não deve depender do ZIP.
- A política para reanálise depois da eliminação deve estar explícita; não prometer reanálise se a fonte foi eliminada.

## 7. Próximo passo recomendado

Desenhar o contrato de materialização (entidades, transação/estados, proveniência, hashes, recuperação e critérios de conclusão) antes de alterar o arquivo ou introduzir uma operação de delete. O desenho deve separar claramente `ReceiptCompleted`, `AnalysisCompleted`, `DecisionConfirmed` e `MaterializationCompleted`.

Esta auditoria não propõe ainda uma estratégia técnica final nem altera a decisão aprovada de ZIP temporário.
