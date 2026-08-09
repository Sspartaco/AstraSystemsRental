using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Vehicles.Api.Dtos;
using AstraSystemsRental.Vehicles.Api.Services;

namespace AstraSystemsRental.Vehicles.Api.Endpoints;

public static class QuoteEndpoints
{
    public static void QuoteEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var quotes = app.MapGroup("/quotes").WithTags("Quotes").RequireAuthorization();

        quotes.MapPost("/", async (CreateQuoteRequest request, IVehicleQuoteService service, IAstraRequestContext requestContext, HttpContext context, CancellationToken ct) =>
                (await service.CreateQuoteAsync(request.PlateNumber, requestContext.UserId, ct)).ToResult(context))
            .WithName("CreateQuote").WithSummary("Starts a valuation quote for a plate; returns a requestId to poll")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        quotes.MapGet("/{requestId:guid}", async (Guid requestId, IVehicleQuoteService service, IAstraRequestContext requestContext, HttpContext context, CancellationToken ct) =>
                (await service.GetQuoteStatusAsync(requestId, requestContext.UserId, ct)).ToResult(context))
            .WithName("GetQuoteStatus").WithSummary("Gets aggregated and per-source status of a quote request owned by the caller")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }
}
