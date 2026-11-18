using System.Data;
using Microsoft.Data.SqlClient;

namespace PhcKnowledge.Import.Receipts;

/// <summary>
/// Session isolation helpers for pooled SQL Server connections.
/// <see cref="IsolationLevel.Serializable"/> via ADO.NET leaves the session
/// isolation level set after the transaction ends; READPAST requires READ COMMITTED.
/// </summary>
internal static class SqlConnectionIsolation
{
    internal const string SetReadCommittedSql =
        "SET TRANSACTION ISOLATION LEVEL READ COMMITTED;";

    /// <summary>
    /// Forces READ COMMITTED on an open connection. On failure, clears the pool so a
    /// potentially contaminated physical connection is not reused, then rethrows.
    /// </summary>
    internal static async Task EnsureReadCommittedOrClearPoolAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        try
        {
            await using var reset = new SqlCommand(SetReadCommittedSql, connection);
            await reset.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            TryClearPool(connection);
            throw;
        }
    }

    /// <summary>
    /// Restores READ COMMITTED after a Serializable transaction has finished.
    /// If restore fails, clears the pool. When <paramref name="operationError"/> is set,
    /// cleanup failures are swallowed so the original failure remains the thrown exception.
    /// </summary>
    internal static async Task RestoreReadCommittedOrClearPoolAsync(
        SqlConnection connection,
        Exception? operationError,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException(
                    "Não foi possível repor READ COMMITTED: a ligação já não está aberta.");
            }

            await using var reset = new SqlCommand(SetReadCommittedSql, connection);
            await reset.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception restoreEx)
        {
            TryClearPool(connection);
            if (operationError is null)
                throw;

            // Preserve the original operation failure; attach restore detail for diagnostics.
            operationError.Data["SqlConnectionIsolation.RestoreError"] = restoreEx.ToString();
        }
    }

    internal static void TryClearPool(SqlConnection connection)
    {
        try
        {
            SqlConnection.ClearPool(connection);
        }
        catch
        {
            // Best-effort: never hide the caller's primary exception path.
        }
    }
}
