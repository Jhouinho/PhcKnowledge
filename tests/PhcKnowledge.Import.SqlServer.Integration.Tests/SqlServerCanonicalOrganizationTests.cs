using System.Collections.Concurrent;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using PhcKnowledge.Host.Organizations;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;
using System.Security.Cryptography;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerCanonicalOrganizationTests
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
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            IF DB_ID(@DatabaseName) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName);
                EXEC sys.sp_executesql @sql;
            END;
            """, connection);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);
        await command.ExecuteNonQueryAsync();
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

    private static async Task ClearOrganizationsAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        // AspNetUsers may reference Organizations after Identity migrations (Phase 1.2).
        await using var command = new SqlCommand("""
            IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NOT NULL DELETE FROM dbo.AspNetUserTokens;
            IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NOT NULL DELETE FROM dbo.AspNetUserRoles;
            IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NOT NULL DELETE FROM dbo.AspNetUserLogins;
            IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NOT NULL DELETE FROM dbo.AspNetUserClaims;
            IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NOT NULL DELETE FROM dbo.AspNetUsers;
            DELETE FROM dbo.Organizations;
            """, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string> SetupAsync()
    {
        var connectionString = RequireConnectionString();
        await EnsureDatabaseAsync(connectionString);
        await EnsureSchemaAsync(connectionString);
        await ClearOrganizationsAsync(connectionString);
        return connectionString;
    }

    [Fact]
    public async Task EnsureCanonical_creates_organization_once()
    {
        var cs = await SetupAsync();
        var service = new SqlServerCanonicalOrganizationService(cs);

        var created = await service.EnsureCanonicalAsync("Empresa Teste");
        Assert.NotEqual(Guid.Empty, created.OrganizationId);
        Assert.Equal("Empresa Teste", created.Name);
        Assert.True(created.IsCanonical);

        var again = await service.EnsureCanonicalAsync("Outro Nome Ignorado");
        Assert.Equal(created.OrganizationId, again.OrganizationId);
        Assert.Equal("Empresa Teste", again.Name);

        await using var connection = new SqlConnection(cs);
        await connection.OpenAsync();
        await using var count = new SqlCommand("SELECT COUNT(*) FROM dbo.Organizations;", connection);
        Assert.Equal(1, (int)(await count.ExecuteScalarAsync() ?? -1));
    }

    [Fact]
    public async Task EnsureCanonical_concurrent_calls_create_single_row()
    {
        var cs = await SetupAsync();
        var service = new SqlServerCanonicalOrganizationService(cs);
        var results = new ConcurrentBag<CanonicalOrganization>();

        await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
        {
            var org = await service.EnsureCanonicalAsync($"Empresa Concurrent {i}");
            results.Add(org);
        }));

        var distinctIds = results.Select(x => x.OrganizationId).Distinct().ToArray();
        Assert.Single(distinctIds);

        await using var connection = new SqlConnection(cs);
        await connection.OpenAsync();
        await using var count = new SqlCommand("SELECT COUNT(*) FROM dbo.Organizations;", connection);
        Assert.Equal(1, (int)(await count.ExecuteScalarAsync() ?? -1));
    }

    [Fact]
    public async Task TryGetCanonical_detects_inconsistent_multiple_canonical_rows()
    {
        var cs = await SetupAsync();

        await using (var connection = new SqlConnection(cs))
        {
            await connection.OpenAsync();
            await using (var drop = new SqlCommand("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'UX_Organizations_Canonical'
                      AND object_id = OBJECT_ID(N'dbo.Organizations'))
                    DROP INDEX UX_Organizations_Canonical ON dbo.Organizations;
                """, connection))
                await drop.ExecuteNonQueryAsync();

            await using var insert = new SqlCommand("""
                INSERT INTO dbo.Organizations (OrganizationId, Name, CreatedAtUtc, IsCanonical)
                VALUES
                  (NEWID(), N'A', SYSUTCDATETIME(), 1),
                  (NEWID(), N'B', SYSUTCDATETIME(), 1);
                """, connection);
            await insert.ExecuteNonQueryAsync();
        }

        var service = new SqlServerCanonicalOrganizationService(cs);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TryGetCanonicalAsync());
        Assert.Contains("várias organizações canónicas", ex.Message, StringComparison.OrdinalIgnoreCase);

        await ClearOrganizationsAsync(cs);
        await using (var connection = new SqlConnection(cs))
        {
            await connection.OpenAsync();
            await using var recreate = new SqlCommand("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'UX_Organizations_Canonical'
                      AND object_id = OBJECT_ID(N'dbo.Organizations'))
                    CREATE UNIQUE INDEX UX_Organizations_Canonical
                        ON dbo.Organizations (IsCanonical)
                        WHERE IsCanonical = 1;
                """, connection);
            await recreate.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task Receipt_attempts_remain_usable_alongside_organizations()
    {
        var cs = await SetupAsync();
        var orgService = new SqlServerCanonicalOrganizationService(cs);
        var canonical = await orgService.EnsureCanonicalAsync("Empresa Receipt Compat");

        var repository = new SqlServerPackageReceiptAttemptRepository(cs);
        var hash = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())).ToLowerInvariant();
        var validation = new IkpPackageValidationResult(
            Guid.NewGuid(), Guid.NewGuid(), "0.1", "PhcKnowledgeScanner", "test",
            "IntegrationTests", hash, 1024, 1, 1, 0, 1, 1, 0, hash);

        // Historical semantics: OrganizationId on attempts is supplied by caller (not FK to Organizations).
        var foreignOrg = Guid.NewGuid();
        Assert.NotEqual(canonical.OrganizationId, foreignOrg);

        var registration = await repository.CreateOrGetPendingAsync(
            foreignOrg, validation, $"sha256/{validation.ZipSha256}.zip", $"staging/{Guid.NewGuid():N}.zip");
        Assert.Equal(foreignOrg, registration.Attempt.OrganizationId);

        var status = await repository.GetByPackageIdAsync(foreignOrg, validation.PackageId);
        Assert.NotNull(status);
        Assert.Equal(foreignOrg, status!.OrganizationId);
    }
}
