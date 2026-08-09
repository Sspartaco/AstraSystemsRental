using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Users.Api.Configuration;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Security;
using Microsoft.Extensions.Options;

namespace AstraSystemsRental.Users.Api.Services;

public sealed record SeedSuperUserData(
    string FirstNames,
    string LastNames,
    string DocumentNumber,
    string Email,
    string Password,
    string ProvidedSecret);

public interface IBootstrapService
{
    Task<OperationResult> SeedSuperUserAsync(SeedSuperUserData data, CancellationToken cancellationToken);
}

public sealed class BootstrapService(
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    IOptions<BootstrapOptions> options) : IBootstrapService
{
    private readonly BootstrapOptions _options = options.Value;

    public async Task<OperationResult> SeedSuperUserAsync(SeedSuperUserData data, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Secret) || _options.Secret.Length < 24)
            return OperationResult.Unauthorized("Bootstrap secret is not configured or is too short (min 24 chars).");

        if (!FixedTimeEquals(data.ProvidedSecret, _options.Secret))
            return OperationResult.Unauthorized("Provided bootstrap secret does not match.");

        var guard = new Guard()
            .NotEmpty(data.FirstNames, "FirstNames")
            .NotEmpty(data.LastNames, "LastNames")
            .NotEmpty(data.DocumentNumber, "DocumentNumber")
            .Email(data.Email, "Email")
            .Must(data.Password is { Length: >= 12 }, "Password must be at least 12 characters.");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        if (await repository.SuperUserExistsAsync(cancellationToken))
            return OperationResult.Conflict("A SuperUser already exists. Bootstrap is disabled.");

        var (hash, salt) = passwordHasher.Hash(data.Password);
        var person = new CreateUserData(
            data.FirstNames.Trim(),
            data.LastNames.Trim(),
            null,
            PersonType.Natural,
            data.DocumentNumber.Trim(),
            null,
            data.Email.Trim());

        var userId = await repository.CreateSuperUserAsync(person, hash, salt, cancellationToken);
        return OperationResult.Created(new { userId });
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = System.Text.Encoding.UTF8.GetBytes(a);
        var bb = System.Text.Encoding.UTF8.GetBytes(b);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Security.Cryptography.SHA256.HashData(ba),
            System.Security.Cryptography.SHA256.HashData(bb));
    }
}
