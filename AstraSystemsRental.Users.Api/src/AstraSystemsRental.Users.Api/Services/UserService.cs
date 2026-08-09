using System.Security.Cryptography;
using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Users.Api.Configuration;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Security;
using Microsoft.Extensions.Options;

namespace AstraSystemsRental.Users.Api.Services;

public sealed class UserService(
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    IMailClient mailClient,
    AstraSystemsRental.Base.Security.IAstraRequestContext requestContext,
    IOptions<ConfirmationOptions> confirmationOptions) : IUserService
{
    private readonly ConfirmationOptions _confirmation = confirmationOptions.Value;

    public async Task<OperationResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard()
            .NotEmpty(request.FirstNames, "FirstNames")
            .NotEmpty(request.LastNames, "LastNames")
            .NotEmpty(request.DocumentNumber, "DocumentNumber")
            .Email(request.Email, "Email")
            .Must(PersonType.IsValid(request.PersonType), "PersonType must be 'Natural' or 'Juridico'.");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        if (await repository.EmailExistsAsync(request.Email, cancellationToken))
            return OperationResult.Conflict("A user with this email already exists.");

        var role = await repository.GetRoleByCodeAsync(_confirmation.DefaultRoleCode, cancellationToken);
        if (role is null)
            return OperationResult.Fail("Default role is not configured.");

        var plan = await repository.GetPlanByCodeAsync(_confirmation.DefaultPlanCode, cancellationToken);
        if (plan is null)
            return OperationResult.Fail("Default plan is not configured.");

        var token = GenerateToken();
        var expiresAtUtc = DateTime.UtcNow.AddHours(_confirmation.TokenLifetimeHours);

        var person = new CreateUserData(
            request.FirstNames.Trim(),
            request.LastNames.Trim(),
            request.Address?.Trim(),
            request.PersonType,
            request.DocumentNumber.Trim(),
            request.CompanySize?.Trim(),
            request.Email.Trim());

        var userId = await repository.CreateUserWithConfirmationAsync(
            person, role.Id, plan, token, expiresAtUtc, cancellationToken);

        var confirmationUrl = $"{_confirmation.BaseUrl}?token={token}";
        var displayName = $"{person.FirstNames} {person.LastNames}".Trim();
        var mailSent = await mailClient.SendWelcomeAsync(person.Email, displayName, confirmationUrl, cancellationToken);

        return OperationResult.Created(new
        {
            userId,
            email = person.Email,
            confirmationRequired = true,
            welcomeMailSent = mailSent
        });
    }

    public async Task<OperationResult> ConfirmAsync(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard()
            .NotEmpty(request.Token, "Token")
            .NotEmpty(request.Password, "Password")
            .Must(request.Password is { Length: >= 8 }, "Password must be at least 8 characters.");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var (hash, salt) = passwordHasher.Hash(request.Password);
        var userId = await repository.ConfirmAsync(request.Token, hash, salt, DateTime.UtcNow, cancellationToken);

        if (userId is null)
            return OperationResult.Fail("Invalid or expired confirmation token.");

        return OperationResult.Ok(new { userId, confirmed = true });
    }

    public async Task<OperationResult> GetProfileAsync(CancellationToken cancellationToken)
    {
        var profile = await repository.GetProfileAsync(requestContext.UserId, cancellationToken);

        return profile is null
            ? OperationResult.NotFound("User profile not found")
            : OperationResult.Ok(profile);
    }

    public async Task<OperationResult> GetOverviewAsync(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var result = await repository.GetUsersOverviewAsync(page, pageSize, search, cancellationToken);
        return OperationResult.Ok(new
        {
            items = result.Items,
            totalCount = result.TotalCount,
            pageNumber = result.PageNumber,
            pageSize = result.PageSize,
            totalPages = result.TotalPages
        });
    }

    private static string GenerateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}
