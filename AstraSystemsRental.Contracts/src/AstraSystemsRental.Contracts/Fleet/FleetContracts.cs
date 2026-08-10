namespace AstraSystemsRental.Contracts.Fleet;

public sealed record PagedDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalPages { get; init; } = 1;
}

public sealed record FleetVehicleDto
{
    public long Id { get; init; }
    public string PlateNumber { get; init; } = string.Empty;
    public string? Brand { get; init; }
    public string? Line { get; init; }
    public short? ModelYear { get; init; }
    public string? VehicleClass { get; init; }
    public string? BodyType { get; init; }
    public string? Color { get; init; }
    public string? ServiceType { get; init; }
    public string? FuelType { get; init; }
    public string? Transmission { get; init; }
    public string? Vin { get; init; }
    public string? EngineNumber { get; init; }
    public string? SerialNumber { get; init; }
    public string? ChassisNumber { get; init; }
    public string? TransitLicenseNumber { get; init; }
    public string? PurchaseInvoiceNumber { get; init; }
    public DateOnly? PurchaseDate { get; init; }
    public decimal? PurchaseValue { get; init; }
    public string Status { get; init; } = "Draft";
    public string? Notes { get; init; }

    /// <summary>Control de concurrencia: el PUT lo exige para no pisar cambios ajenos.</summary>
    public string? RowVersion { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string Display => string.IsNullOrWhiteSpace(Brand) && string.IsNullOrWhiteSpace(Line)
        ? PlateNumber
        : $"{Brand} {Line}".Trim();
}

public sealed record CreateFleetVehicleDto
{
    public string PlateNumber { get; init; } = string.Empty;
    public string? Brand { get; init; }
    public string? Line { get; init; }
    public short? ModelYear { get; init; }
    public string? VehicleClass { get; init; }
    public string? Color { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Completar la ficha de un vehiculo ya registrado. Todos los campos son
/// opcionales salvo RowVersion, que el servidor exige para detectar que otro
/// usuario no modifico el registro mientras tanto.
/// </summary>
public sealed record UpdateFleetVehicleDto
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
    public string RowVersion { get; init; } = string.Empty;
}

public sealed record FleetVehicleDocumentDto
{
    public long Id { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string? DocumentNumber { get; init; }
    public DateOnly? IssuedDate { get; init; }
    public DateOnly? ExpiresDate { get; init; }
    public string Status { get; init; } = "Pending";
    public string? Notes { get; init; }
}
