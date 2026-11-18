using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Rejects users whose OrganizationId is missing, not canonical, or changed after create.
/// </summary>
public sealed class CanonicalOrganizationUserValidator(
    ICanonicalOrganizationService organizations,
    ApplicationDbContext dbContext) : IUserValidator<ApplicationUser>
{
    public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.OrganizationId == Guid.Empty)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "OrganizationIdRequired",
                Description = "OrganizationId é obrigatório."
            });
        }

        var persistedOrganizationId = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == user.Id)
            .Select(u => (Guid?)u.OrganizationId)
            .FirstOrDefaultAsync();

        if (persistedOrganizationId is Guid existing
            && existing != user.OrganizationId)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "OrganizationIdImmutable",
                Description = "OrganizationId não pode ser alterado após a criação do utilizador."
            });
        }

        var canonical = await organizations.TryGetCanonicalAsync();
        if (canonical is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CanonicalOrganizationMissing",
                Description = "A organização canónica não está inicializada."
            });
        }

        if (user.OrganizationId != canonical.OrganizationId)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "OrganizationIdNotCanonical",
                Description = "OrganizationId tem de coincidir com a organização canónica."
            });
        }

        return IdentityResult.Success;
    }
}
