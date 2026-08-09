using System.Net;
using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;

namespace AstraSystemsRental.Mail.Api.Security;

public sealed class InternalApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string HeaderName = "X-Internal-Api-Key";
    private readonly string? _expectedKey = configuration["Internal:ApiKey"];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Contains("/health") || path.Contains("/scalar") || path.Contains("/openapi"))
        {
            await next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_expectedKey) ||
            !context.Request.Headers.TryGetValue(HeaderName, out var provided) ||
            !string.Equals(provided.ToString(), _expectedKey, StringComparison.Ordinal))
        {
            var result = OperationResult.Unauthorized("Invalid or missing internal API key.");
            result.TraceId = context.TraceIdentifier;
            await result.ToResult(context).ExecuteAsync(context);
            return;
        }

        await next(context);
    }
}
