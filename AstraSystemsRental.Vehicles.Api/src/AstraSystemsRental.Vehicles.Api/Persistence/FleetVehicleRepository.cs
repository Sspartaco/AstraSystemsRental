using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Vehicles.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Vehicles.Api.Persistence;

public sealed class FleetVehicleRepository(AstraVehiclesDbContext context)
    : BaseRepository<AstraVehiclesDbContext, FleetVehicle>(context), IFleetVehicleRepository
{
    public Task<PagedResult<FleetVehicle>> GetPagedAsync(OwnerContext owner, int pageNumber, int pageSize, string? status, string? search, CancellationToken cancellationToken)
        => base.GetPagedAsync(pageNumber, pageSize, queryBuilder: query =>
        {
            query = query
                .Where(v => v.OwnerType == owner.OwnerType && v.OwnerId == owner.OwnerId)
                .OrderByDescending(v => v.CreatedAtUtc);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FleetVehicleStatus>(status, ignoreCase: true, out var parsedStatus))
                query = query.Where(v => v.Status == parsedStatus);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpperInvariant();
                query = query.Where(v => v.PlateNumber.Contains(term));
            }

            return query;
        }, cancellationToken: cancellationToken);

    public Task<FleetVehicle?> GetOwnedAsync(long id, OwnerContext owner, CancellationToken cancellationToken)
        => GetFirstOrDefaultAsync(
            v => v.Id == id && v.OwnerType == owner.OwnerType && v.OwnerId == owner.OwnerId,
            cancellationToken);

    /// <summary>
    /// Igual que GetOwnedAsync pero CON tracking. GetFirstOrDefaultAsync usa
    /// AsNoTracking, asi que la entidad que devuelve no la sigue el DbContext:
    /// modificar sus propiedades no marca nada como cambiado y SaveChangesAsync
    /// no emite ningun UPDATE. El endpoint respondia 200 sin escribir nada.
    /// </summary>
    public Task<FleetVehicle?> GetOwnedForUpdateAsync(long id, OwnerContext owner, CancellationToken cancellationToken)
        => DbContext.FleetVehicles
            .FirstOrDefaultAsync(
                v => v.Id == id && v.OwnerType == owner.OwnerType && v.OwnerId == owner.OwnerId,
                cancellationToken);

    public Task<bool> PlateExistsForOwnerAsync(OwnerContext owner, string plateNumber, CancellationToken cancellationToken)
        => AnyAsync(
            v => v.OwnerType == owner.OwnerType && v.OwnerId == owner.OwnerId && v.PlateNumber == plateNumber,
            cancellationToken);

    public Task<int> CountForOwnerAsync(OwnerContext owner, CancellationToken cancellationToken)
        => CountAsync(v => v.OwnerType == owner.OwnerType && v.OwnerId == owner.OwnerId, cancellationToken);

    public new async Task AddAsync(FleetVehicle vehicle, CancellationToken cancellationToken)
    {
        await base.AddAsync(vehicle, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveOwnedAsync(long id, OwnerContext owner, CancellationToken cancellationToken)
    {
        var vehicle = await GetOwnedAsync(id, owner, cancellationToken);
        if (vehicle is null)
            return false;

        Remove(vehicle);
        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task AddStatusHistoryAsync(FleetVehicleStatusHistory entry, CancellationToken cancellationToken)
    {
        DbContext.FleetVehicleStatusHistory.Add(entry);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FleetVehicleStatusHistory>> GetStatusHistoryAsync(long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken)
    {
        var ownedVehicleIds = OwnedVehicleIds(owner);
        return await DbContext.FleetVehicleStatusHistory.AsNoTracking()
            .Where(h => h.FleetVehicleId == fleetVehicleId && ownedVehicleIds.Contains(h.FleetVehicleId))
            .OrderByDescending(h => h.ChangedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FleetVehicleDocument>> GetDocumentsAsync(long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken)
    {
        var ownedVehicleIds = OwnedVehicleIds(owner);
        return await DbContext.FleetVehicleDocuments.AsNoTracking()
            .Where(d => d.FleetVehicleId == fleetVehicleId && ownedVehicleIds.Contains(d.FleetVehicleId))
            .OrderBy(d => d.DocumentType)
            .ToListAsync(cancellationToken);
    }

    public async Task<FleetVehicleDocument?> GetOwnedDocumentAsync(long documentId, long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken)
    {
        var ownedVehicleIds = OwnedVehicleIds(owner);
        return await DbContext.FleetVehicleDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.FleetVehicleId == fleetVehicleId && ownedVehicleIds.Contains(d.FleetVehicleId), cancellationToken);
    }

    public async Task AddDocumentAsync(FleetVehicleDocument document, CancellationToken cancellationToken)
    {
        DbContext.FleetVehicleDocuments.Add(document);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task RecordPendingQuotaCompensationAsync(string nodeKey, OwnerContext owner, string? error, CancellationToken cancellationToken)
    {
        DbContext.PendingQuotaCompensations.Add(new PendingQuotaCompensation
        {
            NodeKey = nodeKey,
            OwnerType = owner.OwnerType,
            OwnerId = owner.OwnerId,
            Error = error,
            CreatedAtUtc = DateTime.UtcNow
        });
        await SaveChangesAsync(cancellationToken);
    }

    private IQueryable<long> OwnedVehicleIds(OwnerContext owner)
        => DbContext.FleetVehicles
            .Where(v => v.OwnerType == owner.OwnerType && v.OwnerId == owner.OwnerId)
            .Select(v => v.Id);
}
