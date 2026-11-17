using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerIntegrationCollection
{
    public const string Name = "SQL Server integration";
}

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerReceiptAttemptTests
{
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

    private static IkpPackageValidationResult NewValidation()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())).ToLowerInvariant();
        return new IkpPackageValidationResult(
            Guid.NewGuid(), Guid.NewGuid(), "0.1", "PhcKnowledgeScanner", "test",
            "IntegrationTests", hash, 1024, 1, 1, 0, 1, 1, 0, hash);
    }

    private static async Task<PackageReceiptAttempt> CreateAttemptAsync(
        SqlServerPackageReceiptAttemptRepository repository, Guid organizationId,
        IkpPackageValidationResult validation)
    {
        var registration = await repository.CreateOrGetPendingAsync(
            organizationId, validation, $"sha256/{validation.ZipSha256}.zip", $"staging/{Guid.NewGuid():N}.zip");
        return registration.Attempt;
    }

    private async Task<string> SetupAsync()
    {
        var connectionString = RequireConnectionString();
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

    [SqlServerFact]
    public async Task Concurrent_claims_grant_exactly_one_lease()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        try
        {
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var attempt = await CreateAttemptAsync(repository, organizationId, NewValidation());
            var now = DateTimeOffset.UtcNow.AddSeconds(2);

            var claims = await Task.WhenAll(
                repository.ClaimByIdAsync(attempt.AttemptId, TimeSpan.FromMinutes(5), now),
                repository.ClaimByIdAsync(attempt.AttemptId, TimeSpan.FromMinutes(5), now));

            Assert.Single(claims.Where(x => x is not null));
        }
        finally { await CleanupAsync(connectionString, organizationId); }
    }

    [SqlServerFact]
    public async Task Expired_lease_can_be_reclaimed_but_old_token_is_fenced()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        try
        {
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var attempt = await CreateAttemptAsync(repository, organizationId, NewValidation());
            var now = DateTimeOffset.UtcNow.AddSeconds(2);
            var first = await repository.ClaimByIdAsync(attempt.AttemptId, TimeSpan.FromSeconds(1), now);
            Assert.NotNull(first);

            var second = await repository.ClaimByIdAsync(
                attempt.AttemptId, TimeSpan.FromMinutes(5), now.AddSeconds(2));
            Assert.NotNull(second);
            Assert.NotEqual(first!.LeaseToken, second!.LeaseToken);
            Assert.False(await repository.RenewLeaseAsync(
                attempt.AttemptId, first.LeaseToken, now.AddMinutes(10)));
            Assert.True(await repository.RenewLeaseAsync(
                attempt.AttemptId, second.LeaseToken, now.AddMinutes(10)));
        }
        finally { await CleanupAsync(connectionString, organizationId); }
    }

    [SqlServerFact]
    public async Task Completion_commits_receipt_and_attempt_state_together()
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

            var receipt = await repository.CompleteAsync(
                attempt.AttemptId, lease!.LeaseToken,
                new ArchivedPackage(attempt.StorageKey, false));

            Assert.Equal(attempt.Validation.PackageId, receipt.Receipt.PackageId);
            Assert.NotNull(await repository.GetCompletedReceiptAsync(attempt.AttemptId));
        }
        finally { await CleanupAsync(connectionString, organizationId); }
    }

    [SqlServerFact]
    public async Task Receipt_insert_is_rolled_back_when_attempt_completion_fails()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        var triggerName = $"TR_TestFailCompletion_{Guid.NewGuid():N}";
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
                        THROW 51000, ''Intentional integration-test failure'', 1;
                    END');
                    """;
                await using var command = new SqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
            }

            await Assert.ThrowsAsync<SqlException>(() => repository.CompleteAsync(
                attempt.AttemptId, lease!.LeaseToken, new ArchivedPackage(attempt.StorageKey, false)));

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using var command = new SqlCommand("""
                    SELECT
                      (SELECT COUNT(*) FROM dbo.PackageIngestionReceipts WHERE OrganizationId = @OrganizationId),
                      (SELECT Status FROM dbo.PackageReceiptAttempts WHERE AttemptId = @AttemptId);
                    """, connection);
                command.Parameters.AddWithValue("@OrganizationId", organizationId);
                command.Parameters.AddWithValue("@AttemptId", attemptId);
                await using var reader = await command.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal(0, reader.GetInt32(0));
                Assert.Equal("PendingArchive", reader.GetString(1));
            }
        }
        finally
        {
            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using var command = new SqlCommand($"IF OBJECT_ID(N'dbo.[{triggerName}]', N'TR') IS NOT NULL DROP TRIGGER dbo.[{triggerName}];", connection);
                await command.ExecuteNonQueryAsync();
            }
            await CleanupAsync(connectionString, organizationId);
        }
    }
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING")))
            Skip = "Defina PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING para executar testes reais de SQL Server.";
    }
}