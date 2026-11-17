# Host operacional de PackageReceipt

## Responsabilidade

PhcKnowledge.Host é um Generic Host .NET 10 que executa os serviços de recuperação e limpeza já existentes em PhcKnowledge.Import. Não introduz endpoints HTTP, autenticação, materialização IKP ou novas entidades.

## Serviços alojados

| Serviço | Arranque | Ciclo predefinido | Responsabilidade |
|---|---:|---:|---|
| SQL schema preflight | Antes dos workers | Uma vez | Confirma que as migrations 001 e 002 foram aplicadas |
| PackageReceiptRecoveryWorker | Imediato | 60 segundos | Reclama e processa tentativas vencidas, em lotes de 20 |
| PackageStagingCleanupWorker | Imediato | 15 minutos | Remove staging órfão com mais de 24 horas |
| PackageReceiptOperationsMonitor | Imediato | 5 minutos | Regista alertas estruturados para PendingArchive com mais de uma hora e NeedsAttention |

Os workers isolam falhas por ciclo e continuam a executar nos ciclos seguintes. O cancelamento do processo é propagado às operações em curso.

## Configuração

Configurar por appsettings.json, variáveis de ambiente ou secret store:

- ConnectionStrings__Knowledge: ligação SQL Server.
- Storage__ArchiveDirectory: raiz persistente do arquivo imutável.
- Storage__StagingDirectory: raiz persistente do staging.
- Recovery__BatchSize: 1–500, predefinição 20.
- Recovery__IntervalSeconds: predefinição 60.
- Recovery__LeaseSeconds: predefinição 300 (5 minutos).
- Staging__CleanupIntervalMinutes: predefinição 15.
- Staging__RetentionHours: predefinição 24.
- Operations__MonitorIntervalMinutes: predefinição 5.
- Operations__PendingAlertAfterMinutes: predefinição 60.

Arquivo e staging têm de ser pastas distintas, persistentes e não aninhadas. A identidade do processo precisa de permissões de leitura/escrita no staging e arquivo e de acesso SQL às tabelas de recepção.

## Migrations e arranque

As migrations não são aplicadas automaticamente pelo host. Antes do arranque, aplicar pela ordem:

1. src/PhcKnowledge.Import/Database/001_package_ingestion_receipts.sql
2. src/PhcKnowledge.Import/Database/002_package_receipt_attempts.sql

O preflight valida conectividade e presença das duas tabelas; se faltarem, o processo falha no arranque com indicação explícita. Não usar bases de dados PHC de clientes para testes.

## Alertas e observabilidade

O monitor emite eventos de log estruturados:
- Warning ALERTA PackageReceipt quando existem tentativas PendingArchive com mais de 60 minutos.
- Error ALERTA PackageReceipt quando existem tentativas NeedsAttention.

A integração com email, Teams, PagerDuty ou outro canal não está incluída. O sistema de observabilidade/deployment deve recolher estes logs e criar regras de alerta para os níveis Warning e Error. Não são registados connection strings nem credenciais.

## Limites desta fase

O host executa apenas a recuperação de receipts e a limpeza de staging. Não disponibiliza API HTTP, autenticação, upload, nem processamento/materialização de artefactos IKP.
