using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Users.Api.Services;

namespace AstraSystemsRental.Users.Api.Endpoints;

public static class QuotaEndpoints
{
    public static void QuotaEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var quotas = app.MapGroup("/quotas").WithTags("Quotas").RequireAuthorization();

        quotas.MapGet("/{nodeKey}", async (string nodeKey, IQuotaService service, HttpContext context, CancellationToken ct) =>
                (await service.GetQuotaAsync(nodeKey, ct)).ToResult(context))
            .WithName("GetQuota").WithSummary("Gets the quota for a node in the caller's current context")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        quotas.MapPost("/{nodeKey}/reserve", async (string nodeKey, IQuotaService service, HttpContext context, CancellationToken ct) =>
                (await service.ReserveAsync(nodeKey, ct)).ToResult(context))
            .WithName("ReserveQuota").WithSummary("Atomically reserves a unit of quota for a node")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        quotas.MapPost("/{nodeKey}/release", async (string nodeKey, IQuotaService service, HttpContext context, CancellationToken ct) =>
                (await service.ReleaseAsync(nodeKey, ct)).ToResult(context))
            .WithName("ReleaseQuota").WithSummary("Releases a previously reserved unit of quota")
            .Produces<ApiResponse>(StatusCodes.Status200OK);
    }
}
