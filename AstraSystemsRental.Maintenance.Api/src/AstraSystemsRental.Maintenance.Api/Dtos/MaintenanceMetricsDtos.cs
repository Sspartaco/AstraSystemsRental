namespace AstraSystemsRental.Maintenance.Api.Dtos;

public sealed record ReservationStatusSliceDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record UpcomingReservationDto
{
    public long Id { get; init; }
    public long FleetVehicleId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ScheduledAtUtc { get; init; }
    public string? ProviderName { get; init; }
    public bool IsWashOnly { get; init; }
}

public sealed record WorkshopLoadSliceDto
{
    public string ProviderName { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record MaintenanceMetricsResponse
{
    public int ActiveReservations { get; init; }
    public int ReservationsToday { get; init; }
    public int ReservationsNext7Days { get; init; }
    public int VehiclesInWorkshop { get; init; }
    public int VehiclesWithRoutine { get; init; }
    public int ReadingsLast30Days { get; init; }
    public int CompletedLast30Days { get; init; }
    public double AverageWorkshopDays { get; init; }
    public IReadOnlyList<ReservationStatusSliceDto> ByStatus { get; init; } = [];
    public IReadOnlyList<UpcomingReservationDto> Upcoming { get; init; } = [];
    public IReadOnlyList<WorkshopLoadSliceDto> ByWorkshop { get; init; } = [];
}
