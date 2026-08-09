using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Services;
using FluentAssertions;
using Moq;

namespace AstraSystemsRental.Users.Api.Tests;

public class RefreshTokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _repository = new();

    private RefreshTokenService CreateService() => new(_repository.Object);

    private static RefreshToken StoredToken(DateTime expiresAtUtc, DateTime? revokedAtUtc = null) => new()
    {
        Id = 1,
        UserId = 42,
        TokenHash = new byte[32],
        ExpiresAtUtc = expiresAtUtc,
        RevokedAtUtc = revokedAtUtc,
        CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
    };

    [Fact(DisplayName = "Issue stores a hash, never the raw token")]
    public async Task Issue_StoresHash()
    {
        RefreshToken? captured = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => captured = token)
            .Returns(Task.CompletedTask);

        var result = await CreateService().IssueAsync(42, "Android", CancellationToken.None);

        result.Token.Should().NotBeNullOrWhiteSpace();
        captured.Should().NotBeNull();
        captured!.TokenHash.Should().HaveCount(32);
        captured.UserId.Should().Be(42);
        captured.DeviceInfo.Should().Be("Android");
        System.Text.Encoding.UTF8.GetString(captured.TokenHash).Should().NotBe(result.Token);
    }

    [Fact(DisplayName = "Validate returns null when the token is unknown")]
    public async Task Validate_UnknownToken_ReturnsNull()
    {
        _repository.Setup(r => r.GetByHashAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await CreateService().ValidateAsync("whatever", DateTime.UtcNow, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Validate returns null when the token is expired")]
    public async Task Validate_Expired_ReturnsNull()
    {
        _repository.Setup(r => r.GetByHashAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoredToken(DateTime.UtcNow.AddMinutes(-1)));

        var result = await CreateService().ValidateAsync("expired", DateTime.UtcNow, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Reusing a revoked token revokes the whole family")]
    public async Task Validate_RevokedToken_RevokesFamily()
    {
        var nowUtc = DateTime.UtcNow;
        _repository.Setup(r => r.GetByHashAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoredToken(nowUtc.AddDays(10), revokedAtUtc: nowUtc.AddMinutes(-5)));

        var result = await CreateService().ValidateAsync("stolen", nowUtc, CancellationToken.None);

        result.Should().BeNull();
        _repository.Verify(r => r.RevokeAllForUserAsync(42, nowUtc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Validate returns the token when it is active")]
    public async Task Validate_Active_ReturnsToken()
    {
        var stored = StoredToken(DateTime.UtcNow.AddDays(10));
        _repository.Setup(r => r.GetByHashAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var result = await CreateService().ValidateAsync("valid", DateTime.UtcNow, CancellationToken.None);

        result.Should().BeSameAs(stored);
        _repository.Verify(r => r.RevokeAllForUserAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Rotate revokes the current token and links the replacement")]
    public async Task Rotate_LinksReplacement()
    {
        var current = StoredToken(DateTime.UtcNow.AddDays(10));
        RefreshToken? replacement = null;

        _repository.Setup(r => r.ReplaceAsync(It.IsAny<RefreshToken>(), It.IsAny<RefreshToken>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, RefreshToken, DateTime, CancellationToken>((_, next, _, _) => replacement = next)
            .Returns(Task.CompletedTask);

        var result = await CreateService().RotateAsync(current, "Android", CancellationToken.None);

        result.Token.Should().NotBeNullOrWhiteSpace();
        replacement.Should().NotBeNull();
        replacement!.UserId.Should().Be(current.UserId);
        replacement.TokenHash.Should().NotEqual(current.TokenHash);
    }

    [Fact(DisplayName = "Revoke returns false for an empty token")]
    public async Task Revoke_EmptyToken_ReturnsFalse()
    {
        var result = await CreateService().RevokeAsync(string.Empty, CancellationToken.None);

        result.Should().BeFalse();
        _repository.Verify(r => r.RevokeByHashAsync(It.IsAny<byte[]>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
