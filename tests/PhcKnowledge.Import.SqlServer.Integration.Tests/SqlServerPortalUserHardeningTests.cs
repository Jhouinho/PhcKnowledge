using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

/// <summary>SQL regression for phase 1.4.2.E hardening (create TX, bootstrap lock, activation bypass).</summary>
[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerPortalUserHardeningTests
{
    private const string StrongPassword = "Str0ng!Passw0rd";

    [Fact]
    public async Task Create_role_assignment_failure_rolls_back_without_residual_user()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs, failingPortalRoleAssigner: true);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Hardening Create Rollback Org");

        var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var ex = await Assert.ThrowsAsync<PortalUserAdministrationException>(
            () => admin.CreateAsync(
                "orphan@example.com", null, ApplicationRoles.Collaborator, StrongPassword));
        Assert.Equal(PortalUserAdministrationError.RoleAssignFailed, ex.Error);
        Assert.Null(await users.FindByEmailAsync("orphan@example.com"));
    }

    [Fact]
    public async Task Concurrent_create_admin_and_demote_preserve_active_administrator()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        Guid existingAdminId;
        using (var prep = sp.CreateScope())
        {
            await PrepareIdentityAsync(prep.ServiceProvider, "Hardening Create Demote Org");
            var admin = prep.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
            existingAdminId = (await admin.CreateAsync(
                "sole-before@example.com", null, ApplicationRoles.Administrator, StrongPassword)).User.UserId;
            await admin.CreateAsync(
                "second-before@example.com", null, ApplicationRoles.Administrator, StrongPassword);
        }

        var results = new ConcurrentBag<string>();
        await Task.WhenAll(
            Task.Run(async () =>
            {
                try
                {
                    using var scope = sp.CreateScope();
                    var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
                    await admin.CreateAsync(
                        "new-admin-conc@example.com", null, ApplicationRoles.Administrator, StrongPassword);
                    results.Add("create-ok");
                }
                catch (Exception ex)
                {
                    results.Add("create-fail:" + ex.GetType().Name);
                }
            }),
            Task.Run(async () =>
            {
                try
                {
                    using var scope = sp.CreateScope();
                    var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
                    await admin.SetRoleAsync(existingAdminId, ApplicationRoles.Collaborator);
                    results.Add("demote-ok");
                }
                catch (PortalUserAdministrationException ex)
                {
                    results.Add("demote-fail:" + ex.Error);
                }
                catch (Exception ex)
                {
                    results.Add("demote-fail:" + ex.GetType().Name);
                }
            }));

        Assert.Contains("create-ok", results);
        using var verify = sp.CreateScope();
        var users = verify.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admins = await users.GetUsersInRoleAsync(ApplicationRoles.Administrator);
        Assert.True(admins.Count(u => u.IsActive) >= 1);
    }

    [Fact]
    public async Task Concurrent_bootstrap_and_create_leave_at_least_one_active_admin()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using (var prep = sp.CreateScope())
            await PrepareIdentityAsync(prep.ServiceProvider, "Hardening Bootstrap Create Org");

        var results = new ConcurrentBag<string>();
        await Task.WhenAll(
            Task.Run(async () =>
            {
                try
                {
                    using var scope = sp.CreateScope();
                    var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
                    await bootstrap.BootstrapAsync("boot-conc@example.com", StrongPassword);
                    results.Add("boot-ok");
                }
                catch (Exception ex)
                {
                    results.Add("boot-fail:" + ex.GetType().Name);
                }
            }),
            Task.Run(async () =>
            {
                try
                {
                    using var scope = sp.CreateScope();
                    var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
                    await admin.CreateAsync(
                        "create-conc@example.com", null, ApplicationRoles.Administrator, StrongPassword);
                    results.Add("create-ok");
                }
                catch (Exception ex)
                {
                    results.Add("create-fail:" + ex.GetType().Name);
                }
            }));

        Assert.True(results.Any(r => r.EndsWith("-ok", StringComparison.Ordinal)));
        using var verify = sp.CreateScope();
        var counter = verify.ServiceProvider.GetRequiredService<CanonicalActiveAdministratorCounter>();
        var org = await verify.ServiceProvider.GetRequiredService<ICanonicalOrganizationService>().TryGetCanonicalAsync();
        Assert.True(await counter.CountAsync(org!.OrganizationId) >= 1);
    }

    [Fact]
    public async Task Concurrent_bootstrap_and_deactivate_keep_active_administrator_invariant()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        Guid adminId;
        using (var prep = sp.CreateScope())
        {
            await PrepareIdentityAsync(prep.ServiceProvider, "Hardening Bootstrap Deactivate Org");
            var admin = prep.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
            adminId = (await admin.CreateAsync(
                "existing-admin@example.com", null, ApplicationRoles.Administrator, StrongPassword)).User.UserId;
            await admin.CreateAsync(
                "other-admin@example.com", null, ApplicationRoles.Administrator, StrongPassword);
        }

        // With two active admins, deactivate one while bootstrap tries (should reject — admins exist).
        var results = new ConcurrentBag<string>();
        await Task.WhenAll(
            Task.Run(async () =>
            {
                try
                {
                    using var scope = sp.CreateScope();
                    var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
                    await bootstrap.BootstrapAsync("should-reject@example.com", StrongPassword);
                    results.Add("boot-ok");
                }
                catch (BootstrapAdminRejectedException)
                {
                    results.Add("boot-rejected");
                }
                catch (Exception ex)
                {
                    results.Add("boot-fail:" + ex.GetType().Name);
                }
            }),
            Task.Run(async () =>
            {
                try
                {
                    using var scope = sp.CreateScope();
                    var admin = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
                    await admin.SetActiveStateAsync(adminId, isActive: false);
                    results.Add("deact-ok");
                }
                catch (Exception ex)
                {
                    results.Add("deact-fail:" + ex.GetType().Name);
                }
            }));

        Assert.Contains("boot-rejected", results);
        using var verify = sp.CreateScope();
        var counter = verify.ServiceProvider.GetRequiredService<CanonicalActiveAdministratorCounter>();
        var org = await verify.ServiceProvider.GetRequiredService<ICanonicalOrganizationService>().TryGetCanonicalAsync();
        Assert.True(await counter.CountAsync(org!.OrganizationId) >= 1);
        Assert.Null(await verify.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
            .FindByEmailAsync("should-reject@example.com"));
    }

    [Fact]
    public async Task Direct_UserActivationService_cannot_deactivate_last_administrator()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Hardening Bypass Org");

        var adminApi = scope.ServiceProvider.GetRequiredService<PortalUserAdministrationService>();
        var activation = scope.ServiceProvider.GetRequiredService<UserActivationService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var only = await adminApi.CreateAsync(
            "bypass-admin@example.com", null, ApplicationRoles.Administrator, StrongPassword);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => activation.SetActiveAsync(only.User.UserId, isActive: false));
        Assert.Contains("fluxo administrativo", ex.Message, StringComparison.OrdinalIgnoreCase);

        var loaded = await users.FindByIdAsync(only.User.UserId.ToString("D"));
        Assert.True(loaded!.IsActive);
    }

    [Fact]
    public async Task Bootstrap_role_failure_rolls_back_without_residual_user()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs, failingBootstrapRoleAssigner: true);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Hardening Bootstrap Rollback Org");

        var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await Assert.ThrowsAsync<BootstrapAdminRejectedException>(
            () => bootstrap.BootstrapAsync("boot-orphan@example.com", StrongPassword));
        Assert.Null(await users.FindByEmailAsync("boot-orphan@example.com"));
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
        bool failingPortalRoleAssigner = false,
        bool failingBootstrapRoleAssigner = false)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<ICanonicalOrganizationService>(
            new SqlServerCanonicalOrganizationService(connectionString));
        services.AddPhcKnowledgeIdentityPersistence(connectionString);
        if (failingPortalRoleAssigner)
        {
            services.RemoveAll<IPortalUserRoleAssigner>();
            services.AddScoped<IPortalUserRoleAssigner, FailingPortalRoleAssigner>();
        }

        if (failingBootstrapRoleAssigner)
        {
            services.RemoveAll<IAdministratorRoleAssigner>();
            services.AddScoped<IAdministratorRoleAssigner, FailingBootstrapRoleAssigner>();
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

    private sealed class FailingPortalRoleAssigner : IPortalUserRoleAssigner
    {
        public Task<IdentityResult> AssignAsync(
            ApplicationUser user,
            string roleName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "ForcedRoleFailure",
                Description = "Forced role assignment failure."
            }));
    }

    private sealed class FailingBootstrapRoleAssigner : IAdministratorRoleAssigner
    {
        public Task<IdentityResult> AssignAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "ForcedBootstrapRoleFailure",
                Description = "Forced bootstrap role failure."
            }));
    }
}
