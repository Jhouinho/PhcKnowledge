# Protocolo de recuperação de PackageReceipt

> Estado: especificação técnica para a evolução do Import. Esta fase não cria ainda um endpoint HTTP nem um processo hospedado: o repositório actual contém uma biblioteca Import, sem host ASP.NET Core/Worker onde registar o serviço de recuperação.

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
- Duração inicial: 5 minutos; renovação aproximadamente a cada minuto enquanto o processamento estiver activo.
- Cada reclamação usa `LeaseToken` novo. Todas as actualizações de progresso/conclusão verificam AttemptId + LeaseToken e lease ainda válida.
- Lease expirada pode ser reclamada por outra instância.
- O processamento é idempotente: o arquivo content-addressed e o receipt são verificados antes de criar novos registos.

## Retry e operação

- Erros transitórios: manter `PendingArchive`, guardar diagnóstico e agendar retry com backoff exponencial limitado a 15 minutos.
- Após uma hora pendente: emitir alerta operacional, mas continuar a tentar automaticamente.
- Corrupção, hash divergente, metadados inconsistentes ou colisão PackageId/conteúdo: `NeedsAttention`.
- Tentativas pendentes não expiram automaticamente.
- Ficheiros de staging sem tentativa SQL associada podem ser eliminados após 24 horas. Nunca aplicar esta regra a staging referenciado por tentativa pendente.
- Se o staging desaparecer mas o arquivo válido existir, concluir usando metadados validados persistidos.
- Se ambos desaparecerem, marcar `NeedsAttention`; será necessário novo envio.
- O ZIP original arquivado mantém-se imutável e permanente, conforme a política já aprovada.

## Persistência

A tabela `PackageReceiptAttempts` contém:
- identidade e tenancy: AttemptId, OrganizationId, PackageId, CaptureId;
- contrato validado: ContractVersion, ProducerProductId, ProducerProductVersion, OriginDatabaseName e ValidationSummaryJson;
- integridade/arquivo: ZipSha256, ZipByteLength, StorageKey;
- staging: StagingKey relativo, nunca caminho absoluto;
- controlo: Status, AttemptCount, NextAttemptAtUtc, LeaseToken, LeaseUntilUtc;
- diagnóstico e auditoria: LastErrorCode, LastErrorMessage, CreatedAtUtc, UpdatedAtUtc, CompletedAtUtc e RowVersion.

Os índices únicos por OrganizationId + PackageId e OrganizationId + ZipSha256 impedem tentativas concorrentes duplicadas dentro da mesma organização, mantendo alinhamento com as constraints actuais do receipt. A política global Package/Receipt multi-organização continua a ser uma evolução separada do modelo 0.2 e não é alterada por esta migration.

## Limites de responsabilidade

- `PackageReceiptAttempt` não substitui `PackageReceipt`.
- Não introduzir Event Sourcing, Outbox genérico, materialização de Observations ou associação a Installation/Environment.
- A recuperação deve ser um serviço do Import, invocado por um BackgroundService no futuro host. Este repositório ainda não tem esse host.
- A recepção sem `IPackageReceiptRepository` mantém-se como modo de validação/arquivo isolado para harnesses; o fluxo de produto com persistência exige OrganizationId.

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
