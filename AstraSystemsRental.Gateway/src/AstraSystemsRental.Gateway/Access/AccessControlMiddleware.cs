using System.Security.Claims;
using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Base.Security;

namespace AstraSystemsRental.Gateway.Access;

public sealed class AccessControlMiddleware(RequestDelegate next, IAccessEvaluator evaluator)
{
    private const string RequestedNodeHeader = "X-Astra-Node";
    private static readonly string[] BypassPrefixes = ["/health", "/scalar", "/openapi", "/apiUsers/auth", "/apiUsers/users"];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (BypassPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var user = context.User;
        var accessContext = BuildContext(user, context);
        var decision = evaluator.Evaluate(accessContext, DateTimeOffset.UtcNow);

        if (decision.IsAllowed)
        {
            await next(context);
            return;
        }

        var result = decision.Outcome switch
        {
            AccessOutcome.Unauthenticated => OperationResult.Unauthorized(decision.Reason),
            _ => OperationResult.Forbidden(decision.Reason)
        };
        result.TraceId = context.TraceIdentifier;
        await result.ToResult(context).ExecuteAsync(context);
    }

    private static AccessContext BuildContext(ClaimsPrincipal user, HttpContext context)
    {
        var isAuthenticated = user.Identity?.IsAuthenticated ?? false;
        var role = user.FindFirst(AstraClaims.Role)?.Value;
        var plan = user.FindFirst(AstraClaims.Plan)?.Value;

        DateTimeOffset? subscriptionEnd = null;
        var subEnd = user.FindFirst(AstraClaims.SubscriptionEnd)?.Value;
        if (long.TryParse(subEnd, out var unix))
            subscriptionEnd = DateTimeOffset.FromUnixTimeSeconds(unix);

        var allowedNodes = user.FindAll(AstraClaims.Node).Select(c => c.Value).ToHashSet(StringComparer.Ordinal);

        string? requestedNode = context.Request.Headers.TryGetValue(RequestedNodeHeader, out var header)
            ? header.ToString()
            : null;

        return new AccessContext(isAuthenticated, role, plan, subscriptionEnd, allowedNodes, requestedNode);
    }
}
