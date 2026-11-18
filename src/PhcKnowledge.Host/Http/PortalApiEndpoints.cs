using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Identity;

namespace PhcKnowledge.Host.Http;

/// <summary>
/// Portal authentication and session endpoints (cookie scheme only).
/// </summary>
public static class PortalApiEndpoints
{
    public const string SessionPath = "/api/portal/session";
    public const string LoginPath = "/api/portal/auth/login";
    public const string LogoutPath = "/api/portal/auth/logout";
    public const string AntiforgeryPath = "/api/portal/auth/antiforgery";

    /// <summary>Only mapped when Environment is Testing (scheme-separation harness).</summary>
    public const string TestSignInPath = "/__test/portal-signin";
    public const string TestAuthCheckPath = "/__test/portal-auth-check";
    public const string TestCollaboratorPath = "/__test/portal-collaborator";
    public const string TestAdministratorPath = "/__test/portal-administrator";

    public const string AntiforgeryHeaderName = "X-CSRF-TOKEN";

    public static IEndpointRouteBuilder MapPortalApi(
        this IEndpointRouteBuilder endpoints,
        IWebHostEnvironment environment)
    {
        endpoints.MapGet(AntiforgeryPath, GetAntiforgeryToken);

        endpoints.MapGet(SessionPath, GetSessionAsync);

        endpoints.MapPost(LoginPath, LoginAsync)
            .WithMetadata(PortalAntiforgeryMetadata.Instance);
        endpoints.MapPost(LogoutPath, LogoutAsync)
            .RequireAuthorization(PortalAuthorizationPolicies.Authenticated)
            .WithMetadata(PortalAntiforgeryMetadata.Instance);

        endpoints.MapPortalAdminUserApi();

        if (environment.IsEnvironment("Testing"))
        {
            endpoints.MapPost(TestSignInPath, async (HttpContext httpContext) =>
            {
                var identity = new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
                        new Claim(ClaimTypes.Name, "test-portal-user")
                    ],
                    PhcAuthenticationSchemes.PortalCookie);
                await httpContext.SignInAsync(
                    PhcAuthenticationSchemes.PortalCookie,
                    new ClaimsPrincipal(identity));
                return Results.Ok();
            }).DisableAntiforgery();

            // Scheme-only probe (no UserManager/DB) for cookie vs JWT separation tests.
            endpoints.MapGet(TestAuthCheckPath, async (HttpContext httpContext) =>
            {
                var auth = await httpContext.AuthenticateAsync(PhcAuthenticationSchemes.PortalCookie);
                return Results.Json(new { authenticated = auth.Succeeded });
            });

            // Role-policy probes (Identity-backed cookies via normal login).
            endpoints.MapGet(TestCollaboratorPath, () => Results.Ok(new { policy = PortalAuthorizationPolicies.Collaborator }))
                .RequireAuthorization(PortalAuthorizationPolicies.Collaborator);
            endpoints.MapGet(TestAdministratorPath, () => Results.Ok(new { policy = PortalAuthorizationPolicies.Administrator }))
                .RequireAuthorization(PortalAuthorizationPolicies.Administrator);
        }

        return endpoints;
    }

    private static async Task<IResult> GetAntiforgeryToken(
        HttpContext httpContext,
        IAntiforgery antiforgery)
    {
        // No default authenticate scheme (1.3.1): antiforgery tokens bind to HttpContext.User.
        // Authenticate PortalCookie explicitly so tokens issued after login match logout/session users.
        var auth = await httpContext.AuthenticateAsync(PhcAuthenticationSchemes.PortalCookie);
        if (auth.Succeeded && auth.Principal is not null)
            httpContext.User = auth.Principal;

        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        return Results.Json(new { token = tokens.RequestToken });
    }

    private static async Task<IResult> GetSessionAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager)
    {
        var auth = await httpContext.AuthenticateAsync(PhcAuthenticationSchemes.PortalCookie);
        if (!auth.Succeeded || auth.Principal is null)
            return Results.Json(AnonymousSession());

        var user = await userManager.GetUserAsync(auth.Principal);
        if (user is null)
            return Results.Json(AnonymousSession());

        var roles = await userManager.GetRolesAsync(user);
        return Results.Json(new PortalSessionResponse(
            Authenticated: true,
            UserId: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            Roles: roles.OrderBy(r => r, StringComparer.Ordinal).ToArray()));
    }

    private static async Task<IResult> LoginAsync(
        HttpContext httpContext,
        [FromBody] PortalLoginRequest? request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        // UseAntiforgery validates via IAntiforgeryMetadata then continues; JSON APIs must check the feature.
        if (!HasValidAntiforgery(httpContext))
            return Results.BadRequest();

        var email = request?.Email?.Trim();
        var password = request?.Password;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return InvalidCredentials();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return InvalidCredentials();

        var check = await signInManager.CheckPasswordSignInAsync(
            user, password, lockoutOnFailure: true);
        if (!check.Succeeded)
            return InvalidCredentials();

        // Login gate; ongoing sessions are revalidated via PortalCookie OnValidatePrincipal.
        if (!user.IsActive)
            return InvalidCredentials();

        await signInManager.SignInAsync(user, isPersistent: false);
        var roles = await userManager.GetRolesAsync(user);
        return Results.Json(new PortalSessionResponse(
            Authenticated: true,
            UserId: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            Roles: roles.OrderBy(r => r, StringComparer.Ordinal).ToArray()));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager)
    {
        if (!HasValidAntiforgery(httpContext))
            return Results.BadRequest();

        await signInManager.SignOutAsync();
        return Results.Ok();
    }

    /// <summary>
    /// AntiforgeryMiddleware validates endpoints marked with <see cref="IAntiforgeryMetadata"/>
    /// but does not short-circuit JSON handlers. On failure it sets
    /// <see cref="IAntiforgeryValidationFeature"/> with <c>IsValid = false</c>; on success the feature is absent.
    /// </summary>
    private static bool HasValidAntiforgery(HttpContext httpContext)
    {
        var feature = httpContext.Features.Get<IAntiforgeryValidationFeature>();
        return feature is not { IsValid: false };
    }

    private static PortalSessionResponse AnonymousSession() =>
        new(false, null, null, null, Array.Empty<string>());

    private static IResult InvalidCredentials() =>
        Results.Json(
            new { error = PortalAuthMessages.InvalidCredentials },
            statusCode: StatusCodes.Status401Unauthorized);
}
