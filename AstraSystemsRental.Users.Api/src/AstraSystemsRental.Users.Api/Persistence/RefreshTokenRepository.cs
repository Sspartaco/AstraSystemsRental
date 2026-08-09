using AstraSystemsRental.Users.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Users.Api.Persistence;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);
    Task<RefreshToken?> GetByHashAsync(byte[] tokenHash, CancellationToken cancellationToken);
    Task ReplaceAsync(RefreshToken current, RefreshToken replacement, DateTime nowUtc, CancellationToken cancellationToken);
    Task<bool> RevokeByHashAsync(byte[] tokenHash, DateTime nowUtc, CancellationToken cancellationToken);
    Task RevokeAllForUserAsync(long userId, DateTime nowUtc, CancellationToken cancellationToken);
}

public sealed class RefreshTokenRepository(AstraUsersDbContext context) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        await context.RefreshTokens.AddAsync(token, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> GetByHashAsync(byte[] tokenHash, CancellationToken cancellationToken)
        => context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task ReplaceAsync(RefreshToken current, RefreshToken replacement, DateTime nowUtc, CancellationToken cancellationToken)
    {
        current.RevokedAtUtc = nowUtc;
        current.ReplacedByTokenHash = replacement.TokenHash;

        await context.RefreshTokens.AddAsync(replacement, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RevokeByHashAsync(byte[] tokenHash, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var token = await context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.RevokedAtUtc is not null)
            return false;

        token.RevokedAtUtc = nowUtc;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllForUserAsync(long userId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAtUtc, nowUtc), cancellationToken);
    }
}
