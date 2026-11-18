namespace PhcKnowledge.Host.Http;

public sealed record PortalLoginRequest(string? Email, string? Password);

public sealed record PortalSessionResponse(
    bool Authenticated,
    Guid? UserId,
    string? Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);

public static class PortalAuthMessages
{
    public const string InvalidCredentials = "Credenciais inválidas.";
}
