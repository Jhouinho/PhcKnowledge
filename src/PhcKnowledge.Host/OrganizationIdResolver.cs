using System.Security.Claims;

namespace PhcKnowledge.Host;

/// <summary>
/// Resolves OrganizationId exclusively from the authenticated principal.
/// Never accepts organization identifiers from request body, query or custom headers.
/// </summary>
public sealed class OrganizationIdResolver(AuthenticationHostSettings settings)
{
    private readonly string _claimType = string.IsNullOrWhiteSpace(settings.OrganizationClaimType)
        ? AuthenticationHostSettings.DefaultOrganizationClaimType
        : settings.OrganizationClaimType;

    public Guid ResolveRequired(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity is not { IsAuthenticated: true })
            throw new OrganizationIdResolutionException(
                "O principal não está autenticado.",
                OrganizationIdResolutionFailure.Unauthenticated);

        // Cardinality is on claim instances, not distinct values: duplicates (even equal) are rejected.
        var claims = principal.FindAll(_claimType).ToArray();
        if (claims.Length == 0)
            throw new OrganizationIdResolutionException(
                $"O claim '{_claimType}' está ausente.",
                OrganizationIdResolutionFailure.ClaimMissing);

        if (claims.Length > 1)
            throw new OrganizationIdResolutionException(
                $"O claim '{_claimType}' tem múltiplos valores.",
                OrganizationIdResolutionFailure.ClaimInvalid);

        var value = claims[0].Value;
        if (string.IsNullOrWhiteSpace(value))
            throw new OrganizationIdResolutionException(
                $"O claim '{_claimType}' está vazio.",
                OrganizationIdResolutionFailure.ClaimInvalid);

        if (!Guid.TryParse(value, out var organizationId) || organizationId == Guid.Empty)
            throw new OrganizationIdResolutionException(
                $"O claim '{_claimType}' não contém um GUID de organização válido.",
                OrganizationIdResolutionFailure.ClaimInvalid);

        return organizationId;
    }
}

public enum OrganizationIdResolutionFailure
{
    Unauthenticated,
    ClaimMissing,
    ClaimInvalid
}

public sealed class OrganizationIdResolutionException : Exception
{
    public OrganizationIdResolutionException(
        string message,
        OrganizationIdResolutionFailure failure)
        : base(message)
    {
        Failure = failure;
    }

    public OrganizationIdResolutionFailure Failure { get; }
}
