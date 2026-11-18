using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Exclusive SQL application lock + Serializable transaction for administrative invariants
/// (role changes, activation). Non-SQL providers (e.g. InMemory tests) run the
/// action without applock.
/// </summary>
public sealed class IdentityAdminInvariantGuard(ApplicationDbContext dbContext)
{
    /// <summary>Shared resource for portal admin user mutations (roles, activation).</summary>
    public const string AdminUsersResource = "PhcKnowledge.Identity.AdminUsers";

    public async Task ExecuteAsync(
        string resourceName,
        string lockFailureMessage,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(action);

        // InMemory (HTTP unit tests) is not relational — GetDbConnection throws.
        if (!dbContext.Database.IsRelational())
        {
            await action(cancellationToken);
            return;
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection is not SqlConnection sqlConnection)
        {
            await action(cancellationToken);
            return;
        }

        if (sqlConnection.State != ConnectionState.Open)
            await dbContext.Database.OpenConnectionAsync(cancellationToken);

        await using var sqlTransaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        dbContext.Database.UseTransaction(sqlTransaction);

        try
        {
            await AcquireAppLockAsync(
                sqlConnection, sqlTransaction, resourceName, lockFailureMessage, cancellationToken);

            await action(cancellationToken);
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

    private static async Task AcquireAppLockAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string resourceName,
        string lockFailureMessage,
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
                THROW 50004, @FailureMessage, 1;
            """, connection, transaction);
        command.Parameters.Add("@Resource", SqlDbType.NVarChar, 255).Value = resourceName;
        command.Parameters.Add("@FailureMessage", SqlDbType.NVarChar, 4000).Value = lockFailureMessage;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
