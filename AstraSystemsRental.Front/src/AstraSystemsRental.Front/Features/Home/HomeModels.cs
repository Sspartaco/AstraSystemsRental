namespace AstraSystemsRental.Front.Features.Home;

public sealed record StatusCountDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record AgeCountDto
{
    public string Bucket { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record WorkshopCountDto
{
    public string ProviderName { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record ExpiringDocumentDto
{
    public long FleetVehicleId { get; init; }
    public string PlateNumber { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public DateOnly? ExpiresDate { get; init; }
    public int DaysRemaining { get; init; }

    public bool IsExpired => DaysRemaining < 0;
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

public sealed record FleetSectionDto
{
    public int TotalVehicles { get; init; }
    public int ActiveVehicles { get; init; }
    public int InMaintenance { get; init; }
    public int BlockedVehicles { get; init; }
    public decimal TotalPurchaseValue { get; init; }
    public double AverageAgeYears { get; init; }
    public int ExpiredDocuments { get; init; }
    public IReadOnlyList<StatusCountDto> ByStatus { get; init; } = [];
    public IReadOnlyList<AgeCountDto> ByAge { get; init; } = [];
    public IReadOnlyList<ExpiringDocumentDto> ExpiringSoon { get; init; } = [];
}

public sealed record WorkshopSectionDto
{
    public int ActiveReservations { get; init; }
    public int ReservationsToday { get; init; }
    public int ReservationsNext7Days { get; init; }
    public int VehiclesInWorkshop { get; init; }
    public int VehiclesWithRoutine { get; init; }
    public int ReadingsLast30Days { get; init; }
    public int CompletedLast30Days { get; init; }
    public double AverageWorkshopDays { get; init; }
    public IReadOnlyList<StatusCountDto> ByStatus { get; init; } = [];
    public IReadOnlyList<WorkshopCountDto> ByWorkshop { get; init; } = [];
    public IReadOnlyList<UpcomingReservationDto> Upcoming { get; init; } = [];
}

public sealed record DashboardDto
{
    public FleetSectionDto? Fleet { get; init; }
    public WorkshopSectionDto? Workshop { get; init; }
    public bool FleetAvailable { get; init; }
    public bool WorkshopAvailable { get; init; }
    public int CoverageRatio { get; init; }
    public int UtilizationRatio { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
}

public sealed record HomeViewModel
{
    public required AstraSystemsRental.Front.Shared.Security.AstraPrincipal Principal { get; init; }
    public DashboardDto? Dashboard { get; init; }
    public bool CanSeeFleet { get; init; }
    public bool CanSeeWorkshop { get; init; }
    public string? Error { get; init; }

    public IReadOnlyList<ExpiringDocumentDto> Attention =>
        Dashboard?.Fleet?.ExpiringSoon ?? [];

    public IReadOnlyList<UpcomingReservationDto> Agenda =>
        Dashboard?.Workshop?.Upcoming ?? [];
}
