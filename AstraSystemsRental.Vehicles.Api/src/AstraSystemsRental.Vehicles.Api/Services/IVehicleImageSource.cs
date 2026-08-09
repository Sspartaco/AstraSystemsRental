namespace AstraSystemsRental.Vehicles.Api.Services;

public sealed record VehicleImageResult(string? ImageUrl, string? Attribution);

public interface IVehicleImageSource
{
    Task<VehicleImageResult> FindAsync(string? brand, string? line, short? modelYear, CancellationToken cancellationToken);
}
