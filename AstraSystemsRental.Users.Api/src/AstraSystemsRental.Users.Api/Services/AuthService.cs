using System.Security.Claims;
using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Security;

namespace AstraSystemsRental.Users.Api.Services;

public sealed class AuthService(
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    IJwtTokenIssuer tokenIssuer,
    IRefreshTokenService refreshTokens) : IAuthService
{
    public async Task<OperationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard()
            .Email(request.Email, "Email")
            .NotEmpty(request.Password, "Password");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var projection = await repository.GetLoginProjectionAsync(request.Email.Trim(), cancellationToken);
        if (projection is null || projection.PasswordHash is null || projection.PasswordSalt is null)
            return OperationResult.Unauthorized("Invalid credentials.");

        if (!passwordHasher.Verify(request.Password, projection.PasswordHash, projection.PasswordSalt))
            return OperationResult.Unauthorized("Invalid credentials.");

        if (!projection.IsConfirmed)
            return OperationResult.Forbidden("Account is not confirmed.");

        var isSuperUser = string.Equals(projection.RoleCode, RoleCode.SuperUser, StringComparison.Ordinal);
        if (!isSuperUser && projection.SubscriptionEndsAtUtc is { } endsAt && endsAt <= DateTime.UtcNow)
            return OperationResult.Forbidden("Subscription has expired.");

        var claims = await BuildClaimsAsync(projection, cancellationToken);

        var token = tokenIssuer.Issue(claims);
        var refresh = await refreshTokens.IssueAsync(projection.UserId, request.DeviceInfo, cancellationToken);

        return OperationResult.Ok(new
        {
            accessToken = token,
            refreshToken = refresh.Token,
            refreshTokenExpiresAtUtc = refresh.ExpiresAtUtc,
            tokenType = "Bearer",
            role = projection.RoleCode,
            plan = projection.PlanCode
        });
    }

    public async Task<OperationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return OperationResult.Unauthorized("A refresh token is required.");

        var nowUtc = DateTime.UtcNow;
        var stored = await refreshTokens.ValidateAsync(request.RefreshToken, nowUtc, cancellationToken);

        if (stored is null)
            return OperationResult.Unauthorized("The refresh token is invalid or expired.");

        var projection = await repository.GetLoginProjectionByIdAsync(stored.UserId, cancellationToken);

        if (projection is null)
            return OperationResult.Unauthorized("The account is no longer active.");

        var isSuperUser = string.Equals(projection.RoleCode, RoleCode.SuperUser, StringComparison.Ordinal);
        if (!isSuperUser && projection.SubscriptionEndsAtUtc is { } endsAt && endsAt <= nowUtc)
            return OperationResult.Forbidden("Subscription has expired.");

        var claims = await BuildClaimsAsync(projection, cancellationToken);
        var token = tokenIssuer.Issue(claims);
        var rotated = await refreshTokens.RotateAsync(stored, request.DeviceInfo, cancellationToken);

        return OperationResult.Ok(new
        {
            accessToken = token,
            refreshToken = rotated.Token,
            refreshTokenExpiresAtUtc = rotated.ExpiresAtUtc,
            tokenType = "Bearer",
            role = projection.RoleCode,
            plan = projection.PlanCode
        });
    }

    public async Task<OperationResult> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await refreshTokens.RevokeAsync(request.RefreshToken ?? string.Empty, cancellationToken);
        return OperationResult.NoContent();
    }

    private async Task<List<Claim>> BuildClaimsAsync(LoginProjection projection, CancellationToken cancellationToken)
    {
        var claims = new List<Claim>
        {
            new(AstraClaims.UserId, projection.UserId.ToString()),
            new(AstraClaims.Email, projection.Email),
            new(AstraClaims.Role, projection.RoleCode)
        };

        if (!string.IsNullOrWhiteSpace(projection.PlanCode))
            claims.Add(new Claim(AstraClaims.Plan, projection.PlanCode));

        if (projection.SubscriptionEndsAtUtc is { } end)
            claims.Add(new Claim(AstraClaims.SubscriptionEnd, new DateTimeOffset(end, TimeSpan.Zero).ToUnixTimeSeconds().ToString()));

        var allowedNodes = await repository.GetAllowedNodesAsync(projection.UserId, cancellationToken);
        foreach (var node in allowedNodes)
            claims.Add(new Claim(AstraClaims.Node, node));

        var memberCompanyIds = await repository.GetMemberCompanyIdsAsync(projection.UserId, cancellationToken);
        foreach (var companyId in memberCompanyIds)
            claims.Add(new Claim(AstraClaims.Company, companyId.ToString()));

        return claims;
    }
}
