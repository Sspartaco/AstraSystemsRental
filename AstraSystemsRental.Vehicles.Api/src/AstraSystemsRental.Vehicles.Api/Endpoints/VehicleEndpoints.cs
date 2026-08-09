using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Vehicles.Api.Dtos;
using AstraSystemsRental.Vehicles.Api.Services;

namespace AstraSystemsRental.Vehicles.Api.Endpoints;

public static class VehicleEndpoints
{
    public static void VehicleEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var vehicles = app.MapGroup("/vehicles").WithTags("Vehicles").RequireAuthorization();

        vehicles.MapGet("/{plateNumber}", async (string plateNumber, IVehicleQuoteService service, HttpContext context, CancellationToken ct) =>
                (await service.GetVehicleAsync(plateNumber, ct)).ToResult(context))
            .WithName("GetVehicle").WithSummary("Gets a vehicle by plate number")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        vehicles.MapPost("/", async (CreateVehicleRequest request, IVehicleQuoteService service, HttpContext context, CancellationToken ct) =>
                (await service.CreateVehicleAsync(request, ct)).ToResult(context))
            .WithName("CreateVehicle").WithSummary("Registers a vehicle manually when not found in the base catalog")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest);
    }
}
