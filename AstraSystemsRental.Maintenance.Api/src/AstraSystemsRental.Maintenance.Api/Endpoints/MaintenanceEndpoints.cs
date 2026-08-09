using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Maintenance.Api.Dtos;
using AstraSystemsRental.Maintenance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Maintenance.Api.Endpoints;

public static class MaintenanceRoutineEndpoints
{
    public static void MaintenanceRoutineEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/maintenance-routines").WithTags("MaintenanceRoutines").RequireAuthorization();

        group.MapGet("/", async (
                [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? search, [FromQuery] bool? isActive,
                IRoutineService service, HttpContext context, CancellationToken ct) =>
                (await service.GetPagedAsync(page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, search, isActive, ct)).ToResult(context))
            .WithName("GetMaintenanceRoutines")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        group.MapPost("/", async ([FromBody] CreateRoutineRequest request, IRoutineService service, HttpContext context, CancellationToken ct) =>
                (await service.CreateAsync(request, ct)).ToResult(context))
            .WithName("CreateMaintenanceRoutine")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:long}", async (long id, IRoutineService service, HttpContext context, CancellationToken ct) =>
                (await service.GetByIdAsync(id, ct)).ToResult(context))
            .WithName("GetMaintenanceRoutine")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:long}", async (long id, [FromBody] UpdateRoutineRequest request, IRoutineService service, HttpContext context, CancellationToken ct) =>
                (await service.UpdateAsync(id, request, ct)).ToResult(context))
            .WithName("UpdateMaintenanceRoutine")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status412PreconditionFailed);

        group.MapDelete("/{id:long}", async (long id, IRoutineService service, HttpContext context, CancellationToken ct) =>
                (await service.DeleteAsync(id, ct)).ToResult(context))
            .WithName("DeleteMaintenanceRoutine")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{id:long}/copy", async (long id, [FromBody] CopyRoutineRequest request, IRoutineService service, HttpContext context, CancellationToken ct) =>
                (await service.CopyAsync(id, request, ct)).ToResult(context))
            .WithName("CopyMaintenanceRoutine")
            .Produces<ApiResponse>(StatusCodes.Status201Created);

        group.MapPost("/{id:long}/periodicities", async (long id, [FromBody] CreatePeriodicityRequest request, IRoutineService service, HttpContext context, CancellationToken ct) =>
                (await service.AddPeriodicityAsync(id, request, ct)).ToResult(context))
            .WithName("AddRoutinePeriodicity")
            .Produces<ApiResponse>(StatusCodes.Status201Created);

        group.MapPost("/{id:long}/concepts", async (long id, [FromBody] CreateConceptRequest request, IRoutineService service, HttpContext context, CancellationToken ct) =>
                (await service.AddConceptAsync(id, request, ct)).ToResult(context))
            .WithName("AddRoutineConcept")
            .Produces<ApiResponse>(StatusCodes.Status201Created);
    }
}

public static class RoutineAssignmentEndpoints
{
    public static void RoutineAssignmentEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/fleet-vehicles").WithTags("RoutineAssignments").RequireAuthorization();

        group.MapGet("/{fleetVehicleId:long}/routine-assignment", async (long fleetVehicleId, IRoutineAssignmentService service, HttpContext context, CancellationToken ct) =>
                (await service.GetAsync(fleetVehicleId, ct)).ToResult(context))
            .WithName("GetRoutineAssignment")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        group.MapPut("/{fleetVehicleId:long}/routine-assignment", async (long fleetVehicleId, [FromBody] AssignRoutineRequest request, IRoutineAssignmentService service, HttpContext context, CancellationToken ct) =>
                (await service.AssignAsync(fleetVehicleId, request, ct)).ToResult(context))
            .WithName("AssignRoutine")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{fleetVehicleId:long}/routine-assignment/history", async (long fleetVehicleId, IRoutineAssignmentService service, HttpContext context, CancellationToken ct) =>
                (await service.GetHistoryAsync(fleetVehicleId, ct)).ToResult(context))
            .WithName("GetRoutineAssignmentHistory")
            .Produces<ApiResponse>(StatusCodes.Status200OK);
    }
}

public static class MileageReadingEndpoints
{
    public static void MileageReadingEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/fleet-vehicles").WithTags("MileageReadings").RequireAuthorization();

        group.MapGet("/{fleetVehicleId:long}/mileage-readings", async (
                long fleetVehicleId, [FromQuery] int page, [FromQuery] int pageSize,
                IMileageReadingService service, HttpContext context, CancellationToken ct) =>
                (await service.GetPagedAsync(fleetVehicleId, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, ct)).ToResult(context))
            .WithName("GetMileageReadings")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        group.MapPost("/{fleetVehicleId:long}/mileage-readings", async (
                long fleetVehicleId, [FromBody] CreateMileageReadingRequest request,
                IMileageReadingService service, HttpContext context, CancellationToken ct) =>
                (await service.AddAsync(fleetVehicleId, request, ct)).ToResult(context))
            .WithName("AddMileageReading")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        group.MapDelete("/{fleetVehicleId:long}/mileage-readings/{readingId:long}", async (
                long fleetVehicleId, long readingId,
                IMileageReadingService service, HttpContext context, CancellationToken ct) =>
                (await service.DeleteAsync(fleetVehicleId, readingId, ct)).ToResult(context))
            .WithName("DeleteMileageReading")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{fleetVehicleId:long}/mileage-readings/next-maintenance", async (
                long fleetVehicleId, IMileageReadingService service, HttpContext context, CancellationToken ct) =>
                (await service.GetNextMaintenanceAsync(fleetVehicleId, ct)).ToResult(context))
            .WithName("GetNextMaintenance")
            .Produces<ApiResponse>(StatusCodes.Status200OK);
    }
}

public static class WorkshopReservationEndpoints
{
    public static void WorkshopReservationEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/workshop-reservations").WithTags("WorkshopReservations").RequireAuthorization();

        group.MapGet("/", async (
                [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] long? fleetVehicleId, [FromQuery] string? status,
                [FromQuery] long? providerId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
                IWorkshopReservationService service, HttpContext context, CancellationToken ct) =>
                (await service.GetPagedAsync(page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, fleetVehicleId, status, providerId, from, to, ct)).ToResult(context))
            .WithName("GetWorkshopReservations")
            .WithSummary("Lists workshop reservations with server-side filtering by vehicle, status, provider and date range")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        group.MapPost("/", async ([FromBody] CreateWorkshopReservationRequest request, IWorkshopReservationService service, HttpContext context, CancellationToken ct) =>
                (await service.CreateAsync(request, ct)).ToResult(context))
            .WithName("CreateWorkshopReservation")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:long}", async (long id, IWorkshopReservationService service, HttpContext context, CancellationToken ct) =>
                (await service.GetByIdAsync(id, ct)).ToResult(context))
            .WithName("GetWorkshopReservation")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{id:long}", async (long id, [FromBody] UpdateWorkshopReservationRequest request, IWorkshopReservationService service, HttpContext context, CancellationToken ct) =>
                (await service.UpdateAsync(id, request, ct)).ToResult(context))
            .WithName("UpdateWorkshopReservation")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status412PreconditionFailed);

        group.MapPost("/{id:long}/status-transitions", async (long id, [FromBody] ChangeReservationStatusRequest request, IWorkshopReservationService service, HttpContext context, CancellationToken ct) =>
                (await service.ChangeStatusAsync(id, request, ct)).ToResult(context))
            .WithName("ChangeWorkshopReservationStatus")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{id:long}/photos", async (
                long id, IFormFile file, [FromForm] string? notes,
                IWorkshopReservationService service, IPhotoStorage storage, HttpContext context, CancellationToken ct) =>
            {
                if (file is null || file.Length == 0)
                    return OperationResult.Fail("A file is required.").ToResult(context);

                var stored = await storage.SaveAsync(id, file, ct);
                if (stored is null)
                    return OperationResult.Fail("Unsupported file type. Use JPG, PNG or WEBP.").ToResult(context);

                return (await service.AddPhotoAsync(id, stored.FileName, stored.StoragePath, file.ContentType, file.Length, notes, ct)).ToResult(context);
            })
            .DisableAntiforgery()
            .WithName("AddWorkshopReservationPhoto")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{id:long}/photos/{photoId:long}/content", async (
                long id, long photoId,
                IWorkshopReservationService service, IPhotoStorage storage, CancellationToken ct) =>
            {
                var storagePath = await service.GetPhotoStoragePathAsync(id, photoId, ct);

                if (storagePath is null)
                    return Results.NotFound();

                var stream = storage.OpenRead(storagePath, out var contentType);

                return stream is null
                    ? Results.NotFound()
                    : Results.Stream(stream, contentType);
            })
            .WithName("GetWorkshopReservationPhoto")
            .WithSummary("Serves the binary of a reservation photo owned by the caller")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        var providers = app.MapGroup("/workshop-providers").WithTags("WorkshopProviders").RequireAuthorization();

        providers.MapGet("/", async (IWorkshopReservationService service, HttpContext context, CancellationToken ct) =>
                (await service.GetProvidersAsync(ct)).ToResult(context))
            .WithName("GetWorkshopProviders")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        providers.MapPost("/", async ([FromBody] CreateWorkshopProviderRequest request, IWorkshopReservationService service, HttpContext context, CancellationToken ct) =>
                (await service.CreateProviderAsync(request, ct)).ToResult(context))
            .WithName("CreateWorkshopProvider")
            .Produces<ApiResponse>(StatusCodes.Status201Created);
    }
}

public static class MaintenanceMetricsEndpoints
{
    public static void MaintenanceMetricsEndpoints_Map(this IEndpointRouteBuilder app)
    {
        app.MapGet("/maintenance-metrics", async (
                [FromServices] IMaintenanceMetricsService service, HttpContext context, CancellationToken ct) =>
                (await service.GetMetricsAsync(ct)).ToResult(context))
            .WithTags("MaintenanceMetrics")
            .RequireAuthorization()
            .WithName("GetMaintenanceMetrics")
            .WithSummary("Aggregated workshop and maintenance metrics for the caller's owner context")
            .Produces<ApiResponse>(StatusCodes.Status200OK);
    }
}
