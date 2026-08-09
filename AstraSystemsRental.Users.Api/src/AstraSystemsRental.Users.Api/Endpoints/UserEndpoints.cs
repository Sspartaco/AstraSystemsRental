using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Security;
using AstraSystemsRental.Users.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Users.Api.Endpoints;

public static class UserEndpoints
{
    public static void UserEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Users");

        group.MapPost("/", async (
                [FromBody] CreateUserRequest request,
                [FromServices] IUserService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateAsync(request, cancellationToken);
                return result.ToResult(context);
            })
            .WithName("CreateUser")
            .WithSummary("Registers a person and creates a pending user")
            .WithDescription("Creates the person and user record, provisions a demo subscription, and sends the welcome confirmation email.")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict)
            .AllowAnonymous();

        group.MapPost("/confirm", async (
                [FromBody] ConfirmEmailRequest request,
                [FromServices] IUserService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.ConfirmAsync(request, cancellationToken);
                return result.ToResult(context);
            })
            .WithName("ConfirmEmail")
            .WithSummary("Confirms an account and sets its password")
            .WithDescription("Validates the confirmation token, activates the user and stores the chosen password.")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        group.MapGet("/me", async (
                [FromServices] IUserService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.GetProfileAsync(cancellationToken)).ToResult(context))
            .WithName("GetMyProfile")
            .WithSummary("Gets the authenticated caller's own profile")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapGet("/overview", async (
                [FromServices] IUserService service,
                HttpContext context,
                CancellationToken cancellationToken,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                [FromQuery] string? search = null) =>
            {
                var result = await service.GetOverviewAsync(page, pageSize, search, cancellationToken);
                return result.ToResult(context);
            })
            .WithName("GetUsersOverview")
            .WithSummary("Lists all users with role, subscription and company (SuperUser only)")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .RequireAuthorization(AstraPolicies.SuperUser);

        group.MapPost("/{id:long}/active", async (
                long id,
                [FromBody] SetActiveRequest request,
                [FromServices] IUserAdminService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.SetActiveAsync(id, request.IsActive, cancellationToken)).ToResult(context))
            .WithName("SetUserActive")
            .WithSummary("Activates or deactivates a user (SuperUser only)")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization(AstraPolicies.SuperUser);

        group.MapPost("/{id:long}/plan", async (
                long id,
                [FromBody] AssignPlanRequest request,
                [FromServices] IUserAdminService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.AssignPlanAsync(id, request.PlanCode, request.DurationDays, cancellationToken)).ToResult(context))
            .WithName("AssignUserPlan")
            .WithSummary("Assigns a subscription plan to a user (SuperUser only)")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization(AstraPolicies.SuperUser);

        group.MapPost("/{id:long}/role", async (
                long id,
                [FromBody] AssignRoleRequest request,
                [FromServices] IRoleService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.AssignRoleAsync(id, request.RoleCode, cancellationToken);
                return result.ToResult(context);
            })
            .WithName("AssignRole")
            .WithSummary("Assigns a role to a user (SuperUser only)")
            .WithDescription("Elevates or changes a user's role. Requires an authenticated SuperUser.")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization(AstraPolicies.SuperUser);
    }
}
