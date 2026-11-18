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

        var values = principal.FindAll(_claimType)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (values.Length == 0)
            throw new OrganizationIdResolutionException(
                $"O claim '{_claimType}' está ausente.",
                OrganizationIdResolutionFailure.ClaimMissing);

        if (values.Length > 1)
            throw new OrganizationIdResolutionException(
                $"O claim '{_claimType}' tem múltiplos valores.",
                OrganizationIdResolutionFailure.ClaimInvalid);

        if (!Guid.TryParse(values[0], out var organizationId) || organizationId == Guid.Empty)
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
