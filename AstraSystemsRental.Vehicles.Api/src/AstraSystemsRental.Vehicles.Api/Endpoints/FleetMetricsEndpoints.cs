using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Vehicles.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Vehicles.Api.Endpoints;

public static class FleetMetricsEndpoints
{
    public static void FleetMetricsEndpoints_Map(this IEndpointRouteBuilder app)
    {
        app.MapGet("/fleet-metrics", async (
                [FromServices] IFleetMetricsService service, HttpContext context, CancellationToken ct) =>
                (await service.GetMetricsAsync(ct)).ToResult(context))
            .WithTags("FleetMetrics")
            .RequireAuthorization()
            .WithName("GetFleetMetrics")
            .WithSummary("Aggregated fleet metrics for the caller's current owner context")
            .Produces<ApiResponse>(StatusCodes.Status200OK);
    }
}
