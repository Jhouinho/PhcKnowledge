using Microsoft.Data.SqlClient;

namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// Limited retry policy for SQL Server deadlocks (error 1205) on analysis insert.
/// Not a global repository retry helper.
/// </summary>
internal static class SqlServerDeadlockRetry
{
    public const int MaxAttempts = 4;

    public static bool IsDeadlock(SqlException exception) =>
        exception.Errors.Cast<SqlError>().Any(error => error.Number == 1205);

    /// <summary>
    /// Progressive short delay: 25ms, 50ms, 100ms for attempts 1..3 before the next try.
    /// </summary>
    public static TimeSpan DelayBeforeRetry(int failedAttempt)
    {
        if (failedAttempt < 1)
            throw new ArgumentOutOfRangeException(nameof(failedAttempt));
        var shift = Math.Clamp(failedAttempt - 1, 0, 3);
        return TimeSpan.FromMilliseconds(25d * (1 << shift));
    }
}
