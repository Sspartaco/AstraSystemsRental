namespace AstraSystemsRental.Vehicles.Api.Configuration;

public sealed class ValuationOptions
{
    public const string SectionName = "Valuation";

    public int CacheTtlDays { get; set; } = 7;
    public int FailedCacheTtlMinutes { get; set; } = 15;
    public int SourceTimeoutSeconds { get; set; } = 8;
    public int MaxConcurrentFetches { get; set; } = 4;
    public int PurgeCompletedRequestsAfterHours { get; set; } = 24;
}
