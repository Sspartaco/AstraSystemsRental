namespace AstraSystemsRental.Users.Api.Services;

public interface IMailClient
{
    Task<bool> SendWelcomeAsync(string toEmail, string displayName, string confirmationUrl, CancellationToken cancellationToken);
}
