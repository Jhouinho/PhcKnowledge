using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;

namespace PhcKnowledge.Host.Http;

public static class IkpPublicErrorCodes
{
    public const string InvalidPackage = "invalid_package";
    public const string UploadTooLarge = "upload_too_large";
    public const string PackageConflict = "package_conflict";
    public const string IngestionPending = "ingestion_pending";
    public const string IngestionNeedsAttention = "ingestion_needs_attention";
    public const string NotFound = "not_found";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string UnsupportedMediaType = "unsupported_media_type";
    public const string BadRequest = "bad_request";
    public const string UploadCapacityExceeded = "upload_capacity_exceeded";
    public const string InternalError = "internal_error";
    public const string IkpApiDisabled = "ikp_api_disabled";
}

public static class IkpProblemDetailsFactory
{
    public static IResult Problem(
        HttpContext httpContext,
        int statusCode,
        string title,
        string code,
        Guid? attemptId = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}"
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        if (attemptId is { } id && id != Guid.Empty)
            problem.Extensions["attemptId"] = id;
        // Never set Detail to exception messages.
        return Results.Problem(problem);
    }

    public static IResult FromException(
        HttpContext httpContext,
        Exception exception,
        ILogger? logger = null)
    {
        if (IsClientCancellation(httpContext, exception))
            return Results.Empty;

        var result = MapException(httpContext, exception);
        if (RequiresInternalErrorLog(exception) && logger is not null)
        {
            // Log exception + TraceId only. Never log JWT tokens or upload body content.
            logger.LogError(
                exception,
                "IKP API internal failure. TraceId={TraceId}",
                httpContext.TraceIdentifier);
        }

        return result;
    }

    private static IResult MapException(HttpContext httpContext, Exception exception) =>
        exception switch
        {
            PackageReceiptNeedsAttentionException needsAttention =>
                Problem(
                    httpContext, StatusCodes.Status409Conflict, "Ingestion needs attention",
                    IkpPublicErrorCodes.IngestionNeedsAttention, needsAttention.AttemptId),

            PackageReceiptConflictException conflict
                when conflict.Kind == PackageReceiptConflictKind.IdentityMismatch =>
                Problem(
                    httpContext, StatusCodes.Status409Conflict, "Package conflict",
                    IkpPublicErrorCodes.PackageConflict),

            PackageReceiptConflictException conflict
                when conflict.Kind == PackageReceiptConflictKind.InternalInconsistency =>
                Problem(
                    httpContext, StatusCodes.Status500InternalServerError, "Internal error",
                    IkpPublicErrorCodes.InternalError),

            IkpPackageValidationException =>
                Problem(
                    httpContext, StatusCodes.Status422UnprocessableEntity, "Invalid package",
                    IkpPublicErrorCodes.InvalidPackage),

            OrganizationIdResolutionException org
                when org.Failure == OrganizationIdResolutionFailure.Unauthenticated =>
                Problem(
                    httpContext, StatusCodes.Status401Unauthorized, "Unauthorized",
                    IkpPublicErrorCodes.Unauthorized),

            OrganizationIdResolutionException =>
                Problem(
                    httpContext, StatusCodes.Status403Forbidden, "Forbidden",
                    IkpPublicErrorCodes.Forbidden),

            BadHttpRequestException bad when bad.StatusCode == StatusCodes.Status413PayloadTooLarge =>
                Problem(
                    httpContext, StatusCodes.Status413PayloadTooLarge, "Upload too large",
                    IkpPublicErrorCodes.UploadTooLarge),

            _ =>
                Problem(
                    httpContext, StatusCodes.Status500InternalServerError, "Internal error",
                    IkpPublicErrorCodes.InternalError)
        };

    private static bool IsClientCancellation(HttpContext httpContext, Exception exception) =>
        exception is OperationCanceledException &&
        httpContext.RequestAborted.IsCancellationRequested;

    /// <summary>
    /// Client-driven <see cref="OperationCanceledException"/> is filtered by
    /// <see cref="IsClientCancellation"/> before logging. Any other cancelled operation
    /// (for example an internal timeout) falls through to the default and must be logged.
    /// </summary>
    private static bool RequiresInternalErrorLog(Exception exception) =>
        exception switch
        {
            PackageReceiptNeedsAttentionException => false,
            PackageReceiptConflictException conflict
                when conflict.Kind == PackageReceiptConflictKind.IdentityMismatch => false,
            PackageReceiptConflictException => true,
            IkpPackageValidationException => false,
            OrganizationIdResolutionException => false,
            BadHttpRequestException bad when bad.StatusCode == StatusCodes.Status413PayloadTooLarge => false,
            _ => true
        };
}
