using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Receipts;

/// <summary>
/// SQL Server implementation of durable receipt-attempt coordination.
/// Claim and completion operations are atomic and fenced by a lease token.
/// </summary>
public sealed class SqlServerPackageReceiptAttemptRepository : IPackageReceiptAttemptRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public SqlServerPackageReceiptAttemptRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<PackageReceiptAttemptRegistration> CreateOrGetPendingAsync(
        Guid organizationId,
        IkpPackageValidationResult validation,
        string storageKey,
        string stagingKey,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("OrganizationId é obrigatório.", nameof(organizationId));
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingKey);

        var now = DateTimeOffset.UtcNow;
        var attemptId = Guid.NewGuid();
        var validationJson = JsonSerializer.Serialize(validation, JsonOptions);
        var summaryJson = CreateValidationSummary(validation);

        const string sql = """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            DECLARE @ExistingAttemptId UNIQUEIDENTIFIER;
            SELECT TOP (1) @ExistingAttemptId = AttemptId
            FROM dbo.PackageReceiptAttempts WITH (UPDLOCK, HOLDLOCK)
            WHERE OrganizationId = @OrganizationId
              AND (PackageId = @PackageId OR ZipSha256 = @ZipSha256);

            IF @ExistingAttemptId IS NOT NULL
            BEGIN
                UPDATE dbo.PackageReceiptAttempts
                   SET StagingKey = @StagingKey, NextAttemptAtUtc = @NowUtc,
                       UpdatedAtUtc = @NowUtc
                 WHERE AttemptId = @ExistingAttemptId
                   AND PackageId = @PackageId AND ZipSha256 = @ZipSha256
                   AND Status = 'PendingArchive'
                   AND (LeaseUntilUtc IS NULL OR LeaseUntilUtc <= @NowUtc);

                SELECT AttemptId, OrganizationId, PackageId, CaptureId, ContractVersion,
                       ProducerProductId, ProducerProductVersion, OriginDatabaseName,
                       ValidationSummaryJson, ValidationJson, ZipSha256, ZipByteLength,
                       StorageKey, StagingKey, Status, AttemptCount, NextAttemptAtUtc,
                       LeaseToken, LeaseUntilUtc, LastErrorCode, LastErrorMessage,
                       CreatedAtUtc, UpdatedAtUtc, CompletedAtUtc
                FROM dbo.PackageReceiptAttempts WHERE AttemptId = @ExistingAttemptId;
                COMMIT TRANSACTION;
                RETURN;
            END;

            INSERT dbo.PackageReceiptAttempts
            (
                AttemptId, OrganizationId, PackageId, CaptureId, ContractVersion,
                ProducerProductId, ProducerProductVersion, OriginDatabaseName,
                ValidationSummaryJson, ValidationJson, ZipSha256, ZipByteLength,
                StorageKey, StagingKey, Status, AttemptCount, NextAttemptAtUtc,
                CreatedAtUtc, UpdatedAtUtc
            )
            VALUES
            (
                @AttemptId, @OrganizationId, @PackageId, @CaptureId, @ContractVersion,
                @ProducerProductId, @ProducerProductVersion, @OriginDatabaseName,
                @ValidationSummaryJson, @ValidationJson, @ZipSha256, @ZipByteLength,
                @StorageKey, @StagingKey, 'PendingArchive', 0, @NowUtc, @NowUtc, @NowUtc
            );

            SELECT AttemptId, OrganizationId, PackageId, CaptureId, ContractVersion,
                   ProducerProductId, ProducerProductVersion, OriginDatabaseName,
                   ValidationSummaryJson, ValidationJson, ZipSha256, ZipByteLength,
                   StorageKey, StagingKey, Status, AttemptCount, NextAttemptAtUtc,
                   LeaseToken, LeaseUntilUtc, LastErrorCode, LastErrorMessage,
                   CreatedAtUtc, UpdatedAtUtc, CompletedAtUtc
            FROM dbo.PackageReceiptAttempts WHERE AttemptId = @AttemptId;
            COMMIT TRANSACTION;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@AttemptId", SqlDbType.UniqueIdentifier).Value = attemptId;
        command.Parameters.Add("@OrganizationId", SqlDbType.UniqueIdentifier).Value = organizationId;
        command.Parameters.Add("@PackageId", SqlDbType.UniqueIdentifier).Value = validation.PackageId;
        command.Parameters.Add("@CaptureId", SqlDbType.UniqueIdentifier).Value = validation.CaptureId;
        command.Parameters.Add("@ContractVersion", SqlDbType.NVarChar, 16).Value = validation.ContractVersion;
        command.Parameters.Add("@ProducerProductId", SqlDbType.NVarChar, 128).Value = validation.ProducerProductId;
        command.Parameters.Add("@ProducerProductVersion", SqlDbType.NVarChar, 64).Value = validation.ProducerProductVersion;
        command.Parameters.Add("@OriginDatabaseName", SqlDbType.NVarChar, 128).Value = (object?)validation.OriginDatabaseName ?? DBNull.Value;
        command.Parameters.Add("@ValidationSummaryJson", SqlDbType.NVarChar, -1).Value = summaryJson;
        command.Parameters.Add("@ValidationJson", SqlDbType.NVarChar, -1).Value = validationJson;
        command.Parameters.Add("@ZipSha256", SqlDbType.Char, 64).Value = validation.ZipSha256;
        command.Parameters.Add("@ZipByteLength", SqlDbType.BigInt).Value = validation.ZipByteLength;
        command.Parameters.Add("@StorageKey", SqlDbType.NVarChar, 512).Value = storageKey;
        command.Parameters.Add("@StagingKey", SqlDbType.NVarChar, 256).Value = stagingKey;
        command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = now.UtcDateTime;

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Não foi possível recuperar a tentativa criada.");
        var attempt = ReadAttempt(reader);
        EnsureSameAttempt(attempt, organizationId, validation, storageKey);
        return new PackageReceiptAttemptRegistration(attempt, attempt.AttemptId != attemptId);
    }

    public async Task<PackageReceiptAttemptLease?> ClaimByIdAsync(
        Guid attemptId, TimeSpan leaseDuration, DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (leaseDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        const string sql = """
            UPDATE dbo.PackageReceiptAttempts WITH (UPDLOCK, ROWLOCK)
               SET LeaseToken = NEWID(), LeaseUntilUtc = @LeaseUntilUtc,
                   AttemptCount = AttemptCount + 1, UpdatedAtUtc = @NowUtc
            OUTPUT inserted.AttemptId, inserted.OrganizationId, inserted.PackageId,
                   inserted.CaptureId, inserted.ContractVersion, inserted.ProducerProductId,
                   inserted.ProducerProductVersion, inserted.OriginDatabaseName,
                   inserted.ValidationSummaryJson, inserted.ValidationJson, inserted.ZipSha256,
                   inserted.ZipByteLength, inserted.StorageKey, inserted.StagingKey,
                   inserted.Status, inserted.AttemptCount, inserted.NextAttemptAtUtc,
                   inserted.LeaseToken, inserted.LeaseUntilUtc, inserted.LastErrorCode,
                   inserted.LastErrorMessage, inserted.CreatedAtUtc, inserted.UpdatedAtUtc,
                   inserted.CompletedAtUtc
             WHERE AttemptId = @AttemptId AND Status = 'PendingArchive'
               AND NextAttemptAtUtc <= @NowUtc
               AND (LeaseUntilUtc IS NULL OR LeaseUntilUtc <= @NowUtc);
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@AttemptId", SqlDbType.UniqueIdentifier).Value = attemptId;
        command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = nowUtc.UtcDateTime;
        command.Parameters.Add("@LeaseUntilUtc", SqlDbType.DateTime2).Value = nowUtc.Add(leaseDuration).UtcDateTime;
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var attempt = ReadAttempt(reader);
        return new PackageReceiptAttemptLease(attempt, attempt.LeaseToken!.Value);
    }

    public async Task<IReadOnlyList<PackageReceiptAttemptLease>> ClaimDueAsync(
        int batchSize, TimeSpan leaseDuration, DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (leaseDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        const string sql = """
            ;WITH due AS
            (
                SELECT TOP (@BatchSize) *
                FROM dbo.PackageReceiptAttempts WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status = 'PendingArchive'
                  AND NextAttemptAtUtc <= @NowUtc
                  AND (LeaseUntilUtc IS NULL OR LeaseUntilUtc <= @NowUtc)
                ORDER BY NextAttemptAtUtc, CreatedAtUtc
            )
            UPDATE due
               SET LeaseToken = NEWID(),
                   LeaseUntilUtc = @LeaseUntilUtc,
                   AttemptCount = AttemptCount + 1,
                   UpdatedAtUtc = @NowUtc
            OUTPUT inserted.AttemptId, inserted.OrganizationId, inserted.PackageId,
                   inserted.CaptureId, inserted.ContractVersion, inserted.ProducerProductId,
                   inserted.ProducerProductVersion, inserted.OriginDatabaseName,
                   inserted.ValidationSummaryJson, inserted.ValidationJson, inserted.ZipSha256,
                   inserted.ZipByteLength, inserted.StorageKey, inserted.StagingKey,
                   inserted.Status, inserted.AttemptCount, inserted.NextAttemptAtUtc,
                   inserted.LeaseToken, inserted.LeaseUntilUtc, inserted.LastErrorCode,
                   inserted.LastErrorMessage, inserted.CreatedAtUtc, inserted.UpdatedAtUtc,
                   inserted.CompletedAtUtc;
            """;
        var leases = new List<PackageReceiptAttemptLease>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@BatchSize", SqlDbType.Int).Value = batchSize;
        command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = nowUtc.UtcDateTime;
        command.Parameters.Add("@LeaseUntilUtc", SqlDbType.DateTime2).Value = nowUtc.Add(leaseDuration).UtcDateTime;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var attempt = ReadAttempt(reader);
            leases.Add(new PackageReceiptAttemptLease(attempt, attempt.LeaseToken!.Value));
        }
        return leases;
    }

    public async Task<PackageReceipt?> GetCompletedReceiptAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r.ReceiptId, r.OrganizationId, r.PackageId, r.CaptureId,
                   r.ContractVersion, r.ProducerProductId, r.ProducerProductVersion,
                   r.OriginDatabaseName, r.ZipSha256, r.ZipByteLength, r.StorageKey,
                   r.ReceivedAtUtc, r.ValidationSummaryJson
            FROM dbo.PackageReceiptAttempts a
            INNER JOIN dbo.PackageIngestionReceipts r
                ON r.OrganizationId = a.OrganizationId AND r.PackageId = a.PackageId
            WHERE a.AttemptId = @AttemptId AND a.Status = 'Completed';
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@AttemptId", SqlDbType.UniqueIdentifier).Value = attemptId;
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        var received = DateTime.SpecifyKind(reader.GetDateTime(11), DateTimeKind.Utc);
        return new PackageReceipt(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2),
            reader.GetGuid(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7), reader.GetString(8).Trim(),
            reader.GetInt64(9), reader.GetString(10), new DateTimeOffset(received), reader.GetString(12));
    }

    public async Task<IReadOnlySet<string>> GetProtectedStagingKeysAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT StagingKey
            FROM dbo.PackageReceiptAttempts
            WHERE Status IN ('PendingArchive', 'NeedsAttention');
            """;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            keys.Add(reader.GetString(0));
        return keys;
    }

    public async Task<bool> RenewLeaseAsync(
        Guid attemptId, Guid leaseToken, DateTimeOffset leaseUntilUtc,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.PackageReceiptAttempts
               SET LeaseUntilUtc = @LeaseUntilUtc, UpdatedAtUtc = SYSUTCDATETIME()
             WHERE AttemptId = @AttemptId AND Status = 'PendingArchive'
               AND LeaseToken = @LeaseToken AND LeaseUntilUtc > SYSUTCDATETIME();
            """;
        return await ExecuteLeaseUpdateAsync(sql, attemptId, leaseToken, cancellationToken,
            ("@LeaseUntilUtc", SqlDbType.DateTime2, leaseUntilUtc.UtcDateTime));
    }

    public async Task<PackageReceiptRegistration> CompleteAsync(
        Guid attemptId, Guid leaseToken, ArchivedPackage archive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archive);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        const string select = """
            SELECT OrganizationId, PackageId, CaptureId, ContractVersion,
                   ProducerProductId, ProducerProductVersion, OriginDatabaseName,
                   ValidationSummaryJson, ValidationJson, ZipSha256, ZipByteLength, StorageKey,
                   Status, LeaseToken, LeaseUntilUtc
            FROM dbo.PackageReceiptAttempts WITH (UPDLOCK, HOLDLOCK)
            WHERE AttemptId = @AttemptId;
            """;
        await using var selectCommand = new SqlCommand(select, connection, transaction);
        selectCommand.Parameters.Add("@AttemptId", SqlDbType.UniqueIdentifier).Value = attemptId;
        Guid organizationId, packageId, captureId;
        string contractVersion, producerId, producerVersion, summaryJson, hash, storageKey;
        string? origin;
        long byteLength;
        IkpPackageValidationResult validation;
        await using (var reader = await selectCommand.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("Tentativa de recepção não encontrada.");
            if (reader.GetString(12) == "Completed")
                throw new InvalidOperationException("A tentativa já está concluída; não pode ser reclamada novamente.");
            if (reader.GetString(12) != "PendingArchive" ||
                reader.IsDBNull(13) || reader.GetGuid(13) != leaseToken ||
                reader.IsDBNull(14) || reader.GetDateTime(14) <= DateTime.UtcNow)
                throw new InvalidOperationException("A lease da tentativa expirou ou deixou de ser proprietária.");

            organizationId = reader.GetGuid(0);
            packageId = reader.GetGuid(1);
            captureId = reader.GetGuid(2);
            contractVersion = reader.GetString(3);
            producerId = reader.GetString(4);
            producerVersion = reader.GetString(5);
            origin = reader.IsDBNull(6) ? null : reader.GetString(6);
            summaryJson = reader.GetString(7);
            validation = JsonSerializer.Deserialize<IkpPackageValidationResult>(reader.GetString(8), JsonOptions)
                ?? throw new InvalidOperationException("ValidationJson inválido na tentativa.");
            hash = reader.GetString(9).Trim();
            byteLength = reader.GetInt64(10);
            storageKey = reader.GetString(11);
        }

        if (!string.Equals(storageKey, archive.StorageKey, StringComparison.Ordinal))
            throw new InvalidOperationException("StorageKey do arquivo não corresponde à tentativa.");

        var receipt = await FindReceiptAsync(connection, transaction, organizationId, packageId, hash, cancellationToken);
        var alreadyRegistered = receipt is not null;
        if (receipt is null)
        {
            receipt = new PackageReceipt(Guid.NewGuid(), organizationId, packageId, captureId,
                contractVersion, producerId, producerVersion, origin, hash, byteLength,
                archive.StorageKey, DateTimeOffset.UtcNow, summaryJson);
            await InsertReceiptAsync(connection, transaction, receipt, cancellationToken);
        }
        else if (receipt.PackageId != packageId ||
                 !string.Equals(receipt.ZipSha256, hash, StringComparison.Ordinal) ||
                 !string.Equals(receipt.StorageKey, archive.StorageKey, StringComparison.Ordinal))
        {
            throw new PackageReceiptConflictException("O receipt existente diverge da tentativa recuperada.");
        }

        const string update = """
            UPDATE dbo.PackageReceiptAttempts
               SET Status = 'Completed', CompletedAtUtc = SYSUTCDATETIME(),
                   LeaseToken = NULL, LeaseUntilUtc = NULL,
                   LastErrorCode = NULL, LastErrorMessage = NULL,
                   UpdatedAtUtc = SYSUTCDATETIME()
             WHERE AttemptId = @AttemptId AND Status = 'PendingArchive'
               AND LeaseToken = @LeaseToken AND LeaseUntilUtc > SYSUTCDATETIME();
            """;
        await using var updateCommand = new SqlCommand(update, connection, transaction);
        updateCommand.Parameters.Add("@AttemptId", SqlDbType.UniqueIdentifier).Value = attemptId;
        updateCommand.Parameters.Add("@LeaseToken", SqlDbType.UniqueIdentifier).Value = leaseToken;
        if (await updateCommand.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidOperationException("A lease expirou antes de concluir a tentativa.");

        await transaction.CommitAsync(cancellationToken);
        return new PackageReceiptRegistration(receipt, alreadyRegistered);
    }

    public Task<bool> ScheduleRetryAsync(
        Guid attemptId, Guid leaseToken, DateTimeOffset nextAttemptAtUtc,
        string errorCode, string errorMessage, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.PackageReceiptAttempts
               SET NextAttemptAtUtc = @NextAttemptAtUtc, LastErrorCode = @ErrorCode,
                   LastErrorMessage = @ErrorMessage, LeaseToken = NULL, LeaseUntilUtc = NULL,
                   UpdatedAtUtc = SYSUTCDATETIME()
             WHERE AttemptId = @AttemptId AND Status = 'PendingArchive'
               AND LeaseToken = @LeaseToken AND LeaseUntilUtc > SYSUTCDATETIME();
            """;
        return ExecuteLeaseUpdateAsync(sql, attemptId, leaseToken, cancellationToken,
            ("@NextAttemptAtUtc", SqlDbType.DateTime2, nextAttemptAtUtc.UtcDateTime),
            ("@ErrorCode", SqlDbType.NVarChar, errorCode),
            ("@ErrorMessage", SqlDbType.NVarChar, Truncate(errorMessage, 1900)));
    }

    public Task<bool> MarkNeedsAttentionAsync(
        Guid attemptId, Guid leaseToken, string errorCode, string errorMessage,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.PackageReceiptAttempts
               SET Status = 'NeedsAttention', LastErrorCode = @ErrorCode,
                   LastErrorMessage = @ErrorMessage, LeaseToken = NULL, LeaseUntilUtc = NULL,
                   UpdatedAtUtc = SYSUTCDATETIME()
             WHERE AttemptId = @AttemptId AND Status = 'PendingArchive'
               AND LeaseToken = @LeaseToken AND LeaseUntilUtc > SYSUTCDATETIME();
            """;
        return ExecuteLeaseUpdateAsync(sql, attemptId, leaseToken, cancellationToken,
            ("@ErrorCode", SqlDbType.NVarChar, errorCode),
            ("@ErrorMessage", SqlDbType.NVarChar, Truncate(errorMessage, 1900)));
    }

    private async Task<bool> ExecuteLeaseUpdateAsync(
        string sql, Guid attemptId, Guid leaseToken, CancellationToken cancellationToken,
        params (string Name, SqlDbType Type, object Value)[] extra)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@AttemptId", SqlDbType.UniqueIdentifier).Value = attemptId;
        command.Parameters.Add("@LeaseToken", SqlDbType.UniqueIdentifier).Value = leaseToken;
        foreach (var item in extra)
            command.Parameters.Add(item.Name, item.Type).Value = item.Value;
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static async Task<PackageReceipt?> FindReceiptAsync(
        SqlConnection connection, SqlTransaction transaction, Guid organizationId,
        Guid packageId, string hash, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1) ReceiptId, OrganizationId, PackageId, CaptureId, ContractVersion,
                   ProducerProductId, ProducerProductVersion, OriginDatabaseName, ZipSha256,
                   ZipByteLength, StorageKey, ReceivedAtUtc, ValidationSummaryJson
            FROM dbo.PackageIngestionReceipts WITH (UPDLOCK, HOLDLOCK)
            WHERE OrganizationId = @OrganizationId AND (PackageId = @PackageId OR ZipSha256 = @ZipSha256);
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@OrganizationId", SqlDbType.UniqueIdentifier).Value = organizationId;
        command.Parameters.Add("@PackageId", SqlDbType.UniqueIdentifier).Value = packageId;
        command.Parameters.Add("@ZipSha256", SqlDbType.Char, 64).Value = hash;
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var received = DateTime.SpecifyKind(reader.GetDateTime(11), DateTimeKind.Utc);
        return new PackageReceipt(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3),
            reader.GetString(4), reader.GetString(5), reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7), reader.GetString(8).Trim(),
            reader.GetInt64(9), reader.GetString(10), new DateTimeOffset(received), reader.GetString(12));
    }

    private static async Task InsertReceiptAsync(
        SqlConnection connection, SqlTransaction transaction, PackageReceipt receipt,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT dbo.PackageIngestionReceipts
            (ReceiptId, OrganizationId, PackageId, CaptureId, ContractVersion, ProducerProductId,
             ProducerProductVersion, OriginDatabaseName, ZipSha256, ZipByteLength, StorageKey,
             ReceivedAtUtc, ValidationSummaryJson)
            VALUES
            (@ReceiptId, @OrganizationId, @PackageId, @CaptureId, @ContractVersion, @ProducerProductId,
             @ProducerProductVersion, @OriginDatabaseName, @ZipSha256, @ZipByteLength, @StorageKey,
             @ReceivedAtUtc, @ValidationSummaryJson);
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@ReceiptId", SqlDbType.UniqueIdentifier).Value = receipt.ReceiptId;
        command.Parameters.Add("@OrganizationId", SqlDbType.UniqueIdentifier).Value = receipt.OrganizationId;
        command.Parameters.Add("@PackageId", SqlDbType.UniqueIdentifier).Value = receipt.PackageId;
        command.Parameters.Add("@CaptureId", SqlDbType.UniqueIdentifier).Value = receipt.CaptureId;
        command.Parameters.Add("@ContractVersion", SqlDbType.NVarChar, 16).Value = receipt.ContractVersion;
        command.Parameters.Add("@ProducerProductId", SqlDbType.NVarChar, 128).Value = receipt.ProducerProductId;
        command.Parameters.Add("@ProducerProductVersion", SqlDbType.NVarChar, 64).Value = receipt.ProducerProductVersion;
        command.Parameters.Add("@OriginDatabaseName", SqlDbType.NVarChar, 128).Value = (object?)receipt.OriginDatabaseName ?? DBNull.Value;
        command.Parameters.Add("@ZipSha256", SqlDbType.Char, 64).Value = receipt.ZipSha256;
        command.Parameters.Add("@ZipByteLength", SqlDbType.BigInt).Value = receipt.ZipByteLength;
        command.Parameters.Add("@StorageKey", SqlDbType.NVarChar, 512).Value = receipt.StorageKey;
        command.Parameters.Add("@ReceivedAtUtc", SqlDbType.DateTime2).Value = receipt.ReceivedAtUtc.UtcDateTime;
        command.Parameters.Add("@ValidationSummaryJson", SqlDbType.NVarChar, -1).Value = receipt.ValidationSummaryJson;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static PackageReceiptAttempt ReadAttempt(SqlDataReader reader)
    {
        var validation = JsonSerializer.Deserialize<IkpPackageValidationResult>(reader.GetString(9), JsonOptions)
            ?? throw new InvalidOperationException("ValidationJson inválido.");
        return new PackageReceiptAttempt(
            reader.GetGuid(0), reader.GetGuid(1), validation, reader.GetString(12), reader.GetString(13),
            reader.GetString(14), reader.GetInt32(15),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(16), DateTimeKind.Utc)),
            reader.IsDBNull(17) ? null : reader.GetGuid(17),
            reader.IsDBNull(18) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(18), DateTimeKind.Utc)),
            reader.IsDBNull(19) ? null : reader.GetString(19),
            reader.IsDBNull(20) ? null : reader.GetString(20),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(21), DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(22), DateTimeKind.Utc)));
    }

    private static void EnsureSameAttempt(
        PackageReceiptAttempt attempt, Guid organizationId,
        IkpPackageValidationResult validation, string storageKey)
    {
        if (attempt.OrganizationId != organizationId ||
            attempt.Validation.PackageId != validation.PackageId ||
            !string.Equals(attempt.Validation.ZipSha256, validation.ZipSha256, StringComparison.Ordinal) ||
            !string.Equals(attempt.StorageKey, storageKey, StringComparison.Ordinal))
            throw new PackageReceiptConflictException("Já existe uma tentativa para o PackageId ou SHA-256 com conteúdo diferente.");
    }

    private static string CreateValidationSummary(IkpPackageValidationResult validation) =>
        JsonSerializer.Serialize(new
        {
            validation.ContractFileCount, validation.ChecksummedFileCount, validation.ArtifactCount,
            validation.ObjectCount, validation.OperationCount, validation.IssueCount,
            validation.LogicalContentDigest
        });

    private static string Truncate(string value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
