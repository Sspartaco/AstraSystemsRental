using System.Net;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Security;
using AstraSystemsRental.Users.Api.Services;
using FluentAssertions;
using Moq;

namespace AstraSystemsRental.Users.Api.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IJwtTokenIssuer> _tokenIssuer = new();
    private readonly Mock<IRefreshTokenService> _refreshTokens = new();
    private readonly IPasswordHasher _passwordHasher = new PasswordHasher();

    public AuthServiceTests()
    {
        _repository.Setup(r => r.GetAllowedNodesAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "dashboard" });

        _repository.Setup(r => r.GetMemberCompanyIdsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());

        _refreshTokens.Setup(r => r.IssueAsync(It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IssuedRefreshToken("refresh-token", DateTime.UtcNow.AddDays(30)));
    }

    private AuthService CreateService() => new(_repository.Object, _passwordHasher, _tokenIssuer.Object, _refreshTokens.Object);

    private LoginProjection ProjectionFor(string password, bool confirmed, string role, DateTime? subEnd)
    {
        var (hash, salt) = _passwordHasher.Hash(password);
        return new LoginProjection(1, "user@codalea.app", hash, salt, confirmed, role, "Basic", subEnd);
    }

    [Fact(DisplayName = "Login returns 401 for unknown user")]
    public async Task Login_UnknownUser_Unauthorized()
    {
        // Arrange
        _repository.Setup(r => r.GetLoginProjectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginProjection?)null);
        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequest("user@codalea.app", "password123"), CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Login returns 401 for wrong password")]
    public async Task Login_WrongPassword_Unauthorized()
    {
        // Arrange
        _repository.Setup(r => r.GetLoginProjectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFor("correct-password", true, RoleCode.Standard, DateTime.UtcNow.AddDays(10)));
        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequest("user@codalea.app", "wrong-password"), CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Login returns 403 when account is not confirmed")]
    public async Task Login_Unconfirmed_Forbidden()
    {
        // Arrange
        _repository.Setup(r => r.GetLoginProjectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFor("password123", false, RoleCode.Standard, DateTime.UtcNow.AddDays(10)));
        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequest("user@codalea.app", "password123"), CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Errors.Should().Contain(e => e.Contains("not confirmed"));
    }

    [Fact(DisplayName = "Login returns 403 when subscription expired for standard user")]
    public async Task Login_ExpiredSubscription_Forbidden()
    {
        // Arrange
        _repository.Setup(r => r.GetLoginProjectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFor("password123", true, RoleCode.Standard, DateTime.UtcNow.AddDays(-1)));
        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequest("user@codalea.app", "password123"), CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Errors.Should().Contain(e => e.Contains("expired"));
    }

    [Fact(DisplayName = "Login bypasses subscription expiry for super user")]
    public async Task Login_SuperUserExpired_IssuesToken()
    {
        // Arrange
        _repository.Setup(r => r.GetLoginProjectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFor("password123", true, RoleCode.SuperUser, DateTime.UtcNow.AddDays(-100)));
        _tokenIssuer.Setup(t => t.Issue(It.IsAny<IEnumerable<System.Security.Claims.Claim>>(), It.IsAny<TimeSpan?>()))
            .Returns("signed-token");
        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequest("user@codalea.app", "password123"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _tokenIssuer.Verify(t => t.Issue(It.IsAny<IEnumerable<System.Security.Claims.Claim>>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact(DisplayName = "Login issues token for valid confirmed active user")]
    public async Task Login_Valid_IssuesToken()
    {
        // Arrange
        _repository.Setup(r => r.GetLoginProjectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectionFor("password123", true, RoleCode.Standard, DateTime.UtcNow.AddDays(10)));
        _tokenIssuer.Setup(t => t.Issue(It.IsAny<IEnumerable<System.Security.Claims.Claim>>(), It.IsAny<TimeSpan?>()))
            .Returns("signed-token");
        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequest("user@codalea.app", "password123"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
