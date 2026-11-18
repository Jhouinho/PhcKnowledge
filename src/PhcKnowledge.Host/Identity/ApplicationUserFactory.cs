using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Creates <see cref="ApplicationUser"/> instances bound exclusively to the
/// canonical organization resolved on the server.
/// </summary>
public sealed class ApplicationUserFactory(ICanonicalOrganizationService organizations)
{
    public async Task<ApplicationUser> CreateAsync(
        string userName,
        string email,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var organization = await organizations.TryGetCanonicalAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "A organização canónica ainda não existe. Executar a inicialização antes de criar utilizadores.");

        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName.Trim(),
            NormalizedUserName = userName.Trim().ToUpperInvariant(),
            Email = email.Trim(),
            NormalizedEmail = email.Trim().ToUpperInvariant(),
            EmailConfirmed = false,
            IsActive = true,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            CreatedAtUtc = now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        user.BindOrganizationOnCreate(organization.OrganizationId);
        return user;
    }
}
