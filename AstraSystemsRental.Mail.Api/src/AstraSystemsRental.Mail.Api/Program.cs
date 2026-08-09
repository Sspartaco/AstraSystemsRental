using AstraSystemsRental.Base.Api;
using AstraSystemsRental.Mail.Api.Configuration;
using AstraSystemsRental.Mail.Api.Endpoints;
using AstraSystemsRental.Mail.Api.Security;
using AstraSystemsRental.Mail.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddAstraApi(new AstraApiOptions
{
    ServiceName = "AstraSystemsRental.Mail",
    PathBase = "/apiMail",
    EnableJwtAuthentication = false
});

builder.Services.Configure<GmailOptions>(builder.Configuration.GetSection(GmailOptions.SectionName));
builder.Services.AddSingleton<ITemplateRenderer, RazorTemplateRenderer>();
builder.Services.AddSingleton<ISmtpSender, GmailSmtpSender>();
builder.Services.AddScoped<IMailService, MailService>();

var app = builder.Build();

app.UseAstraPipeline();
app.UseMiddleware<InternalApiKeyMiddleware>();

app.MailEndpoints_Map();

app.Run();

public partial class Program;
