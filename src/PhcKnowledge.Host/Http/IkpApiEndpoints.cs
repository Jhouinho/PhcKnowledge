using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Ikp.Analysis;
using PhcKnowledge.Import.Receipts;

namespace PhcKnowledge.Host.Http;

public static class IkpApiEndpoints
{
    public const string PackagesPath = "/api/v1/ikp/packages";
    public const string AttemptPath = "/api/v1/ikp/attempts/{attemptId:guid}";
    public const string PackageByIdPath = "/api/v1/ikp/packages/{packageId:guid}";
    public const string PackageAnalysisPath = "/api/v1/ikp/packages/{packageId:guid}/analysis";

    public static IEndpointRouteBuilder MapIkpApi(
        this IEndpointRouteBuilder endpoints,
        bool jwtEnabled = true)
    {
        if (jwtEnabled)
        {
            endpoints.MapPost(PackagesPath, UploadPackageAsync)
                .RequireAuthorization(IkpAuthorizationPolicies.Upload)
                .DisableAntiforgery();

            endpoints.MapGet(AttemptPath, GetAttemptAsync)
                .RequireAuthorization(IkpAuthorizationPolicies.Read);

            endpoints.MapGet(PackageByIdPath, GetPackageAsync)
                .RequireAuthorization(IkpAuthorizationPolicies.Read);

            endpoints.MapGet(PackageAnalysisPath, GetPackageAnalysisAsync)
                .RequireAuthorization(IkpAuthorizationPolicies.Read);

            endpoints.MapPost(PackageAnalysisPath, ExecutePackageAnalysisAsync)
                .RequireAuthorization(IkpAuthorizationPolicies.Read)
                .DisableAntiforgery();
        }
        else
        {
            // JWT disabled: IKP surface is unavailable (not anonymous). Health stays up.
            endpoints.MapPost(PackagesPath, IkpApiDisabledAsync)
                .DisableAntiforgery();
            endpoints.MapGet(AttemptPath, IkpApiDisabledAsync);
            endpoints.MapGet(PackageByIdPath, IkpApiDisabledAsync);
            endpoints.MapGet(PackageAnalysisPath, IkpApiDisabledAsync);
            endpoints.MapPost(PackageAnalysisPath, IkpApiDisabledAsync)
                .DisableAntiforgery();
        }

        endpoints.MapGet("/health/live", () => Results.Json(new { status = "live" }));
        endpoints.MapGet("/health/ready", (HostReadinessState readiness) =>
            readiness.IsSchemaReady
                ? Results.Json(new { status = "ready" })
                : Results.Json(new { status = "not_ready" }, statusCode: StatusCodes.Status503ServiceUnavailable));

        return endpoints;
    }

    private static IResult IkpApiDisabledAsync(HttpContext httpContext) =>
        IkpProblemDetailsFactory.Problem(
            httpContext,
            StatusCodes.Status503ServiceUnavailable,
            "IKP API unavailable",
            IkpPublicErrorCodes.IkpApiDisabled);

    private static async Task<IResult> UploadPackageAsync(
        HttpContext httpContext,
        OrganizationIdResolver organizationIdResolver,
        IIkpIngestionGateway ingestion,
        IPackageReceiptAttemptRepository attempts,
        UploadConcurrencyGate concurrencyGate,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("PhcKnowledge.Host.Http.IkpApi");

        if (!IsApplicationZipContentType(httpContext.Request.ContentType))
        {
            return IkpProblemDetailsFactory.Problem(
                httpContext,
                StatusCodes.Status415UnsupportedMediaType,
                "Unsupported media type",
                IkpPublicErrorCodes.UnsupportedMediaType);
        }

        Guid organizationId;
        try
        {
            organizationId = organizationIdResolver.ResolveRequired(httpContext.User);
        }
        catch (OrganizationIdResolutionException ex)
        {
            return IkpProblemDetailsFactory.FromException(httpContext, ex, logger);
        }

        UploadConcurrencyLease? lease;
        try
        {
            lease = await concurrencyGate.TryAcquireAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Results.Empty;
        }

        if (lease is null)
        {
            httpContext.Response.Headers.RetryAfter = "5";
            return IkpProblemDetailsFactory.Problem(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Upload capacity exceeded",
                IkpPublicErrorCodes.UploadCapacityExceeded);
        }

        try
        {
            return await ExecuteAsync(httpContext, logger, async () =>
            {
                IkpIngestionResult result;
                try
                {
                    result = await ingestion.ReceiveAsync(
                        httpContext.Request.Body,
                        organizationId,
                        cancellationToken);
                }
                catch (PackageReceiptPendingException pending)
                {
                    return await AcceptedPendingAsync(
                        httpContext, attempts, organizationId, pending.AttemptId, cancellationToken);
                }

                var status = await attempts.GetByPackageIdAsync(
                    organizationId, result.Validation.PackageId, cancellationToken);
                if (status is null)
                {
                    logger.LogError(
                        "IKP API internal failure after successful ingestion: status row missing. TraceId={TraceId}",
                        httpContext.TraceIdentifier);
                    return IkpProblemDetailsFactory.Problem(
                        httpContext,
                        StatusCodes.Status500InternalServerError,
                        "Internal error",
                        IkpPublicErrorCodes.InternalError);
                }

                return Results.Json(IkpPublicDtoMapper.ToStatusDto(status), statusCode: StatusCodes.Status200OK);
            });
        }
        finally
        {
            lease.Dispose();
        }
    }

    private static async Task<IResult> AcceptedPendingAsync(
        HttpContext httpContext,
        IPackageReceiptAttemptRepository attempts,
        Guid organizationId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var statusUrl = $"/api/v1/ikp/attempts/{attemptId:D}";
        var status = await attempts.GetAttemptAsync(organizationId, attemptId, cancellationToken);
        if (status is null)
        {
            return Results.Json(
                new IkpIngestionPendingDto(attemptId, "PendingArchive", statusUrl),
                statusCode: StatusCodes.Status202Accepted);
        }

        return Results.Json(
            IkpPublicDtoMapper.ToAcceptedDto(status, statusUrl),
            statusCode: StatusCodes.Status202Accepted);
    }

    private static Task<IResult> GetAttemptAsync(
        Guid attemptId,
        HttpContext httpContext,
        OrganizationIdResolver organizationIdResolver,
        IPackageReceiptAttemptRepository attempts,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("PhcKnowledge.Host.Http.IkpApi");
        return ExecuteAsync(httpContext, logger, async () =>
        {
            var organizationId = organizationIdResolver.ResolveRequired(httpContext.User);
            var status = await attempts.GetAttemptAsync(organizationId, attemptId, cancellationToken);
            if (status is null)
            {
                return IkpProblemDetailsFactory.Problem(
                    httpContext,
                    StatusCodes.Status404NotFound,
                    "Not found",
                    IkpPublicErrorCodes.NotFound);
            }

            return Results.Json(IkpPublicDtoMapper.ToStatusDto(status));
        });
    }

    private static Task<IResult> GetPackageAsync(
        Guid packageId,
        HttpContext httpContext,
        OrganizationIdResolver organizationIdResolver,
        IPackageReceiptAttemptRepository attempts,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("PhcKnowledge.Host.Http.IkpApi");
        return ExecuteAsync(httpContext, logger, async () =>
        {
            var organizationId = organizationIdResolver.ResolveRequired(httpContext.User);
            var status = await attempts.GetByPackageIdAsync(organizationId, packageId, cancellationToken);
            if (status is null)
            {
                return IkpProblemDetailsFactory.Problem(
                    httpContext,
                    StatusCodes.Status404NotFound,
                    "Not found",
                    IkpPublicErrorCodes.NotFound);
            }

            return Results.Json(IkpPublicDtoMapper.ToStatusDto(status));
        });
    }

    private static Task<IResult> GetPackageAnalysisAsync(
        Guid packageId,
        HttpContext httpContext,
        OrganizationIdResolver organizationIdResolver,
        IkpPackageAnalysisService analysisService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("PhcKnowledge.Host.Http.IkpApi");
        return ExecuteAsync(httpContext, logger, async () =>
        {
            var organizationId = organizationIdResolver.ResolveRequired(httpContext.User);
            var persisted = await analysisService.GetPersistedAsync(
                organizationId, packageId, cancellationToken);
            return Results.Json(IkpPublicDtoMapper.ToAnalysisDto(persisted));
        });
    }

    private static Task<IResult> ExecutePackageAnalysisAsync(
        Guid packageId,
        HttpContext httpContext,
        OrganizationIdResolver organizationIdResolver,
        IkpPackageAnalysisService analysisService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("PhcKnowledge.Host.Http.IkpApi");
        return ExecuteAsync(httpContext, logger, async () =>
        {
            var organizationId = organizationIdResolver.ResolveRequired(httpContext.User);
            var persisted = await analysisService.ExecuteAsync(
                organizationId, packageId, cancellationToken);
            return Results.Json(IkpPublicDtoMapper.ToAnalysisDto(persisted));
        });
    }

    private static async Task<IResult> ExecuteAsync(
        HttpContext httpContext,
        ILogger logger,
        Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            return IkpProblemDetailsFactory.FromException(httpContext, ex, logger);
        }
    }

    /// <summary>
    /// Accepts media type <c>application/zip</c> (case-insensitive) with optional parameters.
    /// Rejects other types (including <c>application/zipx</c>), multipart and malformed values.
    /// </summary>
    public static bool IsApplicationZipContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
            return false;

        return mediaType.MediaType.Equals("application/zip", StringComparison.OrdinalIgnoreCase);
    }
}
