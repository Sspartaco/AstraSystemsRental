namespace AstraSystemsRental.Contracts.Maintenance;

public sealed record MileageReadingDto
{
    public long Id { get; init; }
    public long FleetVehicleId { get; init; }
    public string ReadingType { get; init; } = "Kilometers";
    public DateOnly ReadingDate { get; init; }
    public int Value { get; init; }
    public string Source { get; init; } = "Manual";
    public long? SourceReservationId { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record CreateMileageReadingDto
{
    public int Value { get; init; }
    public DateOnly ReadingDate { get; init; }
    public string? Notes { get; init; }
}

public sealed record NextMaintenanceDto
{
    public bool HasRoutine { get; init; }
    public string? RoutineName { get; init; }
    public string? Unit { get; init; }
    public int? CurrentValue { get; init; }
    public int? NextThreshold { get; init; }
    public int? Remaining { get; init; }
    public int? Overdue { get; init; }
    public bool IsOverdue { get; init; }
    public DateOnly? LastReadingDate { get; init; }
}

public sealed record ReservationPhotoDto
{
    public long Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string StoragePath { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record WorkshopReservationDto
{
    public long Id { get; init; }
    public long FleetVehicleId { get; init; }
    public long? ProviderId { get; init; }
    public string? ProviderName { get; init; }
    public string Status { get; init; } = "Pending";
    public DateTime ScheduledAtUtc { get; init; }
    public DateTime? ExpectedEndAtUtc { get; init; }
    public DateTime? PickedUpAtUtc { get; init; }
    public DateTime? ReadyAtUtc { get; init; }
    public DateTime? CollectedAtUtc { get; init; }
    public int? MileageAtReservation { get; init; }
    public bool IsWashOnly { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<ReservationPhotoDto> Photos { get; init; } = [];

    public bool IsActive => Status is "Pending" or "InWorkshop" or "Ready";
}

public sealed record CreateWorkshopReservationDto
{
    public long FleetVehicleId { get; init; }
    public long? ProviderId { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public DateTime? ExpectedEndAtUtc { get; init; }
    public int? MileageAtReservation { get; init; }
    public bool IsWashOnly { get; init; }
    public string? Notes { get; init; }
}

public sealed record WorkshopProviderDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ProviderType { get; init; } = "Company";
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }
    public bool IsActive { get; init; }
}

public sealed record RoutineConceptDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string? Notes { get; init; }
}

public sealed record RoutinePeriodicityDto
{
    public long Id { get; init; }
    public string Unit { get; init; } = string.Empty;
    public int StartsAt { get; init; }
    public int RepeatsEvery { get; init; }
    public IReadOnlyList<RoutineConceptDto> Concepts { get; init; } = [];
}

public sealed record MaintenanceRoutineDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<RoutinePeriodicityDto> Periodicities { get; init; } = [];
}

public sealed record RoutineAssignmentDto
{
    public long Id { get; init; }
    public long FleetVehicleId { get; init; }
    public long RoutineId { get; init; }
    public string RoutineName { get; init; } = string.Empty;
    public DateTime AssignedAtUtc { get; init; }
}
