using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Users.Api.Endpoints;

public static class AuthEndpoints
{
    public static void AuthEndpoints_Map(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
                [FromBody] LoginRequest request,
                [FromServices] IAuthService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.LoginAsync(request, cancellationToken);
                return result.ToResult(context);
            })
            .WithName("Login")
            .WithSummary("Authenticates a user and issues an RS256 access token")
            .WithDescription("Validates credentials, confirmation status and subscription window, then returns a signed JWT.")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .WithTags("Auth")
            .AllowAnonymous();

        app.MapPost("/auth/refresh", async (
                [FromBody] RefreshTokenRequest request,
                [FromServices] IAuthService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.RefreshAsync(request, cancellationToken)).ToResult(context))
            .WithName("RefreshToken")
            .WithSummary("Exchanges a valid refresh token for a new access token, rotating the refresh token")
            .WithDescription("Rotates the refresh token on every use. Reusing an already-rotated token revokes the whole family.")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .WithTags("Auth")
            .AllowAnonymous();

        app.MapPost("/auth/logout", async (
                [FromBody] RefreshTokenRequest request,
                [FromServices] IAuthService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.LogoutAsync(request, cancellationToken)).ToResult(context))
            .WithName("Logout")
            .WithSummary("Revokes the supplied refresh token")
            .Produces<ApiResponse>(StatusCodes.Status204NoContent)
            .WithTags("Auth")
            .AllowAnonymous();
    }
}
