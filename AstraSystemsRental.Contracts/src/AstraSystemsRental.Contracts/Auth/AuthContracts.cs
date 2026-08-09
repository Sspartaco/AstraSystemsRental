namespace AstraSystemsRental.Contracts.Auth;

public sealed record LoginRequestDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? DeviceInfo { get; init; }
}

public sealed record RefreshRequestDto
{
    public string? RefreshToken { get; init; }
    public string? DeviceInfo { get; init; }
}

public sealed record AuthTokensDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public DateTime? RefreshTokenExpiresAtUtc { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public string? Role { get; init; }
    public string? Plan { get; init; }
}

public sealed record NodeDto
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
}

public sealed record UserProfileDto
{
    public long UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstNames { get; init; } = string.Empty;
    public string LastNames { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string PersonType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public string RoleCode { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsConfirmed { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string? PlanCode { get; init; }
    public string? PlanName { get; init; }
    public DateTime? SubscriptionEndsAtUtc { get; init; }
    public int CompanyCount { get; init; }

    public string FullName => $"{FirstNames} {LastNames}".Trim();
}
