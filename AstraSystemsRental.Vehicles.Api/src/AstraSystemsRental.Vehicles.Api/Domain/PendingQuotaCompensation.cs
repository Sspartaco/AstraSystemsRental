namespace AstraSystemsRental.Vehicles.Api.Domain;

public class PendingQuotaCompensation
{
    public long Id { get; set; }
    public string NodeKey { get; set; } = string.Empty;
    public string OwnerType { get; set; } = AstraSystemsRental.Base.Security.OwnerType.User;
    public long OwnerId { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
