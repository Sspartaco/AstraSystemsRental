using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Users.Api.Security;
using AstraSystemsRental.Users.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Users.Api.Endpoints;

public static class LogEndpoints
{
    public static void LogEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/logs").WithTags("Logs").RequireAuthorization(AstraPolicies.Diagnostics);

        group.MapGet("/", async (
                [FromServices] ILogQueryService logs,
                HttpContext context,
                CancellationToken cancellationToken,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 50,
                [FromQuery] string? level = null,
                [FromQuery(Name = "service")] string? serviceName = null,
                [FromQuery] string? search = null) =>
                (await logs.GetLogsAsync(page, pageSize, level, serviceName, search, cancellationToken)).ToResult(context))
            .WithName("GetApplicationLogs")
            .WithSummary("Lists application logs (SysAdmin or SuperUser only)")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden);

        group.MapGet("/services", async (
                [FromServices] ILogQueryService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.GetServicesAsync(cancellationToken)).ToResult(context))
            .WithName("GetLogServices")
            .WithSummary("Lists the distinct services that have emitted logs")
            .Produces<ApiResponse>(StatusCodes.Status200OK);
    }
}
