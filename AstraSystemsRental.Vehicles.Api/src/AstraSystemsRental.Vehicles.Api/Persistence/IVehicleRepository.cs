using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Vehicles.Api.Domain;

namespace AstraSystemsRental.Vehicles.Api.Persistence;

public interface IVehicleRepository : IBaseRepository<VehicleCatalog>
{
    Task<VehicleCatalog?> GetByPlateAsync(string plateNumber, CancellationToken cancellationToken);
    Task UpsertBatchAsync(IReadOnlyCollection<VehicleCatalog> vehicles, CancellationToken cancellationToken);
    Task SetImageAsync(string plateNumber, string? imageUrl, string? attribution, CancellationToken cancellationToken);
}
