namespace AstraSystemsRental.Vehicles.Api.Domain;

public enum QuoteRequestStatus
{
    Running = 0,
    Completed = 1,
    CompletedWithErrors = 2
}

public class QuoteRequest
{
    public long Id { get; set; }
    public Guid RequestId { get; set; }
    public required string PlateNumber { get; set; }
    public long RequestedByUserId { get; set; }
    public QuoteRequestStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
