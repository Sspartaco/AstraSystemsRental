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
    /// IP de la maquina de desarrollo en la red local (Wi-Fi de la casa/oficina).
    /// La usan los dispositivos fisicos: iPhone, Android real y el simulador de iOS.
    /// Cambiar si cambia la IP del equipo (ipconfig / Get-NetIPAddress).
    /// </summary>
    public const string LanHost = "192.168.40.100";

    private const int GatewayPort = 8080;

    /// <summary>
    /// El emulador de Android no ve la red local igual que un telefono: 10.0.2.2 es un
    /// alias interno hacia el host. Un iPhone (o cualquier dispositivo fisico) necesita
    /// la IP real del equipo, y por eso ambos casos se resuelven por separado.
    /// Se puede sobreescribir en tiempo de ejecucion desde Mi cuenta.
    /// </summary>
    public static string GatewayBaseUrl
    {
        get
        {
            var custom = Preferences.Default.Get(GatewayOverrideKey, string.Empty);

            if (!string.IsNullOrWhiteSpace(custom))
                return custom;

            var host = DeviceInfo.Current.Platform == DevicePlatform.Android
                       && DeviceInfo.Current.DeviceType == DeviceType.Virtual
                ? "10.0.2.2"
                : LanHost;

            return $"http://{host}:{GatewayPort}";
        }
    }

    public const string GatewayOverrideKey = "astra.gateway.url";

    public static string DefaultGatewayUrl => $"http://{LanHost}:{GatewayPort}";

    public const string NodeFleet = "vehicle-registry";
    public const string NodeTracking = "maintenance-tracking";
    public const string NodeRoutines = "maintenance-routines";
    public const string NodeReservations = "workshop-reservations";
    public const string NodeReports = "reports";
}
