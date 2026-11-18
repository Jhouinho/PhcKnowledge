using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Creates the first Administrator under <see cref="IdentityAdminInvariantGuard"/>
/// (<c>PhcKnowledge.Identity.AdminUsers</c>). Refuses when an active canonical
/// Administrator already exists; never mutates existing admins.
/// </summary>
public interface IAdministratorRoleAssigner
{
    Task<IdentityResult> AssignAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}

public sealed class IdentityAdministratorRoleAssigner(UserManager<ApplicationUser> userManager)
    : IAdministratorRoleAssigner
{
    public Task<IdentityResult> AssignAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default) =>
        userManager.AddToRoleAsync(user, ApplicationRoles.Administrator);
}

public sealed class FirstAdministratorBootstrapService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ApplicationUserFactory userFactory,
    ApplicationDbContext dbContext,
    ICanonicalOrganizationService organizations,
    CanonicalActiveAdministratorCounter activeAdministrators,
    IdentityAdminInvariantGuard invariantGuard,
    IAdministratorRoleAssigner roleAssigner,
    ILogger<FirstAdministratorBootstrapService> logger)
{
    public async Task<BootstrapAdminResult> BootstrapAsync(
        string email,
        string password,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        email = email.Trim();

        BootstrapAdminResult? result = null;
        try
        {
            await invariantGuard.ExecuteAsync(
                IdentityAdminInvariantGuard.AdminUsersResource,
                "Não foi possível obter o bloqueio de gestão administrativa de utilizadores.",
                async ct =>
                {
                    if (!await roleManager.RoleExistsAsync(ApplicationRoles.Administrator))
                        throw new InvalidOperationException(
                            "A role Administrator não existe. Execute ensure-roles antes do bootstrap-admin.");

                    var org = await organizations.TryGetCanonicalAsync(ct)
                        ?? throw new InvalidOperationException(
                            "A organização canónica ainda não existe. Execute ensure-organization antes do bootstrap-admin.");

                    if (await activeAdministrators.CountAsync(org.OrganizationId, ct) > 0)
                        throw new BootstrapAdminRejectedException(
                            "Já existe um administrador ativo. O bootstrap do primeiro administrador foi recusado.");

                    var existing = await userManager.FindByEmailAsync(email);
                    if (existing is not null)
                        throw new BootstrapAdminRejectedException(
                            "Já existe um utilizador com este email. O bootstrap foi recusado.");

                    var user = await userFactory.CreateAsync(
                        userName: email,
                        email: email,
                        displayName: displayName,
                        ct);

                    if (!user.IsActive)
                        user.IsActive = true;

                    var create = await userManager.CreateAsync(user, password);
                    if (!create.Succeeded)
                    {
                        throw new BootstrapAdminRejectedException(
                            "Não foi possível criar o administrador.");
                    }

                    logger.LogInformation(
                        "Utilizador bootstrap criado (UserId={UserId}). A atribuir role Administrator.",
                        user.Id);

                    var addRole = await roleAssigner.AssignAsync(user, ct);
                    if (!addRole.Succeeded)
                    {
                        logger.LogWarning(
                            "Falha ao atribuir role Administrator ao UserId={UserId}. Rollback da transação.",
                            user.Id);

                        // SQL: IdentityAdminInvariantGuard rolls back the ambient TX (no DeleteAsync).
                        // Non-relational providers have no TX — compensating delete for test hosts only.
                        if (!dbContext.Database.IsRelational()
                            || dbContext.Database.CurrentTransaction is null)
                        {
                            await userManager.DeleteAsync(user);
                        }

                        throw new BootstrapAdminRejectedException(
                            "Não foi possível atribuir a role Administrator.");
                    }

                    result = new BootstrapAdminResult(user.Id, user.Email!);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not BootstrapAdminRejectedException
            and not OperationCanceledException
            and not InvalidOperationException)
        {
            throw SanitizeBootstrapException(ex);
        }

        logger.LogInformation(
            "Bootstrap de administrador concluído (UserId={UserId}).",
            result!.UserId);

        return result;
    }

    /// <summary>
    /// Replaces exception messages that might echo secrets with a safe generic message.
    /// </summary>
    public static Exception SanitizeBootstrapException(Exception ex)
    {
        if (ex is BootstrapAdminRejectedException or InvalidOperationException)
            return ex;

        return new InvalidOperationException(
            "Falha no bootstrap de administrador. Consulte os logs do servidor (sem credenciais).",
            ex);
    }
}

public sealed record BootstrapAdminResult(Guid UserId, string Email);

public sealed class BootstrapAdminRejectedException : InvalidOperationException
{
    public BootstrapAdminRejectedException(string message)
        : base(message)
    {
    }
}
