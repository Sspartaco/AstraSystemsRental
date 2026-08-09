namespace AstraSystemsRental.Vehicles.Api.Domain;

public enum ValuationStatus
{
    Pending = 0,
    Success = 1,
    NotFound = 2,
    Failed = 3
}

public class ValuationCacheEntry
{
    public long Id { get; set; }
    public required string PlateNumber { get; set; }
    public required string SourceCode { get; set; }
    public ValuationStatus Status { get; set; }
    public decimal? ValueMin { get; set; }
    public decimal? ValueMax { get; set; }
    public decimal? ValueAvg { get; set; }
    public string Currency { get; set; } = "COP";
    public string? RawPayload { get; set; }
    public DateTime? FetchedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
