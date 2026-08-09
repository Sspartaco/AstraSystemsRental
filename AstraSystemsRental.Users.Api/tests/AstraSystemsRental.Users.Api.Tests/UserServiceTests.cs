using System.Net;
using AstraSystemsRental.Users.Api.Configuration;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Security;
using AstraSystemsRental.Users.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace AstraSystemsRental.Users.Api.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IMailClient> _mailClient = new();
    private readonly Mock<AstraSystemsRental.Base.Security.IAstraRequestContext> _requestContext = new();
    private readonly IPasswordHasher _passwordHasher = new PasswordHasher();

    private UserService CreateService() => new(
        _repository.Object,
        _passwordHasher,
        _mailClient.Object,
        _requestContext.Object,
        Options.Create(new ConfirmationOptions()));

    private static CreateUserRequest ValidRequest() => new(
        "Jane", "Doe", "123 St", PersonType.Natural, "CC123", null, "jane@codalea.app");

    [Fact(DisplayName = "Create fails with 400 when person type is invalid")]
    public async Task Create_InvalidPersonType_Fails()
    {
        // Arrange
        var service = CreateService();
        var request = ValidRequest() with { PersonType = "Alien" };

        // Act
        var result = await service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Errors.Should().Contain(e => e.Contains("PersonType"));
    }

    [Fact(DisplayName = "Create returns 409 when email already exists")]
    public async Task Create_DuplicateEmail_Conflicts()
    {
        // Arrange
        _repository.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = CreateService();

        // Act
        var result = await service.CreateAsync(ValidRequest(), CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Create provisions user, subscription and sends welcome email")]
    public async Task Create_ValidRequest_CreatesAndSendsMail()
    {
        // Arrange
        _repository.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(r => r.GetRoleByCodeAsync("Demo", It.IsAny<CancellationToken>())).ReturnsAsync(new Role { Id = 2, Code = "Demo", Name = "Demo user", IsActive = true });
        _repository.Setup(r => r.GetPlanByCodeAsync("Demo", It.IsAny<CancellationToken>())).ReturnsAsync(new Plan { Id = 1, Code = "Demo", Name = "Demo plan", DurationDays = 3, IsActive = true });
        _repository.Setup(r => r.CreateUserWithConfirmationAsync(
                It.IsAny<CreateUserData>(), It.IsAny<int>(), It.IsAny<Plan>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100L);
        _mailClient.Setup(m => m.SendWelcomeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        string? tokenSentToRepo = null;
        _repository.Setup(r => r.CreateUserWithConfirmationAsync(
                It.IsAny<CreateUserData>(), It.IsAny<int>(), It.IsAny<Plan>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<CreateUserData, int, Plan, string, DateTime, CancellationToken>((_, _, _, token, _, _) => tokenSentToRepo = token)
            .ReturnsAsync(100L);
        var service = CreateService();

        // Act
        var result = await service.CreateAsync(ValidRequest(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        tokenSentToRepo.Should().NotBeNullOrEmpty();
        _mailClient.Verify(m => m.SendWelcomeAsync("jane@codalea.app", "Jane Doe",
            It.Is<string>(u => u.Contains(tokenSentToRepo!)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Confirm fails when token is invalid or expired")]
    public async Task Confirm_InvalidToken_Fails()
    {
        // Arrange
        _repository.Setup(r => r.ConfirmAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long?)null);
        var service = CreateService();

        // Act
        var result = await service.ConfirmAsync(new ConfirmEmailRequest("bad", "password123"), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("token"));
    }

    [Fact(DisplayName = "Confirm rejects short passwords")]
    public async Task Confirm_ShortPassword_Fails()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ConfirmAsync(new ConfirmEmailRequest("token", "123"), CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Errors.Should().Contain(e => e.Contains("8 characters"));
    }
}
