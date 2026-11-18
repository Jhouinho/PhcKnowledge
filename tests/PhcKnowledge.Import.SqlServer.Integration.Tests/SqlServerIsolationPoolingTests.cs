using System.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

/// <summary>
/// Behavioural checks that Serializable transactions restore READ COMMITTED
/// (or discard the physical connection via ClearPool) before pooling reuse.
/// </summary>
[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerIsolationPoolingTests
{
    private const short ReadCommittedLevel = 2;
    private const short SerializableLevel = 4;

    private static string RequireConnectionString()
    {
        var value = Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING não está definida.");
        var builder = new SqlConnectionStringBuilder(value);
        if (!string.Equals(builder.InitialCatalog, "PhcKnowledgeReceiptTests", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Os testes só podem executar na base de dados isolada PhcKnowledgeReceiptTests.");
        return value;
    }

    private static async Task EnsureDatabaseAsync(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        SqlConnection? connection = null;
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                break;
            }
            catch (SqlException) when (attempt < 59)
            {
                if (connection is not null)
                    await connection.DisposeAsync();
                connection = null;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        if (connection is null)
            throw new InvalidOperationException("Não foi possível estabelecer ligação ao SQL Server após 120 segundos.");

        await using (connection)
        await using (var command = new SqlCommand("""
            IF DB_ID(@DatabaseName) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName);
                EXEC sys.sp_executesql @sql;
            END;
            """, connection))
        {
            command.Parameters.AddWithValue("@DatabaseName", databaseName);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task EnsureSchemaAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var migration in new[]
        {
            "001_package_ingestion_receipts.sql",
            "002_package_receipt_attempts.sql"
        })
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Migrations", migration);
            var sql = await File.ReadAllTextAsync(path);
            foreach (var batch in Regex.Split(sql, @"(?im)^\s*GO\s*(?:--.*)?$").Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                await using var command = new SqlCommand(batch, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    private async Task<string> SetupAsync()
    {
        var baseConnectionString = RequireConnectionString();
        await EnsureDatabaseAsync(baseConnectionString);
        await EnsureSchemaAsync(baseConnectionString);

        // Dedicated pool identity + single physical connection so isolation
        // restoration (or ClearPool) is observable without cross-test noise.
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            MaxPoolSize = 1,
            ApplicationName = $"PhcKnowledgeIso_{Guid.NewGuid():N}"
        };
        var connectionString = builder.ConnectionString;
        SqlConnection.ClearPool(new SqlConnection(connectionString));
        return connectionString;
    }

    private static async Task CleanupAsync(string connectionString, Guid organizationId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            DELETE FROM dbo.PackageIngestionReceipts WHERE OrganizationId = @OrganizationId;
            DELETE FROM dbo.PackageReceiptAttempts WHERE OrganizationId = @OrganizationId;
            """, connection);
        command.Parameters.AddWithValue("@OrganizationId", organizationId);
        await command.ExecuteNonQueryAsync();
    }

    private static IkpPackageValidationResult NewValidation(string? hash = null)
    {
        hash ??= Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())).ToLowerInvariant();
        return new IkpPackageValidationResult(
            Guid.NewGuid(), Guid.NewGuid(), "0.1", "PhcKnowledgeScanner", "test",
            "IsolationTests", hash, 1024, 1, 1, 0, 1, 1, 0, hash);
    }

    private static async Task<PackageReceiptAttempt> CreateAttemptAsync(
        SqlServerPackageReceiptAttemptRepository repository, Guid organizationId,
        IkpPackageValidationResult validation)
    {
        var registration = await repository.CreateOrGetPendingAsync(
            organizationId, validation, $"sha256/{validation.ZipSha256}.zip", $"staging/{Guid.NewGuid():N}.zip");
        return registration.Attempt;
    }

    private static async Task<short> ReadSessionIsolationAsync(SqlConnection connection)
    {
        await using var command = new SqlCommand("""
            SELECT transaction_isolation_level
            FROM sys.dm_exec_sessions
            WHERE session_id = @@SPID;
            """, connection);
        return Convert.ToInt16(await command.ExecuteScalarAsync());
    }

    private static async Task<short> OpenAndReadIsolationAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return await ReadSessionIsolationAsync(connection);
    }

    private static async Task PoisonPoolToSerializableAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        // SET (not an open transaction) is what a committed Serializable ADO transaction
        // leaves behind on the pooled session.
        await using (var poison = new SqlCommand(
            "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;", connection))
            await poison.ExecuteNonQueryAsync();
        Assert.Equal(SerializableLevel, await ReadSessionIsolationAsync(connection));
    }

    private static async Task AssertReadPastWorksAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        // READPAST is rejected under SERIALIZABLE; success proves usable isolation.
        // Must touch the table (not WHERE 1=0) so the engine evaluates the hint.
        await using var command = new SqlCommand("""
            SELECT TOP (1) AttemptId
            FROM dbo.PackageReceiptAttempts WITH (UPDLOCK, READPAST, ROWLOCK);
            """, connection);
        await command.ExecuteScalarAsync();
    }

    [SqlServerFact]
    public async Task CompleteAsync_restores_read_committed_after_commit()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        try
        {
            await PoisonPoolToSerializableAsync(connectionString);
            Assert.Equal(SerializableLevel, await OpenAndReadIsolationAsync(connectionString));

            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var attempt = await CreateAttemptAsync(repository, organizationId, NewValidation());
            var lease = await repository.ClaimByIdAsync(
                attempt.AttemptId, TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow.AddSeconds(2));
            Assert.NotNull(lease);

            await repository.CompleteAsync(
                attempt.AttemptId, lease!.LeaseToken, new ArchivedPackage(attempt.StorageKey, false));

            Assert.Equal(ReadCommittedLevel, await OpenAndReadIsolationAsync(connectionString));
            await AssertReadPastWorksAsync(connectionString);
        }
        finally { await CleanupAsync(connectionString, organizationId); }
    }

    [SqlServerFact]
    public async Task CompleteAsync_restores_read_committed_after_operation_exception()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        try
        {
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var attempt = await CreateAttemptAsync(repository, organizationId, NewValidation());
            var lease = await repository.ClaimByIdAsync(
                attempt.AttemptId, TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow.AddSeconds(2));
            Assert.NotNull(lease);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CompleteAsync(
                attempt.AttemptId, lease!.LeaseToken, new ArchivedPackage("sha256/wrong.zip", false)));
            Assert.Contains("StorageKey", ex.Message, StringComparison.Ordinal);

            Assert.Equal(ReadCommittedLevel, await OpenAndReadIsolationAsync(connectionString));
            await AssertReadPastWorksAsync(connectionString);
        }
        finally { await CleanupAsync(connectionString, organizationId); }
    }

    [SqlServerFact]
    public async Task CompleteAsync_restores_read_committed_after_rollback_from_trigger()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        var triggerName = $"TR_IsoRestore_{Guid.NewGuid():N}";
        var attemptId = Guid.Empty;
        try
        {
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var attempt = await CreateAttemptAsync(repository, organizationId, NewValidation());
            attemptId = attempt.AttemptId;
            var lease = await repository.ClaimByIdAsync(
                attempt.AttemptId, TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow.AddSeconds(2));
            Assert.NotNull(lease);

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = $"""
                    EXEC(N'CREATE TRIGGER dbo.[{triggerName}] ON dbo.PackageReceiptAttempts
                    AFTER UPDATE AS
                    BEGIN
                      IF EXISTS (SELECT 1 FROM inserted WHERE AttemptId = ''{attemptId}'' AND Status = ''Completed'')
                        THROW 51000, ''Intentional isolation restore failure'', 1;
                    END');
                    """;
                await using var command = new SqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
            }

            await Assert.ThrowsAsync<SqlException>(() => repository.CompleteAsync(
                attempt.AttemptId, lease!.LeaseToken, new ArchivedPackage(attempt.StorageKey, false)));

            Assert.Equal(ReadCommittedLevel, await OpenAndReadIsolationAsync(connectionString));
            await AssertReadPastWorksAsync(connectionString);
        }
        finally
        {
            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using var command = new SqlCommand(
                    $"IF OBJECT_ID(N'dbo.[{triggerName}]', N'TR') IS NOT NULL DROP TRIGGER dbo.[{triggerName}];",
                    connection);
                await command.ExecuteNonQueryAsync();
            }
            await CleanupAsync(connectionString, organizationId);
        }
    }

    [SqlServerFact]
    public async Task RegisterArchivedPackageAsync_restores_read_committed_after_commit_and_conflict()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        try
        {
            var repository = new SqlServerPackageReceiptRepository(connectionString);
            var validation = NewValidation();
            var archive = new ArchivedPackage($"sha256/{validation.ZipSha256}.zip", false);

            await repository.RegisterArchivedPackageAsync(organizationId, validation, archive);
            Assert.Equal(ReadCommittedLevel, await OpenAndReadIsolationAsync(connectionString));

            // Same identity → commit path with AlreadyRegistered.
            await repository.RegisterArchivedPackageAsync(organizationId, validation, archive);
            Assert.Equal(ReadCommittedLevel, await OpenAndReadIsolationAsync(connectionString));

            // Same PackageId, different hash → exception while Serializable transaction is open.
            var conflicting = new IkpPackageValidationResult(
                validation.PackageId, validation.CaptureId, validation.ContractVersion,
                validation.ProducerProductId, validation.ProducerProductVersion,
                validation.OriginDatabaseName,
                Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())).ToLowerInvariant(),
                validation.ZipByteLength, validation.ContractFileCount, validation.ChecksummedFileCount,
                validation.ArtifactCount, validation.ObjectCount, validation.OperationCount,
                validation.IssueCount, validation.LogicalContentDigest);
            var conflictArchive = new ArchivedPackage($"sha256/{conflicting.ZipSha256}.zip", false);

            await Assert.ThrowsAsync<PackageReceiptConflictException>(() =>
                repository.RegisterArchivedPackageAsync(organizationId, conflicting, conflictArchive));

            Assert.Equal(ReadCommittedLevel, await OpenAndReadIsolationAsync(connectionString));
            await AssertReadPastWorksAsync(connectionString);
        }
        finally { await CleanupAsync(connectionString, organizationId); }
    }

    [SqlServerFact]
    public async Task Restore_failure_clears_pool_and_preserves_original_exception()
    {
        var connectionString = await SetupAsync();

        // Close the connection to force restore failure without relying on KILL
        // (SqlClient connection resiliency can hide session kills on the next command).
        await using var victim = new SqlConnection(connectionString);
        await victim.OpenAsync();
        await using (var poison = new SqlCommand(
            "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;", victim))
            await poison.ExecuteNonQueryAsync();
        await victim.CloseAsync();

        var original = new InvalidOperationException("falha-original-da-operacao");
        // Must complete without throwing — original failure stays authoritative.
        await SqlConnectionIsolation.RestoreReadCommittedOrClearPoolAsync(
            victim, original, CancellationToken.None);
        Assert.True(original.Data.Contains("SqlConnectionIsolation.RestoreError"));

        // Without a prior operation error, restore failure must surface (after ClearPool).
        await using var victim2 = new SqlConnection(connectionString);
        await victim2.OpenAsync();
        await using (var poison = new SqlCommand(
            "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;", victim2))
            await poison.ExecuteNonQueryAsync();
        await victim2.CloseAsync();

        var restoreFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SqlConnectionIsolation.RestoreReadCommittedOrClearPoolAsync(
                victim2, operationError: null, CancellationToken.None));
        Assert.Contains("já não está aberta", restoreFailure.Message, StringComparison.Ordinal);

        // Pool was cleared: a fresh open must be usable at READ COMMITTED.
        Assert.Equal(ReadCommittedLevel, await OpenAndReadIsolationAsync(connectionString));
        await AssertReadPastWorksAsync(connectionString);
    }

    [SqlServerFact]
    public async Task ClaimDueAsync_survives_pooled_serializable_and_enables_readpast()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        try
        {
            await PoisonPoolToSerializableAsync(connectionString);
            Assert.Equal(SerializableLevel, await OpenAndReadIsolationAsync(connectionString));

            // Direct READPAST on a poisoned pooled session must fail (documents the hazard).
            await using (var poisoned = new SqlConnection(connectionString))
            {
                await poisoned.OpenAsync();
                Assert.Equal(SerializableLevel, await ReadSessionIsolationAsync(poisoned));
                await using var command = new SqlCommand("""
                    SELECT TOP (1) AttemptId
                    FROM dbo.PackageReceiptAttempts WITH (UPDLOCK, READPAST, ROWLOCK);
                    """, poisoned);
                var readPastEx = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteScalarAsync());
                Assert.Contains("READPAST", readPastEx.Message, StringComparison.OrdinalIgnoreCase);
            }

            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var attempt = await CreateAttemptAsync(repository, organizationId, NewValidation());

            // ClaimDueAsync must force READ COMMITTED before READPAST.
            var leases = await repository.ClaimDueAsync(
                batchSize: 10, TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow.AddSeconds(2));
            Assert.Contains(leases, x => x.Attempt.AttemptId == attempt.AttemptId);

            Assert.Equal(ReadCommittedLevel, await OpenAndReadIsolationAsync(connectionString));
        }
        finally { await CleanupAsync(connectionString, organizationId); }
    }

    [SqlServerFact]
    public async Task ProcessDueAsync_recovers_after_serializable_connection_reuse()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        var root = Path.Combine(Path.GetTempPath(), "phc-knowledge-iso-recovery", Guid.NewGuid().ToString("N"));
        var staging = Path.Combine(root, "staging");
        var archiveRoot = Path.Combine(root, "archive");
        Directory.CreateDirectory(staging);
        Directory.CreateDirectory(archiveRoot);

        try
        {
            var payload = System.Text.Encoding.UTF8.GetBytes($"iso-recovery-{Guid.NewGuid():N}");
            var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
            var validation = new IkpPackageValidationResult(
                Guid.NewGuid(), Guid.NewGuid(), "0.1", "PhcKnowledgeScanner", "test",
                "IsolationTests", hash, payload.LongLength, 1, 1, 0, 1, 1, 0, hash);
            var stagingKey = $"iso-{Guid.NewGuid():N}.tmp";
            var stagedPath = Path.Combine(staging, stagingKey);
            await File.WriteAllBytesAsync(stagedPath, payload);

            var store = new FileSystemImmutablePackageStore(archiveRoot);
            var storageKey = store.GetStorageKey(validation);
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var registration = await repository.CreateOrGetPendingAsync(
                organizationId, validation, storageKey, stagingKey);

            var archived = await store.ArchiveValidatedPackageAsync(stagedPath, validation);
            Assert.False(archived.AlreadyExisted);

            // Poison the pool the way CompleteAsync used to, then recover via ProcessDueAsync.
            await PoisonPoolToSerializableAsync(connectionString);

            var recovery = new PackageReceiptRecoveryService(
                new SqlServerPackageReceiptAttemptRepository(connectionString),
                new FileSystemImmutablePackageStore(archiveRoot),
                staging);

            var processed = await recovery.ProcessDueAsync(batchSize: 10);
            Assert.Equal(1, processed);

            var receipt = await repository.GetCompletedReceiptAsync(registration.Attempt.AttemptId);
            Assert.NotNull(receipt);
            Assert.Equal(validation.PackageId, receipt!.PackageId);
            Assert.Equal(ReadCommittedLevel, await OpenAndReadIsolationAsync(connectionString));
        }
        finally
        {
            try { await CleanupAsync(connectionString, organizationId); }
            finally
            {
                if (Directory.Exists(root))
                {
                    foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                        File.SetAttributes(file, FileAttributes.Normal);
                    Directory.Delete(root, recursive: true);
                }
            }
        }
    }

    [SqlServerFact]
    public async Task EnsureReadCommitted_failure_clears_pool()
    {
        var connectionString = await SetupAsync();

        await using var victim = new SqlConnection(connectionString);
        await victim.OpenAsync();
        await using (var poison = new SqlCommand(
            "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;", victim))
            await poison.ExecuteNonQueryAsync();
        await victim.CloseAsync();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            SqlConnectionIsolation.EnsureReadCommittedOrClearPoolAsync(victim, CancellationToken.None));

        Assert.Equal(ReadCommittedLevel, await OpenAndReadIsolationAsync(connectionString));
        await AssertReadPastWorksAsync(connectionString);
    }
}
