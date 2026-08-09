namespace AstraSystemsRental.Vehicles.Api.Dtos;

public sealed record CreateFleetVehicleRequest
{
    public required string PlateNumber { get; init; }
    public string? VehicleClass { get; init; }
    public string? Brand { get; init; }
    public string? Line { get; init; }
    public short? ModelYear { get; init; }
    public string? BodyType { get; init; }
    public string? ServiceType { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? Color { get; init; }
    public string? Vin { get; init; }
    public string? EngineNumber { get; init; }
    public string? SerialNumber { get; init; }
    public string? ChassisNumber { get; init; }
    public string? TransitLicenseNumber { get; init; }
    public string? PurchaseInvoiceNumber { get; init; }
    public DateOnly? PurchaseDate { get; init; }
    public decimal? PurchaseValue { get; init; }
    public string? Notes { get; init; }
}

public sealed record UpdateFleetVehicleRequest
{
    public string? VehicleClass { get; init; }
    public string? Brand { get; init; }
    public string? Line { get; init; }
    public short? ModelYear { get; init; }
    public string? BodyType { get; init; }
    public string? ServiceType { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? Color { get; init; }
    public string? Vin { get; init; }
    public string? EngineNumber { get; init; }
    public string? SerialNumber { get; init; }
    public string? ChassisNumber { get; init; }
    public string? TransitLicenseNumber { get; init; }
    public string? PurchaseInvoiceNumber { get; init; }
    public DateOnly? PurchaseDate { get; init; }
    public decimal? PurchaseValue { get; init; }
    public string? Notes { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record ChangeFleetVehicleStatusRequest(string NewStatus, string? Reason);

public sealed record CreateFleetVehicleDocumentRequest(string DocumentType, string? DocumentNumber, DateOnly? IssuedDate, DateOnly? ExpiresDate, string? Notes);

public sealed record UpdateFleetVehicleDocumentRequest(string? DocumentNumber, DateOnly? IssuedDate, DateOnly? ExpiresDate, string? Notes);

public sealed record FleetVehicleResponse
{
    public required long Id { get; init; }
    public required string PlateNumber { get; init; }
    public string? VehicleClass { get; init; }
    public string? Brand { get; init; }
    public string? Line { get; init; }
    public short? ModelYear { get; init; }
    public string? BodyType { get; init; }
    public string? ServiceType { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? Color { get; init; }
    public string? Vin { get; init; }
    public string? EngineNumber { get; init; }
    public string? SerialNumber { get; init; }
    public string? ChassisNumber { get; init; }
    public string? TransitLicenseNumber { get; init; }
    public string? PurchaseInvoiceNumber { get; init; }
    public DateOnly? PurchaseDate { get; init; }
    public decimal? PurchaseValue { get; init; }
    public required string Status { get; init; }
    public string? Notes { get; init; }
    public required string RowVersion { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
}

public sealed record FleetVehicleStatusHistoryResponse(string? PreviousStatus, string NewStatus, string? Reason, long ChangedByUserId, DateTime ChangedAtUtc);

public sealed record FleetVehicleDocumentResponse(long Id, string DocumentType, string? DocumentNumber, DateOnly? IssuedDate, DateOnly? ExpiresDate, string Status, string? Notes);
