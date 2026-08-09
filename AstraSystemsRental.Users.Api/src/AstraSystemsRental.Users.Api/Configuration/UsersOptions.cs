namespace AstraSystemsRental.Users.Api.Configuration;

public sealed class MailClientOptions
{
    public const string SectionName = "MailApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string InternalApiKey { get; set; } = string.Empty;
}

public sealed class ConfirmationOptions
{
    public const string SectionName = "Confirmation";

    public string BaseUrl { get; set; } = "https://astra.local/confirm";
    public int TokenLifetimeHours { get; set; } = 48;
    public string DefaultRoleCode { get; set; } = "Demo";
    public string DefaultPlanCode { get; set; } = "Demo";
}

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public string Secret { get; set; } = string.Empty;
}
