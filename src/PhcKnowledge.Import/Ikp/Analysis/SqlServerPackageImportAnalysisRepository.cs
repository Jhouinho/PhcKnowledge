using System.Data;
using Microsoft.Data.SqlClient;

namespace PhcKnowledge.Import.Ikp.Analysis;

public sealed class SqlServerPackageImportAnalysisRepository : IPackageImportAnalysisRepository
{
    private readonly string _connectionString;

    public SqlServerPackageImportAnalysisRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<PersistedPackageImportAnalysis?> GetAsync(
        Guid organizationId,
        Guid packageId,
        string zipSha256,
        string analysisSchemaVersion,
        CancellationToken cancellationToken = default)
    {
        ValidateKeys(organizationId, packageId, zipSha256, analysisSchemaVersion);

        const string sql = """
            SELECT AnalysisId, OrganizationId, ReceiptId, PackageId, CaptureId,
                   ZipSha256, ZipByteLength, LogicalContentDigest, AnalysisSchemaVersion,
                   ContractVersion, ReadinessStatus, SemanticFingerprint, AnalysisJson,
                   ObjectCount, ArtifactCount, IssueCount, UnavailableObjectCount,
                   AnalyzedAtUtc, CreatedAtUtc
            FROM dbo.PackageImportAnalyses
            WHERE OrganizationId = @OrganizationId
              AND PackageId = @PackageId
              AND ZipSha256 = @ZipSha256
              AND AnalysisSchemaVersion = @AnalysisSchemaVersion;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        AddKeyParameters(command, organizationId, packageId, zipSha256, analysisSchemaVersion);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return ReadRow(reader);
    }

    public async Task<PersistedPackageImportAnalysis> InsertOrGetExistingAsync(
        PersistedPackageImportAnalysis analysis,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ValidateKeys(
            analysis.OrganizationId,
            analysis.PackageId,
            analysis.ZipSha256,
            analysis.AnalysisSchemaVersion);

        SqlException? lastDeadlock = null;
        for (var attempt = 1; attempt <= SqlServerDeadlockRetry.MaxAttempts; attempt++)
        {
            try
            {
                return await InsertOrGetExistingOnceAsync(analysis, cancellationToken);
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
            "Falha a persistir PackageImportAnalyses após deadlocks SQL Server (1205) repetidos.",
            lastDeadlock);
    }

    private async Task<PersistedPackageImportAnalysis> InsertOrGetExistingOnceAsync(
        PersistedPackageImportAnalysis analysis,
        CancellationToken cancellationToken)
    {
        const string insertSql = """
            INSERT INTO dbo.PackageImportAnalyses
            (
                AnalysisId, OrganizationId, ReceiptId, PackageId, CaptureId,
                ZipSha256, ZipByteLength, LogicalContentDigest, AnalysisSchemaVersion,
                ContractVersion, ReadinessStatus, SemanticFingerprint, AnalysisJson,
                ObjectCount, ArtifactCount, IssueCount, UnavailableObjectCount,
                AnalyzedAtUtc, CreatedAtUtc
            )
            VALUES
            (
                @AnalysisId, @OrganizationId, @ReceiptId, @PackageId, @CaptureId,
                @ZipSha256, @ZipByteLength, @LogicalContentDigest, @AnalysisSchemaVersion,
                @ContractVersion, @ReadinessStatus, @SemanticFingerprint, @AnalysisJson,
                @ObjectCount, @ArtifactCount, @IssueCount, @UnavailableObjectCount,
                @AnalyzedAtUtc, @CreatedAtUtc
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using (var insert = new SqlCommand(insertSql, connection, transaction))
            {
                insert.Parameters.Add("@AnalysisId", SqlDbType.UniqueIdentifier).Value = analysis.AnalysisId;
                insert.Parameters.Add("@OrganizationId", SqlDbType.UniqueIdentifier).Value = analysis.OrganizationId;
                insert.Parameters.Add("@ReceiptId", SqlDbType.UniqueIdentifier).Value = analysis.ReceiptId;
                insert.Parameters.Add("@PackageId", SqlDbType.UniqueIdentifier).Value = analysis.PackageId;
                insert.Parameters.Add("@CaptureId", SqlDbType.UniqueIdentifier).Value = analysis.CaptureId;
                insert.Parameters.Add("@ZipSha256", SqlDbType.Char, 64).Value = analysis.ZipSha256;
                insert.Parameters.Add("@ZipByteLength", SqlDbType.BigInt).Value = analysis.ZipByteLength;
                insert.Parameters.Add("@LogicalContentDigest", SqlDbType.Char, 64).Value =
                    (object?)analysis.LogicalContentDigest ?? DBNull.Value;
                insert.Parameters.Add("@AnalysisSchemaVersion", SqlDbType.NVarChar, 16).Value =
                    analysis.AnalysisSchemaVersion;
                insert.Parameters.Add("@ContractVersion", SqlDbType.NVarChar, 16).Value = analysis.ContractVersion;
                insert.Parameters.Add("@ReadinessStatus", SqlDbType.VarChar, 32).Value = analysis.ReadinessStatus;
                insert.Parameters.Add("@SemanticFingerprint", SqlDbType.Char, 64).Value = analysis.SemanticFingerprint;
                insert.Parameters.Add("@AnalysisJson", SqlDbType.NVarChar, -1).Value = analysis.AnalysisJson;
                insert.Parameters.Add("@ObjectCount", SqlDbType.Int).Value = analysis.ObjectCount;
                insert.Parameters.Add("@ArtifactCount", SqlDbType.Int).Value = analysis.ArtifactCount;
                insert.Parameters.Add("@IssueCount", SqlDbType.Int).Value = analysis.IssueCount;
                insert.Parameters.Add("@UnavailableObjectCount", SqlDbType.Int).Value = analysis.UnavailableObjectCount;
                insert.Parameters.Add("@AnalyzedAtUtc", SqlDbType.DateTime2).Value = analysis.AnalyzedAtUtc.UtcDateTime;
                insert.Parameters.Add("@CreatedAtUtc", SqlDbType.DateTime2).Value = analysis.CreatedAtUtc.UtcDateTime;

                try
                {
                    await insert.ExecuteNonQueryAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return analysis;
                }
                catch (SqlException ex) when (ex.Number is 2601 or 2627)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
            }
        }
        catch
        {
            try { await transaction.RollbackAsync(cancellationToken); } catch { /* best effort */ }
            throw;
        }

        var existing = await GetAsync(
            analysis.OrganizationId,
            analysis.PackageId,
            analysis.ZipSha256,
            analysis.AnalysisSchemaVersion,
            cancellationToken);
        if (existing is null)
            throw new InvalidOperationException(
                "Conflito de unicidade em PackageImportAnalyses sem registo vencedor legível.");
        return existing;
    }

    private static void ValidateKeys(
        Guid organizationId, Guid packageId, string zipSha256, string analysisSchemaVersion)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId é obrigatório.", nameof(organizationId));
        if (packageId == Guid.Empty)
            throw new ArgumentException("PackageId é obrigatório.", nameof(packageId));
        ArgumentException.ThrowIfNullOrWhiteSpace(zipSha256);
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisSchemaVersion);
        if (zipSha256.Length != 64 || zipSha256 != zipSha256.ToLowerInvariant())
            throw new ArgumentException("ZipSha256 inválido.", nameof(zipSha256));
    }

    private static void AddKeyParameters(
        SqlCommand command,
        Guid organizationId,
        Guid packageId,
        string zipSha256,
        string analysisSchemaVersion)
    {
        command.Parameters.Add("@OrganizationId", SqlDbType.UniqueIdentifier).Value = organizationId;
        command.Parameters.Add("@PackageId", SqlDbType.UniqueIdentifier).Value = packageId;
        command.Parameters.Add("@ZipSha256", SqlDbType.Char, 64).Value = zipSha256;
        command.Parameters.Add("@AnalysisSchemaVersion", SqlDbType.NVarChar, 16).Value = analysisSchemaVersion;
    }

    private static PersistedPackageImportAnalysis ReadRow(SqlDataReader reader)
    {
        var analyzed = DateTime.SpecifyKind(reader.GetDateTime(17), DateTimeKind.Utc);
        var created = DateTime.SpecifyKind(reader.GetDateTime(18), DateTimeKind.Utc);
        return new PersistedPackageImportAnalysis(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetGuid(4),
            reader.GetString(5).Trim(),
            reader.GetInt64(6),
            reader.IsDBNull(7) ? null : reader.GetString(7).Trim(),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetString(10),
            reader.GetString(11).Trim(),
            reader.GetString(12),
            reader.GetInt32(13),
            reader.GetInt32(14),
            reader.GetInt32(15),
            reader.GetInt32(16),
            new DateTimeOffset(analyzed),
            new DateTimeOffset(created));
    }
}
