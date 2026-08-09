using AstraSystemsRental.Base.Api;
using AstraSystemsRental.Gateway.Access;

var builder = WebApplication.CreateBuilder(args);

builder.AddAstraApi(new AstraApiOptions
{
    ServiceName = "AstraSystemsRental.Gateway",
    PathBase = "/",
    EnableRateLimiting = true
});

builder.Services.AddSingleton<IAccessEvaluator, AccessEvaluator>();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAstraPipeline();
app.UseMiddleware<AccessControlMiddleware>();

app.MapReverseProxy();

app.Run();

public partial class Program;
