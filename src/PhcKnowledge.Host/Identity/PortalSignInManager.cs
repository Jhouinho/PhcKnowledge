using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhcKnowledge.Host.Authentication;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// SignInManager that issues and clears cookies on <see cref="PhcAuthenticationSchemes.PortalCookie"/>
/// instead of the default Identity application scheme.
/// </summary>
public sealed class PortalSignInManager(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(
        userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override async Task SignInWithClaimsAsync(
        ApplicationUser user,
        AuthenticationProperties? authenticationProperties,
        IEnumerable<Claim> additionalClaims)
    {
        ArgumentNullException.ThrowIfNull(user);

        var userPrincipal = await CreateUserPrincipalAsync(user);
        foreach (var claim in additionalClaims)
            userPrincipal.Identities.First().AddClaim(claim);

        await Context.SignInAsync(
            PhcAuthenticationSchemes.PortalCookie,
            userPrincipal,
            authenticationProperties ?? new AuthenticationProperties());
    }

    public override Task SignOutAsync() =>
        Context.SignOutAsync(PhcAuthenticationSchemes.PortalCookie);
}
