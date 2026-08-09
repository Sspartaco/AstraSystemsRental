namespace AstraSystemsRental.Maintenance.Api.Domain;

public enum WorkshopReservationStatus
{
    Pending = 0,
    InWorkshop = 1,
    Ready = 2,
    Collected = 3,
    Cancelled = 4
}

public enum WorkshopProviderType
{
    Individual = 0,
    Company = 1
}

public sealed class InvalidStatusTransitionException(WorkshopReservationStatus from, WorkshopReservationStatus to)
    : Exception($"Cannot transition a reservation from {from} to {to}.")
{
    public WorkshopReservationStatus From { get; } = from;
    public WorkshopReservationStatus To { get; } = to;
}

public class WorkshopReservation
{
    public long Id { get; set; }
    public string OwnerType { get; set; } = Base.Security.OwnerType.User;
    public long OwnerId { get; set; }
    public long FleetVehicleId { get; set; }
    public long? ProviderId { get; set; }
    public WorkshopReservationStatus Status { get; set; } = WorkshopReservationStatus.Pending;
    public DateTime ScheduledAtUtc { get; set; }
    public DateTime? ExpectedEndAtUtc { get; set; }
    public DateTime? PickedUpAtUtc { get; set; }
    public DateTime? ReadyAtUtc { get; set; }
    public DateTime? CollectedAtUtc { get; set; }
    public int? MileageAtReservation { get; set; }
    public bool IsWashOnly { get; set; }
    public string? Notes { get; set; }
    public long ReservedByUserId { get; set; }
    public byte[]? RowVersion { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public bool IsActive => Status is WorkshopReservationStatus.Pending
        or WorkshopReservationStatus.InWorkshop
        or WorkshopReservationStatus.Ready;

    public void TransitionTo(WorkshopReservationStatus next, DateTime nowUtc)
    {
        if (!CanTransitionTo(next))
            throw new InvalidStatusTransitionException(Status, next);

        Status = next;
        UpdatedAtUtc = nowUtc;

        switch (next)
        {
            case WorkshopReservationStatus.InWorkshop:
                PickedUpAtUtc = nowUtc;
                break;
            case WorkshopReservationStatus.Ready:
                ReadyAtUtc = nowUtc;
                break;
            case WorkshopReservationStatus.Collected:
                CollectedAtUtc = nowUtc;
                break;
        }
    }

    public bool CanTransitionTo(WorkshopReservationStatus next) => Status switch
    {
        WorkshopReservationStatus.Pending => next is WorkshopReservationStatus.InWorkshop or WorkshopReservationStatus.Cancelled,
        WorkshopReservationStatus.InWorkshop => next is WorkshopReservationStatus.Ready or WorkshopReservationStatus.Cancelled,
        WorkshopReservationStatus.Ready => next is WorkshopReservationStatus.Collected,
        _ => false
    };
}

public class WorkshopProvider
{
    public long Id { get; set; }
    public string OwnerType { get; set; } = Base.Security.OwnerType.User;
    public long OwnerId { get; set; }
    public WorkshopProviderType ProviderType { get; set; }
    public required string Name { get; set; }
    public string? DocumentNumber { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class WorkshopReservationPhoto
{
    public long Id { get; set; }
    public long WorkshopReservationId { get; set; }
    public WorkshopReservationStatus Status { get; set; }
    public required string FileName { get; set; }
    public required string StoragePath { get; set; }
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string? Notes { get; set; }
    public long UploadedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
