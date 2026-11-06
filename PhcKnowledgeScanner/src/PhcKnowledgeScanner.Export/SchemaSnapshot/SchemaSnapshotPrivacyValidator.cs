using System.Text.Json;
using System.Text.RegularExpressions;
using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;

namespace PhcKnowledgeScanner.Export.SchemaSnapshot;

/// <summary>
/// Valida que o snapshot está em modo seguro para partilha (sem dados de negócio, credenciais ou SQL completo).
/// </summary>
public sealed partial class SchemaSnapshotPrivacyValidator
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "password",
        "pwd",
        "connectionstring",
        "connection_string",
        "userid",
        "user id",
        "data source=",
        "initial catalog=",
        "server=",
        "accountkey",
        "secret",
        "apikey",
        "api_key"
    ];

    public SchemaSnapshotPrivacyReport Validate(DatabaseSchemaSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var report = new SchemaSnapshotPrivacyReport { IsSafe = true };

        if (snapshot.Manifest.IncludesSqlDefinitions)
        {
            Add(report, "privacy.sql_definitions_enabled",
                "O snapshot inclui definições SQL completas e não é seguro para partilha pública automática.",
                "Error");
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Manifest.SqlServerVersion)
            && LooksLikeConnectionString(snapshot.Manifest.SqlServerVersion))
        {
            Add(report, "privacy.server_version_leak",
                "SqlServerVersion parece conter informação sensível de ligação.",
                "Error");
        }

        ScanJson(report, snapshot);

        foreach (var view in snapshot.Views)
        {
            if (!string.IsNullOrWhiteSpace(view.SqlDefinition))
            {
                Add(report, "privacy.view_sql_present",
                    $"View {view.SchemaName}.{view.Name} contém SqlDefinition.",
                    "Error");
            }
        }

        foreach (var trigger in snapshot.Triggers)
        {
            if (!string.IsNullOrWhiteSpace(trigger.SqlDefinition))
            {
                Add(report, "privacy.trigger_sql_present",
                    $"Trigger {trigger.SchemaName}.{trigger.Name} contém SqlDefinition.",
                    "Error");
            }
        }

        foreach (var routine in snapshot.Procedures.Concat(snapshot.Functions))
        {
            if (!string.IsNullOrWhiteSpace(routine.SqlDefinition))
            {
                Add(report, "privacy.routine_sql_present",
                    $"Rotina {routine.SchemaName}.{routine.Name} contém SqlDefinition.",
                    "Error");
            }
        }

        // Heurística: nomes de ficheiro/caminho com client names no manifesto
        if (ClientNamePattern().IsMatch(snapshot.Manifest.OriginFingerprint)
            && snapshot.Manifest.OriginFingerprint.Length != 16)
        {
            Add(report, "privacy.origin_not_fingerprint",
                "OriginFingerprint não parece um fingerprint anonimizado.",
                "Error");
        }

        report.IsSafe = report.Findings.All(f => f.Severity != "Error");
        return report;
    }

    private static void ScanJson(SchemaSnapshotPrivacyReport report, DatabaseSchemaSnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot, SchemaSnapshotJson.Options);
        var lower = json.ToLowerInvariant();

        foreach (var fragment in SensitiveKeyFragments)
        {
            if (lower.Contains(fragment, StringComparison.Ordinal))
            {
                // Defaults SQL como (getdate()) ou ('') podem conter palavras inocentes; filtrar connection-like.
                if (fragment is "server=" or "data source=" or "initial catalog=" or "connectionstring"
                    or "connection_string" or "password" or "pwd" or "accountkey" or "secret")
                {
                    if (LooksLikeConnectionString(json) || fragment is "password" or "pwd" or "accountkey" or "secret")
                    {
                        // "password" em nome de coluna é comum em schema PHC — só alerta se parecer valor/connection.
                        if (fragment is "password" or "pwd")
                        {
                            if (PasswordAssignmentPattern().IsMatch(json))
                            {
                                Add(report, "privacy.credential_pattern",
                                    $"Padrão potencialmente sensível detetado: '{fragment}'.",
                                    "Error");
                            }

                            continue;
                        }

                        Add(report, "privacy.credential_pattern",
                            $"Padrão potencialmente sensível detetado: '{fragment}'.",
                            "Error");
                    }
                }
            }
        }
    }

    private static bool LooksLikeConnectionString(string text) =>
        text.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase)
        || text.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
        || text.Contains("Password=", StringComparison.OrdinalIgnoreCase)
        || text.Contains("Pwd=", StringComparison.OrdinalIgnoreCase);

    private static void Add(SchemaSnapshotPrivacyReport report, string code, string message, string severity) =>
        report.Findings.Add(new SchemaSnapshotPrivacyFinding
        {
            Code = code,
            Message = message,
            Severity = severity
        });

    [GeneratedRegex(@"Password\s*=\s*[^\s;""]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PasswordAssignmentPattern();

    [GeneratedRegex(@"[^a-f0-9]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ClientNamePattern();
}
