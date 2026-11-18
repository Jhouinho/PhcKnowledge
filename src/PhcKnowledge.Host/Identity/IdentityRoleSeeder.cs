using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Idempotent, concurrency-safe role bootstrap for future CLI / install steps.
/// Does not create users or assign roles.
/// </summary>
public sealed class IdentityRoleSeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    ApplicationDbContext dbContext)
{
    private const string AppLockResource = "PhcKnowledge.Identity.Roles";

    public async Task EnsureRolesAsync(CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection is not SqlConnection sqlConnection)
            throw new InvalidOperationException("IdentityRoleSeeder requer SQL Server (SqlConnection).");

        if (sqlConnection.State != ConnectionState.Open)
            await dbContext.Database.OpenConnectionAsync(cancellationToken);

        await using var sqlTransaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        dbContext.Database.UseTransaction(sqlTransaction);

        try
        {
            await AcquireAppLockAsync(sqlConnection, sqlTransaction, cancellationToken);

            foreach (var roleName in ApplicationRoles.All)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await EnsureRoleUnderLockAsync(roleName);
            }

            await sqlTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await sqlTransaction.RollbackAsync(cancellationToken);
            }
            catch (Exception)
            {
                // Transaction may already be completed.
            }

            throw;
        }
        finally
        {
            dbContext.Database.UseTransaction(null);
        }
    }

    private async Task EnsureRoleUnderLockAsync(string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
            return;

        try
        {
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            });

            if (result.Succeeded)
                return;

            if (await roleManager.RoleExistsAsync(roleName)
                || result.Errors.Any(e =>
                    string.Equals(e.Code, "DuplicateRoleName", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Falha ao criar a role '{roleName}': {errors}");
        }
        catch (DbUpdateException)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                return;
            throw;
        }
    }

    private static async Task AcquireAppLockAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @Resource,
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 30000;
            IF @result < 0
                THROW 50002, N'Não foi possível obter o bloqueio de Identity roles.', 1;
            """, connection, transaction);
        command.Parameters.Add("@Resource", SqlDbType.NVarChar, 255).Value = AppLockResource;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
