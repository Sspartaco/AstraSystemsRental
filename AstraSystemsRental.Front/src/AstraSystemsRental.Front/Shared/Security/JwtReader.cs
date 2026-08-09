using System.IdentityModel.Tokens.Jwt;

namespace AstraSystemsRental.Front.Shared.Security;

public static class JwtReader
{
    public static AstraPrincipal Read(string jwt)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

        var nodes = token.Claims
            .Where(c => c.Type == "node")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        DateTimeOffset? subscriptionEnd = null;
        var subEnd = token.Claims.FirstOrDefault(c => c.Type == "sub_end")?.Value;
        if (long.TryParse(subEnd, out var unix))
            subscriptionEnd = DateTimeOffset.FromUnixTimeSeconds(unix);

        return new AstraPrincipal
        {
            UserId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
            Email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
            Role = token.Claims.FirstOrDefault(c => c.Type == "role")?.Value ?? string.Empty,
            Plan = token.Claims.FirstOrDefault(c => c.Type == "plan")?.Value,
            Nodes = nodes,
            SubscriptionEnd = subscriptionEnd
        };
    }
}
