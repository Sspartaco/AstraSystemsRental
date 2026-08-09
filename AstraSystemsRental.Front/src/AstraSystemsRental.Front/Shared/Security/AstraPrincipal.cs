namespace AstraSystemsRental.Front.Shared.Security;

public sealed class AstraPrincipal
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public string? Plan { get; init; }
    public IReadOnlySet<string> Nodes { get; init; } = new HashSet<string>();
    public DateTimeOffset? SubscriptionEnd { get; init; }

    public bool IsSuperUser => string.Equals(Role, "SuperUser", StringComparison.Ordinal);

    public bool IsSysAdmin => string.Equals(Role, "SysAdmin", StringComparison.Ordinal);

    public bool CanSeeDiagnostics => IsSuperUser || IsSysAdmin;

    public bool HasNode(string nodeKey) =>
        IsSuperUser || IsSysAdmin || Nodes.Contains("*") || Nodes.Contains(nodeKey);

    public bool SubscriptionExpired =>
        !IsSuperUser && SubscriptionEnd is { } end && end <= DateTimeOffset.UtcNow;

    public int? DaysRemaining =>
        SubscriptionEnd is { } end ? (int)Math.Ceiling((end - DateTimeOffset.UtcNow).TotalDays) : null;

    public static readonly AstraPrincipal Anonymous = new()
    {
        UserId = string.Empty,
        Email = string.Empty,
        Role = string.Empty
    };
}
