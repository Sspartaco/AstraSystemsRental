using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Reports.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Reports.Api.Endpoints;

public static class ReportsEndpoints
{
    public static void ReportsEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dashboard").WithTags("Dashboard").RequireAuthorization();

        group.MapGet("/", async (
                [FromServices] IDashboardService service, HttpContext context, CancellationToken ct) =>
                (await service.GetDashboardAsync(ct)).ToResult(context))
            .WithName("GetDashboard")
            .WithSummary("Composed fleet and workshop dashboard for the caller's owner context")
            .Produces<ApiResponse>(StatusCodes.Status200OK);
    }
}
