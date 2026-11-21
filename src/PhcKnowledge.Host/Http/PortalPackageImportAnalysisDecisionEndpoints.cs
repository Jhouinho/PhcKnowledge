using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Import.Ikp.Analysis;

namespace PhcKnowledge.Host.Http;

/// <summary>
/// Portal human confirmation/rejection of package import analyses (Increment C).
/// Cookie + <see cref="PortalAuthorizationPolicies.Collaborator"/>; antiforgery on POST.
/// </summary>
public static class PortalPackageImportAnalysisDecisionEndpoints
{
    private static readonly HashSet<string> AllowedBodyProperties =
        new(StringComparer.OrdinalIgnoreCase) { "decision", "rationale" };

    private static readonly HashSet<string> ForbiddenIdentityProperties =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "decidedbysubject",
            "decidedbyissuer",
            "userid",
            "organizationid",
            "subject",
            "issuer",
            "email",
            "actortype",
            "actor",
            "phc_actor_type"
        };

    public static IEndpointRouteBuilder MapPortalPackageImportAnalysisDecisionApi(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(PortalPackageImportAnalysisDecisionPaths.DecisionPath, GetEffectiveDecisionAsync)
            .RequireAuthorization(PortalAuthorizationPolicies.Collaborator);

        endpoints.MapGet(PortalPackageImportAnalysisDecisionPaths.DecisionsPath, GetDecisionHistoryAsync)
            .RequireAuthorization(PortalAuthorizationPolicies.Collaborator);

        endpoints.MapPost(PortalPackageImportAnalysisDecisionPaths.DecisionsPath, PostDecisionAsync)
            .RequireAuthorization(PortalAuthorizationPolicies.Collaborator)
            .WithMetadata(PortalAntiforgeryMetadata.Instance);

        return endpoints;
    }

    private static async Task<IResult> GetEffectiveDecisionAsync(
        Guid analysisId,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IPackageImportAnalysisRepository analyses,
        PackageImportAnalysisDecisionService decisions,
        CancellationToken cancellationToken)
    {
        var user = await ResolveActivePortalUserAsync(httpContext, userManager);
        if (user is null)
            return Results.Unauthorized();
        if (user.OrganizationId == Guid.Empty)
            return Results.Forbid();

        var analysis = await analyses.GetByAnalysisIdAsync(
            user.OrganizationId, analysisId, cancellationToken);
        if (analysis is null)
            return Results.NotFound();

        var effective = await decisions.GetEffectiveAsync(
            analysis.AnalysisId, user.OrganizationId, cancellationToken);
        return Results.Json(ToStateDto(analysis, effective));
    }

    private static async Task<IResult> GetDecisionHistoryAsync(
        Guid analysisId,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IPackageImportAnalysisRepository analyses,
        PackageImportAnalysisDecisionService decisions,
        CancellationToken cancellationToken)
    {
        var user = await ResolveActivePortalUserAsync(httpContext, userManager);
        if (user is null)
            return Results.Unauthorized();
        if (user.OrganizationId == Guid.Empty)
            return Results.Forbid();

        var analysis = await analyses.GetByAnalysisIdAsync(
            user.OrganizationId, analysisId, cancellationToken);
        if (analysis is null)
            return Results.NotFound();

        var history = await decisions.ListHistoryAsync(
            analysis.AnalysisId, user.OrganizationId, cancellationToken);
        return Results.Json(new PortalPackageImportAnalysisDecisionHistoryResponseDto(
            analysis.AnalysisId,
            analysis.PackageId,
            history.Select(ToHistoryItemDto).ToArray()));
    }

    private static async Task<IResult> PostDecisionAsync(
        Guid analysisId,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IPackageImportAnalysisRepository analyses,
        PackageImportAnalysisDecisionService decisions,
        CancellationToken cancellationToken)
    {
        if (!HasValidAntiforgeryWhenRequired(httpContext))
            return Results.BadRequest();

        var user = await ResolveActivePortalUserAsync(httpContext, userManager);
        if (user is null)
            return Results.Unauthorized();
        if (user.OrganizationId == Guid.Empty)
            return Results.Forbid();

        using var document = await JsonDocument.ParseAsync(httpContext.Request.Body, cancellationToken: cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return BadRequest(PortalPackageImportAnalysisDecisionMessages.InvalidRequest);

        if (!TryParseDecisionBody(document.RootElement, out var kind, out var rationale, out var error))
            return BadRequest(error);

        var analysis = await analyses.GetByAnalysisIdAsync(
            user.OrganizationId, analysisId, cancellationToken);
        if (analysis is null)
            return Results.NotFound();

        try
        {
            var actor = new PackageImportAnalysisDecisionActor(user.Id, user.OrganizationId);
            var result = await decisions.DecideAsync(
                analysis, kind, rationale, actor, cancellationToken);
            return Results.Json(ToWriteResponseDto(analysis, result));
        }
        catch (PackageImportAnalysisDecisionNotEligibleException)
        {
            return Results.Json(
                new { error = PortalPackageImportAnalysisDecisionMessages.NotEligible },
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (PackageImportAnalysisDecisionValidationException)
        {
            return BadRequest(PortalPackageImportAnalysisDecisionMessages.InvalidRationale);
        }
        catch (PackageImportAnalysisDecisionOrganizationMismatchException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException)
        {
            return Results.Json(
                new { error = PortalPackageImportAnalysisDecisionMessages.Conflict },
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<ApplicationUser?> ResolveActivePortalUserAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null || !user.IsActive)
            return null;
        return user;
    }

    private static bool TryParseDecisionBody(
        JsonElement root,
        out PackageImportAnalysisDecisionKind kind,
        out string? rationale,
        out string error)
    {
        kind = default;
        rationale = null;
        error = PortalPackageImportAnalysisDecisionMessages.InvalidRequest;

        foreach (var property in root.EnumerateObject())
        {
            if (ForbiddenIdentityProperties.Contains(property.Name) ||
                !AllowedBodyProperties.Contains(property.Name))
            {
                error = PortalPackageImportAnalysisDecisionMessages.InvalidRequest;
                return false;
            }
        }

        if (!TryGetPropertyIgnoreCase(root, "decision", out var decisionElement))
        {
            error = PortalPackageImportAnalysisDecisionMessages.InvalidDecision;
            return false;
        }

        if (decisionElement.ValueKind != JsonValueKind.String)
        {
            error = PortalPackageImportAnalysisDecisionMessages.InvalidDecision;
            return false;
        }

        var decisionText = decisionElement.GetString();
        if (string.Equals(decisionText, "confirmed", StringComparison.OrdinalIgnoreCase))
            kind = PackageImportAnalysisDecisionKind.Confirmed;
        else if (string.Equals(decisionText, "rejected", StringComparison.OrdinalIgnoreCase))
            kind = PackageImportAnalysisDecisionKind.Rejected;
        else
        {
            error = PortalPackageImportAnalysisDecisionMessages.InvalidDecision;
            return false;
        }

        if (TryGetPropertyIgnoreCase(root, "rationale", out var rationaleElement))
        {
            if (rationaleElement.ValueKind == JsonValueKind.Null)
                rationale = null;
            else if (rationaleElement.ValueKind == JsonValueKind.String)
                rationale = rationaleElement.GetString();
            else
            {
                error = PortalPackageImportAnalysisDecisionMessages.InvalidRationale;
                return false;
            }
        }

        return true;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string name, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static PortalPackageImportAnalysisDecisionStateDto ToStateDto(
        PersistedPackageImportAnalysis analysis,
        PackageImportAnalysisDecision? effective)
    {
        if (effective is null)
        {
            return new PortalPackageImportAnalysisDecisionStateDto(
                analysis.AnalysisId,
                analysis.PackageId,
                "unconfirmed",
                null,
                null,
                null,
                null,
                null,
                null,
                analysis.ReadinessStatus);
        }

        return new PortalPackageImportAnalysisDecisionStateDto(
            analysis.AnalysisId,
            analysis.PackageId,
            FormatDecision(effective.Decision),
            effective.DecisionSequence,
            effective.DecidedAtUtc,
            effective.DecidedBySubject,
            effective.DecidedByIssuer,
            effective.Rationale,
            effective.SemanticFingerprintSnapshot,
            analysis.ReadinessStatus);
    }

    private static PortalPackageImportAnalysisDecisionWriteResponseDto ToWriteResponseDto(
        PersistedPackageImportAnalysis analysis,
        PackageImportAnalysisDecisionWriteResult result) =>
        new(
            analysis.AnalysisId,
            analysis.PackageId,
            FormatDecision(result.Decision.Decision),
            result.Decision.DecisionSequence,
            result.Decision.DecidedAtUtc,
            result.Decision.DecidedBySubject,
            result.Decision.DecidedByIssuer,
            result.Decision.Rationale,
            result.Decision.SemanticFingerprintSnapshot,
            result.IdempotentReplay);

    private static PortalPackageImportAnalysisDecisionHistoryItemDto ToHistoryItemDto(
        PackageImportAnalysisDecision decision) =>
        new(
            decision.DecisionId,
            FormatDecision(decision.Decision),
            decision.DecisionSequence,
            decision.DecidedAtUtc,
            decision.DecidedBySubject,
            decision.DecidedByIssuer,
            decision.Rationale,
            decision.SemanticFingerprintSnapshot);

    private static string FormatDecision(PackageImportAnalysisDecisionKind decision) =>
        decision switch
        {
            PackageImportAnalysisDecisionKind.Confirmed => "confirmed",
            PackageImportAnalysisDecisionKind.Rejected => "rejected",
            _ => decision.ToString().ToLowerInvariant()
        };

    private static IResult BadRequest(string error) =>
        Results.Json(new { error }, statusCode: StatusCodes.Status400BadRequest);

    private static bool HasValidAntiforgeryWhenRequired(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        var requires = endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation == true;
        if (!requires)
            return true;

        var feature = httpContext.Features.Get<IAntiforgeryValidationFeature>();
        return feature is not { IsValid: false };
    }
}
