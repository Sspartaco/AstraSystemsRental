using AstraSystemsRental.Maintenance.Api.Domain;

namespace AstraSystemsRental.Maintenance.Api.Dtos;

public sealed record CreateRoutineRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public sealed record UpdateRoutineRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
    public required string RowVersion { get; init; }
}

public sealed record CreatePeriodicityRequest(string Unit, int StartsAt, int RepeatsEvery);

public sealed record CreateConceptRequest(long PeriodicityId, string Name, decimal Quantity, string? QuantityUnit, string? Notes);

public sealed record CopyRoutineRequest(string NewName);

public sealed record AssignRoutineRequest(long RoutineId);

public sealed record CreateMileageReadingRequest
{
    public required int Value { get; init; }
    public required DateOnly ReadingDate { get; init; }
    public string? ReadingType { get; init; }
    public string? Notes { get; init; }
}

public sealed record CreateWorkshopReservationRequest
{
    public required long FleetVehicleId { get; init; }
    public long? ProviderId { get; init; }
    public required DateTime ScheduledAtUtc { get; init; }
    public DateTime? ExpectedEndAtUtc { get; init; }
    public int? MileageAtReservation { get; init; }
    public bool IsWashOnly { get; init; }
    public string? Notes { get; init; }
}

public sealed record UpdateWorkshopReservationRequest
{
    public long? ProviderId { get; init; }
    public required DateTime ScheduledAtUtc { get; init; }
    public DateTime? ExpectedEndAtUtc { get; init; }
    public bool IsWashOnly { get; init; }
    public string? Notes { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record ChangeReservationStatusRequest(string NewStatus);

public sealed record CreateWorkshopProviderRequest
{
    public required string Name { get; init; }
    public string? ProviderType { get; init; }
    public string? DocumentNumber { get; init; }
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }
    public string? Address { get; init; }
}

public sealed record RoutineResponse
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public required string RowVersion { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
    public IReadOnlyList<PeriodicityResponse> Periodicities { get; init; } = [];
}

public sealed record PeriodicityResponse
{
    public required long Id { get; init; }
    public required string Unit { get; init; }
    public required int StartsAt { get; init; }
    public required int RepeatsEvery { get; init; }
    public IReadOnlyList<ConceptResponse> Concepts { get; init; } = [];
}

public sealed record ConceptResponse
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required decimal Quantity { get; init; }
    public string? QuantityUnit { get; init; }
    public string? Notes { get; init; }
}

public sealed record RoutineAssignmentResponse
{
    public required long Id { get; init; }
    public required long FleetVehicleId { get; init; }
    public required long RoutineId { get; init; }
    public required string RoutineName { get; init; }
    public required DateTime AssignedAtUtc { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record RoutineAssignmentHistoryResponse(long? PreviousRoutineId, string? PreviousRoutineName, long NewRoutineId, string NewRoutineName, long ChangedByUserId, DateTime ChangedAtUtc);

public sealed record MileageReadingResponse
{
    public required long Id { get; init; }
    public required long FleetVehicleId { get; init; }
    public required string ReadingType { get; init; }
    public required DateOnly ReadingDate { get; init; }
    public required int Value { get; init; }
    public required string Source { get; init; }
    public long? SourceReservationId { get; init; }
    public string? Notes { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}

public sealed record NextMaintenanceResponse
{
    public required bool HasRoutine { get; init; }
    public string? RoutineName { get; init; }
    public string? Unit { get; init; }
    public int? CurrentValue { get; init; }
    public int? NextThreshold { get; init; }
    public int? Remaining { get; init; }
    public int? Overdue { get; init; }
    public bool IsOverdue { get; init; }
    public DateOnly? LastReadingDate { get; init; }
}

public sealed record WorkshopReservationResponse
{
    public required long Id { get; init; }
    public required long FleetVehicleId { get; init; }
    public long? ProviderId { get; init; }
    public string? ProviderName { get; init; }
    public required string Status { get; init; }
    public required DateTime ScheduledAtUtc { get; init; }
    public DateTime? ExpectedEndAtUtc { get; init; }
    public DateTime? PickedUpAtUtc { get; init; }
    public DateTime? ReadyAtUtc { get; init; }
    public DateTime? CollectedAtUtc { get; init; }
    public int? MileageAtReservation { get; init; }
    public required bool IsWashOnly { get; init; }
    public string? Notes { get; init; }
    public required string RowVersion { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<ReservationPhotoResponse> Photos { get; init; } = [];
}

public sealed record ReservationPhotoResponse(long Id, string Status, string FileName, string StoragePath, string? Notes, DateTime CreatedAtUtc);

public sealed record WorkshopProviderResponse
{
    public required long Id { get; init; }
    public required string Name { get; init; }
    public required string ProviderType { get; init; }
    public string? DocumentNumber { get; init; }
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }
    public string? Address { get; init; }
    public required bool IsActive { get; init; }
}
