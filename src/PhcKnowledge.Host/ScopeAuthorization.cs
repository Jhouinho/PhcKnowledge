using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PhcKnowledge.Host;

public static class IkpAuthorizationPolicies
{
    public const string Upload = "IkpUpload";
    public const string Read = "IkpRead";
}

/// <summary>
/// Requires an OAuth2 space-delimited scope claim without relying on inbound claim mapping.
/// </summary>
public sealed class ScopeAuthorizationRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}

public sealed class ScopeAuthorizationHandler
    : AuthorizationHandler<ScopeAuthorizationRequirement>
{
    public const string ScopeClaimType = "scope";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeAuthorizationRequirement requirement)
    {
        if (HasScope(context.User, requirement.Scope))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }

    public static bool HasScope(ClaimsPrincipal principal, string requiredScope)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredScope);

        foreach (var claim in principal.FindAll(ScopeClaimType))
        {
            if (string.IsNullOrWhiteSpace(claim.Value))
                continue;

            foreach (var scope in claim.Value.Split(
                         ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.Equals(scope, requiredScope, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }
}
