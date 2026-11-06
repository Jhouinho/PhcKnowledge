using Microsoft.Data.SqlClient;
using PhcKnowledgeScanner.Abstractions.Connection;

namespace PhcKnowledgeScanner.Collectors.Sql;

public static class SqlConnectionStringBuilderService
{
    public static string Build(SqlConnectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Server))
        {
            throw new ArgumentException("Servidor SQL é obrigatório.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Database))
        {
            throw new ArgumentException("Base de dados é obrigatória.", nameof(request));
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = request.Server.Trim(),
            InitialCatalog = request.Database.Trim(),
            Encrypt = true,
            TrustServerCertificate = request.TrustServerCertificate,
            ConnectTimeout = Math.Clamp(request.ConnectTimeoutSeconds, 3, 120),
            ApplicationIntent = ApplicationIntent.ReadOnly,
            ApplicationName = "PhcKnowledgeScanner",
            PersistSecurityInfo = false
        };

        if (request.AuthenticationMode == SqlAuthenticationMode.Windows)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                throw new ArgumentException("Utilizador SQL é obrigatório para autenticação SQL.", nameof(request));
            }

            builder.IntegratedSecurity = false;
            builder.UserID = request.UserName.Trim();
            builder.Password = request.Password ?? string.Empty;
        }

        return builder.ConnectionString;
    }

    /// <summary>Versão segura para logs: sem password.</summary>
    public static string ToLogSafe(SqlConnectionRequest request)
    {
        return $"Server={request.Server}; Database={request.Database}; Auth={request.AuthenticationMode}; User={request.UserName}";
    }
}
