using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Persistence;

namespace AstraSystemsRental.Users.Api.Services;

public interface ICatalogService
{
    Task<OperationResult> GetNodesAsync(CancellationToken cancellationToken);
    Task<OperationResult> CreateNodeAsync(CreateNodeRequest request, CancellationToken cancellationToken);
    Task<OperationResult> GetPlansAsync(CancellationToken cancellationToken);
    Task<OperationResult> CreatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken);
    Task<OperationResult> UpdatePlanAsync(int planId, UpdatePlanRequest request, CancellationToken cancellationToken);
    Task<OperationResult> SetPlanNodeAsync(int planId, SetNodeRequest request, CancellationToken cancellationToken);
    Task<OperationResult> GetRolesAsync(CancellationToken cancellationToken);
    Task<OperationResult> SetRoleNodeAsync(int roleId, SetNodeRequest request, CancellationToken cancellationToken);
}

public sealed class CatalogService(ICatalogRepository repository) : ICatalogService
{
    public async Task<OperationResult> GetNodesAsync(CancellationToken cancellationToken)
        => OperationResult.Ok(await repository.GetNodesAsync(cancellationToken));

    public async Task<OperationResult> CreateNodeAsync(CreateNodeRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard().NotEmpty(request.Key, "Key").NotEmpty(request.Label, "Label");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        if (await repository.NodeExistsAsync(request.Key.Trim(), cancellationToken))
            return OperationResult.Conflict("A node with this key already exists.");

        await repository.CreateNodeAsync(new Node
        {
            Key = request.Key.Trim(),
            Label = request.Label.Trim(),
            Icon = request.Icon ?? string.Empty,
            Route = request.Route ?? string.Empty,
            SortOrder = request.SortOrder,
            IsActive = true
        }, cancellationToken);

        return OperationResult.Created(new { key = request.Key.Trim() });
    }

    public async Task<OperationResult> GetPlansAsync(CancellationToken cancellationToken)
    {
        var plans = await repository.GetPlansWithNodesAsync(cancellationToken);
        return OperationResult.Ok(plans.Select(p => new
        {
            p.Id,
            p.Code,
            p.Name,
            p.DurationDays,
            p.IsActive,
            nodes = p.PlanNodes.Select(n => n.NodeKey).ToArray()
        }));
    }

    public async Task<OperationResult> CreatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard()
            .NotEmpty(request.Code, "Code")
            .NotEmpty(request.Name, "Name")
            .Must(request.DurationDays > 0, "DurationDays must be greater than zero.");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        if (await repository.PlanCodeExistsAsync(request.Code.Trim(), cancellationToken))
            return OperationResult.Conflict("A plan with this code already exists.");

        var planId = await repository.CreatePlanAsync(new Plan
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            DurationDays = request.DurationDays,
            IsActive = true
        }, cancellationToken);

        return OperationResult.Created(new { planId, code = request.Code.Trim() });
    }

    public async Task<OperationResult> UpdatePlanAsync(int planId, UpdatePlanRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard().NotEmpty(request.Name, "Name").Must(request.DurationDays > 0, "DurationDays must be greater than zero.");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var updated = await repository.UpdatePlanAsync(planId, request.Name.Trim(), request.DurationDays, request.IsActive, cancellationToken);
        return updated ? OperationResult.Ok(new { planId }) : OperationResult.NotFound("Plan not found.");
    }

    public async Task<OperationResult> SetPlanNodeAsync(int planId, SetNodeRequest request, CancellationToken cancellationToken)
    {
        if (!await repository.PlanExistsAsync(planId, cancellationToken))
            return OperationResult.NotFound("Plan not found.");
        if (!await repository.NodeExistsAsync(request.NodeKey, cancellationToken))
            return OperationResult.Fail("Node not found in catalog.");

        await repository.SetPlanNodeAsync(planId, request.NodeKey, request.Assign, cancellationToken);
        return OperationResult.Ok(new { planId, request.NodeKey, request.Assign });
    }

    public async Task<OperationResult> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await repository.GetRolesWithNodesAsync(cancellationToken);
        return OperationResult.Ok(roles.Select(r => new
        {
            r.Id,
            r.Code,
            r.Name,
            nodes = r.RoleNodes.Select(n => n.NodeKey).ToArray()
        }));
    }

    public async Task<OperationResult> SetRoleNodeAsync(int roleId, SetNodeRequest request, CancellationToken cancellationToken)
    {
        if (!await repository.RoleExistsAsync(roleId, cancellationToken))
            return OperationResult.NotFound("Role not found.");
        if (!await repository.NodeExistsAsync(request.NodeKey, cancellationToken))
            return OperationResult.Fail("Node not found in catalog.");

        await repository.SetRoleNodeAsync(roleId, request.NodeKey, request.Assign, cancellationToken);
        return OperationResult.Ok(new { roleId, request.NodeKey, request.Assign });
    }
}
