namespace AstraSystemsRental.Reports.Api.Configuration;

public sealed class VehiclesApiOptions
{
    public const string SectionName = "VehiclesApi";

    public string BaseUrl { get; set; } = string.Empty;
}

public sealed class MaintenanceApiOptions
{
    public const string SectionName = "MaintenanceApi";

    public string BaseUrl { get; set; } = string.Empty;
}
