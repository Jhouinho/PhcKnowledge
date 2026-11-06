using Microsoft.Data.SqlClient;
using PhcKnowledgeScanner.Abstractions.Connection;

namespace PhcKnowledgeScanner.Collectors.Sql;

public sealed class SqlConnectionTester
{
    public async Task<ConnectionTestResult> TestAsync(SqlConnectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var cs = SqlConnectionStringBuilderService.Build(request);
            await using var connection = new SqlConnection(cs);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT @@VERSION, DB_NAME(), SUSER_SNAME();";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return new ConnectionTestResult(false, "Ligação aberta, mas sem metadados de sessão.");
            }

            var version = reader.IsDBNull(0) ? null : reader.GetString(0);
            var db = reader.IsDBNull(1) ? null : reader.GetString(1);
            var user = reader.IsDBNull(2) ? null : reader.GetString(2);
            var shortVersion = version is null
                ? null
                : version.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

            return new ConnectionTestResult(
                true,
                "Ligação bem-sucedida (ApplicationIntent=ReadOnly).",
                shortVersion,
                db,
                user);
        }
        catch (SqlException ex)
        {
            return new ConnectionTestResult(false, FormatSqlError(ex));
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult(false, "Falha na ligação: " + ex.Message);
        }
    }

    private static string FormatSqlError(SqlException ex)
    {
        // Não incluir connection string nem password.
        return $"Erro SQL {ex.Number}: {ex.Message}";
    }
}
