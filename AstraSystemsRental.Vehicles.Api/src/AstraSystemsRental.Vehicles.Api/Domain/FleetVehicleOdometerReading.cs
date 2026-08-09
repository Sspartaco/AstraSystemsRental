namespace AstraSystemsRental.Vehicles.Api.Domain;

public static class OdometerReadingSource
{
    public const string Manual = "Manual";
    public const string Workshop = "Workshop";
    public const string Import = "Import";
}

public class FleetVehicleOdometerReading
{
    public long Id { get; set; }
    public long FleetVehicleId { get; set; }
    public int Kilometers { get; set; }
    public DateOnly ReadingDate { get; set; }
    public string Source { get; set; } = OdometerReadingSource.Manual;
    public string? Notes { get; set; }
    public long RecordedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
