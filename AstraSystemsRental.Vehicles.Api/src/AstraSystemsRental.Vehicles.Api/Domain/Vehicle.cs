namespace AstraSystemsRental.Vehicles.Api.Domain;

public class VehicleCatalog
{
    public required string PlateNumber { get; set; }
    public string? VehicleClass { get; set; }
    public string? Brand { get; set; }
    public string? Line { get; set; }
    public short? ModelYear { get; set; }
    public string? FullLine { get; set; }
    public string? Engine { get; set; }
    public bool IsUserAdded { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageAttribution { get; set; }
    public DateTime? ImageFetchedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
