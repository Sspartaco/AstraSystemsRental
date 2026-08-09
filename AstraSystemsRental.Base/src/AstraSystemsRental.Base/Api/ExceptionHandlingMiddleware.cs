using System.Net;
using System.Security.Claims;
using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Diagnostics;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Base.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AstraSystemsRental.Base.Api;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment,
    AstraApiOptions apiOptions,
    IApplicationLogWriter? logWriter = null)
{
    private void Persist(HttpContext context, Exception exception, string level, int statusCode)
    {
        if (logWriter is null)
            return;

        var user = context.User;

        logWriter.Enqueue(new ApplicationLogEntry
        {
            Level = level,
            Service = apiOptions.ServiceName,
            Message = exception.Message,
            ExceptionType = exception.GetType().FullName,
            ExceptionDetail = exception.ToString(),
            TraceId = context.TraceIdentifier,
            RequestMethod = context.Request.Method,
            RequestPath = context.Request.Path.Value,
            StatusCode = statusCode,
            UserId = long.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value, out var id)
                ? id
                : null,
            UserEmail = user.FindFirst(AstraClaims.Email)?.Value
        });
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (CompanyContextForbiddenException ex)
        {
            logger.LogWarning(ex, "Company context forbidden. TraceId: {TraceId}", context.TraceIdentifier);
            Persist(context, ex, "Warning", StatusCodes.Status403Forbidden);

            var forbidden = OperationResult.Fail("CompanyContextForbidden", HttpStatusCode.Forbidden);
            forbidden.TraceId = context.TraceIdentifier;

            await forbidden.ToResult(context).ExecuteAsync(context);
        }
        catch (BadHttpRequestException ex)
        {
            logger.LogWarning(ex, "Malformed request. TraceId: {TraceId}", context.TraceIdentifier);
            Persist(context, ex, "Warning", StatusCodes.Status400BadRequest);

            var badRequest = OperationResult.Fail("The request body is malformed or invalid.", HttpStatusCode.BadRequest);
            badRequest.TraceId = context.TraceIdentifier;

            await badRequest.ToResult(context).ExecuteAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
            Persist(context, ex, "Error", StatusCodes.Status500InternalServerError);

            var message = environment.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            var result = OperationResult.Fail(message, HttpStatusCode.InternalServerError);
            result.TraceId = context.TraceIdentifier;

            await result.ToResult(context).ExecuteAsync(context);
        }
    }
}
