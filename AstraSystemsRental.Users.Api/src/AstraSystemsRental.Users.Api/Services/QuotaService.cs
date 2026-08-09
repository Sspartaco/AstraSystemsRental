using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Persistence;

namespace AstraSystemsRental.Users.Api.Services;

public sealed class QuotaService(IQuotaRepository repository, IAstraRequestContext requestContext) : IQuotaService
{
    private bool IsSuperUser => string.Equals(requestContext.RoleCode, RoleCode.SuperUser, StringComparison.Ordinal);

    public async Task<OperationResult> GetQuotaAsync(string nodeKey, CancellationToken cancellationToken)
    {
        if (IsSuperUser)
            return OperationResult.Ok(new { nodeKey, maxCount = int.MaxValue, planCode = RoleCode.SuperUser });

        var owner = requestContext.Owner;
        var quota = await repository.GetQuotaAsync(owner.OwnerType, owner.OwnerId, nodeKey, cancellationToken);

        if (quota is null)
            return OperationResult.NotFound("No active subscription for the current context.");

        return OperationResult.Ok(new { quota.NodeKey, quota.MaxCount, quota.PlanCode });
    }

    public async Task<OperationResult> ReserveAsync(string nodeKey, CancellationToken cancellationToken)
    {
        if (IsSuperUser)
            return OperationResult.Ok();

        var owner = requestContext.Owner;
        var reserved = await repository.TryReserveAsync(owner.OwnerType, owner.OwnerId, nodeKey, cancellationToken);

        return reserved
            ? OperationResult.Ok()
            : OperationResult.Fail("QuotaExceeded", System.Net.HttpStatusCode.Conflict);
    }

    public async Task<OperationResult> ReleaseAsync(string nodeKey, CancellationToken cancellationToken)
    {
        if (IsSuperUser)
            return OperationResult.Ok();

        var owner = requestContext.Owner;
        await repository.ReleaseAsync(owner.OwnerType, owner.OwnerId, nodeKey, cancellationToken);
        return OperationResult.Ok();
    }
}
