using AstraSystemsRental.Users.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Users.Api.Persistence;

public sealed class QuotaRepository(AstraUsersDbContext context) : IQuotaRepository
{
    public async Task<QuotaLookup?> GetQuotaAsync(string ownerType, long ownerId, string nodeKey, CancellationToken cancellationToken)
    {
        var subscription = await context.EffectiveForOwner(ownerType, ownerId).Include(s => s.Plan).FirstOrDefaultAsync(cancellationToken);
        if (subscription?.Plan is null)
            return null;

        var quota = await context.PlanNodeQuotas.AsNoTracking()
            .FirstOrDefaultAsync(q => q.PlanId == subscription.PlanId && q.NodeKey == nodeKey, cancellationToken);

        if (quota is null)
            return new QuotaLookup(nodeKey, int.MaxValue, subscription.Plan.Code);

        return new QuotaLookup(nodeKey, quota.MaxCount, subscription.Plan.Code);
    }

    public async Task<bool> TryReserveAsync(string ownerType, long ownerId, string nodeKey, CancellationToken cancellationToken)
    {
        var quota = await GetQuotaAsync(ownerType, ownerId, nodeKey, cancellationToken);
        if (quota is null)
            return false;

        if (quota.MaxCount == int.MaxValue)
        {
            await EnsureCounterExistsAsync(ownerType, ownerId, nodeKey, cancellationToken);
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE [subscriptions].[PlanNodeUsageCounters]
                 SET [CurrentCount] = [CurrentCount] + 1
                 WHERE [OwnerType] = {ownerType} AND [OwnerId] = {ownerId} AND [NodeKey] = {nodeKey}
                 """, cancellationToken);
            return true;
        }

        await EnsureCounterExistsAsync(ownerType, ownerId, nodeKey, cancellationToken);

        var affected = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE [subscriptions].[PlanNodeUsageCounters]
             SET [CurrentCount] = [CurrentCount] + 1
             WHERE [OwnerType] = {ownerType} AND [OwnerId] = {ownerId} AND [NodeKey] = {nodeKey}
               AND [CurrentCount] < {quota.MaxCount}
             """, cancellationToken);

        return affected > 0;
    }

    public async Task ReleaseAsync(string ownerType, long ownerId, string nodeKey, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE [subscriptions].[PlanNodeUsageCounters]
             SET [CurrentCount] = CASE WHEN [CurrentCount] > 0 THEN [CurrentCount] - 1 ELSE 0 END
             WHERE [OwnerType] = {ownerType} AND [OwnerId] = {ownerId} AND [NodeKey] = {nodeKey}
             """, cancellationToken);
    }

    private async Task EnsureCounterExistsAsync(string ownerType, long ownerId, string nodeKey, CancellationToken cancellationToken)
    {
        var exists = await context.PlanNodeUsageCounters.AsNoTracking()
            .AnyAsync(c => c.OwnerType == ownerType && c.OwnerId == ownerId && c.NodeKey == nodeKey, cancellationToken);

        if (exists)
            return;

        context.PlanNodeUsageCounters.Add(new PlanNodeUsageCounter
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            NodeKey = nodeKey,
            CurrentCount = 0
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
        }
    }
}
