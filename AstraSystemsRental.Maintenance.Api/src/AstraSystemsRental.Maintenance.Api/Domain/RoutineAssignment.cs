namespace AstraSystemsRental.Maintenance.Api.Domain;

public class RoutineAssignment
{
    public long Id { get; set; }
    public string OwnerType { get; set; } = Base.Security.OwnerType.User;
    public long OwnerId { get; set; }
    public long FleetVehicleId { get; set; }
    public long RoutineId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public long AssignedByUserId { get; set; }
    public byte[]? RowVersion { get; set; }

    public void UpdateRoutine(long newRoutineId, long changedByUserId)
    {
        RoutineId = newRoutineId;
        AssignedByUserId = changedByUserId;
        AssignedAtUtc = DateTime.UtcNow;
    }
}

public class RoutineAssignmentHistory
{
    public long Id { get; set; }
    public long FleetVehicleId { get; set; }
    public long? PreviousRoutineId { get; set; }
    public long NewRoutineId { get; set; }
    public long ChangedByUserId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
