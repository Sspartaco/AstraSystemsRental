using AstraSystemsRental.Vehicles.Api.Domain;
using AstraSystemsRental.Vehicles.Api.Persistence;
using ClosedXML.Excel;

namespace AstraSystemsRental.Vehicles.Api.Services;

public sealed class VehicleImportService(IVehicleRepository repository, ILogger<VehicleImportService> logger) : IVehicleImportService
{
    private const int BatchSize = 500;

    public async Task<VehicleImportResult> ImportFromExcelAsync(string filePath, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RowsUsed().Skip(1);

        var now = DateTime.UtcNow;
        var batch = new List<VehicleCatalog>(BatchSize);
        var total = 0;
        var imported = 0;

        foreach (var row in rows)
        {
            total++;
            var plate = row.Cell(1).GetString().Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(plate))
                continue;

            batch.Add(new VehicleCatalog
            {
                PlateNumber = plate,
                VehicleClass = NullIfEmpty(row.Cell(2).GetString()),
                Brand = NullIfEmpty(row.Cell(3).GetString()),
                Line = NullIfEmpty(row.Cell(4).GetString()),
                ModelYear = TryParseYear(row.Cell(5).GetString()),
                FullLine = NullIfEmpty(row.Cell(6).GetString()),
                Engine = NullIfEmpty(row.Cell(7).GetString()),
                IsUserAdded = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

            if (batch.Count >= BatchSize)
            {
                await repository.UpsertBatchAsync(batch, cancellationToken);
                imported += batch.Count;
                logger.LogInformation("Imported {Count} vehicle rows so far", imported);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await repository.UpsertBatchAsync(batch, cancellationToken);
            imported += batch.Count;
        }

        return new VehicleImportResult(total, imported);
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static short? TryParseYear(string value)
        => short.TryParse(value.Trim(), out var year) ? year : null;
}
