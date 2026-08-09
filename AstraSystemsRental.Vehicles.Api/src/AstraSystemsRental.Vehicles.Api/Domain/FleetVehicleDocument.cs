namespace AstraSystemsRental.Vehicles.Api.Domain;

public static class FleetDocumentType
{
    public const string Soat = "SOAT";
    public const string Tecnomecanica = "Tecnomecanica";
    public const string TarjetaOperacion = "TarjetaOperacion";
    public const string Poliza = "Poliza";
    public const string Otro = "Otro";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Soat, Tecnomecanica, TarjetaOperacion, Poliza, Otro
    };
}

public enum FleetDocumentStatus
{
    Valid = 0,
    Expired = 1,
    Pending = 2
}

public class FleetVehicleDocument
{
    public long Id { get; set; }
    public long FleetVehicleId { get; set; }
    public required string DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiresDate { get; set; }
    public FleetDocumentStatus Status { get; set; } = FleetDocumentStatus.Pending;
    public string? Notes { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
