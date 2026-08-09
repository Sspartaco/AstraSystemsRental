namespace AstraSystemsRental.Users.Api.Domain;

public sealed record LoginProjection(
    long UserId,
    string Email,
    byte[]? PasswordHash,
    byte[]? PasswordSalt,
    bool IsConfirmed,
    string RoleCode,
    string? PlanCode,
    DateTime? SubscriptionEndsAtUtc);

public sealed record UserCredentials(
    long UserId,
    string Email,
    byte[]? PasswordHash,
    byte[]? PasswordSalt,
    bool IsConfirmed,
    string RoleCode);

public sealed record UserOverview(
    long UserId,
    string FirstNames,
    string LastNames,
    string Email,
    string RoleCode,
    bool IsActive,
    bool IsConfirmed,
    string? CompanyName,
    string? PlanCode,
    DateTime? SubscriptionEndsAtUtc);
