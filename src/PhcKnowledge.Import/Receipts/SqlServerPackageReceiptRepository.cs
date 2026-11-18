using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Receipts;

/// <summary>
/// Persists an organization-scoped receipt only after the original IKP ZIP
/// has been validated and archived. It does not assign an InstallationId.
/// </summary>
public sealed class SqlServerPackageReceiptRepository : IPackageReceiptRepository
{
    private const string SelectExistingSql = """
        SELECT TOP (1)
            ReceiptId, OrganizationId, PackageId, CaptureId, ContractVersion,
            ProducerProductId, ProducerProductVersion, OriginDatabaseName,
            ZipSha256, ZipByteLength, StorageKey, ReceivedAtUtc, ValidationSummaryJson
        FROM dbo.PackageIngestionReceipts WITH (UPDLOCK, HOLDLOCK)
        WHERE OrganizationId = @OrganizationId
          AND (PackageId = @PackageId OR ZipSha256 = @ZipSha256);
        """;

    private readonly string _connectionString;

    public SqlServerPackageReceiptRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<PackageReceiptRegistration> RegisterArchivedPackageAsync(
        Guid organizationId,
        IkpPackageValidationResult validation,
        ArchivedPackage archive,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId é obrigatório.", nameof(organizationId));
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(archive);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        Exception? operationError = null;
        try
        {
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);

            var existing = await FindExistingAsync(
                connection, transaction, organizationId, validation.PackageId,
                validation.ZipSha256, cancellationToken);

            if (existing is not null)
            {
                EnsureSamePackage(existing, validation, archive);
                await transaction.CommitAsync(cancellationToken);
                return new PackageReceiptRegistration(existing, true);
            }

            var receipt = CreateReceipt(organizationId, validation, archive);
            try
            {
                await InsertAsync(connection, transaction, receipt, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new PackageReceiptRegistration(receipt, false);
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                await transaction.RollbackAsync(cancellationToken);
                var concurrent = await FindExistingOutsideTransactionAsync(
                    organizationId, validation.PackageId, validation.ZipSha256, cancellationToken);
                if (concurrent is null)
                    throw;
                EnsureSamePackage(concurrent, validation, archive);
                return new PackageReceiptRegistration(concurrent, true);
            }
        }
        catch (Exception ex)
        {
            operationError = ex;
            throw;
        }
        finally
        {
            // Transaction is disposed before this finally (await using). Restore only after
            // commit/rollback; on restore failure ClearPool so SERIALIZABLE cannot leak.
            await SqlConnectionIsolation.RestoreReadCommittedOrClearPoolAsync(
                connection, operationError, CancellationToken.None);
        }
    }

    private static PackageReceipt CreateReceipt(
        Guid organizationId, IkpPackageValidationResult validation, ArchivedPackage archive)
    {
        var summary = JsonSerializer.Serialize(new
        {
            validation.ContractFileCount,
            validation.ChecksummedFileCount,
            validation.ArtifactCount,
            validation.ObjectCount,
            validation.OperationCount,
            validation.IssueCount,
            validation.LogicalContentDigest
        });

        return new PackageReceipt(
            Guid.NewGuid(),
            organizationId,
            validation.PackageId,
            validation.CaptureId,
            validation.ContractVersion,
            validation.ProducerProductId,
            validation.ProducerProductVersion,
            validation.OriginDatabaseName,
            validation.ZipSha256,
            validation.ZipByteLength,
            archive.StorageKey,
            DateTimeOffset.UtcNow,
            summary);
    }

    private static void EnsureSamePackage(
        PackageReceipt existing, IkpPackageValidationResult validation, ArchivedPackage archive)
    {
        if (existing.PackageId != validation.PackageId ||
            !string.Equals(existing.ZipSha256, validation.ZipSha256, StringComparison.Ordinal) ||
            !string.Equals(existing.StorageKey, archive.StorageKey, StringComparison.Ordinal))
        {
            throw new PackageReceiptConflictException(
                "Já existe um registo desta organização para o PackageId ou SHA-256 com conteúdo diferente.",
                PackageReceiptConflictKind.IdentityMismatch);
        }
    }

    private static async Task<PackageReceipt?> FindExistingAsync(
        SqlConnection connection, SqlTransaction transaction, Guid organizationId,
        Guid packageId, string zipSha256, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(SelectExistingSql, connection, transaction);
        AddIdentityParameters(command, organizationId, packageId, zipSha256);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadReceipt(reader) : null;
    }

    private async Task<PackageReceipt?> FindExistingOutsideTransactionAsync(
        Guid organizationId, Guid packageId, string zipSha256, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(SelectExistingSql.Replace(
            " WITH (UPDLOCK, HOLDLOCK)", string.Empty, StringComparison.Ordinal), connection);
        AddIdentityParameters(command, organizationId, packageId, zipSha256);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadReceipt(reader) : null;
    }

    private static async Task InsertAsync(
        SqlConnection connection, SqlTransaction transaction, PackageReceipt receipt,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.PackageIngestionReceipts
            (
                ReceiptId, OrganizationId, PackageId, CaptureId, ContractVersion,
                ProducerProductId, ProducerProductVersion, OriginDatabaseName,
                ZipSha256, ZipByteLength, StorageKey, ReceivedAtUtc, ValidationSummaryJson
            )
            VALUES
            (
                @ReceiptId, @OrganizationId, @PackageId, @CaptureId, @ContractVersion,
                @ProducerProductId, @ProducerProductVersion, @OriginDatabaseName,
                @ZipSha256, @ZipByteLength, @StorageKey, @ReceivedAtUtc, @ValidationSummaryJson
            );
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@ReceiptId", SqlDbType.UniqueIdentifier).Value = receipt.ReceiptId;
        command.Parameters.Add("@OrganizationId", SqlDbType.UniqueIdentifier).Value = receipt.OrganizationId;
        command.Parameters.Add("@PackageId", SqlDbType.UniqueIdentifier).Value = receipt.PackageId;
        command.Parameters.Add("@CaptureId", SqlDbType.UniqueIdentifier).Value = receipt.CaptureId;
        command.Parameters.Add("@ContractVersion", SqlDbType.NVarChar, 16).Value = receipt.ContractVersion;
        command.Parameters.Add("@ProducerProductId", SqlDbType.NVarChar, 128).Value = receipt.ProducerProductId;
        command.Parameters.Add("@ProducerProductVersion", SqlDbType.NVarChar, 64).Value = receipt.ProducerProductVersion;
        command.Parameters.Add("@OriginDatabaseName", SqlDbType.NVarChar, 128).Value =
            (object?)receipt.OriginDatabaseName ?? DBNull.Value;
        command.Parameters.Add("@ZipSha256", SqlDbType.Char, 64).Value = receipt.ZipSha256;
        command.Parameters.Add("@ZipByteLength", SqlDbType.BigInt).Value = receipt.ZipByteLength;
        command.Parameters.Add("@StorageKey", SqlDbType.NVarChar, 512).Value = receipt.StorageKey;
        command.Parameters.Add("@ReceivedAtUtc", SqlDbType.DateTime2).Value = receipt.ReceivedAtUtc.UtcDateTime;
        command.Parameters.Add("@ValidationSummaryJson", SqlDbType.NVarChar, -1).Value = receipt.ValidationSummaryJson;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddIdentityParameters(
        SqlCommand command, Guid organizationId, Guid packageId, string zipSha256)
    {
        command.Parameters.Add("@OrganizationId", SqlDbType.UniqueIdentifier).Value = organizationId;
        command.Parameters.Add("@PackageId", SqlDbType.UniqueIdentifier).Value = packageId;
        command.Parameters.Add("@ZipSha256", SqlDbType.Char, 64).Value = zipSha256;
    }

    private static PackageReceipt ReadReceipt(SqlDataReader reader)
    {
        var receivedAt = DateTime.SpecifyKind(reader.GetDateTime(11), DateTimeKind.Utc);
        return new PackageReceipt(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.GetString(8).Trim(),
            reader.GetInt64(9),
            reader.GetString(10),
            new DateTimeOffset(receivedAt),
            reader.GetString(12));
    }
}