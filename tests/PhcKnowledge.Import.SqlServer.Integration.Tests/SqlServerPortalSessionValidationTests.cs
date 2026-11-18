using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

/// <summary>
/// SQL integration coverage for portal cookie session snapshot lookup + validation (phase 1.3.3).
/// </summary>
[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerPortalSessionValidationTests
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

    [Fact]
    public async Task Session_becomes_invalid_after_sql_deactivation_and_stamp_rotation()
    {
        var connectionString = RequireConnectionString();
        await EnsureDatabaseAsync(connectionString);
        await EnsureSqlScriptsAsync(connectionString);
        await ClearIdentityAndOrganizationsAsync(connectionString);

        await using var provider = BuildServices(connectionString);
        using var scope = provider.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<IIdentityMigrator>();
        await migrator.MigrateAsync();

        var orgs = scope.ServiceProvider.GetRequiredService<ICanonicalOrganizationService>();
        var org = await orgs.EnsureCanonicalAsync("Session Validation Org");

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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

        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var activation = scope.ServiceProvider.GetRequiredService<UserActivationService>();
        var validator = scope.ServiceProvider.GetRequiredService<IPortalCookiePrincipalValidator>();

        var user = await factory.CreateAsync("session-sql", "session-sql@example.com", "SQL Session");
        Assert.True((await users.CreateAsync(user, "Str0ng!Passw0rd")).Succeeded);

        var loaded = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotNull(loaded);
        var stamp = loaded!.SecurityStamp;
        Assert.False(string.IsNullOrEmpty(stamp));

        var principal = CreatePrincipal(user.Id, stamp!);
        Assert.True(await validator.IsSessionValidAsync(principal));

        await activation.SetActiveAsync(user.Id, isActive: false);
        Assert.False(await validator.IsSessionValidAsync(principal));

        // Reactivate with a new stamp (activation service rotates stamp).
        await activation.SetActiveAsync(user.Id, isActive: true);
        Assert.False(await validator.IsSessionValidAsync(principal),
            "Cookie stamp must not remain valid after stamp rotation.");

        var reloaded = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotNull(reloaded);
        Assert.True(await validator.IsSessionValidAsync(CreatePrincipal(user.Id, reloaded!.SecurityStamp!)));
    }

    [Fact]
    public async Task Projected_lookup_reads_current_sql_row_without_full_graph()
    {
        var connectionString = RequireConnectionString();
        await EnsureDatabaseAsync(connectionString);
        await EnsureSqlScriptsAsync(connectionString);
        await ClearIdentityAndOrganizationsAsync(connectionString);

        await using var provider = BuildServices(connectionString);
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IIdentityMigrator>().MigrateAsync();

        var orgs = scope.ServiceProvider.GetRequiredService<ICanonicalOrganizationService>();
        var org = await orgs.EnsureCanonicalAsync("Lookup Org");
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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

        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var lookup = scope.ServiceProvider.GetRequiredService<IPortalUserSessionLookup>();

        var user = await factory.CreateAsync("lookup-sql", "lookup-sql@example.com");
        Assert.True((await users.CreateAsync(user, "Str0ng!Passw0rd")).Succeeded);

        var snapshot = await lookup.FindAsync(user.Id);
        Assert.NotNull(snapshot);
        Assert.True(snapshot!.IsActive);
        Assert.Equal((await users.FindByIdAsync(user.Id.ToString("D")))!.SecurityStamp, snapshot.SecurityStamp);
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId, string stamp)
    {
        var options = new IdentityOptions();
        return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(options.ClaimsIdentity.UserIdClaimType, userId.ToString("D")),
                new Claim(options.ClaimsIdentity.SecurityStampClaimType, stamp)
            ],
            authenticationType: PhcAuthenticationSchemes.PortalCookie));
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
