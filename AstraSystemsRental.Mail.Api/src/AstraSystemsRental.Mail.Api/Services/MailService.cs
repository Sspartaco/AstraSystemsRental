using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Mail.Api.Dtos;

namespace AstraSystemsRental.Mail.Api.Services;

public sealed class MailService(ITemplateRenderer renderer, ISmtpSender sender) : IMailService
{
    private const string WelcomeTemplate = "Welcome.cshtml";
    private const string WelcomeSubject = "Welcome to AstraSystemsRental — confirm your account";

    public async Task<OperationResult> SendWelcomeAsync(SendWelcomeRequest request, CancellationToken cancellationToken = default)
    {
        var guard = new Guard()
            .Email(request.ToEmail, "ToEmail")
            .NotEmpty(request.DisplayName, "DisplayName")
            .NotEmpty(request.ConfirmationUrl, "ConfirmationUrl");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var model = new WelcomeModel(request.DisplayName, request.ConfirmationUrl, DateTime.UtcNow.Year);
        var html = await renderer.RenderAsync(WelcomeTemplate, model);

        await sender.SendAsync(request.ToEmail, WelcomeSubject, html, cancellationToken);

        return OperationResult.Ok(new { sent = true, request.ToEmail });
    }
}
