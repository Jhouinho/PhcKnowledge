using Microsoft.AspNetCore.Identity;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Internal collaborator account. OrganizationId must reference the canonical
/// Organizations row; it is never supplied by clients and is immutable after create.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid OrganizationId { get; private set; }

    /// <summary>Administrative enablement flag (distinct from Identity lockout).</summary>
    public bool IsActive { get; set; } = true;

    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public OrganizationRow? Organization { get; set; }

    /// <summary>
    /// Binds the canonical organization at creation time only.
    /// Subsequent calls with a different id are rejected.
    /// </summary>
    internal void BindOrganizationOnCreate(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId é obrigatório.", nameof(organizationId));

        if (OrganizationId != Guid.Empty && OrganizationId != organizationId)
            throw new InvalidOperationException("OrganizationId é imutável após a criação.");

        OrganizationId = organizationId;
    }
}

/// <summary>
/// EF mapping of dbo.Organizations (created by SQL script 003). Excluded from EF migrations.
/// </summary>
public sealed class OrganizationRow
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public bool IsCanonical { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}

public static class ApplicationRoles
{
    public const string Administrator = "Administrator";
    public const string Collaborator = "Collaborator";

    public static IReadOnlyList<string> All { get; } = [Administrator, Collaborator];
}
