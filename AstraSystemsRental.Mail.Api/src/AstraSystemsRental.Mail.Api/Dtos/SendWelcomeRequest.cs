namespace AstraSystemsRental.Mail.Api.Dtos;

public sealed record SendWelcomeRequest(string ToEmail, string DisplayName, string ConfirmationUrl);

public sealed record WelcomeModel(string DisplayName, string ConfirmationUrl, int Year);
