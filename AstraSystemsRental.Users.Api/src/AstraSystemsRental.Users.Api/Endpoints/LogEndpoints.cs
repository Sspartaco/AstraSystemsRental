using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Diagnostics;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Base.Validation;
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

        // Fuera del grupo anterior a proposito: reportar un fallo propio lo hace
        // CUALQUIER usuario autenticado desde su telefono, no solo quien puede
        // LEER los logs. Con la politica Diagnostics, los errores de la app
        // (que es donde mas ciegos estabamos) nunca llegarian.
        app.MapPost("/logs/client", async (
                [FromBody] ClientLogRequest request,
                [FromServices] IApplicationLogWriter writer,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var guard = new Guard()
                    .NotEmpty(request.Message, "Message");

                if (guard.HasErrors)
                    return OperationResult.Fail(guard.Errors).ToResult(context);

                writer.Enqueue(new ApplicationLogEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    Level = request.Level is "Error" or "Warning" or "Information" ? request.Level : "Error",
                    // El prefijo separa lo que ocurrio en el dispositivo de lo que
                    // fallo en el servidor: en la vista de Logs son cosas distintas.
                    Service = $"Mobile.{(string.IsNullOrWhiteSpace(request.Platform) ? "Unknown" : request.Platform)}",
                    Message = request.Message,
                    ExceptionType = request.ExceptionType,
                    ExceptionDetail = request.ExceptionDetail,
                    RequestMethod = request.RequestMethod,
                    RequestPath = request.RequestPath,
                    StatusCode = request.StatusCode,
                    TraceId = request.TraceId,
                    UserId = context.User.FindFirst(AstraClaims.UserId)?.Value is { } id && long.TryParse(id, out var userId)
                        ? userId
                        : null,
                    UserEmail = context.User.FindFirst(AstraClaims.Email)?.Value
                });

                return OperationResult.Ok(new { accepted = true }).ToResult(context);
            })
            .RequireAuthorization()
            .WithTags("Logs")
            .WithName("IngestClientLog")
            .WithSummary("Records an error reported by the mobile app")
            .Produces<ApiResponse>(StatusCodes.Status200OK);
    }
}

/// <summary>
/// Fallo ocurrido en el dispositivo. Sin esto, los errores de la app son
/// invisibles salvo que un usuario los reporte a mano.
/// </summary>
public sealed record ClientLogRequest
{
    public string Message { get; init; } = string.Empty;
    public string? Level { get; init; }
    public string? Platform { get; init; }
    public string? ExceptionType { get; init; }
    public string? ExceptionDetail { get; init; }
    public string? RequestMethod { get; init; }
    public string? RequestPath { get; init; }
    public int? StatusCode { get; init; }
    public string? TraceId { get; init; }
}
