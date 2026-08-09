using AstraSystemsRental.Front.Shared.Security;
using FluentAssertions;

namespace AstraSystemsRental.Front.Tests;

public class AuthorizationTests
{
    private static AstraPrincipal Principal(string role, IEnumerable<string> nodes, DateTimeOffset? subEnd = null) => new()
    {
        UserId = "1",
        Email = "user@codalea.app",
        Role = role,
        Plan = "Demo",
        Nodes = nodes.ToHashSet(StringComparer.Ordinal),
        SubscriptionEnd = subEnd
    };

    [Fact(DisplayName = "SuperUser has access to any node")]
    public void SuperUser_HasAllNodes()
    {
        var p = Principal("SuperUser", []);
        p.HasNode("fleet").Should().BeTrue();
        p.HasNode("anything").Should().BeTrue();
    }

    [Fact(DisplayName = "Wildcard node grants access to any node")]
    public void Wildcard_HasAllNodes()
    {
        var p = Principal("Standard", ["*"]);
        p.HasNode("reports").Should().BeTrue();
    }

    [Fact(DisplayName = "Standard user only sees explicitly allowed nodes")]
    public void Standard_OnlyAllowedNodes()
    {
        var p = Principal("Standard", ["dashboard"]);
        p.HasNode("dashboard").Should().BeTrue();
        p.HasNode("fleet").Should().BeFalse();
        p.HasNode("reports").Should().BeFalse();
    }

    [Fact(DisplayName = "Node filtering keeps only allowed nodes for a Demo-plan user")]
    public void Sidebar_FiltersByNodes()
    {
        var p = Principal("Standard", ["dashboard"]);
        var catalog = new[] { "dashboard", "fleet", "reports" };
        var visible = catalog.Where(p.HasNode).ToList();
        visible.Should().ContainSingle().Which.Should().Be("dashboard");
    }

    [Fact(DisplayName = "SuperUser sees every catalog node")]
    public void Sidebar_SuperUserSeesAll()
    {
        var p = Principal("SuperUser", []);
        var catalog = new[] { "dashboard", "fleet", "reports" };
        var visible = catalog.Where(p.HasNode).ToList();
        visible.Should().HaveCount(catalog.Length);
    }

    [Fact(DisplayName = "Expired subscription is detected for non-super users")]
    public void ExpiredSubscription_Detected()
    {
        var expired = Principal("Standard", ["dashboard"], DateTimeOffset.UtcNow.AddDays(-1));
        expired.SubscriptionExpired.Should().BeTrue();

        var superUser = Principal("SuperUser", [], DateTimeOffset.UtcNow.AddDays(-1));
        superUser.SubscriptionExpired.Should().BeFalse();
    }
}
