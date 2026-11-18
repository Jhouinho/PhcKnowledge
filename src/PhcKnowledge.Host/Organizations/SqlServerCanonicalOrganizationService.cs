using System.Data;
using Microsoft.Data.SqlClient;

namespace PhcKnowledge.Host.Organizations;

public sealed class SqlServerCanonicalOrganizationService(string connectionString)
    : ICanonicalOrganizationService
{
    private const string AppLockResource = "PhcKnowledge.Organizations.Canonical";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("A connection string é obrigatória.", nameof(connectionString))
        : connectionString;

    public async Task<CanonicalOrganization?> TryGetCanonicalAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await ReadCanonicalAsync(connection, transaction: null, cancellationToken);
    }

    public async Task<CanonicalOrganization> EnsureCanonicalAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        name = name.Trim();
        if (name.Length > 200)
            throw new ArgumentException("O nome da organização não pode exceder 200 caracteres.", nameof(name));

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        try
        {
            await AcquireAppLockAsync(connection, transaction, cancellationToken);

            var existing = await ReadCanonicalAsync(connection, transaction, cancellationToken);
            if (existing is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return existing;
            }

            await using (var insert = new SqlCommand("""
                INSERT INTO dbo.Organizations (OrganizationId, Name, CreatedAtUtc, IsCanonical)
                OUTPUT inserted.OrganizationId, inserted.Name, inserted.CreatedAtUtc, inserted.IsCanonical
                VALUES (NEWID(), @Name, SYSUTCDATETIME(), 1);
                """, connection, transaction))
            {
                insert.Parameters.Add("@Name", SqlDbType.NVarChar, 200).Value = name;
                await using var reader = await insert.ExecuteReaderAsync(
                    CommandBehavior.SingleRow, cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException("A inserção da organização canónica não devolveu linha.");

                var created = new CanonicalOrganization(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc)),
                    reader.GetBoolean(3));
                await reader.CloseAsync();
                await transaction.CommitAsync(cancellationToken);
                return created;
            }
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
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
                THROW 50001, N'Não foi possível obter o bloqueio de Organizations.', 1;
            """, connection, transaction);
        command.Parameters.Add("@Resource", SqlDbType.NVarChar, 255).Value = AppLockResource;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<CanonicalOrganization?> ReadCanonicalAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var countCommand = new SqlCommand("""
            SELECT COUNT_BIG(*)
            FROM dbo.Organizations
            WHERE IsCanonical = 1;
            """, connection, transaction);
        var count = (long)(await countCommand.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("A contagem de Organizations canónicas falhou."));

        if (count > 1)
            throw new InvalidOperationException(
                "Foram encontradas várias organizações canónicas. Corrigir os dados antes de continuar.");

        if (count == 0)
            return null;

        await using var select = new SqlCommand("""
            SELECT OrganizationId, Name, CreatedAtUtc, IsCanonical
            FROM dbo.Organizations
            WHERE IsCanonical = 1;
            """, connection, transaction);
        await using var reader = await select.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException(
                "Inconsistência: contagem canónica positiva sem linha legível.");

        return new CanonicalOrganization(
            reader.GetGuid(0),
            reader.GetString(1),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc)),
            reader.GetBoolean(3));
    }
}
