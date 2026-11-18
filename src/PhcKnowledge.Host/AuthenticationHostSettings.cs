using Microsoft.Extensions.Configuration;

namespace PhcKnowledge.Host;

/// <summary>
/// JWT Bearer / OIDC settings. Authority (metadata) and Audience are required.
/// Issuer may differ from Authority when the IdP advertises a distinct iss claim;
/// when omitted, Issuer defaults to Authority.
/// </summary>
public sealed class AuthenticationHostSettings
{
    public const string DefaultOrganizationClaimType = "organization_id";
    public const string UploadScope = "ikp.upload";
    public const string ReadScope = "ikp.read";

    public required string Authority { get; init; }
    public required string Audience { get; init; }
    /// <summary>Expected JWT iss. Defaults to Authority when not configured.</summary>
    public required string Issuer { get; init; }
    public string OrganizationClaimType { get; init; } = DefaultOrganizationClaimType;
    public bool RequireHttpsMetadata { get; init; } = true;

    public static AuthenticationHostSettings Load(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var authorityRaw = configuration["Authentication:Authority"];
        if (string.IsNullOrWhiteSpace(authorityRaw))
            throw new InvalidOperationException("A configuração Authentication:Authority é obrigatória.");

        var audience = configuration["Authentication:Audience"];
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("A configuração Authentication:Audience é obrigatória.");

        // Authority is used for OIDC metadata discovery; trailing slashes are normalized.
        var authority = authorityRaw.Trim().TrimEnd('/');

        // Issuer is compared to the JWT iss claim and must keep its exact configured value
        // (including a trailing slash). When omitted, default to the normalized Authority.
        var issuerRaw = configuration["Authentication:Issuer"];
        var issuer = string.IsNullOrWhiteSpace(issuerRaw)
            ? authority
            : issuerRaw.Trim();

        var claimType = configuration["Authentication:OrganizationClaimType"];
        if (string.IsNullOrWhiteSpace(claimType))
            claimType = DefaultOrganizationClaimType;

        var requireHttps = true;
        var requireHttpsRaw = configuration["Authentication:RequireHttpsMetadata"];
        if (!string.IsNullOrWhiteSpace(requireHttpsRaw) &&
            !bool.TryParse(requireHttpsRaw, out requireHttps))
            throw new InvalidOperationException(
                "A configuração Authentication:RequireHttpsMetadata tem de ser um valor booleano.");

        return new AuthenticationHostSettings
        {
            Authority = authority,
            Audience = audience.Trim(),
            Issuer = issuer,
            OrganizationClaimType = claimType.Trim(),
            RequireHttpsMetadata = requireHttps
        };
    }
}
