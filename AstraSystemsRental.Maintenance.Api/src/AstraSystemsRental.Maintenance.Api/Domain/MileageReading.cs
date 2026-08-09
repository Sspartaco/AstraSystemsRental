namespace AstraSystemsRental.Maintenance.Api.Domain;

public enum ReadingType
{
    Kilometers = 0,
    Hours = 1
}

public static class MileageReadingSource
{
    public const string Manual = "Manual";
    public const string Workshop = "Workshop";
    public const string Import = "Import";

    public static readonly string[] All = [Manual, Workshop, Import];
}

public class MileageReading
{
    public long Id { get; set; }
    public string OwnerType { get; set; } = Base.Security.OwnerType.User;
    public long OwnerId { get; set; }
    public long FleetVehicleId { get; set; }
    public ReadingType ReadingType { get; set; }
    public DateOnly ReadingDate { get; set; }
    public int Value { get; set; }
    public string Source { get; set; } = MileageReadingSource.Manual;
    public long? SourceReservationId { get; set; }
    public string? Notes { get; set; }
    public long RecordedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
