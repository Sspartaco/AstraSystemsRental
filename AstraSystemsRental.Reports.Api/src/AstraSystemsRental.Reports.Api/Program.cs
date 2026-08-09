using AstraSystemsRental.Base.Api;
using AstraSystemsRental.Reports.Api.Configuration;
using AstraSystemsRental.Reports.Api.Endpoints;
using AstraSystemsRental.Reports.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddAstraApi(new AstraApiOptions
{
    ServiceName = "AstraSystemsRental.Reports",
    PathBase = "/apiReports"
});

builder.Services.Configure<VehiclesApiOptions>(builder.Configuration.GetSection(VehiclesApiOptions.SectionName));
builder.Services.Configure<MaintenanceApiOptions>(builder.Configuration.GetSection(MaintenanceApiOptions.SectionName));

builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddHttpClient<IFleetMetricsSource, FleetMetricsSource>((provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<VehiclesApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        client.BaseAddress = new Uri(options.BaseUrl);
}).AddStandardResilienceHandler();

builder.Services.AddHttpClient<IWorkshopMetricsSource, WorkshopMetricsSource>((provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<MaintenanceApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        client.BaseAddress = new Uri(options.BaseUrl);
}).AddStandardResilienceHandler();

var app = builder.Build();

app.UseAstraPipeline();

app.ReportsEndpoints_Map();

app.Run();

public partial class Program;
