using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Maintenance.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Maintenance.Api.Persistence;

public sealed class WorkshopReservationRepository(AstraMaintenanceDbContext context)
    : BaseRepository<AstraMaintenanceDbContext, WorkshopReservation>(context), IWorkshopReservationRepository
{
    private static readonly WorkshopReservationStatus[] ActiveStatuses =
    [
        WorkshopReservationStatus.Pending,
        WorkshopReservationStatus.InWorkshop,
        WorkshopReservationStatus.Ready
    ];

    public Task<PagedResult<WorkshopReservation>> GetPagedAsync(
        OwnerContext owner, int pageNumber, int pageSize, long? fleetVehicleId, string? status,
        long? providerId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
        => base.GetPagedAsync(pageNumber, pageSize, queryBuilder: query =>
        {
            query = query.Where(r => r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId);

            if (fleetVehicleId is { } vehicleId)
                query = query.Where(r => r.FleetVehicleId == vehicleId);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WorkshopReservationStatus>(status, ignoreCase: true, out var parsed))
                query = query.Where(r => r.Status == parsed);

            if (providerId is { } provider)
                query = query.Where(r => r.ProviderId == provider);

            if (from is { } fromDate)
            {
                var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue);
                query = query.Where(r => r.ScheduledAtUtc >= fromUtc);
            }

            if (to is { } toDate)
            {
                var toUtc = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(r => r.ScheduledAtUtc < toUtc);
            }

            return query.OrderByDescending(r => r.ScheduledAtUtc);
        }, cancellationToken: cancellationToken);

    public Task<WorkshopReservation?> GetOwnedAsync(long id, OwnerContext owner, CancellationToken cancellationToken)
        => DbContext.WorkshopReservations
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId, cancellationToken);

    public async Task<IReadOnlyList<WorkshopReservation>> GetForVehicleAsync(long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken)
        => await Query()
            .Where(r => r.FleetVehicleId == fleetVehicleId && r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId)
            .OrderByDescending(r => r.ScheduledAtUtc)
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveAsync(OwnerContext owner, CancellationToken cancellationToken)
        => CountAsync(r => r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId && ActiveStatuses.Contains(r.Status), cancellationToken);

    public new async Task AddAsync(WorkshopReservation reservation, CancellationToken cancellationToken)
    {
        await base.AddAsync(reservation, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkshopReservationPhoto>> GetPhotosAsync(long reservationId, CancellationToken cancellationToken)
        => await DbContext.WorkshopReservationPhotos.AsNoTracking()
            .Where(p => p.WorkshopReservationId == reservationId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkshopReservationPhoto>> GetPhotosForReservationsAsync(IReadOnlyList<long> reservationIds, CancellationToken cancellationToken)
        => await DbContext.WorkshopReservationPhotos.AsNoTracking()
            .Where(p => reservationIds.Contains(p.WorkshopReservationId))
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddPhotoAsync(WorkshopReservationPhoto photo, CancellationToken cancellationToken)
    {
        DbContext.WorkshopReservationPhotos.Add(photo);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<WorkshopProvider?> GetOwnedProviderAsync(long id, OwnerContext owner, CancellationToken cancellationToken)
        => DbContext.WorkshopProviders
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerType == owner.OwnerType && p.OwnerId == owner.OwnerId, cancellationToken);

    public async Task<IReadOnlyList<WorkshopProvider>> GetProvidersAsync(OwnerContext owner, CancellationToken cancellationToken)
        => await DbContext.WorkshopProviders.AsNoTracking()
            .Where(p => p.OwnerType == owner.OwnerType && p.OwnerId == owner.OwnerId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task AddProviderAsync(WorkshopProvider provider, CancellationToken cancellationToken)
    {
        DbContext.WorkshopProviders.Add(provider);
        await SaveChangesAsync(cancellationToken);
    }
}
