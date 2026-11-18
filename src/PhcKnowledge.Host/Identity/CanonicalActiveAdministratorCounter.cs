using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Shared count of active Administrators in the canonical organization
/// (used by admin mutations, activation guardrails, and bootstrap).
/// </summary>
public sealed class CanonicalActiveAdministratorCounter(
    ApplicationDbContext dbContext,
    RoleManager<IdentityRole<Guid>> roleManager)
{
    public async Task<int> CountAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var adminRole = await roleManager.FindByNameAsync(ApplicationRoles.Administrator);
        if (adminRole is null)
            return 0;

        return await (
            from u in dbContext.Users.AsNoTracking()
            join ur in dbContext.Set<IdentityUserRole<Guid>>().AsNoTracking()
                on u.Id equals ur.UserId
            where u.OrganizationId == organizationId
                  && u.IsActive
                  && ur.RoleId == adminRole.Id
            select u.Id
        ).Distinct().CountAsync(cancellationToken);
    }

    public async Task<bool> IsInAdministratorRoleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var adminRole = await roleManager.FindByNameAsync(ApplicationRoles.Administrator);
        if (adminRole is null)
            return false;

        return await dbContext.Set<IdentityUserRole<Guid>>()
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRole.Id, cancellationToken);
    }
}
