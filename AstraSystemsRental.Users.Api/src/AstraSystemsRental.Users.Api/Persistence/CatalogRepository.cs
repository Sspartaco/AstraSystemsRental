using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Users.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Users.Api.Persistence;

public sealed class CatalogRepository(AstraUsersDbContext context)
    : BaseRepository<AstraUsersDbContext, Node>(context), ICatalogRepository
{
    public async Task<IReadOnlyList<Node>> GetNodesAsync(CancellationToken cancellationToken)
        => await DbContext.Nodes.AsNoTracking().Where(n => n.IsActive).OrderBy(n => n.SortOrder).ToListAsync(cancellationToken);

    public async Task<bool> NodeExistsAsync(string key, CancellationToken cancellationToken)
        => await AnyAsync(n => n.Key == key, cancellationToken);

    public async Task CreateNodeAsync(Node node, CancellationToken cancellationToken)
    {
        await AddAsync(node, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Plan>> GetPlansWithNodesAsync(CancellationToken cancellationToken)
        => await DbContext.Plans.AsNoTracking().Include(p => p.PlanNodes).OrderBy(p => p.Id).ToListAsync(cancellationToken);

    public async Task<bool> PlanCodeExistsAsync(string code, CancellationToken cancellationToken)
        => await DbContext.Plans.AnyAsync(p => p.Code == code, cancellationToken);

    public async Task<int> CreatePlanAsync(Plan plan, CancellationToken cancellationToken)
    {
        await DbContext.Plans.AddAsync(plan, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return plan.Id;
    }

    public async Task<bool> PlanExistsAsync(int planId, CancellationToken cancellationToken)
        => await DbContext.Plans.AnyAsync(p => p.Id == planId, cancellationToken);

    public async Task<bool> UpdatePlanAsync(int planId, string name, int durationDays, bool isActive, CancellationToken cancellationToken)
    {
        var plan = await DbContext.Plans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan is null)
            return false;

        plan.Name = name;
        plan.DurationDays = durationDays;
        plan.IsActive = isActive;
        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetPlanNodeAsync(int planId, string nodeKey, bool assign, CancellationToken cancellationToken)
    {
        var exists = await DbContext.PlanNodes.FirstOrDefaultAsync(pn => pn.PlanId == planId && pn.NodeKey == nodeKey, cancellationToken);
        if (assign && exists is null)
            await DbContext.PlanNodes.AddAsync(new PlanNode { PlanId = planId, NodeKey = nodeKey }, cancellationToken);
        else if (!assign && exists is not null)
            DbContext.PlanNodes.Remove(exists);
        else
            return true;

        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Role>> GetRolesWithNodesAsync(CancellationToken cancellationToken)
        => await DbContext.Roles.AsNoTracking().Include(r => r.RoleNodes).OrderBy(r => r.Id).ToListAsync(cancellationToken);

    public async Task<bool> RoleExistsAsync(int roleId, CancellationToken cancellationToken)
        => await DbContext.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);

    public async Task<bool> SetRoleNodeAsync(int roleId, string nodeKey, bool assign, CancellationToken cancellationToken)
    {
        var exists = await DbContext.RoleNodes.FirstOrDefaultAsync(rn => rn.RoleId == roleId && rn.NodeKey == nodeKey, cancellationToken);
        if (assign && exists is null)
            await DbContext.RoleNodes.AddAsync(new RoleNode { RoleId = roleId, NodeKey = nodeKey }, cancellationToken);
        else if (!assign && exists is not null)
            DbContext.RoleNodes.Remove(exists);
        else
            return true;

        await SaveChangesAsync(cancellationToken);
        return true;
    }
}
