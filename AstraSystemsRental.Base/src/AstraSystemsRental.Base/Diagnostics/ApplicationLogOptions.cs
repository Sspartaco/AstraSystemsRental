namespace AstraSystemsRental.Base.Diagnostics;

public sealed class ApplicationLogOptions
{
    public const string SectionName = "ApplicationLogs";

    public bool Enabled { get; set; } = true;
    public string ConnectionString { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
}
