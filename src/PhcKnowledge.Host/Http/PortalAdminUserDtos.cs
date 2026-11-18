namespace PhcKnowledge.Host.Http;

public sealed record PortalAdminUserListItemDto(
    Guid UserId,
    string? Email,
    string? UserName,
    string? DisplayName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAtUtc);

public sealed record PortalAdminUserDetailDto(
    Guid UserId,
    string? Email,
    string? UserName,
    string? DisplayName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAtUtc);

public sealed record PortalAdminUserListResponse(
    IReadOnlyList<PortalAdminUserListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record PortalAdminCreateUserRequest(
    string? Email,
    string? DisplayName,
    string? Role,
    string? Password);

public sealed record PortalAdminSetRoleRequest(string? Role);

public static class PortalAdminUserMessages
{
    public const string InvalidRequest = "Pedido inválido.";
    public const string InvalidRole = "A role indicada não é válida.";
    public const string DuplicateEmail = "Já existe um utilizador com este email.";
    public const string CreateFailed = "Não foi possível criar o utilizador.";
    public const string RoleAssignFailed = "Não foi possível atribuir a role ao utilizador.";
    public const string LastActiveAdministrator =
        "Não é possível remover o último administrador ativo.";
    public const string ActivationFailed = "Não foi possível actualizar o estado do utilizador.";
    public const string CanonicalOrganizationMissing =
        "A organização canónica ainda não está disponível.";
}
