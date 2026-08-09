using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Mail.Api.Dtos;
using AstraSystemsRental.Mail.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Mail.Api.Endpoints;

public static class MailEndpoints
{
    public static void MailEndpoints_Map(this IEndpointRouteBuilder app)
    {
        app.MapPost("/mail/welcome", async (
                [FromBody] SendWelcomeRequest request,
                [FromServices] IMailService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.SendWelcomeAsync(request, cancellationToken);
                return result.ToResult(context);
            })
            .WithName("SendWelcomeMail")
            .WithSummary("Sends the welcome email with the account confirmation link")
            .WithDescription("Renders the HTML5 welcome template and delivers it through the configured Gmail SMTP relay.")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .WithTags("Mail");
    }
}
