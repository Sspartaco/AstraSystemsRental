using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Vehicles.Api.Domain;

namespace AstraSystemsRental.Vehicles.Api.Persistence;

public interface IFleetVehicleRepository
{
    Task<PagedResult<FleetVehicle>> GetPagedAsync(OwnerContext owner, int pageNumber, int pageSize, string? status, string? search, CancellationToken cancellationToken);
    Task<FleetVehicle?> GetOwnedAsync(long id, OwnerContext owner, CancellationToken cancellationToken);

    /// <summary>Con tracking: obligatorio para que SaveChangesAsync emita el UPDATE.</summary>
    Task<FleetVehicle?> GetOwnedForUpdateAsync(long id, OwnerContext owner, CancellationToken cancellationToken);
    Task<bool> PlateExistsForOwnerAsync(OwnerContext owner, string plateNumber, CancellationToken cancellationToken);
    Task<int> CountForOwnerAsync(OwnerContext owner, CancellationToken cancellationToken);
    Task AddAsync(FleetVehicle vehicle, CancellationToken cancellationToken);
    Task<bool> RemoveOwnedAsync(long id, OwnerContext owner, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task RecordPendingQuotaCompensationAsync(string nodeKey, OwnerContext owner, string? error, CancellationToken cancellationToken);

    Task AddStatusHistoryAsync(FleetVehicleStatusHistory entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<FleetVehicleStatusHistory>> GetStatusHistoryAsync(long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken);

    Task<IReadOnlyList<FleetVehicleDocument>> GetDocumentsAsync(long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken);
    Task<FleetVehicleDocument?> GetOwnedDocumentAsync(long documentId, long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken);
    Task AddDocumentAsync(FleetVehicleDocument document, CancellationToken cancellationToken);
}
