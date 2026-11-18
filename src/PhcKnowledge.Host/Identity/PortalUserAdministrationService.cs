using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Administrative user queries, creation, role changes and activation for the canonical organization.
/// </summary>
public interface IPortalUserRoleAssigner
{
    Task<IdentityResult> AssignAsync(
        ApplicationUser user,
        string roleName,
        CancellationToken cancellationToken = default);
}

public sealed class IdentityPortalUserRoleAssigner(UserManager<ApplicationUser> userManager)
    : IPortalUserRoleAssigner
{
    public Task<IdentityResult> AssignAsync(
        ApplicationUser user,
        string roleName,
        CancellationToken cancellationToken = default) =>
        userManager.AddToRoleAsync(user, roleName);
}

/// <summary>Seam for security stamp rotation after effective role changes (testable).</summary>
public interface IPortalUserSecurityStampRotator
{
    Task<IdentityResult> RotateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);
}

public sealed class IdentityPortalUserSecurityStampRotator(UserManager<ApplicationUser> userManager)
    : IPortalUserSecurityStampRotator
{
    public Task<IdentityResult> RotateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default) =>
        userManager.UpdateSecurityStampAsync(user);
}

public sealed class PortalUserAdministrationService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ApplicationUserFactory userFactory,
    ICanonicalOrganizationService organizations,
    IPortalUserRoleAssigner roleAssigner,
    IPortalUserSecurityStampRotator securityStampRotator,
    UserActivationService activationService,
    CanonicalActiveAdministratorCounter activeAdministrators,
    IdentityAdminInvariantGuard invariantGuard,
    ILogger<PortalUserAdministrationService> logger)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public async Task<PortalUserListResult> ListAsync(
        string? query,
        bool? isActive,
        string? role,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var org = await RequireCanonicalAsync(cancellationToken);
        ValidateOptionalRoleFilter(role);

        var usersQuery = dbContext.Users.AsNoTracking()
            .Where(u => u.OrganizationId == org.OrganizationId);

        if (isActive is not null)
            usersQuery = usersQuery.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            var termLower = term.ToLowerInvariant();
            usersQuery = usersQuery.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(termLower)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(termLower)) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(termLower)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleEntity = await roleManager.FindByNameAsync(role.Trim());
            if (roleEntity is null)
            {
                return new PortalUserListResult([], page, pageSize, 0);
            }

            var roleId = roleEntity.Id;
            usersQuery =
                from u in usersQuery
                join ur in dbContext.Set<IdentityUserRole<Guid>>().AsNoTracking()
                    on u.Id equals ur.UserId
                where ur.RoleId == roleId
                select u;
        }

        var totalCount = await usersQuery.CountAsync(cancellationToken);
        var pageUsers = await usersQuery
            .OrderBy(u => u.Email)
            .ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.UserName,
                u.DisplayName,
                u.IsActive,
                u.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var rolesByUser = await LoadApplicationRolesAsync(
            pageUsers.Select(u => u.Id).ToArray(),
            cancellationToken);

        var items = pageUsers.Select(u => new PortalUserListItem(
            u.Id,
            u.Email,
            u.UserName,
            u.DisplayName,
            u.IsActive,
            rolesByUser.GetValueOrDefault(u.Id, []),
            u.CreatedAtUtc)).ToArray();

        return new PortalUserListResult(items, page, pageSize, totalCount);
    }

    public async Task<PortalUserListItem?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var org = await RequireCanonicalAsync(cancellationToken);
        var user = await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.OrganizationId == org.OrganizationId)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.UserName,
                u.DisplayName,
                u.IsActive,
                u.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        var roles = await LoadApplicationRolesAsync([user.Id], cancellationToken);
        return new PortalUserListItem(
            user.Id,
            user.Email,
            user.UserName,
            user.DisplayName,
            user.IsActive,
            roles.GetValueOrDefault(user.Id, []),
            user.CreatedAtUtc);
    }

    public async Task<PortalUserCreateResult> CreateAsync(
        string email,
        string? displayName,
        string role,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        email = email.Trim();
        role = role.Trim();

        if (!ApplicationRoles.All.Contains(role, StringComparer.Ordinal))
            throw new PortalUserAdministrationException(
                PortalUserAdministrationError.InvalidRole,
                "A role indicada não é válida.");

        _ = await RequireCanonicalAsync(cancellationToken);

        if (!await roleManager.RoleExistsAsync(role))
            throw new PortalUserAdministrationException(
                PortalUserAdministrationError.InvalidRole,
                "A role indicada não está disponível. Execute ensure-roles.");

        PortalUserCreateResult? created = null;
        await invariantGuard.ExecuteAsync(
            IdentityAdminInvariantGuard.AdminUsersResource,
            "Não foi possível obter o bloqueio de gestão administrativa de utilizadores.",
            async ct =>
            {
                var existing = await userManager.FindByEmailAsync(email);
                if (existing is not null)
                    throw new PortalUserAdministrationException(
                        PortalUserAdministrationError.DuplicateEmail,
                        "Já existe um utilizador com este email.");

                var user = await userFactory.CreateAsync(
                    userName: email,
                    email: email,
                    displayName: displayName,
                    ct);

                user.IsActive = true;

                var create = await userManager.CreateAsync(user, password);
                if (!create.Succeeded)
                {
                    // Never log the password or echo Identity descriptions to clients.
                    logger.LogWarning(
                        "Falha ao criar utilizador administrativo (erros Identity).");
                    throw new PortalUserAdministrationException(
                        PortalUserAdministrationError.CreateFailed,
                        "Não foi possível criar o utilizador.");
                }

                logger.LogInformation("Utilizador administrativo criado (UserId={UserId}).", user.Id);

                var assign = await roleAssigner.AssignAsync(user, role, ct);
                if (!assign.Succeeded)
                {
                    logger.LogWarning(
                        "Falha ao atribuir role ao UserId={UserId}. Rollback da transação.",
                        user.Id);

                    // SQL ambient TX rolls back via IdentityAdminInvariantGuard.
                    // Non-relational hosts have no TX — compensating delete for tests only.
                    if (!dbContext.Database.IsRelational()
                        || dbContext.Database.CurrentTransaction is null)
                    {
                        await userManager.DeleteAsync(user);
                    }

                    throw new PortalUserAdministrationException(
                        PortalUserAdministrationError.RoleAssignFailed,
                        "Não foi possível atribuir a role ao utilizador.");
                }

                var roles = await LoadApplicationRolesAsync([user.Id], ct);
                created = new PortalUserCreateResult(new PortalUserListItem(
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.DisplayName,
                    user.IsActive,
                    roles.GetValueOrDefault(user.Id, []),
                    user.CreatedAtUtc));
            },
            cancellationToken);

        return created!;
    }

    /// <summary>
    /// Sets exactly one application role. Returns null when the user is missing or outside
    /// the canonical organization. Idempotent when the role is already exclusive.
    /// </summary>
    public async Task<PortalUserListItem?> SetRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        role = role.Trim();

        if (!ApplicationRoles.All.Contains(role, StringComparer.Ordinal))
            throw new PortalUserAdministrationException(
                PortalUserAdministrationError.InvalidRole,
                "A role indicada não é válida.");

        if (!await roleManager.RoleExistsAsync(role))
            throw new PortalUserAdministrationException(
                PortalUserAdministrationError.InvalidRole,
                "A role indicada não está disponível. Execute ensure-roles.");

        var org = await RequireCanonicalAsync(cancellationToken);

        var exists = await dbContext.Users.AsNoTracking()
            .AnyAsync(u => u.Id == userId && u.OrganizationId == org.OrganizationId, cancellationToken);
        if (!exists)
            return null;

        var currentRoles = (await LoadApplicationRolesAsync([userId], cancellationToken))
            .GetValueOrDefault(userId, []);
        if (currentRoles.Length == 1
            && string.Equals(currentRoles[0], role, StringComparison.Ordinal))
        {
            return await GetAsync(userId, cancellationToken);
        }

        PortalUserListItem? result = null;
        await invariantGuard.ExecuteAsync(
            IdentityAdminInvariantGuard.AdminUsersResource,
            "Não foi possível obter o bloqueio de gestão administrativa de utilizadores.",
            async ct =>
            {
                var user = await userManager.FindByIdAsync(userId.ToString("D"));
                if (user is null || user.OrganizationId != org.OrganizationId)
                {
                    result = null;
                    return;
                }

                var rolesNow = (await LoadApplicationRolesAsync([user.Id], ct))
                    .GetValueOrDefault(user.Id, []);
                if (rolesNow.Length == 1
                    && string.Equals(rolesNow[0], role, StringComparison.Ordinal))
                {
                    result = ToListItem(user, rolesNow);
                    return;
                }

                var isCurrentlyAdmin = rolesNow.Contains(
                    ApplicationRoles.Administrator, StringComparer.Ordinal);
                var willBeAdmin = string.Equals(
                    role, ApplicationRoles.Administrator, StringComparison.Ordinal);

                if (user.IsActive && isCurrentlyAdmin && !willBeAdmin)
                {
                    var activeAdmins = await activeAdministrators.CountAsync(org.OrganizationId, ct);
                    if (activeAdmins <= 1)
                    {
                        throw new PortalUserAdministrationException(
                            PortalUserAdministrationError.LastActiveAdministrator,
                            "Não é possível remover o último administrador ativo.");
                    }
                }

                foreach (var existingRole in rolesNow)
                {
                    if (string.Equals(existingRole, role, StringComparison.Ordinal))
                        continue;

                    var remove = await userManager.RemoveFromRoleAsync(user, existingRole);
                    if (!remove.Succeeded)
                    {
                        throw new PortalUserAdministrationException(
                            PortalUserAdministrationError.RoleAssignFailed,
                            FormatIdentityErrors(remove.Errors));
                    }
                }

                if (!rolesNow.Contains(role, StringComparer.Ordinal))
                {
                    var add = await userManager.AddToRoleAsync(user, role);
                    if (!add.Succeeded)
                    {
                        throw new PortalUserAdministrationException(
                            PortalUserAdministrationError.RoleAssignFailed,
                            FormatIdentityErrors(add.Errors));
                    }
                }

                var stamp = await securityStampRotator.RotateAsync(user, ct);
                if (!stamp.Succeeded)
                {
                    throw new PortalUserAdministrationException(
                        PortalUserAdministrationError.RoleAssignFailed,
                        FormatIdentityErrors(stamp.Errors));
                }

                logger.LogInformation(
                    "Role administrativa actualizada (UserId={UserId}, Role={Role}).",
                    user.Id,
                    role);

                var finalRoles = (await LoadApplicationRolesAsync([user.Id], ct))
                    .GetValueOrDefault(user.Id, []);
                result = ToListItem(user, finalRoles);
            },
            cancellationToken);

        return result;
    }

    /// <summary>
    /// Activates or deactivates a canonical-organization user. Returns null when missing/foreign.
    /// Idempotent when already in the desired state. Deactivating an active Administrator is
    /// protected by <see cref="IdentityAdminInvariantGuard"/> so at least one active admin remains.
    /// </summary>
    public async Task<PortalUserListItem?> SetActiveStateAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var org = await RequireCanonicalAsync(cancellationToken);

        var exists = await dbContext.Users.AsNoTracking()
            .AnyAsync(u => u.Id == userId && u.OrganizationId == org.OrganizationId, cancellationToken);
        if (!exists)
            return null;

        var snapshot = await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive })
            .FirstOrDefaultAsync(cancellationToken);
        if (snapshot is not null && snapshot.IsActive == isActive)
            return await GetAsync(userId, cancellationToken);

        PortalUserListItem? result = null;
        await invariantGuard.ExecuteAsync(
            IdentityAdminInvariantGuard.AdminUsersResource,
            "Não foi possível obter o bloqueio de gestão administrativa de utilizadores.",
            async ct =>
            {
                var user = await userManager.FindByIdAsync(userId.ToString("D"));
                if (user is null || user.OrganizationId != org.OrganizationId)
                {
                    result = null;
                    return;
                }

                if (user.IsActive == isActive)
                {
                    var roles = (await LoadApplicationRolesAsync([user.Id], ct))
                        .GetValueOrDefault(user.Id, []);
                    result = ToListItem(user, roles);
                    return;
                }

                if (!isActive)
                {
                    var rolesNow = (await LoadApplicationRolesAsync([user.Id], ct))
                        .GetValueOrDefault(user.Id, []);
                    var isAdmin = rolesNow.Contains(
                        ApplicationRoles.Administrator, StringComparer.Ordinal);
                    if (user.IsActive && isAdmin)
                    {
                        var activeAdmins = await activeAdministrators.CountAsync(org.OrganizationId, ct);
                        if (activeAdmins <= 1)
                        {
                            throw new PortalUserAdministrationException(
                                PortalUserAdministrationError.LastActiveAdministrator,
                                "Não é possível remover o último administrador ativo.");
                        }
                    }
                }

                try
                {
                    // Joins the guard's ambient SQL transaction (no nested BeginTransaction).
                    await activationService.SetActiveAsync(user, isActive, ct);
                }
                catch (InvalidOperationException ex)
                {
                    throw new PortalUserAdministrationException(
                        PortalUserAdministrationError.ActivationFailed,
                        ex.Message);
                }

                logger.LogInformation(
                    "Estado IsActive actualizado (UserId={UserId}, IsActive={IsActive}).",
                    user.Id,
                    isActive);

                var finalRoles = (await LoadApplicationRolesAsync([user.Id], ct))
                    .GetValueOrDefault(user.Id, []);
                result = ToListItem(user, finalRoles);
            },
            cancellationToken);

        return result;
    }

    private static PortalUserListItem ToListItem(ApplicationUser user, IReadOnlyList<string> roles) =>
        new(
            user.Id,
            user.Email,
            user.UserName,
            user.DisplayName,
            user.IsActive,
            roles,
            user.CreatedAtUtc);

    private async Task<CanonicalOrganization> RequireCanonicalAsync(CancellationToken cancellationToken)
    {
        return await organizations.TryGetCanonicalAsync(cancellationToken)
            ?? throw new PortalUserAdministrationException(
                PortalUserAdministrationError.CanonicalOrganizationMissing,
                "A organização canónica ainda não existe.");
    }

    private static void ValidateOptionalRoleFilter(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return;
        if (!ApplicationRoles.All.Contains(role.Trim(), StringComparer.Ordinal))
            throw new PortalUserAdministrationException(
                PortalUserAdministrationError.InvalidRole,
                "A role indicada não é válida.");
    }

    private async Task<Dictionary<Guid, string[]>> LoadApplicationRolesAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, string[]>();

        var appRoleNames = ApplicationRoles.All.ToArray();
        var rows = await (
            from ur in dbContext.Set<IdentityUserRole<Guid>>().AsNoTracking()
            join r in dbContext.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId) && r.Name != null && appRoleNames.Contains(r.Name)
            select new { ur.UserId, RoleName = r.Name! }
        ).ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.RoleName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .ToArray());
    }

    private static string FormatIdentityErrors(IEnumerable<IdentityError> errors) =>
        string.Join("; ", errors.Select(e => e.Description));
}

public sealed record PortalUserListItem(
    Guid UserId,
    string? Email,
    string? UserName,
    string? DisplayName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAtUtc);

public sealed record PortalUserListResult(
    IReadOnlyList<PortalUserListItem> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record PortalUserCreateResult(PortalUserListItem User);

public enum PortalUserAdministrationError
{
    InvalidRole,
    DuplicateEmail,
    CreateFailed,
    RoleAssignFailed,
    LastActiveAdministrator,
    ActivationFailed,
    CanonicalOrganizationMissing
}

public sealed class PortalUserAdministrationException : Exception
{
    public PortalUserAdministrationException(PortalUserAdministrationError error, string message)
        : base(message)
    {
        Error = error;
    }

    public PortalUserAdministrationError Error { get; }
}
