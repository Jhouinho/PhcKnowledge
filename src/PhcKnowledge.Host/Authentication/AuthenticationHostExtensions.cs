using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using PhcKnowledge.Host.Http;
using PhcKnowledge.Host.Identity;

namespace PhcKnowledge.Host.Authentication;

public static class AuthenticationHostExtensions
{
    /// <summary>
    /// Registers portal cookie always, and JWT Bearer only when enabled.
    /// No default authenticate scheme — policies must name their scheme explicitly.
    /// </summary>
    public static IServiceCollection AddPhcKnowledgeAuthentication(
        this IServiceCollection services,
        AuthenticationHostSettings settings,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(environment);

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var cookieSecurePolicy =
            environment.IsDevelopment() || environment.IsEnvironment("Testing")
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

        services.AddAntiforgery(options =>
        {
            options.HeaderName = PortalApiEndpoints.AntiforgeryHeaderName;
            options.Cookie.Name = "PhcKnowledge.AntiForgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
        });

        var authBuilder = services.AddAuthentication();

        authBuilder.AddCookie(PhcAuthenticationSchemes.PortalCookie, options =>
        {
            options.Cookie.Name = "PhcKnowledge.Portal";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.SlidingExpiration = true;

            // Revalidate IsActive + SecurityStamp on every cookie authentication (no interval).
            options.Events.OnValidatePrincipal = static async context =>
            {
                var validator = context.HttpContext.RequestServices
                    .GetRequiredService<IPortalCookiePrincipalValidator>();
                await validator.ValidatePrincipalAsync(context);
            };

            // Portal APIs return status codes instead of redirecting to a login page.
            options.Events.OnRedirectToLogin = static context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = static context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        if (settings.JwtEnabled)
        {
            // WebApplicationFactory tests use Environment=Testing and register a test
            // handler under the same JwtBearer scheme name (no duplicate registration).
            if (!environment.IsEnvironment("Testing"))
            {
                authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = settings.Authority;
                    options.Audience = settings.Audience;
                    options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = settings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = settings.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        NameClaimType = "sub",
                        RoleClaimType = "role"
                    };
                });
            }
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                PortalAuthorizationPolicies.Authenticated,
                policy => policy
                    .AddAuthenticationSchemes(PhcAuthenticationSchemes.PortalCookie)
                    .RequireAuthenticatedUser());

            options.AddPolicy(
                PortalAuthorizationPolicies.Collaborator,
                policy => policy
                    .AddAuthenticationSchemes(PhcAuthenticationSchemes.PortalCookie)
                    .RequireAuthenticatedUser()
                    .RequireRole(ApplicationRoles.Collaborator, ApplicationRoles.Administrator));

            options.AddPolicy(
                PortalAuthorizationPolicies.Administrator,
                policy => policy
                    .AddAuthenticationSchemes(PhcAuthenticationSchemes.PortalCookie)
                    .RequireAuthenticatedUser()
                    .RequireRole(ApplicationRoles.Administrator));

            if (settings.JwtEnabled)
            {
                options.AddPolicy(
                    IkpAuthorizationPolicies.Upload,
                    policy => policy
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .AddRequirements(new ScopeAuthorizationRequirement(AuthenticationHostSettings.UploadScope)));

                options.AddPolicy(
                    IkpAuthorizationPolicies.Read,
                    policy => policy
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .AddRequirements(new ScopeAuthorizationRequirement(AuthenticationHostSettings.ReadScope)));
            }
        });

        return services;
    }
}
