using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AstraSystemsRental.Base.Security;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AstraSystemsRental.Base.Tests;

public class JwtRs256Tests
{
    private static JwtOptions BuildOptionsWithKeyPair()
    {
        using var rsa = RSA.Create(2048);
        return new JwtOptions
        {
            Issuer = "https://astra.local",
            Audience = "astra-app",
            AccessTokenMinutes = 30,
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
            PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem()
        };
    }

    [Fact(DisplayName = "Token signed with private key validates with public key")]
    public void IssuedToken_ValidatesWithPublicKey()
    {
        // Arrange
        var options = BuildOptionsWithKeyPair();
        using var keyProvider = new RsaKeyProvider(Options.Create(options));
        var issuer = new JwtTokenIssuer(keyProvider, Options.Create(options));
        var claims = new[]
        {
            new Claim(AstraClaims.UserId, "42"),
            new Claim(AstraClaims.Role, "SuperUser")
        };

        // Act
        var token = issuer.Issue(claims);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = keyProvider.PublicSecurityKey,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        }, out _);

        // Assert
        principal.FindFirst(AstraClaims.UserId)!.Value.Should().Be("42");
        principal.FindFirst(AstraClaims.Role)!.Value.Should().Be("SuperUser");
    }

    [Fact(DisplayName = "Provider with only public key cannot sign")]
    public void PublicOnlyProvider_ThrowsOnSign()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var options = new JwtOptions
        {
            Issuer = "https://astra.local",
            Audience = "astra-app",
            PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem()
        };
        using var keyProvider = new RsaKeyProvider(Options.Create(options));

        // Act
        var act = () => keyProvider.SigningCredentials;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
