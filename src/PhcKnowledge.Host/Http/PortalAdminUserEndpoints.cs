using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Identity;

namespace PhcKnowledge.Host.Http;

public static class PortalAdminUserEndpoints
{
    public const string UsersPath = "/api/portal/admin/users";
    public const string UserByIdPath = "/api/portal/admin/users/{userId:guid}";
    public const string UserRolePath = "/api/portal/admin/users/{userId:guid}/role";
    public const string UserActivatePath = "/api/portal/admin/users/{userId:guid}/activate";
    public const string UserDeactivatePath = "/api/portal/admin/users/{userId:guid}/deactivate";

    public static IEndpointRouteBuilder MapPortalAdminUserApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(UsersPath, ListUsersAsync)
            .RequireAuthorization(PortalAuthorizationPolicies.Administrator);

        endpoints.MapGet(UserByIdPath, GetUserAsync)
            .RequireAuthorization(PortalAuthorizationPolicies.Administrator);

        endpoints.MapPost(UsersPath, CreateUserAsync)
            .RequireAuthorization(PortalAuthorizationPolicies.Administrator)
            .WithMetadata(PortalAntiforgeryMetadata.Instance);

        endpoints.MapPut(UserRolePath, SetUserRoleAsync)
            .RequireAuthorization(PortalAuthorizationPolicies.Administrator)
            .WithMetadata(PortalAntiforgeryMetadata.Instance);

        endpoints.MapPost(UserActivatePath, ActivateUserAsync)
            .RequireAuthorization(PortalAuthorizationPolicies.Administrator)
            .WithMetadata(PortalAntiforgeryMetadata.Instance);

        endpoints.MapPost(UserDeactivatePath, DeactivateUserAsync)
            .RequireAuthorization(PortalAuthorizationPolicies.Administrator)
            .WithMetadata(PortalAntiforgeryMetadata.Instance);

        return endpoints;
    }

    private static async Task<IResult> ListUsersAsync(
        PortalUserAdministrationService adminUsers,
        [FromQuery] string? q,
        [FromQuery] bool? isActive,
        [FromQuery] string? role,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await adminUsers.ListAsync(
                q,
                isActive,
                role,
                page ?? 1,
                pageSize ?? PortalUserAdministrationService.DefaultPageSize,
                cancellationToken);

            return Results.Json(new PortalAdminUserListResponse(
                result.Items.Select(ToListItemDto).ToArray(),
                result.Page,
                result.PageSize,
                result.TotalCount));
        }
        catch (PortalUserAdministrationException ex)
        {
            return MapAdministrationError(ex);
        }
    }

    private static async Task<IResult> GetUserAsync(
        Guid userId,
        PortalUserAdministrationService adminUsers,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await adminUsers.GetAsync(userId, cancellationToken);
            if (user is null)
                return Results.NotFound();

            return Results.Json(ToDetailDto(user));
        }
        catch (PortalUserAdministrationException ex)
        {
            return MapAdministrationError(ex);
        }
    }

    private static async Task<IResult> CreateUserAsync(
        HttpContext httpContext,
        [FromBody] PortalAdminCreateUserRequest? request,
        PortalUserAdministrationService adminUsers)
    {
        if (!HasValidAntiforgeryWhenRequired(httpContext))
            return Results.BadRequest();

        if (request is null
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Role)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.Json(
                new { error = PortalAdminUserMessages.InvalidRequest },
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var created = await adminUsers.CreateAsync(
                request.Email,
                request.DisplayName,
                request.Role,
                request.Password,
                httpContext.RequestAborted);

            return Results.Json(ToDetailDto(created.User), statusCode: StatusCodes.Status201Created);
        }
        catch (PortalUserAdministrationException ex)
        {
            return MapAdministrationError(ex);
        }
    }

    private static async Task<IResult> SetUserRoleAsync(
        Guid userId,
        HttpContext httpContext,
        [FromBody] PortalAdminSetRoleRequest? request,
        PortalUserAdministrationService adminUsers)
    {
        if (!HasValidAntiforgeryWhenRequired(httpContext))
            return Results.BadRequest();

        if (request is null || string.IsNullOrWhiteSpace(request.Role))
        {
            return Results.Json(
                new { error = PortalAdminUserMessages.InvalidRequest },
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var updated = await adminUsers.SetRoleAsync(
                userId,
                request.Role,
                httpContext.RequestAborted);

            if (updated is null)
                return Results.NotFound();

            return Results.Json(ToDetailDto(updated));
        }
        catch (PortalUserAdministrationException ex)
        {
            return MapAdministrationError(ex);
        }
    }

    private static Task<IResult> ActivateUserAsync(
        Guid userId,
        HttpContext httpContext,
        PortalUserAdministrationService adminUsers) =>
        SetActiveStateAsync(userId, isActive: true, httpContext, adminUsers);

    private static Task<IResult> DeactivateUserAsync(
        Guid userId,
        HttpContext httpContext,
        PortalUserAdministrationService adminUsers) =>
        SetActiveStateAsync(userId, isActive: false, httpContext, adminUsers);

    private static async Task<IResult> SetActiveStateAsync(
        Guid userId,
        bool isActive,
        HttpContext httpContext,
        PortalUserAdministrationService adminUsers)
    {
        if (!HasValidAntiforgeryWhenRequired(httpContext))
            return Results.BadRequest();

        try
        {
            var updated = await adminUsers.SetActiveStateAsync(
                userId,
                isActive,
                httpContext.RequestAborted);

            if (updated is null)
                return Results.NotFound();

            return Results.Json(ToDetailDto(updated));
        }
        catch (PortalUserAdministrationException ex)
        {
            return MapAdministrationError(ex);
        }
    }

    private static IResult MapAdministrationError(PortalUserAdministrationException ex) =>
        ex.Error switch
        {
            PortalUserAdministrationError.InvalidRole => Results.Json(
                new { error = PortalAdminUserMessages.InvalidRole },
                statusCode: StatusCodes.Status400BadRequest),
            PortalUserAdministrationError.DuplicateEmail => Results.Json(
                new { error = PortalAdminUserMessages.DuplicateEmail },
                statusCode: StatusCodes.Status409Conflict),
            PortalUserAdministrationError.CreateFailed => Results.Json(
                new { error = PortalAdminUserMessages.CreateFailed },
                statusCode: StatusCodes.Status400BadRequest),
            PortalUserAdministrationError.RoleAssignFailed => Results.Json(
                new { error = PortalAdminUserMessages.RoleAssignFailed },
                statusCode: StatusCodes.Status400BadRequest),
            PortalUserAdministrationError.LastActiveAdministrator => Results.Json(
                new { error = PortalAdminUserMessages.LastActiveAdministrator },
                statusCode: StatusCodes.Status409Conflict),
            PortalUserAdministrationError.ActivationFailed => Results.Json(
                new { error = PortalAdminUserMessages.ActivationFailed },
                statusCode: StatusCodes.Status400BadRequest),
            PortalUserAdministrationError.CanonicalOrganizationMissing => Results.Json(
                new { error = PortalAdminUserMessages.CanonicalOrganizationMissing },
                statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.Json(
                new { error = PortalAdminUserMessages.InvalidRequest },
                statusCode: StatusCodes.Status400BadRequest)
        };

    private static PortalAdminUserListItemDto ToListItemDto(PortalUserListItem user) =>
        new(
            user.UserId,
            user.Email,
            user.UserName,
            user.DisplayName,
            user.IsActive,
            user.Roles,
            user.CreatedAtUtc);

    private static PortalAdminUserDetailDto ToDetailDto(PortalUserListItem user) =>
        new(
            user.UserId,
            user.Email,
            user.UserName,
            user.DisplayName,
            user.IsActive,
            user.Roles,
            user.CreatedAtUtc);

    /// <summary>
    /// AntiforgeryMiddleware validates marked endpoints but does not short-circuit JSON handlers.
    /// </summary>
    private static bool HasValidAntiforgeryWhenRequired(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        var requires = endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation == true;
        if (!requires)
            return true;

        var feature = httpContext.Features.Get<Microsoft.AspNetCore.Antiforgery.IAntiforgeryValidationFeature>();
        return feature is not { IsValid: false };
    }
}
