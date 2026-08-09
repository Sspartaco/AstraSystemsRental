using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Vehicles.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Vehicles.Api.Persistence;

public sealed class VehicleRepository(AstraVehiclesDbContext context)
    : BaseRepository<AstraVehiclesDbContext, VehicleCatalog>(context), IVehicleRepository
{
    public Task<VehicleCatalog?> GetByPlateAsync(string plateNumber, CancellationToken cancellationToken)
        => EntitySet.AsNoTracking().FirstOrDefaultAsync(v => v.PlateNumber == plateNumber, cancellationToken);

    public async Task UpsertBatchAsync(IReadOnlyCollection<VehicleCatalog> vehicles, CancellationToken cancellationToken)
    {
        var plates = vehicles.Select(v => v.PlateNumber).ToArray();
        var existing = await EntitySet
            .Where(v => plates.Contains(v.PlateNumber))
            .ToDictionaryAsync(v => v.PlateNumber, cancellationToken);

        var toInsert = new List<VehicleCatalog>();
        foreach (var vehicle in vehicles)
        {
            if (existing.TryGetValue(vehicle.PlateNumber, out var current))
            {
                current.VehicleClass = vehicle.VehicleClass;
                current.Brand = vehicle.Brand;
                current.Line = vehicle.Line;
                current.ModelYear = vehicle.ModelYear;
                current.FullLine = vehicle.FullLine;
                current.Engine = vehicle.Engine;
                current.UpdatedAtUtc = vehicle.UpdatedAtUtc;
            }
            else
            {
                toInsert.Add(vehicle);
            }
        }

        if (toInsert.Count > 0)
            await EntitySet.AddRangeAsync(toInsert, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetImageAsync(string plateNumber, string? imageUrl, string? attribution, CancellationToken cancellationToken)
    {
        var vehicle = await EntitySet.FirstOrDefaultAsync(v => v.PlateNumber == plateNumber, cancellationToken);
        if (vehicle is null)
            return;

        vehicle.ImageUrl = imageUrl;
        vehicle.ImageAttribution = attribution;
        vehicle.ImageFetchedAtUtc = DateTime.UtcNow;
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
