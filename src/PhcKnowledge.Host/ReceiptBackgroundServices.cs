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
            DECLARE @RequiredColumns TABLE (TableName sysname NOT NULL, ColumnName sysname NOT NULL);
            INSERT @RequiredColumns (TableName, ColumnName)
            VALUES
              (N'PackageIngestionReceipts', N'ReceiptId'),
              (N'PackageIngestionReceipts', N'OrganizationId'),
              (N'PackageIngestionReceipts', N'PackageId'),
              (N'PackageIngestionReceipts', N'CaptureId'),
              (N'PackageIngestionReceipts', N'ContractVersion'),
              (N'PackageIngestionReceipts', N'ProducerProductId'),
              (N'PackageIngestionReceipts', N'ProducerProductVersion'),
              (N'PackageIngestionReceipts', N'OriginDatabaseName'),
              (N'PackageIngestionReceipts', N'ZipSha256'),
              (N'PackageIngestionReceipts', N'ZipByteLength'),
              (N'PackageIngestionReceipts', N'StorageKey'),
              (N'PackageIngestionReceipts', N'ReceivedAtUtc'),
              (N'PackageIngestionReceipts', N'ValidationSummaryJson'),
              (N'PackageReceiptAttempts', N'AttemptId'),
              (N'PackageReceiptAttempts', N'OrganizationId'),
              (N'PackageReceiptAttempts', N'PackageId'),
              (N'PackageReceiptAttempts', N'CaptureId'),
              (N'PackageReceiptAttempts', N'ContractVersion'),
              (N'PackageReceiptAttempts', N'ProducerProductId'),
              (N'PackageReceiptAttempts', N'ProducerProductVersion'),
              (N'PackageReceiptAttempts', N'OriginDatabaseName'),
              (N'PackageReceiptAttempts', N'ValidationSummaryJson'),
              (N'PackageReceiptAttempts', N'ValidationJson'),
              (N'PackageReceiptAttempts', N'ZipSha256'),
              (N'PackageReceiptAttempts', N'ZipByteLength'),
              (N'PackageReceiptAttempts', N'StorageKey'),
              (N'PackageReceiptAttempts', N'StagingKey'),
              (N'PackageReceiptAttempts', N'Status'),
              (N'PackageReceiptAttempts', N'AttemptCount'),
              (N'PackageReceiptAttempts', N'NextAttemptAtUtc'),
              (N'PackageReceiptAttempts', N'LeaseToken'),
              (N'PackageReceiptAttempts', N'LeaseUntilUtc'),
              (N'PackageReceiptAttempts', N'LastErrorCode'),
              (N'PackageReceiptAttempts', N'LastErrorMessage'),
              (N'PackageReceiptAttempts', N'CreatedAtUtc'),
              (N'PackageReceiptAttempts', N'UpdatedAtUtc'),
              (N'PackageReceiptAttempts', N'CompletedAtUtc');

            SELECT STRING_AGG(
                CONVERT(nvarchar(max), CONCAT(required.TableName, N'.', required.ColumnName)), N', ')
            FROM @RequiredColumns AS required
            LEFT JOIN sys.columns AS actual
              ON actual.object_id = OBJECT_ID(QUOTENAME(N'dbo') + N'.' + QUOTENAME(required.TableName), N'U')
             AND actual.name = required.ColumnName
            WHERE actual.column_id IS NULL;
            """, connection);

        var missingColumns = await command.ExecuteScalarAsync(cancellationToken) as string;
        if (!string.IsNullOrWhiteSpace(missingColumns))
            throw new InvalidOperationException(
                $"Schema de PackageReceipt incompleto. Colunas em falta: {missingColumns}. Aplicar/actualizar as migrations 001 e 002.");

        logger.LogInformation("SQL Server preflight concluído; tabelas e colunas necessárias de PackageReceipt disponíveis.");
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
    private string? _lastAlertFingerprint;
    private DateTimeOffset? _lastAlertAtUtc;

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
        var overdue = new List<OperationalAttempt>();
        var needsAttention = new List<OperationalAttempt>();
        long overdueCount;
        long needsAttentionCount;

        await using (var connection = new SqlConnection(settings.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand("""
                SELECT COUNT_BIG(*)
                FROM dbo.PackageReceiptAttempts
                WHERE Status = 'PendingArchive' AND CreatedAtUtc <= @PendingCutoffUtc;

                SELECT TOP (5) AttemptId, CreatedAtUtc, LastErrorCode, LastErrorMessage
                FROM dbo.PackageReceiptAttempts
                WHERE Status = 'PendingArchive' AND CreatedAtUtc <= @PendingCutoffUtc
                ORDER BY CreatedAtUtc;

                SELECT COUNT_BIG(*)
                FROM dbo.PackageReceiptAttempts
                WHERE Status = 'NeedsAttention';

                SELECT TOP (5) AttemptId, CreatedAtUtc, LastErrorCode, LastErrorMessage
                FROM dbo.PackageReceiptAttempts
                WHERE Status = 'NeedsAttention'
                ORDER BY CreatedAtUtc;
                """, connection);
            command.Parameters.Add("@PendingCutoffUtc", System.Data.SqlDbType.DateTime2)
                .Value = DateTime.UtcNow.Subtract(settings.PendingAlertThreshold);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("A consulta de tentativas pendentes não devolveu resultado.");
            overdueCount = reader.GetInt64(0);

            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException("A consulta de detalhe PendingArchive não devolveu resultado.");
            while (await reader.ReadAsync(cancellationToken))
                overdue.Add(ReadOperationalAttempt(reader));

            if (!await reader.NextResultAsync(cancellationToken) || !await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("A consulta de contagem NeedsAttention não devolveu resultado.");
            needsAttentionCount = reader.GetInt64(0);

            if (!await reader.NextResultAsync(cancellationToken))
                throw new InvalidOperationException("A consulta de detalhe NeedsAttention não devolveu resultado.");
            while (await reader.ReadAsync(cancellationToken))
                needsAttention.Add(ReadOperationalAttempt(reader));
        }

        if (overdueCount == 0 && needsAttentionCount == 0)
        {
            _lastAlertFingerprint = null;
            _lastAlertAtUtc = null;
            logger.LogDebug("Estado operacional de PackageReceipt sem pendências críticas.");
            return;
        }

        var fingerprint = CreateFingerprint(overdueCount, overdue, needsAttentionCount, needsAttention);
        var now = DateTimeOffset.UtcNow;
        var changed = !string.Equals(fingerprint, _lastAlertFingerprint, StringComparison.Ordinal);
        var reminderDue = _lastAlertAtUtc is null ||
                          now - _lastAlertAtUtc.Value >= settings.AlertRepeatInterval;
        if (!changed && !reminderDue)
            return;

        _lastAlertFingerprint = fingerprint;
        _lastAlertAtUtc = now;

        if (overdueCount > 0)
            logger.LogWarning(
                "ALERTA PackageReceipt: {OverdueCount} tentativa(s) PendingArchive há mais de {ThresholdMinutes} minutos. Amostra: {Attempts}.",
                overdueCount, settings.PendingAlertThreshold.TotalMinutes, FormatAttempts(overdue));

        if (needsAttentionCount > 0)
            logger.LogError(
                "ALERTA PackageReceipt: {NeedsAttentionCount} tentativa(s) NeedsAttention. Amostra: {Attempts}.",
                needsAttentionCount, FormatAttempts(needsAttention));
    }

    private static OperationalAttempt ReadOperationalAttempt(SqlDataReader reader) =>
        new(
            reader.GetGuid(0),
            DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3));

    private static string CreateFingerprint(
        long overdueCount, IReadOnlyList<OperationalAttempt> overdue,
        long needsAttentionCount, IReadOnlyList<OperationalAttempt> needsAttention) =>
        string.Join("|",
            overdueCount,
            string.Join(";", overdue.Select(AttemptFingerprint)),
            needsAttentionCount,
            string.Join(";", needsAttention.Select(AttemptFingerprint)));

    private static string AttemptFingerprint(OperationalAttempt attempt) =>
        $"{attempt.AttemptId:N}:{attempt.ErrorCode}:{attempt.ErrorMessage}";

    private static string FormatAttempts(IEnumerable<OperationalAttempt> attempts) =>
        string.Join("; ", attempts.Select(attempt =>
            $"{attempt.AttemptId} (desde {attempt.CreatedAtUtc:O}; erro={attempt.ErrorCode ?? "n/d"}; detalhe={attempt.ErrorMessage ?? "n/d"})"));

    private sealed record OperationalAttempt(
        Guid AttemptId, DateTime CreatedAtUtc, string? ErrorCode, string? ErrorMessage);
}
