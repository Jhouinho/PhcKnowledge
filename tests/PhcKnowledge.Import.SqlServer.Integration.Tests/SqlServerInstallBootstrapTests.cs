using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

/// <summary>
/// SQL integration tests for install bootstrap (organization, roles, first admin, concurrency).
/// </summary>
[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerInstallBootstrapTests
{
    private const string StrongPassword = "Str0ng!Passw0rd";

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

    [SqlServerFact]
    public async Task Ensure_organization_is_idempotent_and_preserves_guid()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        var orgs = sp.GetRequiredService<ICanonicalOrganizationService>();

        var first = await orgs.EnsureCanonicalAsync("Install Org A");
        var second = await orgs.EnsureCanonicalAsync("Install Org B Should Be Ignored");

        Assert.Equal(first.OrganizationId, second.OrganizationId);
        Assert.Equal("Install Org A", second.Name);
    }

    [SqlServerFact]
    public async Task Ensure_roles_is_idempotent_and_preserves_ids()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IIdentityMigrator>().MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<IdentityRoleSeeder>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await seeder.EnsureRolesAsync();
        var adminId = (await roles.FindByNameAsync(ApplicationRoles.Administrator))!.Id;
        var collabId = (await roles.FindByNameAsync(ApplicationRoles.Collaborator))!.Id;

        await seeder.EnsureRolesAsync();
        Assert.Equal(adminId, (await roles.FindByNameAsync(ApplicationRoles.Administrator))!.Id);
        Assert.Equal(collabId, (await roles.FindByNameAsync(ApplicationRoles.Collaborator))!.Id);
    }

    [SqlServerFact]
    public async Task Bootstrap_admin_creates_active_administrator_on_canonical_org()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Bootstrap Org");

        var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var orgs = scope.ServiceProvider.GetRequiredService<ICanonicalOrganizationService>();

        var result = await bootstrap.BootstrapAsync("admin@example.com", StrongPassword, "Admin");
        var user = await users.FindByIdAsync(result.UserId.ToString("D"));
        Assert.NotNull(user);
        Assert.True(user!.IsActive);
        Assert.Equal((await orgs.TryGetCanonicalAsync())!.OrganizationId, user.OrganizationId);
        Assert.True(await users.IsInRoleAsync(user, ApplicationRoles.Administrator));
    }

    [SqlServerFact]
    public async Task Bootstrap_admin_rejects_when_administrator_already_exists()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Reject Second Org");

        var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
        await bootstrap.BootstrapAsync("first-admin@example.com", StrongPassword);

        var ex = await Assert.ThrowsAsync<BootstrapAdminRejectedException>(
            () => bootstrap.BootstrapAsync("second-admin@example.com", StrongPassword));
        Assert.Contains("administrador ativo", ex.Message, StringComparison.OrdinalIgnoreCase);

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await users.FindByEmailAsync("second-admin@example.com"));
    }

    [SqlServerFact]
    public async Task Bootstrap_admin_rejects_duplicate_email_without_creating_admin_role_user()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Dup Email Org");

        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await factory.CreateAsync("dup@example.com", "dup@example.com");
        Assert.True((await users.CreateAsync(existing, StrongPassword)).Succeeded);

        var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
        var ex = await Assert.ThrowsAsync<BootstrapAdminRejectedException>(
            () => bootstrap.BootstrapAsync("dup@example.com", StrongPassword));
        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await users.IsInRoleAsync(existing, ApplicationRoles.Administrator));
    }

    [SqlServerFact]
    public async Task Invalid_password_does_not_create_user()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Weak Pwd Org");

        var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
        await Assert.ThrowsAsync<BootstrapAdminRejectedException>(
            () => bootstrap.BootstrapAsync("weak@example.com", "short"));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await users.FindByEmailAsync("weak@example.com"));
    }

    [SqlServerFact]
    public async Task Role_assignment_failure_does_not_leave_partial_user()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs, failingRoleAssigner: true);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Role Fail Org");

        var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
        await Assert.ThrowsAsync<BootstrapAdminRejectedException>(
            () => bootstrap.BootstrapAsync("partial@example.com", StrongPassword));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await users.FindByEmailAsync("partial@example.com"));
    }

    [SqlServerFact]
    public async Task Concurrent_bootstrap_creates_exactly_one_administrator()
    {
        var cs = await PrepareEmptyAsync();
        await using var sp = BuildServices(cs);
        using (var prep = sp.CreateScope())
            await PrepareIdentityAsync(prep.ServiceProvider, "Concurrent Org");

        var results = new ConcurrentBag<(bool Ok, string? Error)>();
        await Task.WhenAll(
            RunBootstrapAttemptAsync(sp, "a1@example.com", results),
            RunBootstrapAttemptAsync(sp, "a2@example.com", results));

        Assert.Equal(1, results.Count(r => r.Ok));
        Assert.Equal(1, results.Count(r => !r.Ok));

        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admins = await users.GetUsersInRoleAsync(ApplicationRoles.Administrator);
        Assert.Single(admins);
    }

    [SqlServerFact]
    public async Task Bootstrap_logs_do_not_contain_password()
    {
        var cs = await PrepareEmptyAsync();
        var sink = new ListLoggerProvider();
        await using var sp = BuildServices(cs, loggerProvider: sink);
        using var scope = sp.CreateScope();
        await PrepareIdentityAsync(scope.ServiceProvider, "Log Org");

        var password = "UniqueSecret!Pass99";
        var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
        await bootstrap.BootstrapAsync("logadmin@example.com", password);

        var all = string.Join("\n", sink.Messages);
        Assert.DoesNotContain(password, all, StringComparison.Ordinal);
        Assert.DoesNotContain("UniqueSecret", all, StringComparison.Ordinal);
    }

    private static async Task RunBootstrapAttemptAsync(
        ServiceProvider root,
        string email,
        ConcurrentBag<(bool Ok, string? Error)> results)
    {
        try
        {
            using var scope = root.CreateScope();
            var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
            await bootstrap.BootstrapAsync(email, StrongPassword);
            results.Add((true, null));
        }
        catch (Exception ex)
        {
            results.Add((false, ex.Message));
        }
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
        bool failingRoleAssigner = false,
        ILoggerProvider? loggerProvider = null)
    {
        var services = new ServiceCollection();
        if (loggerProvider is not null)
            services.AddLogging(b => b.AddProvider(loggerProvider));
        else
            services.AddLogging(b => b.AddDebug());

        services.AddSingleton<ICanonicalOrganizationService>(
            new SqlServerCanonicalOrganizationService(connectionString));
        services.AddPhcKnowledgeIdentityPersistence(connectionString);
        if (failingRoleAssigner)
        {
            // Last registration wins for IAdministratorRoleAssigner.
            services.AddScoped<IAdministratorRoleAssigner, FailingAdministratorRoleAssigner>();
        }

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
            "003_organizations.sql",
            "004_package_import_analyses.sql"
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

    private sealed class FailingAdministratorRoleAssigner : IAdministratorRoleAssigner
    {
        public Task<IdentityResult> AssignAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "ForcedRoleFailure",
                Description = "Forced role assignment failure."
            }));
    }

    private sealed class ListLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new ListLogger(Messages);

        public void Dispose()
        {
        }

        private sealed class ListLogger(List<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                messages.Add(formatter(state, exception));
                if (exception is not null)
                    messages.Add(exception.ToString());
            }
        }
    }
}
