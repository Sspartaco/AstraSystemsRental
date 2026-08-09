using AstraSystemsRental.Users.Api.Configuration;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Security;
using AstraSystemsRental.Users.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace AstraSystemsRental.Users.Api.Tests;

public class BootstrapServiceTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly IPasswordHasher _passwordHasher = new PasswordHasher();
    private const string ValidSecret = "this-is-a-long-bootstrap-secret-value";

    private BootstrapService CreateService(string secret = ValidSecret) =>
        new(_repository.Object, _passwordHasher, Options.Create(new BootstrapOptions { Secret = secret }));

    private static SeedSuperUserData Data(string secret = ValidSecret, string password = "Sup3rSecretPass!") =>
        new("Root", "Admin", "DOC1", "root@codalea.app", password, secret);

    [Fact(DisplayName = "Rejects when provided secret does not match")]
    public async Task WrongSecret_Rejected()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SeedSuperUserAsync(Data(secret: "wrong-secret-but-long-enough-value"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        _repository.Verify(r => r.CreateSuperUserAsync(It.IsAny<CreateUserData>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Rejects when configured secret is missing or too short")]
    public async Task ShortConfiguredSecret_Rejected()
    {
        // Arrange
        var service = CreateService(secret: "short");

        // Act
        var result = await service.SeedSuperUserAsync(Data(secret: "short"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Refuses to create a second SuperUser (idempotent lock)")]
    public async Task SuperUserExists_Refused()
    {
        // Arrange
        _repository.Setup(r => r.SuperUserExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = CreateService();

        // Act
        var result = await service.SeedSuperUserAsync(Data(), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        _repository.Verify(r => r.CreateSuperUserAsync(It.IsAny<CreateUserData>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Rejects a weak password")]
    public async Task WeakPassword_Rejected()
    {
        // Arrange
        _repository.Setup(r => r.SuperUserExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = CreateService();

        // Act
        var result = await service.SeedSuperUserAsync(Data(password: "short"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Creates the SuperUser when secret matches and none exists")]
    public async Task ValidBootstrap_Creates()
    {
        // Arrange
        _repository.Setup(r => r.SuperUserExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(r => r.CreateSuperUserAsync(It.IsAny<CreateUserData>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        var service = CreateService();

        // Act
        var result = await service.SeedSuperUserAsync(Data(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
    }
}
