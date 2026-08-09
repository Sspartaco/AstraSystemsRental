using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Maintenance.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Maintenance.Api.Persistence;

public sealed class MileageReadingRepository(AstraMaintenanceDbContext context)
    : BaseRepository<AstraMaintenanceDbContext, MileageReading>(context), IMileageReadingRepository
{
    public Task<PagedResult<MileageReading>> GetPagedAsync(long fleetVehicleId, OwnerContext owner, int pageNumber, int pageSize, CancellationToken cancellationToken)
        => base.GetPagedAsync(pageNumber, pageSize, queryBuilder: query => query
            .Where(r => r.FleetVehicleId == fleetVehicleId && r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId)
            .OrderByDescending(r => r.ReadingDate)
            .ThenByDescending(r => r.Id), cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<MileageReading>> GetAllForVehicleAsync(long fleetVehicleId, OwnerContext owner, ReadingType readingType, CancellationToken cancellationToken)
        => await Query()
            .Where(r => r.FleetVehicleId == fleetVehicleId && r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId && r.ReadingType == readingType)
            .OrderBy(r => r.ReadingDate)
            .ToListAsync(cancellationToken);

    public Task<MileageReading?> GetOwnedAsync(long id, long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken)
        => DbContext.MileageReadings
            .FirstOrDefaultAsync(r => r.Id == id && r.FleetVehicleId == fleetVehicleId && r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId, cancellationToken);

    public Task<bool> ExistsAsync(long fleetVehicleId, OwnerContext owner, DateOnly readingDate, int value, CancellationToken cancellationToken)
        => AnyAsync(r => r.FleetVehicleId == fleetVehicleId && r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId
                         && r.ReadingDate == readingDate && r.Value == value, cancellationToken);

    public new async Task AddAsync(MileageReading reading, CancellationToken cancellationToken)
    {
        await base.AddAsync(reading, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveOwnedAsync(long id, long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken)
    {
        var reading = await GetOwnedAsync(id, fleetVehicleId, owner, cancellationToken);
        if (reading is null)
            return false;

        Remove(reading);
        await SaveChangesAsync(cancellationToken);
        return true;
    }
}
