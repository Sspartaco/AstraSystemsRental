using AstraSystemsRental.Front.Services;

namespace AstraSystemsRental.Front.Shared.Security;

public sealed class AstraSessionMiddleware(RequestDelegate next)
{
    private static readonly string[] AnonymousPaths =
        ["/auth", "/login", "/css", "/js", "/lib", "/favicon", "/health"];

    public async Task InvokeAsync(HttpContext context, ISessionService session, ICurrentUser currentUser)
    {
        var token = session.GetToken();
        var path = context.Request.Path.Value ?? string.Empty;
        var isAnonymousPath = AnonymousPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(token))
        {
            var principal = JwtReader.Read(token);
            ((CurrentUser)currentUser).Principal = principal;

            if (principal.SubscriptionExpired && !isAnonymousPath && !path.StartsWith("/subscription", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/subscription/expired");
                return;
            }
        }
        else if (!isAnonymousPath && path != "/")
        {
            context.Response.Redirect("/auth/login");
            return;
        }

        await next(context);
    }
}
