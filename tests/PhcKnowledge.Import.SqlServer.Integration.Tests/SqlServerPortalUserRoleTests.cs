using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

/// <summary>SQL coverage for portal admin SetRole (phase 1.4.2.C).</summary>
[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerPortalUserRoleTests
{
    private const string StrongPassword = "Str0ng!Passw0rd";

    [SqlServerFact]
    public async Task SetRole_rotates_stamp_and_invalidates_previous_cookie_principal()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Role Stamp Org");

        var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var validator = scope.ServiceProvider.GetRequiredService<IPortalCookiePrincipalValidator>();

        var created = await admin.CreateAsync(
            "role-stamp@example.com", "Stamp", ApplicationRoles.Collaborator, StrongPassword);
        var user = await users.FindByIdAsync(created.User.UserId.ToString("D"));
        Assert.NotNull(user);
        var stamp = user!.SecurityStamp;
        var principal = CreatePrincipal(user.Id, stamp!);
        Assert.True(await validator.IsSessionValidAsync(principal));

        var updated = await admin.SetRoleAsync(user.Id, ApplicationRoles.Administrator);
        Assert.NotNull(updated);
        Assert.Equal([ApplicationRoles.Administrator], updated!.Roles.ToArray());

        var reloaded = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotEqual(stamp, reloaded!.SecurityStamp);
        Assert.False(await validator.IsSessionValidAsync(principal));
        Assert.True(await validator.IsSessionValidAsync(
            CreatePrincipal(user.Id, reloaded.SecurityStamp!)));
    }

    [SqlServerFact]
    public async Task SetRole_idempotent_keeps_security_stamp()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Role Idempotent Org");

        var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var created = await admin.CreateAsync(
            "role-idem@example.com", null, ApplicationRoles.Collaborator, StrongPassword);
        var before = await users.FindByIdAsync(created.User.UserId.ToString("D"));
        var stamp = before!.SecurityStamp;

        await admin.SetRoleAsync(created.User.UserId, ApplicationRoles.Collaborator);
        var after = await users.FindByIdAsync(created.User.UserId.ToString("D"));
        Assert.Equal(stamp, after!.SecurityStamp);
    }

    [SqlServerFact]
    public async Task Last_active_administrator_cannot_be_demoted_but_second_can()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Last Admin Org");

        var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
        var first = await admin.CreateAsync(
            "admin1@example.com", null, ApplicationRoles.Administrator, StrongPassword);
        var second = await admin.CreateAsync(
            "admin2@example.com", null, ApplicationRoles.Administrator, StrongPassword);

        var demoted = await admin.SetRoleAsync(second.User.UserId, ApplicationRoles.Collaborator);
        Assert.Equal([ApplicationRoles.Collaborator], demoted!.Roles.ToArray());

        var ex = await Assert.ThrowsAsync<PortalUserAdministrationException>(
            () => admin.SetRoleAsync(first.User.UserId, ApplicationRoles.Collaborator));
        Assert.Equal(PortalUserAdministrationError.LastActiveAdministrator, ex.Error);
    }

    [SqlServerFact]
    public async Task Concurrent_demotions_leave_exactly_one_active_administrator()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        Guid adminA;
        Guid adminB;
        using (var prep = sp.CreateScope())
        {
            await PrepareIdentityAsync(prep.ServiceProvider, "Concurrent Role Org");
            var admin = prep.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
            adminA = (await admin.CreateAsync(
                "conc-a@example.com", null, ApplicationRoles.Administrator, StrongPassword)).User.UserId;
            adminB = (await admin.CreateAsync(
                "conc-b@example.com", null, ApplicationRoles.Administrator, StrongPassword)).User.UserId;
        }

        var results = new ConcurrentBag<(bool Ok, PortalUserAdministrationError? Error)>();
        await Task.WhenAll(
            RunSetRoleAsync(sp, adminA, ApplicationRoles.Collaborator, results),
            RunSetRoleAsync(sp, adminB, ApplicationRoles.Collaborator, results));

        Assert.Equal(1, results.Count(r => r.Ok));
        Assert.Equal(1, results.Count(r => !r.Ok));
        Assert.Contains(results, r => r.Error == PortalUserAdministrationError.LastActiveAdministrator);

        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admins = await users.GetUsersInRoleAsync(ApplicationRoles.Administrator);
        Assert.Single(admins, u => u.IsActive);
    }

    [SqlServerFact]
    public async Task Stamp_rotation_failure_rolls_back_role_mutation()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs, failingStampRotator: true);
        Guid targetId;
        using (var scope = sp.CreateScope())
        {
            await PrepareIdentityAsync(scope.ServiceProvider, "Role Rollback Org");

            var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
            await admin.CreateAsync(
                "keep-admin@example.com", null, ApplicationRoles.Administrator, StrongPassword);
            var target = await admin.CreateAsync(
                "rollback-collab@example.com", null, ApplicationRoles.Collaborator, StrongPassword);
            targetId = target.User.UserId;

            var ex = await Assert.ThrowsAsync<PortalUserAdministrationException>(
                () => admin.SetRoleAsync(targetId, ApplicationRoles.Administrator));
            Assert.Equal(PortalUserAdministrationError.RoleAssignFailed, ex.Error);
        }

        // Fresh scope avoids Identity in-memory role cache after a rolled-back mutation.
        using (var verify = sp.CreateScope())
        {
            var users = verify.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(targetId.ToString("D"));
            Assert.True(await users.IsInRoleAsync(user!, ApplicationRoles.Collaborator));
            Assert.False(await users.IsInRoleAsync(user!, ApplicationRoles.Administrator));
        }
    }

    private static async Task RunSetRoleAsync(
        ServiceProvider root,
        Guid userId,
        string role,
        ConcurrentBag<(bool Ok, PortalUserAdministrationError? Error)> results)
    {
        try
        {
            using var scope = root.CreateScope();
            var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
            await admin.SetRoleAsync(userId, role);
            results.Add((true, null));
        }
        catch (PortalUserAdministrationException ex)
        {
            results.Add((false, ex.Error));
        }
        catch (Exception)
        {
            results.Add((false, null));
        }
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId, string securityStamp)
    {
        var options = new IdentityOptions();
        return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(options.ClaimsIdentity.UserIdClaimType, userId.ToString("D")),
                new Claim(options.ClaimsIdentity.SecurityStampClaimType, securityStamp)
            ],
            authenticationType: PhcAuthenticationSchemes.PortalCookie));
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

    private static ServiceProvider BuildServices(
        string connectionString,
        bool failingStampRotator = false)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<ICanonicalOrganizationService>(
            new SqlServerCanonicalOrganizationService(connectionString));
        services.AddPhcKnowledgeIdentityPersistence(connectionString);
        if (failingStampRotator)
        {
            services.RemoveAll<IPortalUserSecurityStampRotator>();
            services.AddScoped<IPortalUserSecurityStampRotator, FailingSecurityStampRotator>();
        }

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

    private sealed class FailingSecurityStampRotator : IPortalUserSecurityStampRotator
    {
        public Task<IdentityResult> RotateAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "ForcedStampFailure",
                Description = "Forced security stamp rotation failure."
            }));
    }
}
