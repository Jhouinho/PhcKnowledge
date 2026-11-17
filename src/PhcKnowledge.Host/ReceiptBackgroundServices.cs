using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Import.Ikp;

namespace PhcKnowledge.Host;

public sealed class SqlServerSchemaPreflightService(
    ReceiptHostSettings settings,
    ILogger<SqlServerSchemaPreflightService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(settings.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT CASE
                WHEN OBJECT_ID(N'dbo.PackageIngestionReceipts', N'U') IS NOT NULL
                 AND OBJECT_ID(N'dbo.PackageReceiptAttempts', N'U') IS NOT NULL
                THEN 1 ELSE 0 END;
            """, connection);

        var schemaReady = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        if (!schemaReady)
            throw new InvalidOperationException(
                "O schema de recepção não está instalado. Aplicar 001_package_ingestion_receipts.sql e 002_package_receipt_attempts.sql antes de iniciar o host.");

        logger.LogInformation("SQL Server preflight concluído; schema de PackageReceipt disponível.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class PackageReceiptRecoveryWorker(
    PackageReceiptRecoveryService recovery,
    ReceiptHostSettings settings,
    ILogger<PackageReceiptRecoveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(settings.RecoveryInterval);
        do
        {
            try
            {
                var processed = await recovery.ProcessDueAsync(
                    settings.RecoveryBatchSize, stoppingToken);
                if (processed > 0)
                    logger.LogInformation("Recuperação de PackageReceipt: {ProcessedCount} tentativa(s) concluída(s).", processed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha no ciclo de recuperação de PackageReceipt. O próximo ciclo tentará novamente.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

public sealed class PackageStagingCleanupWorker(
    PackageStagingCleanupService cleanup,
    ReceiptHostSettings settings,
    ILogger<PackageStagingCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(settings.CleanupInterval);
        do
        {
            try
            {
                var deleted = await cleanup.CleanupAsync(DateTimeOffset.UtcNow, stoppingToken);
                if (deleted > 0)
                    logger.LogInformation("Limpeza de staging: {DeletedCount} ficheiro(s) órfão(s) removido(s).", deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha no ciclo de limpeza de staging. O próximo ciclo tentará novamente.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

public sealed class PackageReceiptOperationsMonitor(
    ReceiptHostSettings settings,
    ILogger<PackageReceiptOperationsMonitor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(settings.OperationsMonitorInterval);
        do
        {
            try
            {
                await CheckAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Não foi possível consultar o estado operacional das recepções.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(settings.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("""
            SELECT COUNT_BIG(*), MIN(CreatedAtUtc)
            FROM dbo.PackageReceiptAttempts
            WHERE Status = 'PendingArchive' AND CreatedAtUtc <= @PendingCutoffUtc;

            SELECT COUNT_BIG(*)
            FROM dbo.PackageReceiptAttempts
            WHERE Status = 'NeedsAttention';
            """, connection);
        command.Parameters.Add("@PendingCutoffUtc", System.Data.SqlDbType.DateTime2)
            .Value = DateTime.UtcNow.Subtract(settings.PendingAlertThreshold);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("A consulta de tentativas pendentes não devolveu resultado.");

        var overdueCount = reader.GetInt64(0);
        DateTime? oldestPendingUtc = reader.IsDBNull(1)
            ? null
            : DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc);

        if (!await reader.NextResultAsync(cancellationToken) || !await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("A consulta de tentativas NeedsAttention não devolveu resultado.");

        var needsAttentionCount = reader.GetInt64(0);

        if (overdueCount > 0)
            logger.LogWarning(
                "ALERTA PackageReceipt: {OverdueCount} tentativa(s) continuam PendingArchive há mais de {ThresholdMinutes} minutos. Mais antiga: {OldestPendingUtc}.",
                overdueCount, settings.PendingAlertThreshold.TotalMinutes, oldestPendingUtc);

        if (needsAttentionCount > 0)
            logger.LogError(
                "ALERTA PackageReceipt: existem {NeedsAttentionCount} tentativa(s) em NeedsAttention que exigem análise operacional.",
                needsAttentionCount);

        if (overdueCount == 0 && needsAttentionCount == 0)
            logger.LogDebug("Estado operacional de PackageReceipt sem pendências críticas.");
    }
}
