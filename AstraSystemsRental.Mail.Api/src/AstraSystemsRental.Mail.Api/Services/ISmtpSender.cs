namespace AstraSystemsRental.Mail.Api.Services;

public interface ISmtpSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
