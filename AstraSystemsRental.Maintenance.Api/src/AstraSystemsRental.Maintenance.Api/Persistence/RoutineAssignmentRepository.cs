using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Maintenance.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Maintenance.Api.Persistence;

public sealed class RoutineAssignmentRepository(AstraMaintenanceDbContext context)
    : BaseRepository<AstraMaintenanceDbContext, RoutineAssignment>(context), IRoutineAssignmentRepository
{
    public Task<RoutineAssignment?> GetForVehicleAsync(long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken)
        => DbContext.RoutineAssignments
            .FirstOrDefaultAsync(a => a.FleetVehicleId == fleetVehicleId && a.OwnerType == owner.OwnerType && a.OwnerId == owner.OwnerId, cancellationToken);

    public new async Task AddAsync(RoutineAssignment assignment, CancellationToken cancellationToken)
    {
        await base.AddAsync(assignment, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task AddHistoryAsync(RoutineAssignmentHistory history, CancellationToken cancellationToken)
    {
        DbContext.RoutineAssignmentHistory.Add(history);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoutineAssignmentHistory>> GetHistoryAsync(long fleetVehicleId, CancellationToken cancellationToken)
        => await DbContext.RoutineAssignmentHistory.AsNoTracking()
            .Where(h => h.FleetVehicleId == fleetVehicleId)
            .OrderByDescending(h => h.ChangedAtUtc)
            .ToListAsync(cancellationToken);
}
