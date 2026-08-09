using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AstraSystemsRental.Base.Security;

public sealed class RsaKeyProvider : IDisposable
{
    private readonly RSA? _privateKey;
    private readonly RSA _publicKey;

    public RsaKeyProvider(IOptions<JwtOptions> options)
    {
        var value = options.Value;

        var privatePem = ReadPem(value.PrivateKeyPem, value.PrivateKeyPath);
        if (!string.IsNullOrWhiteSpace(privatePem))
        {
            _privateKey = RSA.Create();
            _privateKey.ImportFromPem(privatePem);
        }

        var publicPem = ReadPem(value.PublicKeyPem, value.PublicKeyPath);
        if (string.IsNullOrWhiteSpace(publicPem) && _privateKey is not null)
        {
            publicPem = _privateKey.ExportSubjectPublicKeyInfoPem();
        }

        if (string.IsNullOrWhiteSpace(publicPem))
            throw new InvalidOperationException("No RSA public key configured for JWT validation.");

        _publicKey = RSA.Create();
        _publicKey.ImportFromPem(publicPem);
    }

    public SigningCredentials SigningCredentials
    {
        get
        {
            if (_privateKey is null)
                throw new InvalidOperationException("No RSA private key configured for JWT signing.");

            return new SigningCredentials(new RsaSecurityKey(_privateKey), SecurityAlgorithms.RsaSha256);
        }
    }

    public SecurityKey PublicSecurityKey => new RsaSecurityKey(_publicKey);

    private static string? ReadPem(string? inline, string? path)
    {
        if (!string.IsNullOrWhiteSpace(inline))
            return inline;

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            return File.ReadAllText(path);

        return null;
    }

    public void Dispose()
    {
        _privateKey?.Dispose();
        _publicKey.Dispose();
    }
}
