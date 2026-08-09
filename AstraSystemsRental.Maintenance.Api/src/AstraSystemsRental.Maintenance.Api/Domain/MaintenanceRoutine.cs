namespace AstraSystemsRental.Maintenance.Api.Domain;

public enum MeasurementUnit
{
    Kilometers = 0,
    Hours = 1,
    Days = 2
}

public class MaintenanceRoutine
{
    public long Id { get; set; }
    public string OwnerType { get; set; } = Base.Security.OwnerType.User;
    public long OwnerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[]? RowVersion { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class MaintenanceRoutinePeriodicity
{
    public long Id { get; set; }
    public long RoutineId { get; set; }
    public MeasurementUnit Unit { get; set; }
    public int StartsAt { get; set; }
    public int RepeatsEvery { get; set; }
}

public class MaintenanceRoutineConcept
{
    public long Id { get; set; }
    public long PeriodicityId { get; set; }
    public required string Name { get; set; }
    public decimal Quantity { get; set; }
    public MeasurementUnit? QuantityUnit { get; set; }
    public string? Notes { get; set; }
}
