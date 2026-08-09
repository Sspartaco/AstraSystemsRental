using System.Net;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Services;
using FluentAssertions;
using Moq;

namespace AstraSystemsRental.Users.Api.Tests;

public class CompanyServiceTests
{
    private readonly Mock<ICompanyRepository> _repository = new();
    private CompanyService CreateService() => new(_repository.Object);

    [Fact(DisplayName = "Create company fails when document already exists")]
    public async Task Create_DuplicateDocument_Conflicts()
    {
        _repository.Setup(r => r.DocumentExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await CreateService().CreateAsync(new CreateCompanyRequest("Astra Ltd", "NIT900", null), CancellationToken.None);
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Create company succeeds with valid data")]
    public async Task Create_Valid_Created()
    {
        _repository.Setup(r => r.DocumentExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(7L);
        var result = await CreateService().CreateAsync(new CreateCompanyRequest("Astra Ltd", "NIT900", "ops@astra.app"), CancellationToken.None);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact(DisplayName = "Assign member fails when user already belongs to the company")]
    public async Task Assign_AlreadyMember_Conflicts()
    {
        _repository.Setup(r => r.CompanyExistsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.UserExistsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.IsMemberAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await CreateService().AssignMemberAsync(1, new AssignMemberRequest(5, false), CancellationToken.None);
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Assign member succeeds for a new member")]
    public async Task Assign_NewMember_Ok()
    {
        _repository.Setup(r => r.CompanyExistsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.UserExistsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.IsMemberAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var result = await CreateService().AssignMemberAsync(1, new AssignMemberRequest(5, true), CancellationToken.None);
        result.Success.Should().BeTrue();
        _repository.Verify(r => r.AssignMemberAsync(1, 5, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Company subscription fails when plan is unknown")]
    public async Task Subscription_UnknownPlan_Fails()
    {
        _repository.Setup(r => r.CompanyExistsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.GetPlanByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Plan?)null);
        var result = await CreateService().CreateSubscriptionAsync(1, new CreateCompanySubscriptionRequest("Ghost"), CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact(DisplayName = "Company subscription is created with an active plan")]
    public async Task Subscription_ValidPlan_Created()
    {
        _repository.Setup(r => r.CompanyExistsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.GetPlanByCodeAsync("Basic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plan { Id = 2, Code = "Basic", Name = "Basic", DurationDays = 30, IsActive = true });
        _repository.Setup(r => r.CreateSubscriptionAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(99L);
        var result = await CreateService().CreateSubscriptionAsync(1, new CreateCompanySubscriptionRequest("Basic"), CancellationToken.None);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
