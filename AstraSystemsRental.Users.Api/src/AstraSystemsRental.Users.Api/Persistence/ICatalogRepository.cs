using AstraSystemsRental.Users.Api.Domain;

namespace AstraSystemsRental.Users.Api.Persistence;

public interface ICatalogRepository
{
    Task<IReadOnlyList<Node>> GetNodesAsync(CancellationToken cancellationToken);
    Task<bool> NodeExistsAsync(string key, CancellationToken cancellationToken);
    Task CreateNodeAsync(Node node, CancellationToken cancellationToken);

    Task<IReadOnlyList<Plan>> GetPlansWithNodesAsync(CancellationToken cancellationToken);
    Task<bool> PlanCodeExistsAsync(string code, CancellationToken cancellationToken);
    Task<int> CreatePlanAsync(Plan plan, CancellationToken cancellationToken);
    Task<bool> UpdatePlanAsync(int planId, string name, int durationDays, bool isActive, CancellationToken cancellationToken);
    Task<bool> PlanExistsAsync(int planId, CancellationToken cancellationToken);
    Task<bool> SetPlanNodeAsync(int planId, string nodeKey, bool assign, CancellationToken cancellationToken);

    Task<IReadOnlyList<Role>> GetRolesWithNodesAsync(CancellationToken cancellationToken);
    Task<bool> RoleExistsAsync(int roleId, CancellationToken cancellationToken);
    Task<bool> SetRoleNodeAsync(int roleId, string nodeKey, bool assign, CancellationToken cancellationToken);
}
