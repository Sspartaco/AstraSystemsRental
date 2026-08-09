using AstraSystemsRental.Base.Api;
using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Maintenance.Api.Configuration;
using AstraSystemsRental.Maintenance.Api.Endpoints;
using AstraSystemsRental.Maintenance.Api.Persistence;
using AstraSystemsRental.Maintenance.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddAstraApi(new AstraApiOptions
{
    ServiceName = "AstraSystemsRental.Maintenance",
    PathBase = "/apiMaintenance"
});

builder.Services.AddAstraDbContext<AstraMaintenanceDbContext>(builder.Configuration);

builder.Services.Configure<MaintenanceOptions>(builder.Configuration.GetSection(MaintenanceOptions.SectionName));
builder.Services.Configure<UsersApiOptions>(builder.Configuration.GetSection(UsersApiOptions.SectionName));
builder.Services.Configure<VehiclesApiOptions>(builder.Configuration.GetSection(VehiclesApiOptions.SectionName));
builder.Services.Configure<MailApiOptions>(builder.Configuration.GetSection(MailApiOptions.SectionName));

builder.Services.AddScoped<IRoutineRepository, RoutineRepository>();
builder.Services.AddScoped<IRoutineAssignmentRepository, RoutineAssignmentRepository>();
builder.Services.AddScoped<IMileageReadingRepository, MileageReadingRepository>();
builder.Services.AddScoped<IWorkshopReservationRepository, WorkshopReservationRepository>();

builder.Services.AddScoped<IMaintenanceContextGuard, MaintenanceContextGuard>();
builder.Services.AddScoped<IRoutineService, RoutineService>();
builder.Services.AddScoped<IRoutineAssignmentService, RoutineAssignmentService>();
builder.Services.AddScoped<IMileageReadingService, MileageReadingService>();
builder.Services.AddScoped<IWorkshopReservationService, WorkshopReservationService>();
builder.Services.AddScoped<IMaintenanceMetricsService, MaintenanceMetricsService>();
builder.Services.AddSingleton<IPhotoStorage, LocalPhotoStorage>();

builder.Services.AddHttpClient<IUsersApiClient, UsersApiClient>((provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<UsersApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        client.BaseAddress = new Uri(options.BaseUrl);
}).AddStandardResilienceHandler();

builder.Services.AddHttpClient<IVehiclesApiClient, VehiclesApiClient>((provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<VehiclesApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        client.BaseAddress = new Uri(options.BaseUrl);
}).AddStandardResilienceHandler();

builder.Services.AddHttpClient<IMailApiClient, MailApiClient>((provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<MailApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        client.BaseAddress = new Uri(options.BaseUrl);
}).AddStandardResilienceHandler();

var app = builder.Build();

app.UseAstraPipeline();

app.MaintenanceRoutineEndpoints_Map();
app.RoutineAssignmentEndpoints_Map();
app.MileageReadingEndpoints_Map();
app.WorkshopReservationEndpoints_Map();
app.MaintenanceMetricsEndpoints_Map();

app.Run();

public partial class Program;
