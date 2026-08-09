using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AstraSystemsRental.Front.Shared.Security;
using FluentAssertions;

namespace AstraSystemsRental.Front.Tests;

public class JwtReaderTests
{
    private static string BuildToken(params Claim[] claims)
    {
        var token = new JwtSecurityToken(claims: claims);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact(DisplayName = "Reads role, plan, nodes and subscription end from the JWT")]
    public void Read_ExtractsClaims()
    {
        var end = DateTimeOffset.UtcNow.AddDays(10).ToUnixTimeSeconds();
        var jwt = BuildToken(
            new Claim("sub", "42"),
            new Claim("email", "admin@codalea.app"),
            new Claim("role", "SuperUser"),
            new Claim("plan", "Basic"),
            new Claim("node", "dashboard"),
            new Claim("node", "fleet"),
            new Claim("sub_end", end.ToString()));

        var principal = JwtReader.Read(jwt);

        principal.UserId.Should().Be("42");
        principal.Email.Should().Be("admin@codalea.app");
        principal.Role.Should().Be("SuperUser");
        principal.Plan.Should().Be("Basic");
        principal.Nodes.Should().Contain(["dashboard", "fleet"]);
        principal.SubscriptionEnd.Should().NotBeNull();
    }

    [Fact(DisplayName = "Handles a token with no nodes and no subscription end")]
    public void Read_MinimalToken()
    {
        var jwt = BuildToken(
            new Claim("sub", "1"),
            new Claim("email", "demo@codalea.app"),
            new Claim("role", "Demo"));

        var principal = JwtReader.Read(jwt);

        principal.Nodes.Should().BeEmpty();
        principal.SubscriptionEnd.Should().BeNull();
        principal.Plan.Should().BeNull();
    }
}
