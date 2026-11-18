using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerRealPackageIngestionTests
{
    private const string ExpectedPackageId = "b0b7e61f-8ea4-4cf0-a57d-99d37ab220ea";
    private const string ExpectedCaptureId = "49826eeb-0388-48bb-a495-2031f30a58a4";
    private const string ExpectedZipSha256 = "dc06320f4fea6bbaaff2f63f699ce42031a47e8704a6e021512f37d43d266cd5";
    private const long ExpectedZipByteLength = 783_363;

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

    private static string RequireRealPackagePath()
    {
        var value = Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_REAL_IKP_PATH");
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("PHC_KNOWLEDGE_REAL_IKP_PATH não está definida.");

        var path = Path.GetFullPath(value);
        if (!File.Exists(path))
            throw new FileNotFoundException("O pacote IKP real indicado não existe.", path);

        return path;
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
            "002_package_receipt_attempts.sql",
            "003_organizations.sql"
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
        var connectionString = RequireConnectionString();
        _ = RequireRealPackagePath();
        await EnsureDatabaseAsync(connectionString);
        await EnsureSchemaAsync(connectionString);
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

    private static async Task<PersistedReceiptState> ReadStateAsync(
        string connectionString, Guid organizationId, Guid packageId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT
                (SELECT COUNT(*) FROM dbo.PackageReceiptAttempts
                 WHERE OrganizationId = @OrganizationId AND PackageId = @PackageId),
                (SELECT COUNT(*) FROM dbo.PackageIngestionReceipts
                 WHERE OrganizationId = @OrganizationId AND PackageId = @PackageId),
                (SELECT TOP (1) Status FROM dbo.PackageReceiptAttempts
                 WHERE OrganizationId = @OrganizationId AND PackageId = @PackageId),
                (SELECT TOP (1) CaptureId FROM dbo.PackageReceiptAttempts
                 WHERE OrganizationId = @OrganizationId AND PackageId = @PackageId),
                (SELECT TOP (1) ZipSha256 FROM dbo.PackageReceiptAttempts
                 WHERE OrganizationId = @OrganizationId AND PackageId = @PackageId),
                (SELECT TOP (1) ZipByteLength FROM dbo.PackageReceiptAttempts
                 WHERE OrganizationId = @OrganizationId AND PackageId = @PackageId),
                (SELECT TOP (1) StorageKey FROM dbo.PackageReceiptAttempts
                 WHERE OrganizationId = @OrganizationId AND PackageId = @PackageId);
            """, connection);
        command.Parameters.AddWithValue("@OrganizationId", organizationId);
        command.Parameters.AddWithValue("@PackageId", packageId);

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new PersistedReceiptState(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.IsDBNull(4) ? null : reader.GetString(4).Trim(),
            reader.IsDBNull(5) ? null : reader.GetInt64(5),
            reader.IsDBNull(6) ? null : reader.GetString(6));
    }

    private static void AssertExpectedCapture(IkpPackageValidationResult validation)
    {
        Assert.Equal(Guid.Parse(ExpectedPackageId), validation.PackageId);
        Assert.Equal(Guid.Parse(ExpectedCaptureId), validation.CaptureId);
        Assert.Equal(ExpectedZipSha256, validation.ZipSha256);
        Assert.Equal(ExpectedZipByteLength, validation.ZipByteLength);
    }

    private static string ArchivePath(string archiveRoot, string storageKey) =>
        Path.Combine(archiveRoot, storageKey.Replace('/', Path.DirectorySeparatorChar));

    private static void DeleteTemporaryDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);

        Directory.Delete(path, recursive: true);
    }

    [RealCaptureSqlServerFact]
    public async Task Real_capture_is_received_archived_and_repeated_idempotently()
    {
        var connectionString = await SetupAsync();
        var packagePath = RequireRealPackagePath();
        var organizationId = Guid.NewGuid();
        var root = Path.Combine(Path.GetTempPath(), "phc-knowledge-real-ikp", Guid.NewGuid().ToString("N"));
        var staging = Path.Combine(root, "staging");
        var archive = Path.Combine(root, "archive");
        Directory.CreateDirectory(staging);
        Directory.CreateDirectory(archive);

        try
        {
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var store = new FileSystemImmutablePackageStore(archive);
            var ingestion = new IkpPackageIngestionService(
                new IkpPackageValidator(), store, staging, attemptRepository: repository);

            IkpIngestionResult first;
            await using (var input = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                first = await ingestion.ReceiveValidateArchiveAsync(input, organizationId);

            AssertExpectedCapture(first.Validation);
            Assert.NotNull(first.Receipt);
            Assert.Equal(organizationId, first.Receipt!.OrganizationId);
            Assert.Equal(first.Validation.PackageId, first.Receipt.PackageId);
            Assert.Equal(first.Validation.CaptureId, first.Receipt.CaptureId);
            Assert.Equal(first.Validation.ZipSha256, first.Receipt.ZipSha256);
            Assert.Equal(first.Validation.ZipByteLength, first.Receipt.ZipByteLength);

            var archivedPath = ArchivePath(archive, first.Archive.StorageKey);
            Assert.True(File.Exists(archivedPath));
            Assert.True((File.GetAttributes(archivedPath) & FileAttributes.ReadOnly) != 0);

            var secondStream = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            IkpIngestionResult second;
            await using (secondStream)
                second = await ingestion.ReceiveValidateArchiveAsync(secondStream, organizationId);

            AssertExpectedCapture(second.Validation);
            Assert.NotNull(second.Receipt);
            Assert.Equal(first.Receipt.ReceiptId, second.Receipt!.ReceiptId);
            Assert.Equal(first.Archive.StorageKey, second.Archive.StorageKey);
            Assert.True(second.Archive.AlreadyExisted);

            var state = await ReadStateAsync(
                connectionString, organizationId, first.Validation.PackageId);
            Assert.Equal(1, state.AttemptCount);
            Assert.Equal(1, state.ReceiptCount);
            Assert.Equal("Completed", state.Status);
            Assert.Equal(first.Validation.CaptureId, state.CaptureId);
            Assert.Equal(first.Validation.ZipSha256, state.ZipSha256);
            Assert.Equal(first.Validation.ZipByteLength, state.ZipByteLength);
            Assert.Equal(first.Archive.StorageKey, state.StorageKey);
        }
        finally
        {
            try
            {
                await CleanupAsync(connectionString, organizationId);
            }
            finally
            {
                DeleteTemporaryDirectory(root);
            }
        }
    }

    [RealCaptureSqlServerFact]
    public async Task Recovery_after_archive_before_receipt_completes_without_a_second_upload()
    {
        var connectionString = await SetupAsync();
        var packagePath = RequireRealPackagePath();
        var organizationId = Guid.NewGuid();
        var root = Path.Combine(Path.GetTempPath(), "phc-knowledge-real-recovery", Guid.NewGuid().ToString("N"));
        var staging = Path.Combine(root, "staging");
        var archive = Path.Combine(root, "archive");
        Directory.CreateDirectory(staging);
        Directory.CreateDirectory(archive);

        try
        {
            var validator = new IkpPackageValidator();
            IkpPackageValidationResult validation;
            await using (var input = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                validation = await validator.ValidateAsync(input);
            AssertExpectedCapture(validation);

            var stagingKey = $"real-capture-{Guid.NewGuid():N}.tmp";
            var stagedPath = Path.Combine(staging, stagingKey);
            File.Copy(packagePath, stagedPath);

            var store = new FileSystemImmutablePackageStore(archive);
            var storageKey = store.GetStorageKey(validation);
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var registration = await repository.CreateOrGetPendingAsync(
                organizationId, validation, storageKey, stagingKey);

            Assert.Equal("PendingArchive", registration.Attempt.Status);

            // Simulate a process stop after the immutable file is committed but before
            // the SQL transaction creates PackageIngestionReceipts and marks Completed.
            var archived = await store.ArchiveValidatedPackageAsync(stagedPath, validation);
            Assert.False(archived.AlreadyExisted);
            Assert.True(File.Exists(ArchivePath(archive, storageKey)));

            var interruptedState = await ReadStateAsync(
                connectionString, organizationId, validation.PackageId);
            Assert.Equal(1, interruptedState.AttemptCount);
            Assert.Equal(0, interruptedState.ReceiptCount);
            Assert.Equal("PendingArchive", interruptedState.Status);

            // New service/repository/store instances represent a host restart.
            var restartedRepository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var restartedStore = new FileSystemImmutablePackageStore(archive);
            var recovery = new PackageReceiptRecoveryService(
                restartedRepository, restartedStore, staging);

            var processed = await recovery.ProcessDueAsync(batchSize: 10);
            Assert.Equal(1, processed);

            var receipt = await restartedRepository.GetCompletedReceiptAsync(organizationId, registration.Attempt.AttemptId);
            Assert.NotNull(receipt);
            Assert.Equal(validation.PackageId, receipt!.PackageId);
            Assert.Equal(validation.CaptureId, receipt.CaptureId);
            Assert.Equal(validation.ZipSha256, receipt.ZipSha256);
            Assert.Equal(validation.ZipByteLength, receipt.ZipByteLength);
            Assert.Equal(storageKey, receipt.StorageKey);
            Assert.False(File.Exists(stagedPath));

            var recoveredState = await ReadStateAsync(
                connectionString, organizationId, validation.PackageId);
            Assert.Equal(1, recoveredState.AttemptCount);
            Assert.Equal(1, recoveredState.ReceiptCount);
            Assert.Equal("Completed", recoveredState.Status);
            Assert.Equal(validation.CaptureId, recoveredState.CaptureId);
            Assert.Equal(validation.ZipSha256, recoveredState.ZipSha256);
            Assert.Equal(validation.ZipByteLength, recoveredState.ZipByteLength);
            Assert.Equal(storageKey, recoveredState.StorageKey);
        }
        finally
        {
            try
            {
                await CleanupAsync(connectionString, organizationId);
            }
            finally
            {
                DeleteTemporaryDirectory(root);
            }
        }
    }

    private sealed record PersistedReceiptState(
        int AttemptCount,
        int ReceiptCount,
        string? Status,
        Guid? CaptureId,
        string? ZipSha256,
        long? ZipByteLength,
        string? StorageKey);
}

public sealed class RealCaptureSqlServerFactAttribute : FactAttribute
{
    public RealCaptureSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING")))
        {
            Skip = "Defina PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING para executar testes reais de SQL Server.";
            return;
        }

        var path = Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_REAL_IKP_PATH");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            Skip = "Defina PHC_KNOWLEDGE_REAL_IKP_PATH para o ZIP IKP real CSDefault202603-ikp.zip.";
    }
}
