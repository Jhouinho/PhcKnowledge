namespace PhcKnowledgeScanner.Abstractions.Connection;

/// <summary>
/// Pedido de ligação. A palavra-passe nunca deve ser persistida, registada em logs nem exportada.
/// </summary>
public sealed class SqlConnectionRequest
{
    public required string Server { get; init; }
    public required string Database { get; init; }
    public required SqlAuthenticationMode AuthenticationMode { get; init; }
    public string? UserName { get; init; }

    /// <summary>Apenas em memória. Não serializar.</summary>
    public string? Password { get; init; }

    public bool TrustServerCertificate { get; init; } = true;
    public int ConnectTimeoutSeconds { get; init; } = 15;
}
