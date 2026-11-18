using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Toggles <see cref="ApplicationUser.IsActive"/> and refreshes the security stamp
/// in a single Identity update. Joins an ambient EF transaction when present.
/// Deactivation of Administrators requires the administrative protected flow
/// (<see cref="IdentityAdminInvariantGuard"/>) on relational providers and always
/// enforces the last-active-administrator invariant.
/// </summary>
public sealed class UserActivationService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    CanonicalActiveAdministratorCounter activeAdministrators,
    ICanonicalOrganizationService organizations)
{
    /// <summary>
    /// Sets activation state. No-ops (without stamp rotation) when already in the desired state.
    /// Returns <c>true</c> when a mutation was persisted.
    /// </summary>
    public async Task<bool> SetActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString("D"))
            ?? throw new InvalidOperationException($"Utilizador '{userId}' não encontrado.");

        return await SetActiveAsync(user, isActive, cancellationToken);
    }

    /// <summary>
    /// Sets activation state for an already-loaded tracked user.
    /// </summary>
    public async Task<bool> SetActiveAsync(
        ApplicationUser user,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.IsActive == isActive)
            return false;

        cancellationToken.ThrowIfCancellationRequested();

        if (!isActive)
            await EnsureAdministratorDeactivationAllowedAsync(user, cancellationToken);

        // InMemory has no real transactions; SQL under IdentityAdminInvariantGuard already owns one.
        var ownsTransaction = dbContext.Database.IsRelational()
            && dbContext.Database.CurrentTransaction is null;

        IDbContextTransaction? transaction = null;
        if (ownsTransaction)
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var previousActive = user.IsActive;
        var previousStamp = user.SecurityStamp;
        try
        {
            user.IsActive = isActive;
            // Single Identity save: new SecurityStamp + IsActive in one UPDATE.
            var result = await userManager.UpdateSecurityStampAsync(user);
            if (!result.Succeeded)
            {
                user.IsActive = previousActive;
                user.SecurityStamp = previousStamp;
                if (ownsTransaction)
                    await transaction!.RollbackAsync(cancellationToken);

                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"Falha ao actualizar IsActive/SecurityStamp: {errors}");
            }

            if (ownsTransaction)
                await transaction!.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            if (ownsTransaction)
            {
                try
                {
                    await transaction!.RollbackAsync(cancellationToken);
                }
                catch (Exception)
                {
                    // Transaction may already be completed.
                }
            }

            var entry = dbContext.Entry(user);
            if (entry.State != EntityState.Detached)
            {
                try
                {
                    await entry.ReloadAsync(cancellationToken);
                }
                catch (Exception)
                {
                    // Best-effort restore of tracked entity.
                }
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private async Task EnsureAdministratorDeactivationAllowedAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (!await activeAdministrators.IsInAdministratorRoleAsync(user.Id, cancellationToken))
            return;

        // Relational callers must be inside IdentityAdminInvariantGuard (ambient TX).
        // InMemory admin HTTP tests have no ambient TX; last-admin count still applies.
        if (dbContext.Database.IsRelational() && dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Desativação de Administrator requer o fluxo administrativo protegido.");
        }

        var org = await organizations.TryGetCanonicalAsync(cancellationToken)
            ?? throw new InvalidOperationException("A organização canónica ainda não existe.");

        if (user.OrganizationId != org.OrganizationId)
            return;

        var activeAdmins = await activeAdministrators.CountAsync(org.OrganizationId, cancellationToken);
        if (activeAdmins <= 1)
        {
            throw new InvalidOperationException(
                "Não é possível remover o último administrador ativo.");
        }
    }
}
