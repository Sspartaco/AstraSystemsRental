using System.Security.Cryptography;
using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Persistence;

namespace AstraSystemsRental.Users.Api.Services;

public sealed record IssuedRefreshToken(string Token, DateTime ExpiresAtUtc);

public interface IRefreshTokenService
{
    Task<IssuedRefreshToken> IssueAsync(long userId, string? deviceInfo, CancellationToken cancellationToken);
    Task<RefreshToken?> ValidateAsync(string token, DateTime nowUtc, CancellationToken cancellationToken);
    Task<IssuedRefreshToken> RotateAsync(RefreshToken current, string? deviceInfo, CancellationToken cancellationToken);
    Task<bool> RevokeAsync(string token, CancellationToken cancellationToken);
}

public sealed class RefreshTokenService(IRefreshTokenRepository repository) : IRefreshTokenService
{
    public const int LifetimeDays = 30;

    public async Task<IssuedRefreshToken> IssueAsync(long userId, string? deviceInfo, CancellationToken cancellationToken)
    {
        var token = GenerateToken();
        var nowUtc = DateTime.UtcNow;

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(token),
            ExpiresAtUtc = nowUtc.AddDays(LifetimeDays),
            DeviceInfo = Truncate(deviceInfo, 200),
            CreatedAtUtc = nowUtc
        };

        await repository.AddAsync(entity, cancellationToken);

        return new IssuedRefreshToken(token, entity.ExpiresAtUtc);
    }

    public async Task<RefreshToken?> ValidateAsync(string token, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var stored = await repository.GetByHashAsync(Hash(token), cancellationToken);

        if (stored is null)
            return null;

        if (stored.RevokedAtUtc is not null)
        {
            await repository.RevokeAllForUserAsync(stored.UserId, nowUtc, cancellationToken);
            return null;
        }

        return stored.IsActive(nowUtc) ? stored : null;
    }

    public async Task<IssuedRefreshToken> RotateAsync(RefreshToken current, string? deviceInfo, CancellationToken cancellationToken)
    {
        var token = GenerateToken();
        var nowUtc = DateTime.UtcNow;
        var hash = Hash(token);

        var replacement = new RefreshToken
        {
            UserId = current.UserId,
            TokenHash = hash,
            ExpiresAtUtc = nowUtc.AddDays(LifetimeDays),
            DeviceInfo = Truncate(deviceInfo, 200) ?? current.DeviceInfo,
            CreatedAtUtc = nowUtc
        };

        await repository.ReplaceAsync(current, replacement, nowUtc, cancellationToken);

        return new IssuedRefreshToken(token, replacement.ExpiresAtUtc);
    }

    public Task<bool> RevokeAsync(string token, CancellationToken cancellationToken)
        => string.IsNullOrWhiteSpace(token)
            ? Task.FromResult(false)
            : repository.RevokeByHashAsync(Hash(token), DateTime.UtcNow, cancellationToken);

    private static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static byte[] Hash(string token)
        => SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));

    private static string? Truncate(string? value, int max)
        => value is null || value.Length <= max ? value : value[..max];
}
