using System.Collections.Concurrent;
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

/// <summary>SQL coverage for portal admin activate/deactivate (phase 1.4.2.D).</summary>
[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerPortalUserActivationAdminTests
{
    private const string StrongPassword = "Str0ng!Passw0rd";

    [Fact]
    public async Task Deactivate_invalidates_cookie_and_reactivation_allows_new_principal_only()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Act Cookie Org");

        var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var validator = scope.ServiceProvider.GetRequiredService<IPortalCookiePrincipalValidator>();

        await admin.CreateAsync("keeper@example.com", null, ApplicationRoles.Administrator, StrongPassword);
        var target = await admin.CreateAsync(
            "act-cookie@example.com", null, ApplicationRoles.Collaborator, StrongPassword);
        var user = await users.FindByIdAsync(target.User.UserId.ToString("D"));
        var stamp = user!.SecurityStamp!;
        var oldPrincipal = CreatePrincipal(user.Id, stamp);
        Assert.True(await validator.IsSessionValidAsync(oldPrincipal));

        var deactivated = await admin.SetActiveStateAsync(user.Id, isActive: false);
        Assert.False(deactivated!.IsActive);
        Assert.False(await validator.IsSessionValidAsync(oldPrincipal));

        var afterOff = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotEqual(stamp, afterOff!.SecurityStamp);

        var reactivated = await admin.SetActiveStateAsync(user.Id, isActive: true);
        Assert.True(reactivated!.IsActive);
        Assert.False(await validator.IsSessionValidAsync(oldPrincipal),
            "Old cookie must not become valid again after reactivation.");

        var afterOn = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.True(await validator.IsSessionValidAsync(
            CreatePrincipal(user.Id, afterOn!.SecurityStamp!)));
    }

    [Fact]
    public async Task Idempotent_activate_keeps_stamp_last_admin_cannot_deactivate()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Act Idempotent Org");

        var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var only = await admin.CreateAsync(
            "only-admin-act@example.com", null, ApplicationRoles.Administrator, StrongPassword);
        var stamp = (await users.FindByIdAsync(only.User.UserId.ToString("D")))!.SecurityStamp;

        var again = await admin.SetActiveStateAsync(only.User.UserId, isActive: true);
        Assert.True(again!.IsActive);
        Assert.Equal(stamp, (await users.FindByIdAsync(only.User.UserId.ToString("D")))!.SecurityStamp);

        var ex = await Assert.ThrowsAsync<PortalUserAdministrationException>(
            () => admin.SetActiveStateAsync(only.User.UserId, isActive: false));
        Assert.Equal(PortalUserAdministrationError.LastActiveAdministrator, ex.Error);
        Assert.True((await users.FindByIdAsync(only.User.UserId.ToString("D")))!.IsActive);
    }

    [Fact]
    public async Task Second_active_administrator_can_be_deactivated()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Act Two Admins Org");

        var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
        var first = await admin.CreateAsync(
            "a1-act@example.com", null, ApplicationRoles.Administrator, StrongPassword);
        var second = await admin.CreateAsync(
            "a2-act@example.com", null, ApplicationRoles.Administrator, StrongPassword);

        var off = await admin.SetActiveStateAsync(second.User.UserId, isActive: false);
        Assert.False(off!.IsActive);
        Assert.True((await admin.GetAsync(first.User.UserId))!.IsActive);
    }

    [Fact]
    public async Task Concurrent_deactivate_and_demote_leave_one_active_administrator()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        Guid adminA;
        Guid adminB;
        using (var prep = sp.CreateScope())
        {
            await PrepareIdentityAsync(prep.ServiceProvider, "Act Concurrent Org");
            var admin = prep.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
            adminA = (await admin.CreateAsync(
                "conc-act-a@example.com", null, ApplicationRoles.Administrator, StrongPassword)).User.UserId;
            adminB = (await admin.CreateAsync(
                "conc-act-b@example.com", null, ApplicationRoles.Administrator, StrongPassword)).User.UserId;
        }

        var results = new ConcurrentBag<(bool Ok, PortalUserAdministrationError? Error)>();
        await Task.WhenAll(
            RunDeactivateAsync(sp, adminA, results),
            RunSetRoleAsync(sp, adminB, ApplicationRoles.Collaborator, results));

        Assert.Equal(1, results.Count(r => r.Ok));
        Assert.Equal(1, results.Count(r => !r.Ok));
        Assert.Contains(results, r => r.Error == PortalUserAdministrationError.LastActiveAdministrator);

        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admins = await users.GetUsersInRoleAsync(ApplicationRoles.Administrator);
        Assert.Single(admins, u => u.IsActive);
    }

    [Fact]
    public async Task Activation_failure_rolls_back_is_active()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs, forceActivationFailure: true);
        Guid targetId;
        using (var scope = sp.CreateScope())
        {
            await PrepareIdentityAsync(scope.ServiceProvider, "Act Rollback Org");
            var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
            await admin.CreateAsync(
                "keep-act@example.com", null, ApplicationRoles.Administrator, StrongPassword);
            targetId = (await admin.CreateAsync(
                "fail-act@example.com", null, ApplicationRoles.Collaborator, StrongPassword)).User.UserId;

            var ex = await Assert.ThrowsAsync<PortalUserAdministrationException>(
                () => admin.SetActiveStateAsync(targetId, isActive: false));
            Assert.Equal(PortalUserAdministrationError.ActivationFailed, ex.Error);
        }

        using (var verify = sp.CreateScope())
        {
            var users = verify.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(targetId.ToString("D"));
            Assert.True(user!.IsActive);
        }
    }

    private static async Task RunDeactivateAsync(
        ServiceProvider root,
        Guid userId,
        ConcurrentBag<(bool Ok, PortalUserAdministrationError? Error)> results)
    {
        try
        {
            using var scope = root.CreateScope();
            var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
            await admin.SetActiveStateAsync(userId, isActive: false);
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
        bool forceActivationFailure = false)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<ICanonicalOrganizationService>(
            new SqlServerCanonicalOrganizationService(connectionString));
        services.AddPhcKnowledgeIdentityPersistence(connectionString);
        if (forceActivationFailure)
        {
            services.AddScoped<IUserValidator<ApplicationUser>, FailWhenDeactivatingValidator>();
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

    /// <summary>Fails updates when IsActive becomes false (mirrors identity persistence rollback tests).</summary>
    private sealed class FailWhenDeactivatingValidator : IUserValidator<ApplicationUser>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            if (!user.IsActive)
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "ForcedFailure",
                    Description = "ForcedFailure on deactivate."
                }));
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}
