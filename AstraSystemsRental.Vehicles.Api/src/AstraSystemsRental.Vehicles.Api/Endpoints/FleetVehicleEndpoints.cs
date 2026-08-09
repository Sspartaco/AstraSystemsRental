using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Vehicles.Api.Dtos;
using AstraSystemsRental.Vehicles.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Vehicles.Api.Endpoints;

public static class FleetVehicleEndpoints
{
    public static void FleetVehicleEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/fleet-vehicles").WithTags("FleetVehicles").RequireAuthorization();

        group.MapGet("/", async (
                [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? status, [FromQuery] string? search,
                [FromServices] IFleetVehicleService service, HttpContext context, CancellationToken ct) =>
                (await service.GetPagedAsync(page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, status, search, ct)).ToResult(context))
            .WithName("GetFleetVehicles").WithSummary("Lists fleet vehicles for the caller's current owner context")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        group.MapPost("/", async (
                [FromBody] CreateFleetVehicleRequest request,
                [FromServices] IFleetVehicleService service, HttpContext context, CancellationToken ct) =>
                (await service.CreateAsync(request, ct)).ToResult(context))
            .WithName("CreateFleetVehicle").WithSummary("Registers a vehicle in the fleet, reserving plan quota")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:long}", async (long id, IFleetVehicleService service, HttpContext context, CancellationToken ct) =>
                (await service.GetByIdAsync(id, ct)).ToResult(context))
            .WithName("GetFleetVehicle").WithSummary("Gets a fleet vehicle owned by the caller's current context")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:long}", async (long id, [FromBody] UpdateFleetVehicleRequest request, IFleetVehicleService service, HttpContext context, CancellationToken ct) =>
                (await service.UpdateAsync(id, request, ct)).ToResult(context))
            .WithName("UpdateFleetVehicle").WithSummary("Updates a fleet vehicle with optimistic concurrency")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status412PreconditionFailed);

        group.MapDelete("/{id:long}", async (long id, IFleetVehicleService service, HttpContext context, CancellationToken ct) =>
                (await service.DeleteAsync(id, ct)).ToResult(context))
            .WithName("DeleteFleetVehicle").WithSummary("Removes a fleet vehicle and releases plan quota")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:long}/status-transitions", async (long id, [FromBody] ChangeFleetVehicleStatusRequest request, IFleetVehicleService service, HttpContext context, CancellationToken ct) =>
                (await service.ChangeStatusAsync(id, request, ct)).ToResult(context))
            .WithName("ChangeFleetVehicleStatus").WithSummary("Changes vehicle status and records history")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:long}/status-history", async (long id, IFleetVehicleService service, HttpContext context, CancellationToken ct) =>
                (await service.GetStatusHistoryAsync(id, ct)).ToResult(context))
            .WithName("GetFleetVehicleStatusHistory").WithSummary("Lists status change history")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        group.MapGet("/{id:long}/documents", async (long id, IFleetVehicleService service, HttpContext context, CancellationToken ct) =>
                (await service.GetDocumentsAsync(id, ct)).ToResult(context))
            .WithName("GetFleetVehicleDocuments").WithSummary("Lists document metadata for a vehicle")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        group.MapPost("/{id:long}/documents", async (long id, [FromBody] CreateFleetVehicleDocumentRequest request, IFleetVehicleService service, HttpContext context, CancellationToken ct) =>
                (await service.AddDocumentAsync(id, request, ct)).ToResult(context))
            .WithName("CreateFleetVehicleDocument").WithSummary("Creates a document metadata entry")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:long}/documents/{documentId:long}", async (long id, long documentId, [FromBody] UpdateFleetVehicleDocumentRequest request, IFleetVehicleService service, HttpContext context, CancellationToken ct) =>
                (await service.UpdateDocumentAsync(id, documentId, request, ct)).ToResult(context))
            .WithName("UpdateFleetVehicleDocument").WithSummary("Updates a document metadata entry")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }
}
