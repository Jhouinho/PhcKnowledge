using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

/// <summary>SQL coverage for portal admin user create/list (phase 1.4.2.B).</summary>
[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerPortalUserAdministrationTests
{
    private const string StrongPassword = "Str0ng!Passw0rd";

    [Fact]
    public async Task Create_and_list_are_scoped_to_canonical_organization()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Admin Users SQL Org");

        var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
        var created = await admin.CreateAsync(
            "sql-collab@example.com",
            "SQL Collab",
            ApplicationRoles.Collaborator,
            StrongPassword);

        Assert.True(created.User.IsActive);
        Assert.Equal([ApplicationRoles.Collaborator], created.User.Roles.ToArray());

        var list = await admin.ListAsync(query: "sql-collab", isActive: true, role: ApplicationRoles.Collaborator,
            page: 1, pageSize: 10);
        Assert.Contains(list.Items, u => u.UserId == created.User.UserId);

        var detail = await admin.GetAsync(created.User.UserId);
        Assert.NotNull(detail);
        Assert.Equal("SQL Collab", detail!.DisplayName);
    }

    [Fact]
    public async Task Create_rejects_duplicate_email_on_sql()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Dup SQL Org");

        var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
        await admin.CreateAsync("dup-sql@example.com", null, ApplicationRoles.Administrator, StrongPassword);

        var ex = await Assert.ThrowsAsync<PortalUserAdministrationException>(
            () => admin.CreateAsync("dup-sql@example.com", null, ApplicationRoles.Collaborator, StrongPassword));
        Assert.Equal(PortalUserAdministrationError.DuplicateEmail, ex.Error);
    }

    private static async Task PrepareIdentityAsync(IServiceProvider services, string orgName)
    {
        await services.GetRequiredService<IIdentityMigrator>().MigrateAsync();
        var orgs = services.GetRequiredService<ICanonicalOrganizationService>();
        var org = await orgs.EnsureCanonicalAsync(orgName);
        var db = services.GetRequiredService<ApplicationDbContext>();
        if (!await db.Organizations.AnyAsync(o => o.OrganizationId == org.OrganizationId))
        {
            db.Organizations.Add(new OrganizationRow
            {
                OrganizationId = org.OrganizationId,
                Name = org.Name,
                CreatedAtUtc = DateTime.UtcNow,
                IsCanonical = true
            });
            await db.SaveChangesAsync();
        }

        await services.GetRequiredService<IdentityRoleSeeder>().EnsureRolesAsync();
    }

    private static async Task<string> PrepareEmptyAsync()
    {
        var cs = RequireConnectionString();
        await EnsureDatabaseAsync(cs);
        await EnsureSqlScriptsAsync(cs);
        await ClearIdentityAndOrganizationsAsync(cs);
        return cs;
    }

    private static ServiceProvider BuildServices(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<ICanonicalOrganizationService>(
            new SqlServerCanonicalOrganizationService(connectionString));
        services.AddPhcKnowledgeIdentityPersistence(connectionString);
        return services.BuildServiceProvider();
    }

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

    private static async Task EnsureSqlScriptsAsync(string connectionString)
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

    private static async Task ClearIdentityAndOrganizationsAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NOT NULL DELETE FROM dbo.AspNetUserTokens;
            IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NOT NULL DELETE FROM dbo.AspNetUserRoles;
            IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NOT NULL DELETE FROM dbo.AspNetUserLogins;
            IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NOT NULL DELETE FROM dbo.AspNetUserClaims;
            IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NOT NULL DELETE FROM dbo.AspNetRoleClaims;
            IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NOT NULL DELETE FROM dbo.AspNetUsers;
            IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NOT NULL DELETE FROM dbo.AspNetRoles;
            DELETE FROM dbo.Organizations;
            """, connection);
        await command.ExecuteNonQueryAsync();
    }
}
