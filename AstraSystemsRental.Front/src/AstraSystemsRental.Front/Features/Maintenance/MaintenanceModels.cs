namespace AstraSystemsRental.Front.Features.Maintenance;

public sealed record ConceptDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string? QuantityUnit { get; init; }
    public string? Notes { get; init; }
}

public sealed record PeriodicityDto
{
    public long Id { get; init; }
    public string Unit { get; init; } = string.Empty;
    public int StartsAt { get; init; }
    public int RepeatsEvery { get; init; }
    public IReadOnlyList<ConceptDto> Concepts { get; init; } = [];
}

public sealed record RoutineDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<PeriodicityDto> Periodicities { get; init; } = [];
}

public sealed record PagedDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
}

public sealed record RoutineAssignmentDto
{
    public long Id { get; init; }
    public long FleetVehicleId { get; init; }
    public long RoutineId { get; init; }
    public string RoutineName { get; init; } = string.Empty;
    public DateTime AssignedAtUtc { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public sealed record AssignmentHistoryDto(long? PreviousRoutineId, string? PreviousRoutineName, long NewRoutineId, string NewRoutineName, long ChangedByUserId, DateTime ChangedAtUtc);

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

public sealed record ReservationPhotoDto(long Id, string Status, string FileName, string StoragePath, string? Notes, DateTime CreatedAtUtc);

public sealed record ReservationDto
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
    public string RowVersion { get; init; } = string.Empty;
    public IReadOnlyList<ReservationPhotoDto> Photos { get; init; } = [];
}

public sealed record ProviderDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ProviderType { get; init; } = "Company";
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }
    public bool IsActive { get; init; }
}

public sealed record FleetVehicleOptionVm(long Id, string PlateNumber, string? Brand, string? Line, string Status);

public sealed record VehicleMaintenanceVm
{
    public required FleetVehicleOptionVm Vehicle { get; init; }
    public RoutineAssignmentDto? Assignment { get; init; }
    public NextMaintenanceDto? NextMaintenance { get; init; }
    public IReadOnlyList<MileageReadingDto> Readings { get; init; } = [];
    public IReadOnlyList<ReservationDto> Reservations { get; init; } = [];
    public IReadOnlyList<AssignmentHistoryDto> History { get; init; } = [];
    public IReadOnlyList<RoutineDto> AvailableRoutines { get; init; } = [];
    public IReadOnlyList<ProviderDto> Providers { get; init; } = [];
    public bool CanManageRoutines { get; init; }
    public bool CanManageReservations { get; init; }
    public string InitialTab { get; init; } = "routine";
    public string? Error { get; init; }
}

public sealed record MaintenanceIndexVm
{
    public IReadOnlyList<FleetVehicleOptionVm> Vehicles { get; init; } = [];
    public string? Search { get; init; }
    public string? Error { get; init; }
}

public sealed record RoutinesIndexVm
{
    public IReadOnlyList<RoutineDto> Items { get; init; } = [];
    public int PageNumber { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public string? Search { get; init; }
    public string? Error { get; init; }
}

public sealed record ReservationListItemVm
{
    public required ReservationDto Reservation { get; init; }
    public string? PlateNumber { get; init; }
    public string? Brand { get; init; }
    public string? Line { get; init; }
}

public sealed record ReservationsIndexVm
{
    public IReadOnlyList<ReservationListItemVm> Items { get; init; } = [];
    public int PageNumber { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public string? Status { get; init; }
    public long? ProviderId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public IReadOnlyList<FleetVehicleOptionVm> Vehicles { get; init; } = [];
    public IReadOnlyList<ProviderDto> Providers { get; init; } = [];
    public string? Error { get; init; }

    public IReadOnlyList<IGrouping<DateOnly, ReservationListItemVm>> Agenda =>
        Items.GroupBy(i => DateOnly.FromDateTime(i.Reservation.ScheduledAtUtc))
             .OrderBy(g => g.Key)
             .ToList();
}

public sealed record CreateGlobalReservationForm
{
    public long FleetVehicleId { get; init; }
    public long? ProviderId { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public DateTime? ExpectedEndAtUtc { get; init; }
    public int? MileageAtReservation { get; init; }
    public bool IsWashOnly { get; init; }
    public string? Notes { get; init; }
}

public sealed record CreateRoutineForm(string Name, string? Description);
public sealed record AddPeriodicityForm
{
    public string Unit { get; init; } = "Kilometers";
    public int StartsAt { get; init; }
    public int RepeatsEvery { get; init; }
}

public sealed record AddConceptForm
{
    public long PeriodicityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Quantity { get; init; } = 1;
    public string? Notes { get; init; }
}

public sealed record AssignRoutineForm
{
    public long RoutineId { get; init; }
}

public sealed record AddReadingForm
{
    public int Value { get; init; }
    public DateOnly ReadingDate { get; init; }
    public string? Notes { get; init; }
}
public sealed record CreateReservationForm
{
    public long? ProviderId { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public DateTime? ExpectedEndAtUtc { get; init; }
    public int? MileageAtReservation { get; init; }
    public bool IsWashOnly { get; init; }
    public string? Notes { get; init; }
}
public sealed record ChangeReservationStatusForm(string NewStatus);
public sealed record CreateProviderForm(string Name, string? ProviderType, string? ContactPhone, string? ContactEmail);
