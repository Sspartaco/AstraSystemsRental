using System.Net;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Services;
using FluentAssertions;
using Moq;

namespace AstraSystemsRental.Users.Api.Tests;

public class RoleServiceTests
{
    private readonly Mock<IUserRepository> _repository = new();

    private RoleService CreateService() => new(_repository.Object);

    [Fact(DisplayName = "Rejects an unknown role code")]
    public async Task UnknownRole_Rejected()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.AssignRoleAsync(1, "Wizard", CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _repository.Verify(r => r.SetRoleAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Returns 404 when the user does not exist")]
    public async Task MissingUser_NotFound()
    {
        // Arrange
        _repository.Setup(r => r.SetRoleAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = CreateService();

        // Act
        var result = await service.AssignRoleAsync(99, RoleCode.SuperUser, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Assigns a valid role to an existing user")]
    public async Task ValidAssignment_Succeeds()
    {
        // Arrange
        _repository.Setup(r => r.SetRoleAsync(5, RoleCode.SuperUser, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = CreateService();

        // Act
        var result = await service.AssignRoleAsync(5, RoleCode.SuperUser, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }
}
