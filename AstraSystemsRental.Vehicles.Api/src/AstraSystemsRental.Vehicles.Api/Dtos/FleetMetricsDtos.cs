namespace AstraSystemsRental.Vehicles.Api.Dtos;

public sealed record FleetStatusSliceDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record FleetAgeSliceDto
{
    public string Bucket { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record ExpiringDocumentDto
{
    public long FleetVehicleId { get; init; }
    public string PlateNumber { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public DateOnly? ExpiresDate { get; init; }
    public int DaysRemaining { get; init; }
}

public sealed record FleetMetricsResponse
{
    public int TotalVehicles { get; init; }
    public int ActiveVehicles { get; init; }
    public int InMaintenance { get; init; }
    public int BlockedVehicles { get; init; }
    public decimal TotalPurchaseValue { get; init; }
    public double AverageAgeYears { get; init; }
    public int ExpiredDocuments { get; init; }
    public IReadOnlyList<FleetStatusSliceDto> ByStatus { get; init; } = [];
    public IReadOnlyList<FleetAgeSliceDto> ByAge { get; init; } = [];
    public IReadOnlyList<ExpiringDocumentDto> ExpiringSoon { get; init; } = [];
}
