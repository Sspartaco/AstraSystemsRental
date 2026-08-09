using System.Net;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Services;
using FluentAssertions;
using Moq;

namespace AstraSystemsRental.Users.Api.Tests;

public class CatalogServiceTests
{
    private readonly Mock<ICatalogRepository> _repository = new();
    private CatalogService CreateService() => new(_repository.Object);

    [Fact(DisplayName = "Create plan fails when code already exists")]
    public async Task CreatePlan_DuplicateCode_Conflicts()
    {
        _repository.Setup(r => r.PlanCodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await CreateService().CreatePlanAsync(new CreatePlanRequest("Basic", "Basic", 30), CancellationToken.None);
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Create plan rejects non-positive duration")]
    public async Task CreatePlan_BadDuration_Fails()
    {
        var result = await CreateService().CreatePlanAsync(new CreatePlanRequest("Pro", "Pro", 0), CancellationToken.None);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Assign node to plan fails when node is not in catalog")]
    public async Task SetPlanNode_UnknownNode_Fails()
    {
        _repository.Setup(r => r.PlanExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.NodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var result = await CreateService().SetPlanNodeAsync(1, new SetNodeRequest("ghost", true), CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact(DisplayName = "Assign node to plan succeeds for a valid node")]
    public async Task SetPlanNode_Valid_Ok()
    {
        _repository.Setup(r => r.PlanExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.NodeExistsAsync("fleet", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.SetPlanNodeAsync(1, "fleet", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await CreateService().SetPlanNodeAsync(1, new SetNodeRequest("fleet", true), CancellationToken.None);
        result.Success.Should().BeTrue();
        _repository.Verify(r => r.SetPlanNodeAsync(1, "fleet", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Assign node to role fails when role not found")]
    public async Task SetRoleNode_MissingRole_NotFound()
    {
        _repository.Setup(r => r.RoleExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var result = await CreateService().SetRoleNodeAsync(9, new SetNodeRequest("reports", true), CancellationToken.None);
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Create node rejects duplicate key")]
    public async Task CreateNode_Duplicate_Conflicts()
    {
        _repository.Setup(r => r.NodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await CreateService().CreateNodeAsync(new CreateNodeRequest("fleet", "Flota", null, null, 0), CancellationToken.None);
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
