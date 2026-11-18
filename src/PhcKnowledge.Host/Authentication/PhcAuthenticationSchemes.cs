namespace PhcKnowledge.Host.Authentication;

/// <summary>Stable authentication scheme names for the Host.</summary>
public static class PhcAuthenticationSchemes
{
    /// <summary>Identity portal cookie (collaborator browser sessions).</summary>
    public const string PortalCookie = "PortalCookie";
}

/// <summary>Authorization policies that accept only the portal cookie scheme.</summary>
public static class PortalAuthorizationPolicies
{
    /// <summary>Any authenticated portal session (no role required).</summary>
    public const string Authenticated = "PortalAuthenticated";

    /// <summary>Collaborator or Administrator via PortalCookie.</summary>
    public const string Collaborator = "PortalCollaborator";

    /// <summary>Administrator only via PortalCookie.</summary>
    public const string Administrator = "PortalAdministrator";
}
