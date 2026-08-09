namespace AstraSystemsRental.Mail.Api.Configuration;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "AstraSystemsRental";
}
