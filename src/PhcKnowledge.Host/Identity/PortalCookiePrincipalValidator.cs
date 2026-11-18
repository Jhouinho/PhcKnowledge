using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Validates portal cookie principals against current persistence on every authentication.
/// No ValidationInterval tolerance — fail-closed on missing user, inactivity, stamp mismatch, or DB errors.
/// </summary>
public interface IPortalCookiePrincipalValidator
{
    /// <summary>
    /// Returns whether the principal still represents an active user with a matching security stamp.
    /// Does not mutate cookies; used by cookie events and direct tests.
    /// </summary>
    Task<bool> IsSessionValidAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task ValidatePrincipalAsync(CookieValidatePrincipalContext context);
}

/// <summary>Projected persistence lookup for portal cookie revalidation.</summary>
public interface IPortalUserSessionLookup
{
    Task<PortalCookieSessionSnapshot?> FindAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>Minimal persistence projection for portal cookie revalidation.</summary>
public sealed record PortalCookieSessionSnapshot(bool IsActive, string? SecurityStamp);

public sealed class EfPortalUserSessionLookup(ApplicationDbContext dbContext) : IPortalUserSessionLookup
{
    public async Task<PortalCookieSessionSnapshot?> FindAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new PortalCookieSessionSnapshot(u.IsActive, u.SecurityStamp))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class PortalCookiePrincipalValidator(
    IPortalUserSessionLookup sessionLookup,
    IOptions<IdentityOptions> identityOptions,
    ILogger<PortalCookiePrincipalValidator> logger,
    PortalCookieValidationDiagnostics diagnostics) : IPortalCookiePrincipalValidator
{
    public async Task<bool> IsSessionValidAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        diagnostics.RecordValidationAttempt();

        if (principal.Identity?.IsAuthenticated != true)
            return false;

        var claimsIdentity = identityOptions.Value.ClaimsIdentity;
        var userIdValue = principal.FindFirstValue(claimsIdentity.UserIdClaimType);
        if (string.IsNullOrEmpty(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
            return false;

        var stampClaim = principal.FindFirstValue(claimsIdentity.SecurityStampClaimType);
        if (string.IsNullOrEmpty(stampClaim))
            return false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = await sessionLookup.FindAsync(userId, cancellationToken);
            if (snapshot is null)
                return false;

            if (!snapshot.IsActive)
                return false;

            return string.Equals(snapshot.SecurityStamp, stampClaim, StringComparison.Ordinal);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Portal cookie session validation failed closed.");
            return false;
        }
    }

    public async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var principal = context.Principal;
        if (principal is null)
        {
            await RejectAsync(context);
            return;
        }

        var valid = await IsSessionValidAsync(principal, context.HttpContext.RequestAborted);
        if (!valid)
            await RejectAsync(context);
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}

/// <summary>
/// Diagnostics for tests (anonymous requests must not increment).
/// </summary>
public sealed class PortalCookieValidationDiagnostics
{
    private long _validationAttempts;

    public long ValidationAttempts => Interlocked.Read(ref _validationAttempts);

    public void RecordValidationAttempt() => Interlocked.Increment(ref _validationAttempts);

    public void Reset() => Interlocked.Exchange(ref _validationAttempts, 0);
}
