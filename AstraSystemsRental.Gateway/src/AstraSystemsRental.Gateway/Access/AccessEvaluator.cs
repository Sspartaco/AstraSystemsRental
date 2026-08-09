namespace AstraSystemsRental.Gateway.Access;

public interface IAccessEvaluator
{
    AccessResult Evaluate(AccessContext context, DateTimeOffset nowUtc);
}

public sealed class AccessEvaluator : IAccessEvaluator
{
    private const string SuperUserRole = "SuperUser";
    private const string WildcardNode = "*";

    public AccessResult Evaluate(AccessContext context, DateTimeOffset nowUtc)
    {
        if (!context.IsAuthenticated)
            return new AccessResult(AccessOutcome.Unauthenticated, "A valid access token is required.");

        var isSuperUser = string.Equals(context.RoleCode, SuperUserRole, StringComparison.Ordinal);
        if (isSuperUser)
            return AccessResult.Allowed;

        if (context.SubscriptionEnd is { } end && end <= nowUtc)
            return new AccessResult(AccessOutcome.SubscriptionExpired, "Subscription has expired.");

        if (string.IsNullOrWhiteSpace(context.RequestedNode))
            return AccessResult.Allowed;

        if (context.AllowedNodes.Contains(WildcardNode) || context.AllowedNodes.Contains(context.RequestedNode))
            return AccessResult.Allowed;

        return new AccessResult(AccessOutcome.NodeForbidden, $"Access to node '{context.RequestedNode}' is not allowed for this plan.");
    }
}
