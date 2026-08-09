using System.Threading.RateLimiting;
using AstraSystemsRental.Base.Diagnostics;
using AstraSystemsRental.Base.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace AstraSystemsRental.Base.Api;

public static class AstraApiExtensions
{
    public static WebApplicationBuilder AddAstraApi(this WebApplicationBuilder builder, AstraApiOptions options)
    {
        builder.Services.AddSingleton(options);
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAstraRequestContext, AstraRequestContext>();

        var logOptions = new ApplicationLogOptions { ServiceName = options.ServiceName };
        builder.Configuration.GetSection(ApplicationLogOptions.SectionName).Bind(logOptions);
        logOptions.ServiceName = options.ServiceName;

        if (string.IsNullOrWhiteSpace(logOptions.ConnectionString))
            logOptions.ConnectionString = builder.Configuration.GetConnectionString("Default") ?? string.Empty;

        builder.Services.AddSingleton(logOptions);
        builder.Services.AddSingleton<ApplicationLogWriter>();
        builder.Services.AddSingleton<IApplicationLogWriter>(sp => sp.GetRequiredService<ApplicationLogWriter>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ApplicationLogWriter>());

        if (options.EnableJwtAuthentication)
            builder.Services.AddAstraJwtAuthentication(builder.Configuration);

        if (options.EnableRateLimiting)
        {
            builder.Services.AddRateLimiter(rate =>
            {
                rate.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                rate.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ResolvePartitionKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.RateLimitPermitPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });
        }

        return builder;
    }

    private static string ResolvePartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirst(AstraClaims.UserId)?.Value;

        if (!string.IsNullOrEmpty(userId))
            return $"user:{userId}";

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var first = forwardedFor.Split(',')[0].Trim();

            if (first.Length > 0)
                return $"ip:{first}";
        }

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    public static WebApplication UseAstraPipeline(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<AstraApiOptions>();

        app.UsePathBase(options.PathBase);
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        if (!app.Environment.IsProduction())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(scalar =>
            {
                scalar
                    .WithTitle($"{options.ServiceName} API")
                    .WithTheme(ScalarTheme.Moon)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        }

        if (options.EnableJwtAuthentication)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        if (options.EnableRateLimiting)
            app.UseRateLimiter();

        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = options.ServiceName }))
            .WithTags("Health")
            .ExcludeFromDescription();

        return app;
    }
}
