using System.Net.Http.Json;
using AstraSystemsRental.Users.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AstraSystemsRental.Users.Api.Services;

public sealed class MailClient(HttpClient httpClient, IOptions<MailClientOptions> options, ILogger<MailClient> logger) : IMailClient
{
    private readonly MailClientOptions _options = options.Value;

    public async Task<bool> SendWelcomeAsync(string toEmail, string displayName, string confirmationUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/apiMail/mail/welcome");
        request.Headers.Add("X-Internal-Api-Key", _options.InternalApiKey);
        request.Content = JsonContent.Create(new { toEmail, displayName, confirmationUrl });

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return true;

            logger.LogWarning("Mail API returned {StatusCode} when sending welcome email to {Email}", response.StatusCode, toEmail);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reach Mail API for welcome email to {Email}", toEmail);
            return false;
        }
    }
}
