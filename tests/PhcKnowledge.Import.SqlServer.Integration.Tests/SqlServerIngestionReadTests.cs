using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerIngestionReadTests
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
        var connectionString = RequireConnectionString();
        await EnsureDatabaseAsync(connectionString);
        await EnsureSchemaAsync(connectionString);
        return connectionString;
    }

    private static async Task CleanupAsync(string connectionString, params Guid[] organizationIds)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var organizationId in organizationIds)
        {
            await using var command = new SqlCommand("""
                DELETE FROM dbo.PackageIngestionReceipts WHERE OrganizationId = @OrganizationId;
                DELETE FROM dbo.PackageReceiptAttempts WHERE OrganizationId = @OrganizationId;
                """, connection);
            command.Parameters.AddWithValue("@OrganizationId", organizationId);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static IkpPackageValidationResult NewValidation(int objectCount = 3, int artifactCount = 2)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())).ToLowerInvariant();
        return new IkpPackageValidationResult(
            Guid.NewGuid(), Guid.NewGuid(), "0.1", "PhcKnowledgeScanner", "test",
            "ReadTests", hash, 2048, 1, 1, artifactCount, objectCount, 1, 0, hash);
    }

    [SqlServerFact]
    public async Task GetAttemptAsync_returns_own_org_attempt_without_internal_error_text()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        try
        {
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var validation = NewValidation(objectCount: 7, artifactCount: 4);
            var attempt = (await repository.CreateOrGetPendingAsync(
                organizationId, validation, $"sha256/{validation.ZipSha256}.zip",
                $"staging/{Guid.NewGuid():N}.tmp")).Attempt;

            var lease = await repository.ClaimByIdAsync(
                attempt.AttemptId, TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow.AddSeconds(2));
            Assert.NotNull(lease);
            await repository.MarkNeedsAttentionAsync(
                attempt.AttemptId, lease!.LeaseToken,
                "SimulatedErrorCode",
                @"Internal failure at C:\Secrets\path.sql: SqlException stack");

            var status = await repository.GetAttemptAsync(organizationId, attempt.AttemptId);
            Assert.NotNull(status);
            Assert.Equal(attempt.AttemptId, status!.AttemptId);
            Assert.Equal(organizationId, status.OrganizationId);
            Assert.Equal(validation.PackageId, status.PackageId);
            Assert.Equal(validation.CaptureId, status.CaptureId);
            Assert.Equal("NeedsAttention", status.Status);
            Assert.Equal(validation.ZipSha256, status.ZipSha256);
            Assert.Equal(validation.ZipByteLength, status.ZipByteLength);
            Assert.Equal($"sha256/{validation.ZipSha256}.zip", status.StorageKey);
            Assert.Equal(7, status.ObjectCount);
            Assert.Equal(4, status.ArtifactCount);
            Assert.Equal("SimulatedErrorCode", status.LastErrorCode);
            Assert.Null(status.ReceiptId);
            Assert.Null(status.CompletedAtUtc);

            // Read model must not surface LastErrorMessage / staging / validation JSON.
            Assert.Null(typeof(PackageIngestionStatus).GetProperty("LastErrorMessage"));
            Assert.Null(typeof(PackageIngestionStatus).GetProperty("StagingKey"));
            Assert.Null(typeof(PackageIngestionStatus).GetProperty("ValidationJson"));
            Assert.DoesNotContain("Secrets", status.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SqlException", status.ToString(), StringComparison.Ordinal);

            Assert.Null(await repository.GetAttemptAsync(otherOrganizationId, attempt.AttemptId));
            Assert.Null(await repository.GetAttemptAsync(organizationId, Guid.NewGuid()));
        }
        finally
        {
            await CleanupAsync(connectionString, organizationId, otherOrganizationId);
        }
    }

    [SqlServerFact]
    public async Task GetByPackageIdAsync_is_org_scoped_and_deterministic_for_unique_package()
    {
        var connectionString = await SetupAsync();
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        try
        {
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var validation = NewValidation();
            var attempt = (await repository.CreateOrGetPendingAsync(
                organizationId, validation, $"sha256/{validation.ZipSha256}.zip",
                $"staging/{Guid.NewGuid():N}.tmp")).Attempt;

            // Same PackageId in another organization is a different attempt row.
            var otherValidation = validation with
            {
                ZipSha256 = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())).ToLowerInvariant()
            };
            var otherAttempt = (await repository.CreateOrGetPendingAsync(
                otherOrganizationId, otherValidation, $"sha256/{otherValidation.ZipSha256}.zip",
                $"staging/{Guid.NewGuid():N}.tmp")).Attempt;
            Assert.Equal(validation.PackageId, otherAttempt.Validation.PackageId);
            Assert.NotEqual(attempt.AttemptId, otherAttempt.AttemptId);

            var own = await repository.GetByPackageIdAsync(organizationId, validation.PackageId);
            Assert.NotNull(own);
            Assert.Equal(attempt.AttemptId, own!.AttemptId);
            Assert.Equal(organizationId, own.OrganizationId);

            var foreign = await repository.GetByPackageIdAsync(otherOrganizationId, validation.PackageId);
            Assert.NotNull(foreign);
            Assert.Equal(otherAttempt.AttemptId, foreign!.AttemptId);
            Assert.Equal(otherOrganizationId, foreign.OrganizationId);

            Assert.Null(await repository.GetByPackageIdAsync(organizationId, Guid.NewGuid()));
            // Cross-org lookup by attempt id remains null (already covered); package lookup never returns the other org's row when org filter differs.
            Assert.NotEqual(own.AttemptId, foreign.AttemptId);

            // Completing yields ReceiptId on the read model.
            var lease = await repository.ClaimByIdAsync(
                attempt.AttemptId, TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow.AddSeconds(2));
            Assert.NotNull(lease);
            var registration = await repository.CompleteAsync(
                attempt.AttemptId, lease!.LeaseToken, new ArchivedPackage(attempt.StorageKey, false));
            var completed = await repository.GetByPackageIdAsync(organizationId, validation.PackageId);
            Assert.NotNull(completed);
            Assert.Equal("Completed", completed!.Status);
            Assert.Equal(registration.Receipt.ReceiptId, completed.ReceiptId);
            Assert.NotNull(completed.CompletedAtUtc);

            var byAttempt = await repository.GetAttemptAsync(organizationId, attempt.AttemptId);
            Assert.Equal(completed, byAttempt);
        }
        finally
        {
            await CleanupAsync(connectionString, organizationId, otherOrganizationId);
        }
    }

    [SqlServerFact]
    public async Task GetByPackageIdAsync_returns_null_for_foreign_org_exclusive_package()
    {
        var connectionString = await SetupAsync();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        try
        {
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var packageB = NewValidation();
            var attemptB = (await repository.CreateOrGetPendingAsync(
                organizationB, packageB, $"sha256/{packageB.ZipSha256}.zip",
                $"staging/{Guid.NewGuid():N}.tmp")).Attempt;

            Assert.Equal(organizationB, attemptB.OrganizationId);
            Assert.Equal(packageB.PackageId, attemptB.Validation.PackageId);

            Assert.Null(await repository.GetByPackageIdAsync(organizationA, packageB.PackageId));
            Assert.Null(await repository.GetAttemptAsync(organizationA, attemptB.AttemptId));

            var own = await repository.GetByPackageIdAsync(organizationB, packageB.PackageId);
            Assert.NotNull(own);
            Assert.Equal(attemptB.AttemptId, own!.AttemptId);
            Assert.Equal(organizationB, own.OrganizationId);
        }
        finally
        {
            await CleanupAsync(connectionString, organizationA, organizationB);
        }
    }
    [SqlServerFact]
    public async Task GetCompletedReceiptAsync_is_org_scoped_and_returns_null_for_miss_or_foreign_org()
    {
        var connectionString = await SetupAsync();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        try
        {
            var repository = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var validation = NewValidation();
            var attempt = (await repository.CreateOrGetPendingAsync(
                organizationA, validation, $"sha256/{validation.ZipSha256}.zip",
                $"staging/{Guid.NewGuid():N}.tmp")).Attempt;

            var lease = await repository.ClaimByIdAsync(
                attempt.AttemptId, TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow.AddSeconds(2));
            Assert.NotNull(lease);
            var completed = await repository.CompleteAsync(
                attempt.AttemptId, lease!.LeaseToken, new ArchivedPackage(attempt.StorageKey, false));

            var own = await repository.GetCompletedReceiptAsync(organizationA, attempt.AttemptId);
            Assert.NotNull(own);
            Assert.Equal(completed.Receipt.ReceiptId, own!.ReceiptId);
            Assert.Equal(organizationA, own.OrganizationId);
            Assert.Equal(validation.PackageId, own.PackageId);

            Assert.Null(await repository.GetCompletedReceiptAsync(organizationB, attempt.AttemptId));
            Assert.Null(await repository.GetCompletedReceiptAsync(organizationA, Guid.NewGuid()));

            await Assert.ThrowsAsync<ArgumentException>(
                () => repository.GetCompletedReceiptAsync(Guid.Empty, attempt.AttemptId));
            await Assert.ThrowsAsync<ArgumentException>(
                () => repository.GetCompletedReceiptAsync(organizationA, Guid.Empty));
        }
        finally
        {
            await CleanupAsync(connectionString, organizationA, organizationB);
        }
    }

}
