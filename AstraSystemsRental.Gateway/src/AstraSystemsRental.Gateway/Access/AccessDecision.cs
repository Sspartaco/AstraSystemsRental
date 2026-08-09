namespace AstraSystemsRental.Gateway.Access;

public enum AccessOutcome
{
    Allowed,
    Unauthenticated,
    SubscriptionExpired,
    NodeForbidden
}

public sealed record AccessContext(
    bool IsAuthenticated,
    string? RoleCode,
    string? PlanCode,
    DateTimeOffset? SubscriptionEnd,
    IReadOnlySet<string> AllowedNodes,
    string? RequestedNode);

public sealed record AccessResult(AccessOutcome Outcome, string Reason)
{
    public bool IsAllowed => Outcome == AccessOutcome.Allowed;

    public static readonly AccessResult Allowed = new(AccessOutcome.Allowed, "Access granted.");
}
