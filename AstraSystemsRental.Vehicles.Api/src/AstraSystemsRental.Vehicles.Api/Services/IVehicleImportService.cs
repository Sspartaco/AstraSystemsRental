namespace AstraSystemsRental.Vehicles.Api.Services;

public sealed record VehicleImportResult(int TotalRows, int Imported);

public interface IVehicleImportService
{
    Task<VehicleImportResult> ImportFromExcelAsync(string filePath, CancellationToken cancellationToken);
}
