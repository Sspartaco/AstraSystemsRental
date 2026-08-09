namespace AstraSystemsRental.Maintenance.Api.Configuration;

public sealed class UsersApiOptions
{
    public const string SectionName = "UsersApi";

    public string BaseUrl { get; set; } = string.Empty;
}

public sealed class VehiclesApiOptions
{
    public const string SectionName = "VehiclesApi";

    public string BaseUrl { get; set; } = string.Empty;
}

public sealed class MailApiOptions
{
    public const string SectionName = "MailApi";

    public string BaseUrl { get; set; } = string.Empty;
}

public sealed class MaintenanceOptions
{
    public const string SectionName = "Maintenance";

    public int DailyProjectionKm { get; set; } = 600;
    public int MaxKilometers { get; set; } = 1_000_000;
    public int MaxHours { get; set; } = 50_000;
}
