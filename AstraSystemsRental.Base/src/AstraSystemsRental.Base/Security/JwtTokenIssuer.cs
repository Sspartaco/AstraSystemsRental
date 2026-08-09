using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace AstraSystemsRental.Base.Security;

public interface IJwtTokenIssuer
{
    string Issue(IEnumerable<Claim> claims, TimeSpan? lifetime = null);
}

public sealed class JwtTokenIssuer(RsaKeyProvider keyProvider, IOptions<JwtOptions> options) : IJwtTokenIssuer
{
    private readonly JwtOptions _options = options.Value;

    public string Issue(IEnumerable<Claim> claims, TimeSpan? lifetime = null)
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(lifetime ?? TimeSpan.FromMinutes(_options.AccessTokenMinutes));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: keyProvider.SigningCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
