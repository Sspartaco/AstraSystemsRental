using AstraSystemsRental.Base.Api;
using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Vehicles.Api.Cli;
using AstraSystemsRental.Vehicles.Api.Configuration;
using AstraSystemsRental.Vehicles.Api.Endpoints;
using AstraSystemsRental.Vehicles.Api.Persistence;
using AstraSystemsRental.Vehicles.Api.Services;
using AstraSystemsRental.Vehicles.Api.Services.ValuationSources;

if (args.Length > 0 && args[0] == ImportVehicleCatalogCommand.CommandName)
    return await ImportVehicleCatalogCommand.RunAsync(args);

var builder = WebApplication.CreateBuilder(args);

builder.AddAstraApi(new AstraApiOptions
{
    ServiceName = "AstraSystemsRental.Vehicles",
    PathBase = "/apiVehicles"
});

builder.Services.AddAstraDbContext<AstraVehiclesDbContext>(builder.Configuration);
builder.Services.Configure<ValuationOptions>(builder.Configuration.GetSection(ValuationOptions.SectionName));
builder.Services.Configure<UsersApiOptions>(builder.Configuration.GetSection(UsersApiOptions.SectionName));

builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<IVehicleQuoteService, VehicleQuoteService>();
builder.Services.AddScoped<IVehicleImportService, VehicleImportService>();
builder.Services.AddScoped<IFleetVehicleRepository, FleetVehicleRepository>();
builder.Services.AddScoped<IFleetVehicleService, FleetVehicleService>();
builder.Services.AddScoped<IFleetMetricsService, FleetMetricsService>();

builder.Services.AddHttpClient<IUsersApiClient, UsersApiClient>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<UsersApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddHttpClient<WikimediaVehicleImageSource>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AstraSystemsRental/1.0 (vehicle-image-lookup)");
}).AddStandardResilienceHandler();
builder.Services.AddScoped<IVehicleImageSource>(sp => sp.GetRequiredService<WikimediaVehicleImageSource>());

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<TucarroValuationSource>(ConfigureScrapingClient).AddStandardResilienceHandler();
builder.Services.AddHttpClient<AsoUsadosValuationSource>(ConfigureScrapingClient).AddStandardResilienceHandler();
builder.Services.AddHttpClient<RevistaMotorValuationSource>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36");
}).AddStandardResilienceHandler();

builder.Services.AddScoped<IValuationSource>(sp => sp.GetRequiredService<TucarroValuationSource>());
builder.Services.AddScoped<IValuationSource>(sp => sp.GetRequiredService<AsoUsadosValuationSource>());
builder.Services.AddScoped<IValuationSource>(sp => sp.GetRequiredService<RevistaMotorValuationSource>());
builder.Services.AddScoped<IValuationSource, FasecoldaValuationSource>();
builder.Services.AddScoped<IValuationSource, OtrasFuentesValuationSource>();

builder.Services.AddSingleton<QuoteOrchestrationService>();
builder.Services.AddSingleton<IQuoteOrchestrator>(sp => sp.GetRequiredService<QuoteOrchestrationService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<QuoteOrchestrationService>());

var app = builder.Build();

app.UseAstraPipeline();

app.VehicleEndpoints_Map();
app.QuoteEndpoints_Map();
app.FleetVehicleEndpoints_Map();
app.FleetMetricsEndpoints_Map();

app.Run();

return 0;

static void ConfigureScrapingClient(HttpClient client)
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es-CO,es;q=0.9");
}

public partial class Program;
