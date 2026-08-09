using AstraSystemsRental.Mobile.Services;
using AstraSystemsRental.Mobile.ViewModels;
using AstraSystemsRental.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace AstraSystemsRental.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(_ => { });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<ISessionStore, SessionStore>();
        builder.Services.AddSingleton<IOfflineQueue, OfflineQueue>();

        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri(AppConfig.GatewayBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        });

        builder.Services.AddSingleton<IAstraApiClient, AstraApiClient>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();

        builder.Services.AddSingleton<LoginViewModel>();
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddSingleton<FleetViewModel>();
        builder.Services.AddSingleton<TrackingViewModel>();
        builder.Services.AddSingleton<ReservationsViewModel>();
        builder.Services.AddSingleton<RoutinesViewModel>();
        builder.Services.AddSingleton<ProfileViewModel>();
        builder.Services.AddSingleton<UsersViewModel>();
        builder.Services.AddTransient<VehicleDetailViewModel>();

        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<DashboardPage>();
        builder.Services.AddSingleton<FleetPage>();
        builder.Services.AddSingleton<TrackingPage>();
        builder.Services.AddSingleton<ReservationsPage>();
        builder.Services.AddSingleton<RoutinesPage>();
        builder.Services.AddSingleton<ProfilePage>();
        builder.Services.AddSingleton<UsersPage>();
        builder.Services.AddTransient<VehicleDetailPage>();

        return builder.Build();
    }
}

public static class AppConfig
{
    /// <summary>
    /// 10.0.2.2 es el host de la maquina de desarrollo visto desde el emulador de Android.
    /// En un dispositivo real hay que apuntar a la IP de la red local o a la URL publica.
    /// </summary>
    public const string GatewayBaseUrl = "http://10.0.2.2:8080";

    public const string NodeFleet = "vehicle-registry";
    public const string NodeTracking = "maintenance-tracking";
    public const string NodeRoutines = "maintenance-routines";
    public const string NodeReservations = "workshop-reservations";
    public const string NodeReports = "reports";
}
