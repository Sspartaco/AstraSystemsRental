namespace AstraSystemsRental.Vehicles.Api.Domain;

public enum FleetVehicleStatus
{
    Draft = 0,
    Active = 1,
    Maintenance = 2,
    Blocked = 3,
    Inactive = 4,
    Sold = 5
}

public class FleetVehicle
{
    public long Id { get; set; }
    public string OwnerType { get; set; } = Base.Security.OwnerType.User;
    public long OwnerId { get; set; }
    public required string PlateNumber { get; set; }
    public string? VehicleClass { get; set; }
    public string? Brand { get; set; }
    public string? Line { get; set; }
    public short? ModelYear { get; set; }
    public string? BodyType { get; set; }
    public string? ServiceType { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string? Color { get; set; }
    public string? Vin { get; set; }
    public string? EngineNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? ChassisNumber { get; set; }
    public string? TransitLicenseNumber { get; set; }
    public string? PurchaseInvoiceNumber { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchaseValue { get; set; }
    public FleetVehicleStatus Status { get; set; } = FleetVehicleStatus.Draft;
    public string? Notes { get; set; }
    public byte[]? RowVersion { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
