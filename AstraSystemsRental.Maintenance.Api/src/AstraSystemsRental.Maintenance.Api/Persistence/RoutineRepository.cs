using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Maintenance.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Maintenance.Api.Persistence;

public sealed class RoutineRepository(AstraMaintenanceDbContext context)
    : BaseRepository<AstraMaintenanceDbContext, MaintenanceRoutine>(context), IRoutineRepository
{
    public Task<PagedResult<MaintenanceRoutine>> GetPagedAsync(OwnerContext owner, int pageNumber, int pageSize, string? search, bool? isActive, CancellationToken cancellationToken)
        => base.GetPagedAsync(pageNumber, pageSize, queryBuilder: query =>
        {
            query = query.Where(r => r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId);

            if (isActive is { } active)
                query = query.Where(r => r.IsActive == active);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(r => r.Name.Contains(term));
            }

            return query.OrderByDescending(r => r.CreatedAtUtc);
        }, cancellationToken: cancellationToken);

    public Task<MaintenanceRoutine?> GetOwnedAsync(long id, OwnerContext owner, CancellationToken cancellationToken)
        => GetFirstOrDefaultAsync(r => r.Id == id && r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId, cancellationToken);

    public Task<bool> NameExistsAsync(OwnerContext owner, string name, long? excludeId, CancellationToken cancellationToken)
        => AnyAsync(r => r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId && r.Name == name && (excludeId == null || r.Id != excludeId), cancellationToken);

    public Task<int> CountActiveAsync(OwnerContext owner, CancellationToken cancellationToken)
        => CountAsync(r => r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId && r.IsActive, cancellationToken);

    public new async Task AddAsync(MaintenanceRoutine routine, CancellationToken cancellationToken)
    {
        await base.AddAsync(routine, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveOwnedAsync(long id, OwnerContext owner, CancellationToken cancellationToken)
    {
        var routine = await DbContext.MaintenanceRoutines
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId, cancellationToken);

        if (routine is null)
            return false;

        Remove(routine);
        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<MaintenanceRoutinePeriodicity>> GetPeriodicitiesAsync(long routineId, CancellationToken cancellationToken)
        => await DbContext.MaintenanceRoutinePeriodicities.AsNoTracking()
            .Where(p => p.RoutineId == routineId)
            .OrderBy(p => p.Unit).ThenBy(p => p.StartsAt)
            .ToListAsync(cancellationToken);

    public Task<MaintenanceRoutinePeriodicity?> GetPeriodicityAsync(long periodicityId, long routineId, CancellationToken cancellationToken)
        => DbContext.MaintenanceRoutinePeriodicities
            .FirstOrDefaultAsync(p => p.Id == periodicityId && p.RoutineId == routineId, cancellationToken);

    public async Task AddPeriodicityAsync(MaintenanceRoutinePeriodicity periodicity, CancellationToken cancellationToken)
    {
        DbContext.MaintenanceRoutinePeriodicities.Add(periodicity);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceRoutineConcept>> GetConceptsAsync(IReadOnlyList<long> periodicityIds, CancellationToken cancellationToken)
        => await DbContext.MaintenanceRoutineConcepts.AsNoTracking()
            .Where(c => periodicityIds.Contains(c.PeriodicityId))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task AddConceptAsync(MaintenanceRoutineConcept concept, CancellationToken cancellationToken)
    {
        DbContext.MaintenanceRoutineConcepts.Add(concept);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<MaintenanceRoutinePeriodicity?> GetPeriodicityForUnitAsync(long routineId, MeasurementUnit unit, CancellationToken cancellationToken)
        => DbContext.MaintenanceRoutinePeriodicities.AsNoTracking()
            .Where(p => p.RoutineId == routineId && p.Unit == unit)
            .OrderBy(p => p.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);
}
