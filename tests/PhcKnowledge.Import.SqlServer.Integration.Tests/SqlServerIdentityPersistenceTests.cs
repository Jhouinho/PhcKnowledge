using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerIdentityPersistenceTests
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

    private static ServiceProvider BuildServices(
        string connectionString,
        bool forceDeactivateFailure = false)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<ICanonicalOrganizationService>(
            _ => new SqlServerCanonicalOrganizationService(connectionString));
        services.AddPhcKnowledgeIdentityPersistence(connectionString);
        if (forceDeactivateFailure)
            services.AddScoped<IUserValidator<ApplicationUser>, ForceDeactivateFailureValidator>();
        return services.BuildServiceProvider();
    }

    private async Task<(string Cs, ServiceProvider Sp, CanonicalOrganization Org)> SetupAsync(
        bool forceDeactivateFailure = false)
    {
        var cs = RequireConnectionString();
        await EnsureDatabaseAsync(cs);
        await EnsureSqlScriptsAsync(cs);
        await ClearIdentityAndOrganizationsAsync(cs);

        var sp = BuildServices(cs, forceDeactivateFailure);
        var migrator = sp.GetRequiredService<IIdentityMigrator>();
        await migrator.MigrateAsync();

        var orgs = sp.GetRequiredService<ICanonicalOrganizationService>();
        var org = await orgs.EnsureCanonicalAsync("Empresa Identity Test");
        return (cs, sp, org);
    }

    [SqlServerFact]
    public async Task ApplicationUser_persists_with_guid_key_and_canonical_organization()
    {
        var (cs, sp, org) = await SetupAsync();
        await using var _ = sp;

        using var scope = sp.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await factory.CreateAsync("alice", "alice@example.com", "Alice Demo");
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(org.OrganizationId, user.OrganizationId);
        Assert.True(user.IsActive);
        Assert.Equal("Alice Demo", user.DisplayName);

        var create = await users.CreateAsync(user, "Str0ng!Passw0rd");
        Assert.True(create.Succeeded, string.Join("; ", create.Errors.Select(e => e.Description)));

        var loaded = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded!.Id);
        Assert.Equal(org.OrganizationId, loaded.OrganizationId);
        Assert.True(loaded.IsActive);
        Assert.Equal("Alice Demo", loaded.DisplayName);
        Assert.Equal(user.CreatedAtUtc, loaded.CreatedAtUtc);

        await using var connection = new SqlConnection(cs);
        await connection.OpenAsync();
        await using var fk = new SqlCommand("""
            SELECT COUNT(*)
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.tables pt ON fkc.referenced_object_id = pt.object_id
            INNER JOIN sys.columns pc ON fkc.referenced_object_id = pc.object_id AND fkc.referenced_column_id = pc.column_id
            INNER JOIN sys.tables ct ON fkc.parent_object_id = ct.object_id
            INNER JOIN sys.columns cc ON fkc.parent_object_id = cc.object_id AND fkc.parent_column_id = cc.column_id
            WHERE ct.name = N'AspNetUsers'
              AND cc.name = N'OrganizationId'
              AND pt.name = N'Organizations'
              AND pc.name = N'OrganizationId';
            """, connection);
        Assert.Equal(1, (int)(await fk.ExecuteScalarAsync() ?? -1));
    }

    [SqlServerFact]
    public async Task UpdateAsync_allows_profile_changes_without_changing_organization()
    {
        var (_, sp, org) = await SetupAsync();
        await using var _ = sp;

        using var scope = sp.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await factory.CreateAsync("dave", "dave@example.com", "Dave");
        Assert.True((await users.CreateAsync(user, "Str0ng!Passw0rd")).Succeeded);

        var loaded = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotNull(loaded);
        loaded!.DisplayName = "David";
        loaded.Email = "david@example.com";
        loaded.NormalizedEmail = "DAVID@EXAMPLE.COM";

        var update = await users.UpdateAsync(loaded);
        Assert.True(update.Succeeded, string.Join("; ", update.Errors.Select(e => e.Description)));

        var again = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotNull(again);
        Assert.Equal(org.OrganizationId, again!.OrganizationId);
        Assert.Equal("David", again.DisplayName);
        Assert.Equal("david@example.com", again.Email);
    }

    [SqlServerFact]
    public async Task UpdateAsync_rejects_organization_id_change()
    {
        var (_, sp, org) = await SetupAsync();
        await using var _ = sp;

        using var scope = sp.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await factory.CreateAsync("erin", "erin@example.com");
        Assert.True((await users.CreateAsync(user, "Str0ng!Passw0rd")).Succeeded);

        var loaded = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotNull(loaded);
        typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.OrganizationId))!
            .SetValue(loaded, Guid.NewGuid());

        var update = await users.UpdateAsync(loaded!);
        Assert.False(update.Succeeded);
        Assert.Contains(update.Errors, e => e.Code == "OrganizationIdImmutable");

        using var verifyScope = sp.CreateScope();
        var verifyUsers = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await verifyUsers.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotNull(persisted);
        Assert.Equal(org.OrganizationId, persisted!.OrganizationId);
    }

    [SqlServerFact]
    public async Task CreateAsync_rejects_guid_empty_organization()
    {
        var (_, sp, _) = await SetupAsync();
        await using var _ = sp;

        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "emptyorg",
            Email = "emptyorg@example.com",
            NormalizedUserName = "EMPTYORG",
            NormalizedEmail = "EMPTYORG@EXAMPLE.COM",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        var create = await users.CreateAsync(user, "Str0ng!Passw0rd");
        Assert.False(create.Succeeded);
        Assert.Contains(create.Errors, e => e.Code == "OrganizationIdRequired");
    }

    [SqlServerFact]
    public async Task Rejects_user_with_nonexistent_organization_id()
    {
        var (_, sp, _) = await SetupAsync();
        await using var _ = sp;

        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "orphan",
            Email = "orphan@example.com",
            NormalizedUserName = "ORPHAN",
            NormalizedEmail = "ORPHAN@EXAMPLE.COM",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        user.BindOrganizationOnCreate(Guid.NewGuid());

        var create = await users.CreateAsync(user, "Str0ng!Passw0rd");
        Assert.False(create.Succeeded);
        Assert.Contains(create.Errors, e => e.Code == "OrganizationIdNotCanonical");
    }

    [SqlServerFact]
    public async Task IsActive_and_security_stamp_persist_together_on_deactivation()
    {
        var (cs, sp, _) = await SetupAsync();
        await using var _ = sp;

        using var scope = sp.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var activation = scope.ServiceProvider.GetRequiredService<UserActivationService>();

        var user = await factory.CreateAsync("carol", "carol@example.com");
        Assert.True((await users.CreateAsync(user, "Str0ng!Passw0rd")).Succeeded);
        var stampBefore = (await users.FindByIdAsync(user.Id.ToString("D")))!.SecurityStamp;

        await activation.SetActiveAsync(user.Id, isActive: false);

        await using var connection = new SqlConnection(cs);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT IsActive, SecurityStamp
            FROM dbo.AspNetUsers
            WHERE Id = @Id;
            """, connection);
        command.Parameters.AddWithValue("@Id", user.Id);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.False(reader.GetBoolean(0));
        Assert.NotEqual(stampBefore, reader.GetString(1));
    }

    [SqlServerFact]
    public async Task SetActiveAsync_noop_keeps_security_stamp_when_already_desired()
    {
        var (_, sp, _) = await SetupAsync();
        await using var _ = sp;

        using var scope = sp.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var activation = scope.ServiceProvider.GetRequiredService<UserActivationService>();

        var user = await factory.CreateAsync("noop2", "noop2@example.com");
        Assert.True((await users.CreateAsync(user, "Str0ng!Passw0rd")).Succeeded);
        var stampBefore = (await users.FindByIdAsync(user.Id.ToString("D")))!.SecurityStamp;

        await activation.SetActiveAsync(user.Id, isActive: true);

        var loaded = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotNull(loaded);
        Assert.True(loaded!.IsActive);
        Assert.Equal(stampBefore, loaded.SecurityStamp);
    }

    [SqlServerFact]
    public async Task SetActiveAsync_rolls_back_when_update_fails()
    {
        var (cs, sp, _) = await SetupAsync(forceDeactivateFailure: true);
        await using var _ = sp;

        Guid userId;
        string stampBefore;
        using (var scope = sp.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var activation = scope.ServiceProvider.GetRequiredService<UserActivationService>();

            var user = await factory.CreateAsync("rollback", "rollback@example.com");
            Assert.True((await users.CreateAsync(user, "Str0ng!Passw0rd")).Succeeded);
            userId = user.Id;
            stampBefore = (await users.FindByIdAsync(user.Id.ToString("D")))!.SecurityStamp!;

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => activation.SetActiveAsync(user.Id, isActive: false));
            Assert.Contains("ForcedFailure", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        await using var connection = new SqlConnection(cs);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT IsActive, SecurityStamp
            FROM dbo.AspNetUsers
            WHERE Id = @Id;
            """, connection);
        command.Parameters.AddWithValue("@Id", userId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.GetBoolean(0));
        Assert.Equal(stampBefore, reader.GetString(1));
    }

    [SqlServerFact]
    public async Task Role_seeder_preserves_ids_on_rerun()
    {
        var (_, sp, _) = await SetupAsync();
        await using var _ = sp;

        using var scope = sp.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentityRoleSeeder>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await seeder.EnsureRolesAsync();
        var first = roles.Roles.OrderBy(r => r.Name).ToList()
            .Select(r => (r.Name, r.Id)).ToList();
        Assert.Equal(2, first.Count);

        await seeder.EnsureRolesAsync();
        var second = roles.Roles.OrderBy(r => r.Name).ToList()
            .Select(r => (r.Name, r.Id)).ToList();
        Assert.Equal(first, second);
    }

    [SqlServerFact]
    public async Task Role_seeder_concurrent_executions_create_roles_once()
    {
        var cs = RequireConnectionString();
        await EnsureDatabaseAsync(cs);
        await EnsureSqlScriptsAsync(cs);
        await ClearIdentityAndOrganizationsAsync(cs);

        await using var bootstrap = BuildServices(cs);
        await bootstrap.GetRequiredService<IIdentityMigrator>().MigrateAsync();
        await bootstrap.GetRequiredService<ICanonicalOrganizationService>()
            .EnsureCanonicalAsync("Empresa Identity Test");

        await using var sp1 = BuildServices(cs);
        await using var sp2 = BuildServices(cs);

        await Task.WhenAll(
            Task.Run(async () =>
            {
                using var scope = sp1.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IdentityRoleSeeder>().EnsureRolesAsync();
            }),
            Task.Run(async () =>
            {
                using var scope = sp2.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IdentityRoleSeeder>().EnsureRolesAsync();
            }));

        using var verify = sp1.CreateScope();
        var roles = verify.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var all = roles.Roles.ToList();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, r => r.Name == ApplicationRoles.Administrator);
        Assert.Contains(all, r => r.Name == ApplicationRoles.Collaborator);
    }

    [SqlServerFact]
    public async Task Migration_reuses_existing_organizations_table_without_recreate()
    {
        var (cs, sp, org) = await SetupAsync();
        await using var _ = sp;

        await using var connection = new SqlConnection(cs);
        await connection.OpenAsync();
        await using var count = new SqlCommand("SELECT COUNT(*) FROM dbo.Organizations;", connection);
        Assert.Equal(1, (int)(await count.ExecuteScalarAsync() ?? -1));

        await using (var orgRow = new SqlCommand(
            "SELECT OrganizationId, Name, IsCanonical FROM dbo.Organizations WHERE OrganizationId = @Id;",
            connection))
        {
            orgRow.Parameters.AddWithValue("@Id", org.OrganizationId);
            await using var reader = await orgRow.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(org.OrganizationId, reader.GetGuid(0));
            Assert.Equal("Empresa Identity Test", reader.GetString(1));
            Assert.True(reader.GetBoolean(2));
        }

        await using (var history = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE N'%InitialIdentity';",
            connection))
        {
            Assert.Equal(1, (int)(await history.ExecuteScalarAsync() ?? -1));
        }

        await using var col = new SqlCommand("""
            SELECT COUNT(*)
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.Organizations')
              AND name IN (N'OrganizationId', N'Name', N'CreatedAtUtc', N'IsCanonical');
            """, connection);
        Assert.Equal(4, (int)(await col.ExecuteScalarAsync() ?? -1));
    }

    [SqlServerFact]
    public async Task Db_fk_rejects_organization_that_bypasses_validator()
    {
        var (cs, sp, _) = await SetupAsync();
        await using var _ = sp;

        await using var connection = new SqlConnection(cs);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            INSERT INTO dbo.AspNetUsers (
                Id, OrganizationId, IsActive, DisplayName, CreatedAtUtc,
                UserName, NormalizedUserName, Email, NormalizedEmail,
                EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
                PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES (
                @Id, @OrgId, 1, NULL, SYSUTCDATETIME(),
                N'bypass', N'BYPASS', N'bypass@example.com', N'BYPASS@EXAMPLE.COM',
                0, N'x', N'stamp', N'conc',
                0, 0, 1, 0);
            """, connection);
        command.Parameters.AddWithValue("@Id", Guid.NewGuid());
        command.Parameters.AddWithValue("@OrgId", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
        Assert.Contains("FK_AspNetUsers_Organizations_OrganizationId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test-only validator: fails whenever IsActive is false so SetActiveAsync cannot persist.
    /// </summary>
    private sealed class ForceDeactivateFailureValidator : IUserValidator<ApplicationUser>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            if (!user.IsActive)
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "ForcedFailure",
                    Description = "ForcedFailure"
                }));
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}
