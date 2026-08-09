namespace AstraSystemsRental.Base.Api;

public sealed class AstraApiOptions
{
    public required string ServiceName { get; init; }
    public required string PathBase { get; init; }
    public bool EnableJwtAuthentication { get; init; } = true;
    public bool EnableRateLimiting { get; init; } = true;
    public int RateLimitPermitPerMinute { get; init; } = 600;
}
