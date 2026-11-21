using System.Data;
using Microsoft.Data.SqlClient;

namespace PhcKnowledge.Import.Ikp.Analysis;

/// <summary>
/// SQL Server append-only store for package import analysis decisions (Increment C).
/// Inserts run under Serializable; unique violations (2601/2627) surface as
/// <see cref="PackageImportAnalysisDecisionSequenceConflictException"/> for the domain service
/// to re-read and resolve idempotency or retry with the next sequence. Deadlocks (1205) retry here.
/// </summary>
public sealed class SqlServerPackageImportAnalysisDecisionRepository : IPackageImportAnalysisDecisionRepository
{
    private readonly string _connectionString;

    public SqlServerPackageImportAnalysisDecisionRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<PackageImportAnalysisDecision?> GetEffectiveAsync(
        Guid analysisId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(analysisId, organizationId);

        const string sql = """
            SELECT TOP (1)
                DecisionId, AnalysisId, OrganizationId, Decision, DecisionSequence,
                DecidedAtUtc, DecidedBySubject, DecidedByIssuer, Rationale, SemanticFingerprintSnapshot
            FROM dbo.PackageImportAnalysisDecisions
            WHERE AnalysisId = @AnalysisId
              AND OrganizationId = @OrganizationId
            ORDER BY DecisionSequence DESC;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@AnalysisId", SqlDbType.UniqueIdentifier).Value = analysisId;
        command.Parameters.Add("@OrganizationId", SqlDbType.UniqueIdentifier).Value = organizationId;
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return ReadRow(reader);
    }

    public async Task<IReadOnlyList<PackageImportAnalysisDecision>> ListHistoryAsync(
        Guid analysisId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(analysisId, organizationId);

        const string sql = """
            SELECT
                DecisionId, AnalysisId, OrganizationId, Decision, DecisionSequence,
                DecidedAtUtc, DecidedBySubject, DecidedByIssuer, Rationale, SemanticFingerprintSnapshot
            FROM dbo.PackageImportAnalysisDecisions
            WHERE AnalysisId = @AnalysisId
              AND OrganizationId = @OrganizationId
            ORDER BY DecisionSequence DESC;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@AnalysisId", SqlDbType.UniqueIdentifier).Value = analysisId;
        command.Parameters.Add("@OrganizationId", SqlDbType.UniqueIdentifier).Value = organizationId;
        var list = new List<PackageImportAnalysisDecision>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            list.Add(ReadRow(reader));
        return list;
    }

    public async Task AppendAsync(
        PackageImportAnalysisDecision decision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ValidateKeys(decision.AnalysisId, decision.OrganizationId);
        if (decision.DecisionId == Guid.Empty)
            throw new ArgumentException("DecisionId é obrigatório.", nameof(decision));
        if (decision.DecisionSequence < 1)
            throw new ArgumentOutOfRangeException(nameof(decision), "DecisionSequence deve ser >= 1.");
        if (string.IsNullOrWhiteSpace(decision.DecidedBySubject) || decision.DecidedBySubject.Length != 36)
            throw new ArgumentException("DecidedBySubject inválido.", nameof(decision));
        if (string.IsNullOrWhiteSpace(decision.DecidedByIssuer) || decision.DecidedByIssuer.Length > 64)
            throw new ArgumentException("DecidedByIssuer inválido.", nameof(decision));
        if (string.IsNullOrWhiteSpace(decision.SemanticFingerprintSnapshot) ||
            decision.SemanticFingerprintSnapshot.Length != 64)
        {
            throw new ArgumentException("SemanticFingerprintSnapshot inválido.", nameof(decision));
        }

        if (decision.Rationale is { Length: > PackageImportAnalysisDecisionRules.RationaleMaxLength })
            throw new ArgumentException("Rationale excede o máximo permitido.", nameof(decision));

        SqlException? lastDeadlock = null;
        for (var attempt = 1; attempt <= SqlServerDeadlockRetry.MaxAttempts; attempt++)
        {
            try
            {
                await AppendOnceAsync(decision, cancellationToken);
                return;
            }
            catch (SqlException ex) when (SqlServerDeadlockRetry.IsDeadlock(ex))
            {
                lastDeadlock = ex;
                if (attempt >= SqlServerDeadlockRetry.MaxAttempts)
                    break;
                await Task.Delay(SqlServerDeadlockRetry.DelayBeforeRetry(attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Falha a persistir PackageImportAnalysisDecisions após deadlocks SQL Server (1205) repetidos.",
            lastDeadlock);
    }

    private async Task AppendOnceAsync(
        PackageImportAnalysisDecision decision,
        CancellationToken cancellationToken)
    {
        const string insertSql = """
            INSERT INTO dbo.PackageImportAnalysisDecisions
            (
                DecisionId, AnalysisId, OrganizationId, Decision, DecisionSequence,
                DecidedAtUtc, DecidedBySubject, DecidedByIssuer, Rationale, SemanticFingerprintSnapshot
            )
            VALUES
            (
                @DecisionId, @AnalysisId, @OrganizationId, @Decision, @DecisionSequence,
                @DecidedAtUtc, @DecidedBySubject, @DecidedByIssuer, @Rationale, @SemanticFingerprintSnapshot
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var insert = new SqlCommand(insertSql, connection, transaction);
            insert.Parameters.Add("@DecisionId", SqlDbType.UniqueIdentifier).Value = decision.DecisionId;
            insert.Parameters.Add("@AnalysisId", SqlDbType.UniqueIdentifier).Value = decision.AnalysisId;
            insert.Parameters.Add("@OrganizationId", SqlDbType.UniqueIdentifier).Value = decision.OrganizationId;
            insert.Parameters.Add("@Decision", SqlDbType.VarChar, 16).Value = decision.Decision.ToString();
            insert.Parameters.Add("@DecisionSequence", SqlDbType.Int).Value = decision.DecisionSequence;
            insert.Parameters.Add("@DecidedAtUtc", SqlDbType.DateTime2).Value = decision.DecidedAtUtc.UtcDateTime;
            insert.Parameters.Add("@DecidedBySubject", SqlDbType.NVarChar, 36).Value = decision.DecidedBySubject;
            insert.Parameters.Add("@DecidedByIssuer", SqlDbType.NVarChar, 64).Value = decision.DecidedByIssuer;
            insert.Parameters.Add("@Rationale", SqlDbType.NVarChar, 2000).Value =
                (object?)decision.Rationale ?? DBNull.Value;
            insert.Parameters.Add("@SemanticFingerprintSnapshot", SqlDbType.Char, 64).Value =
                decision.SemanticFingerprintSnapshot;

            try
            {
                await insert.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (SqlException ex) when (IsUniqueViolation(ex))
            {
                try { await transaction.RollbackAsync(cancellationToken); } catch { /* best effort */ }
                throw new PackageImportAnalysisDecisionSequenceConflictException(
                    decision.AnalysisId, decision.OrganizationId, decision.DecisionSequence, ex);
            }
        }
        catch (PackageImportAnalysisDecisionSequenceConflictException)
        {
            throw;
        }
        catch
        {
            try { await transaction.RollbackAsync(cancellationToken); } catch { /* best effort */ }
            throw;
        }
    }

    private static bool IsUniqueViolation(SqlException exception) =>
        exception.Errors.Cast<SqlError>().Any(error => error.Number is 2601 or 2627);

    private static void ValidateKeys(Guid analysisId, Guid organizationId)
    {
        if (analysisId == Guid.Empty)
            throw new ArgumentException("AnalysisId é obrigatório.", nameof(analysisId));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId é obrigatório.", nameof(organizationId));
    }

    private static PackageImportAnalysisDecision ReadRow(SqlDataReader reader)
    {
        var decidedAt = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc);
        var decisionText = reader.GetString(3);
        if (!Enum.TryParse<PackageImportAnalysisDecisionKind>(decisionText, ignoreCase: false, out var kind))
            throw new InvalidOperationException($"Decision SQL inválida: '{decisionText}'.");

        return new PackageImportAnalysisDecision(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            kind,
            reader.GetInt32(4),
            new DateTimeOffset(decidedAt),
            reader.GetString(6),
            reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.GetString(9).TrimEnd()); // CHAR padding only; rationale is NVARCHAR and untouched
    }
}
