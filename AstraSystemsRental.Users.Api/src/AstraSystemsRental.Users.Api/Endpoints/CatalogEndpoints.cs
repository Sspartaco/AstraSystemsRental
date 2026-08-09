using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Security;
using AstraSystemsRental.Users.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Users.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void CatalogEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var nodes = app.MapGroup("/nodes").WithTags("Catalog");

        nodes.MapGet("/", async ([FromServices] ICatalogService service, HttpContext context, CancellationToken ct) =>
                (await service.GetNodesAsync(ct)).ToResult(context))
            .WithName("GetNodes").WithSummary("Lists the node catalog")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .RequireAuthorization();

        nodes.MapPost("/", async ([FromBody] CreateNodeRequest request, [FromServices] ICatalogService service, HttpContext context, CancellationToken ct) =>
                (await service.CreateNodeAsync(request, ct)).ToResult(context))
            .WithName("CreateNode").WithSummary("Creates a node in the catalog")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict)
            .RequireAuthorization(AstraPolicies.SuperUser);

        var plans = app.MapGroup("/plans").WithTags("Catalog").RequireAuthorization(AstraPolicies.SuperUser);

        plans.MapGet("/", async ([FromServices] ICatalogService service, HttpContext context, CancellationToken ct) =>
                (await service.GetPlansAsync(ct)).ToResult(context))
            .WithName("GetPlans").WithSummary("Lists plans with their nodes")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        plans.MapPost("/", async ([FromBody] CreatePlanRequest request, [FromServices] ICatalogService service, HttpContext context, CancellationToken ct) =>
                (await service.CreatePlanAsync(request, ct)).ToResult(context))
            .WithName("CreatePlan").WithSummary("Creates a plan")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        plans.MapPut("/{planId:int}", async (int planId, [FromBody] UpdatePlanRequest request, [FromServices] ICatalogService service, HttpContext context, CancellationToken ct) =>
                (await service.UpdatePlanAsync(planId, request, ct)).ToResult(context))
            .WithName("UpdatePlan").WithSummary("Updates a plan")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        plans.MapPost("/{planId:int}/nodes", async (int planId, [FromBody] SetNodeRequest request, [FromServices] ICatalogService service, HttpContext context, CancellationToken ct) =>
                (await service.SetPlanNodeAsync(planId, request, ct)).ToResult(context))
            .WithName("SetPlanNode").WithSummary("Assigns or removes a node from a plan")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        var roles = app.MapGroup("/roles").WithTags("Catalog").RequireAuthorization(AstraPolicies.SuperUser);

        roles.MapGet("/", async ([FromServices] ICatalogService service, HttpContext context, CancellationToken ct) =>
                (await service.GetRolesAsync(ct)).ToResult(context))
            .WithName("GetRoles").WithSummary("Lists roles with their nodes")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        roles.MapPost("/{roleId:int}/nodes", async (int roleId, [FromBody] SetNodeRequest request, [FromServices] ICatalogService service, HttpContext context, CancellationToken ct) =>
                (await service.SetRoleNodeAsync(roleId, request, ct)).ToResult(context))
            .WithName("SetRoleNode").WithSummary("Assigns or removes a node from a role")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }
}
