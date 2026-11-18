using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace PhcKnowledge.Install;

/// <summary>
/// Applies the platform SQL scripts 001–003 in order (idempotent IF NOT EXISTS scripts).
/// Does not apply EF Identity migrations.
/// </summary>
public sealed class SqlScriptApplicator(string connectionString)
{
    private static readonly string[] ScriptNames =
    [
        "001_package_ingestion_receipts.sql",
        "002_package_receipt_attempts.sql",
        "003_organizations.sql"
    ];

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        var scriptsDirectory = Path.Combine(AppContext.BaseDirectory, "SqlScripts");
        if (!Directory.Exists(scriptsDirectory))
            throw new InvalidOperationException(
                $"Pasta de scripts SQL não encontrada: {scriptsDirectory}");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var name in ScriptNames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(scriptsDirectory, name);
            if (!File.Exists(path))
                throw new InvalidOperationException($"Script SQL em falta: {name}");

            var sql = await File.ReadAllTextAsync(path, cancellationToken);
            foreach (var batch in Regex.Split(sql, @"(?im)^\s*GO\s*(?:--.*)?$")
                         .Where(b => !string.IsNullOrWhiteSpace(b)))
            {
                await using var command = new SqlCommand(batch, connection);
                command.CommandTimeout = 120;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}
