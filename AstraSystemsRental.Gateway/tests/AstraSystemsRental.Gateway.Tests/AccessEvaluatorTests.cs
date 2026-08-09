using AstraSystemsRental.Gateway.Access;
using FluentAssertions;

namespace AstraSystemsRental.Gateway.Tests;

public class AccessEvaluatorTests
{
    private readonly AccessEvaluator _evaluator = new();
    private readonly DateTimeOffset _now = new(2026, 7, 12, 0, 0, 0, TimeSpan.Zero);

    private static AccessContext Context(
        bool authenticated = true,
        string? role = "Standard",
        string? plan = "Basic",
        DateTimeOffset? subEnd = null,
        IEnumerable<string>? nodes = null,
        string? requestedNode = null) =>
        new(authenticated, role, plan, subEnd,
            (nodes ?? []).ToHashSet(StringComparer.Ordinal), requestedNode);

    [Fact(DisplayName = "Unauthenticated request is rejected")]
    public void Unauthenticated_Rejected()
    {
        // Act
        var result = _evaluator.Evaluate(Context(authenticated: false), _now);

        // Assert
        result.Outcome.Should().Be(AccessOutcome.Unauthenticated);
    }

    [Fact(DisplayName = "Super user bypasses subscription and node checks")]
    public void SuperUser_AlwaysAllowed()
    {
        // Arrange
        var context = Context(role: "SuperUser", subEnd: _now.AddDays(-100), requestedNode: "anything");

        // Act
        var result = _evaluator.Evaluate(context, _now);

        // Assert
        result.IsAllowed.Should().BeTrue();
    }

    [Fact(DisplayName = "Expired subscription is rejected for standard user")]
    public void ExpiredSubscription_Rejected()
    {
        // Arrange
        var context = Context(subEnd: _now.AddSeconds(-1), nodes: ["dashboard"], requestedNode: "dashboard");

        // Act
        var result = _evaluator.Evaluate(context, _now);

        // Assert
        result.Outcome.Should().Be(AccessOutcome.SubscriptionExpired);
    }

    [Fact(DisplayName = "Requested node not in allowed set is forbidden")]
    public void ForbiddenNode_Rejected()
    {
        // Arrange
        var context = Context(subEnd: _now.AddDays(10), nodes: ["dashboard"], requestedNode: "reports");

        // Act
        var result = _evaluator.Evaluate(context, _now);

        // Assert
        result.Outcome.Should().Be(AccessOutcome.NodeForbidden);
    }

    [Fact(DisplayName = "Allowed node within active subscription is granted")]
    public void AllowedNode_Granted()
    {
        // Arrange
        var context = Context(subEnd: _now.AddDays(10), nodes: ["dashboard", "reports"], requestedNode: "reports");

        // Act
        var result = _evaluator.Evaluate(context, _now);

        // Assert
        result.IsAllowed.Should().BeTrue();
    }

    [Fact(DisplayName = "Wildcard node grants access to any node")]
    public void WildcardNode_Granted()
    {
        // Arrange
        var context = Context(subEnd: _now.AddDays(10), nodes: ["*"], requestedNode: "fleet");

        // Act
        var result = _evaluator.Evaluate(context, _now);

        // Assert
        result.IsAllowed.Should().BeTrue();
    }

    [Fact(DisplayName = "Request without a specific node only requires an active subscription")]
    public void NoRequestedNode_Granted()
    {
        // Arrange
        var context = Context(subEnd: _now.AddDays(10), nodes: ["dashboard"], requestedNode: null);

        // Act
        var result = _evaluator.Evaluate(context, _now);

        // Assert
        result.IsAllowed.Should().BeTrue();
    }
}
