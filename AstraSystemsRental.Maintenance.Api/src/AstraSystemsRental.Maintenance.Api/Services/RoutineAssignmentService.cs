using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Maintenance.Api.Domain;
using AstraSystemsRental.Maintenance.Api.Dtos;
using AstraSystemsRental.Maintenance.Api.Persistence;

namespace AstraSystemsRental.Maintenance.Api.Services;

public interface IRoutineAssignmentService
{
    Task<OperationResult> GetAsync(long fleetVehicleId, CancellationToken cancellationToken);
    Task<OperationResult> AssignAsync(long fleetVehicleId, AssignRoutineRequest request, CancellationToken cancellationToken);
    Task<OperationResult> GetHistoryAsync(long fleetVehicleId, CancellationToken cancellationToken);
}

public sealed class RoutineAssignmentService(
    IRoutineAssignmentRepository repository,
    IRoutineRepository routineRepository,
    IMaintenanceContextGuard contextGuard,
    IAstraRequestContext requestContext) : IRoutineAssignmentService
{
    public async Task<OperationResult> GetAsync(long fleetVehicleId, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetForVehicleAsync(fleetVehicleId, requestContext.Owner, cancellationToken);
        if (assignment is null)
            return OperationResult.Ok(null);

        var routine = await routineRepository.GetOwnedAsync(assignment.RoutineId, requestContext.Owner, cancellationToken);

        return OperationResult.Ok(new RoutineAssignmentResponse
        {
            Id = assignment.Id,
            FleetVehicleId = assignment.FleetVehicleId,
            RoutineId = assignment.RoutineId,
            RoutineName = routine?.Name ?? string.Empty,
            AssignedAtUtc = assignment.AssignedAtUtc,
            RowVersion = Convert.ToBase64String(assignment.RowVersion ?? [])
        });
    }

    public async Task<OperationResult> AssignAsync(long fleetVehicleId, AssignRoutineRequest request, CancellationToken cancellationToken)
    {
        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var vehicleCheck = await contextGuard.EnsureVehicleAccessibleAsync(fleetVehicleId, cancellationToken);
        if (vehicleCheck is not null)
            return vehicleCheck;

        var owner = requestContext.Owner;
        var routine = await routineRepository.GetOwnedAsync(request.RoutineId, owner, cancellationToken);
        if (routine is null)
            return OperationResult.NotFound("Routine not found.");

        if (!routine.IsActive)
            return OperationResult.Fail("The routine is inactive and cannot be assigned.");

        var existing = await repository.GetForVehicleAsync(fleetVehicleId, owner, cancellationToken);
        long? previousRoutineId = existing?.RoutineId;

        if (existing is null)
        {
            existing = new RoutineAssignment
            {
                OwnerType = owner.OwnerType,
                OwnerId = owner.OwnerId,
                FleetVehicleId = fleetVehicleId,
                RoutineId = routine.Id,
                AssignedAtUtc = DateTime.UtcNow,
                AssignedByUserId = requestContext.UserId
            };
            await repository.AddAsync(existing, cancellationToken);
        }
        else
        {
            if (existing.RoutineId == routine.Id)
                return OperationResult.Ok(await BuildResponseAsync(existing, routine.Name));

            existing.UpdateRoutine(routine.Id, requestContext.UserId);
            await repository.SaveChangesAsync(cancellationToken);
        }

        await repository.AddHistoryAsync(new RoutineAssignmentHistory
        {
            FleetVehicleId = fleetVehicleId,
            PreviousRoutineId = previousRoutineId,
            NewRoutineId = routine.Id,
            ChangedByUserId = requestContext.UserId,
            ChangedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return OperationResult.Ok(await BuildResponseAsync(existing, routine.Name));
    }

    public async Task<OperationResult> GetHistoryAsync(long fleetVehicleId, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetForVehicleAsync(fleetVehicleId, requestContext.Owner, cancellationToken);
        if (assignment is null)
            return OperationResult.Ok(Array.Empty<RoutineAssignmentHistoryResponse>());

        var history = await repository.GetHistoryAsync(fleetVehicleId, cancellationToken);
        var routineIds = history
            .SelectMany(h => new[] { h.PreviousRoutineId, h.NewRoutineId })
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var names = new Dictionary<long, string>();
        foreach (var routineId in routineIds)
        {
            var routine = await routineRepository.GetOwnedAsync(routineId, requestContext.Owner, cancellationToken);
            if (routine is not null)
                names[routineId] = routine.Name;
        }

        var items = history.Select(h => new RoutineAssignmentHistoryResponse(
            h.PreviousRoutineId,
            h.PreviousRoutineId is { } prev && names.TryGetValue(prev, out var prevName) ? prevName : null,
            h.NewRoutineId,
            names.TryGetValue(h.NewRoutineId, out var newName) ? newName : string.Empty,
            h.ChangedByUserId,
            h.ChangedAtUtc));

        return OperationResult.Ok(items);
    }

    private Task<RoutineAssignmentResponse> BuildResponseAsync(RoutineAssignment assignment, string routineName)
        => Task.FromResult(new RoutineAssignmentResponse
        {
            Id = assignment.Id,
            FleetVehicleId = assignment.FleetVehicleId,
            RoutineId = assignment.RoutineId,
            RoutineName = routineName,
            AssignedAtUtc = assignment.AssignedAtUtc,
            RowVersion = Convert.ToBase64String(assignment.RowVersion ?? [])
        });
}
