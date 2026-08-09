namespace AstraSystemsRental.Vehicles.Api.Domain;

public class FleetVehicleStatusHistory
{
    public long Id { get; set; }
    public long FleetVehicleId { get; set; }
    public FleetVehicleStatus? PreviousStatus { get; set; }
    public FleetVehicleStatus NewStatus { get; set; }
    public string? Reason { get; set; }
    public long ChangedByUserId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
