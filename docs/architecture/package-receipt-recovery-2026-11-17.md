> **Nota de precedência — 2026-11-20:** relatório de implementação do protocolo de receipts/recovery, com referências de branch/PR históricas. A API HTTP foi adicionada posteriormente; a afirmação de que não existe endpoint HTTP já não descreve a branch atual. A política de ZIP permanente descrita neste relatório também diverge da decisão atual: o ZIP é transporte temporário e deve ser eliminado após materialização e confirmação íntegra. Ver `../../PROJECT_BASELINE.md`, `../../DECISIONS.md` e `../../PROJECT_STATUS.md`. Não alterar o comportamento de arquivo sem plano de migração e recuperação aprovado.

# Protocolo de recuperação de PackageReceipt

> Estado: protocolo implementado na branch `feat/package-receipt-recovery` através da biblioteca `PhcKnowledge.Import` e do Generic Host `PhcKnowledge.Host`. A PR #9 permanece em Draft e ainda não foi integrada na `main`. Não existe ainda endpoint HTTP, autenticação ou upload HTTP.

## Objectivo

Eliminar a janela de falha entre o arquivo imutável do ZIP e a gravação de `PackageIngestionReceipts`. A tentativa é persistida antes do arquivo e funciona como ponto de recuperação durável. O receipt continua a representar apenas uma recepção concluída.

## Sequência normativa

1. Receber o upload para staging persistente, usando um nome aleatório gerado pelo servidor. O staging deve estar no mesmo domínio operacional de armazenamento que o processo de recuperação e sobreviver a reinícios.
2. Validar o ZIP e confirmar SHA-256 e tamanho contra o ficheiro staged.
3. Criar/reutilizar uma `PackageReceiptAttempt` para OrganizationId + PackageId + ZipSha256. A tentativa guarda todos os metadados validados necessários para criar o receipt, o `StagingKey` relativo e o `StorageKey` esperado. Não guardar caminhos absolutos fornecidos pelo cliente.
4. Arquivar por SHA-256 no store imutável.
5. Numa única transacção SQL, inserir `PackageIngestionReceipts` (se ainda não existir) e marcar a tentativa `Completed`. Se já existir receipt compatível, reutilizá-lo e concluir a tentativa.
6. Só depois do commit, remover o staging. A limpeza é idempotente e nunca remove ficheiros referenciados por tentativas não concluídas.

## Estados

- `PendingArchive`: estado recuperável, quer o ZIP ainda esteja em staging quer já tenha sido arquivado.
- `Completed`: receipt confirmado e tentativa concluída na mesma transacção SQL.
- `NeedsAttention`: inconsistência permanente ou acção humana necessária. Não é aplicado a indisponibilidades transitórias.

Não é necessário estado `Archived`: o worker verifica a existência, tamanho e SHA-256 do objecto no arquivo. Evita-se uma transição adicional entre filesystem e SQL.

## Lease e concorrência

- Reclamação atómica no SQL Server; apenas uma lease válida por tentativa.
- Duração inicial: 5 minutos; o processador renova a lease a cada minuto durante o arquivo/verificação e volta a confirmar a lease antes da transacção final.
- Cada reclamação usa `LeaseToken` novo. Todas as actualizações de progresso/conclusão verificam AttemptId + LeaseToken e lease ainda válida.
- Lease expirada pode ser reclamada por outra instância.
- O processamento é idempotente: o arquivo content-addressed e o receipt são verificados antes de criar novos registos.

## Retry e operação

- Erros transitórios: manter `PendingArchive`, guardar diagnóstico e agendar retry com backoff exponencial limitado a 15 minutos. Para SQL Server, só são repetidos códigos conhecidos como transitórios (timeouts, interrupções de rede, deadlocks, lock timeouts e throttling/indisponibilidade transitória Azure SQL); erros SQL não classificados como transitórios seguem para `NeedsAttention`.
- Regra operacional: o `PackageReceiptOperationsMonitor` emite Warning para `PendingArchive` acima de uma hora e Error para `NeedsAttention`, incluindo contagens, amostras de AttemptIds e últimos erros. A repetição é suprimida até alteração da amostra/contagem ou decorrer uma hora. Os logs ainda precisam de ser encaminhados para o sistema de observabilidade/deployment.
- Corrupção, hash divergente, metadados inconsistentes ou colisão PackageId/conteúdo: `NeedsAttention`.
- Tentativas pendentes não expiram automaticamente.
- Ficheiros de staging sem tentativa SQL associada podem ser eliminados após 24 horas. `PackageStagingCleanupService` consulta as tentativas pendentes e protege os respectivos `StagingKey`; a invocação periódica depende do host.
- Se o staging desaparecer mas o arquivo válido existir, concluir usando metadados validados persistidos.
- Se ambos desaparecerem, marcar `NeedsAttention`; será necessário novo envio.
- O ZIP original arquivado mantém-se imutável e permanente, conforme a política já aprovada.

## Persistência

A tabela `PackageReceiptAttempts` contém:
- identidade e tenancy: AttemptId, OrganizationId, PackageId, CaptureId;
- contrato validado: ContractVersion, ProducerProductId, ProducerProductVersion, OriginDatabaseName, ValidationSummaryJson e ValidationJson (serialização completa do resultado validado);
- integridade/arquivo: ZipSha256, ZipByteLength, StorageKey;
- staging: StagingKey relativo, nunca caminho absoluto;
- controlo: Status, AttemptCount, NextAttemptAtUtc, LeaseToken, LeaseUntilUtc;
- diagnóstico e auditoria: LastErrorCode, LastErrorMessage, CreatedAtUtc, UpdatedAtUtc, CompletedAtUtc e RowVersion.

Os índices únicos por OrganizationId + PackageId e OrganizationId + ZipSha256 impedem tentativas concorrentes duplicadas dentro da mesma organização, mantendo alinhamento com as constraints actuais do receipt. A política global Package/Receipt multi-organização continua a ser uma evolução separada do modelo 0.2 e não é alterada por esta migration.

## Implementação na branch

- `SqlServerPackageReceiptAttemptRepository`: criação/reutilização idempotente, claim por ID ou lote, lease, retry, NeedsAttention, conclusão transaccional de receipt + tentativa e leitura do receipt concluído.
- `PackageReceiptRecoveryService`: recuperação imediata e processamento de tentativas vencidas; valida o staging path e confirma o arquivo por hash quando o staging já não existe.
- `FileSystemImmutablePackageStore`: fornece a chave content-addressed determinística e verifica objectos arquivados sem os alterar.
- `IkpPackageIngestionService`: quando recebe o repositório de tentativas, regista a tentativa antes do arquivo e preserva o staging quando a recepção fica pendente. A persistência de receipts só é possível através deste percurso durável; sem repositório de tentativas, o serviço limita-se a validar e arquivar para harnesses.
- `PackageStagingCleanupService`: remove apenas ficheiros antigos sem referência a tentativas `PendingArchive` ou `NeedsAttention`.
- `PhcKnowledge.Host`: Generic Host .NET 10 com preflight SQL e workers de recuperação (60s), limpeza (15 min) e monitorização (5 min).
- Testes unitários cobrem os principais caminhos do processador, configuração do host, workers e protecção do staging. Testes de integração SQL Server 2022 cobrem claims concorrentes, fencing, atomicidade/rollback, preflight completo/incompleto e reutilização de uma única tentativa para recepções concorrentes do mesmo pacote.

## Limites de responsabilidade

- `PackageReceiptAttempt` não substitui `PackageReceipt`.
- Não introduzir Event Sourcing, Outbox genérico, materialização de Observations ou associação a Installation/Environment.
- O Generic Host executa os workers, mas ainda requer configuração de deployment, identidade/permissões e armazenamento persistente no ambiente de destino.
- O percurso de produto deve configurar `SqlServerPackageReceiptAttemptRepository`; sem o repositório de tentativas, o serviço não regista receipts.
- A suite SQL Server 2022 em GitHub Actions executa as migrations e os testes de integração numa base de dados efémera isolada; não substitui a validação de deployment no ambiente de destino.
- Os alertas são emitidos como logs Warning/Error; a entrega a email, Teams, PagerDuty ou outro canal depende da integração com a plataforma de observabilidade.
- A deduplicação de alertas é em memória e reinicia com o processo.

## Critérios de aceitação

1. Falha depois de criar tentativa e antes do arquivo é recuperável.
2. Falha depois do arquivo e antes do receipt é recuperável sem novo upload.
3. Receipt e estado Completed são confirmados atomicamente.
4. Lease impede duas instâncias de concluir a mesma tentativa em simultâneo.
5. Lease expirada permite recuperação.
6. Repetição do mesmo pacote é idempotente; PackageId/SHA divergentes geram conflito.
7. Limpeza não remove staging de tentativas pendentes.
8. Corrupção do arquivo nunca é sobrescrita.
9. Testes de repository/migration são executados contra SQL Server real; testes unitários isolados não substituem esta validação.
