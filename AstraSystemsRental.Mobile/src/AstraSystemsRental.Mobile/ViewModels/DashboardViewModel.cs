using System.Collections.ObjectModel;
using System.Windows.Input;
using AstraSystemsRental.Contracts.Display;
using AstraSystemsRental.Contracts.Reports;
using AstraSystemsRental.Mobile.Services;

namespace AstraSystemsRental.Mobile.ViewModels;

public sealed record AgendaItem(string Time, string Day, string Vehicle, string Provider, string Status, long FleetVehicleId);

public sealed record AttentionItem(string Plate, string DocumentType, string Detail, bool IsExpired, long FleetVehicleId);

public sealed class DashboardViewModel : BaseViewModel
{
    private readonly IAstraApiClient _api;
    private readonly ISyncService _sync;

    private DashboardDto? _dashboard;
    private string _greeting = "Hola";

    public DashboardViewModel(IAstraApiClient api, ISyncService sync)
    {
        _api = api;
        _sync = sync;
        LoadCommand = new Command(async () => await LoadAsync());
        SyncCommand = new Command(async () => await SyncNowAsync());
    }

    public ObservableCollection<AgendaItem> Agenda { get; } = [];
    public ObservableCollection<AttentionItem> Attention { get; } = [];

    public ICommand LoadCommand { get; }
    public ICommand SyncCommand { get; }

    public string Greeting
    {
        get => _greeting;
        private set => Set(ref _greeting, value);
    }

    public int ActiveVehicles => _dashboard?.Fleet?.ActiveVehicles ?? 0;
    public int TotalVehicles => _dashboard?.Fleet?.TotalVehicles ?? 0;
    public int InWorkshop => _dashboard?.Workshop?.VehiclesInWorkshop ?? 0;
    public int ActiveReservations => _dashboard?.Workshop?.ActiveReservations ?? 0;
    public int ExpiredDocuments => _dashboard?.Fleet?.ExpiredDocuments ?? 0;
    public int CoverageRatio => _dashboard?.CoverageRatio ?? 0;

    public bool FleetUnavailable => _dashboard is not null && !_dashboard.FleetAvailable;
    public bool WorkshopUnavailable => _dashboard is not null && !_dashboard.WorkshopAvailable;

    public int PendingCount => _sync.PendingCount;
    public bool HasPending => PendingCount > 0;

    public bool HasAgenda => Agenda.Count > 0;
    public bool HasAttention => Attention.Count > 0;

    private static DateTime LocalNow()
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"));
        }
        catch
        {
            return DateTime.Now;
        }
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        Error = null;
        // Zona fija de operacion: el emulador arranca en UTC y mostraba "Buenas noches" a las 10 AM.
        Greeting = LocalNow().Hour switch
        {
            < 12 => "Buenos días",
            < 19 => "Buenas tardes",
            _ => "Buenas noches"
        };

        try
        {
            IsOffline = !_sync.IsOnline;

            var result = await _api.GetAsync<DashboardDto>("/apiReports/dashboard", AppConfig.NodeFleet);

            if (!result.Success)
            {
                Error = result.Offline
                    ? "Sin conexión. Los indicadores necesitan red."
                    : ErrorText.Translate(result.Error);
                return;
            }

            _dashboard = result.Data;
            RebuildCollections();
            NotifyAll();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SyncNowAsync()
    {
        if (!_sync.IsOnline)
        {
            Error = "Sin conexión. Los pendientes se enviarán cuando vuelva la red.";
            return;
        }

        var outcome = await _sync.SyncAsync();
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(HasPending));

        Error = outcome.Conflicts > 0
            ? $"{outcome.Synced} sincronizados, {outcome.Conflicts} con conflicto. Revisalos en Mi cuenta."
            : null;

        await LoadAsync();
    }

    private void RebuildCollections()
    {
        Agenda.Clear();
        Attention.Clear();

        var today = DateOnly.FromDateTime(LocalNow());

        foreach (var reservation in _dashboard?.Workshop?.Upcoming ?? [])
        {
            var local = reservation.ScheduledAtUtc.ToLocalTime();
            var date = DateOnly.FromDateTime(local);

            var day = date == today ? "Hoy"
                : date == today.AddDays(1) ? "Mañana"
                : local.ToString("dd MMM");

            Agenda.Add(new AgendaItem(
                local.ToString("HH:mm"),
                day,
                $"Vehículo #{reservation.FleetVehicleId}",
                reservation.ProviderName ?? "Taller sin asignar",
                StatusText.Reservation(reservation.Status),
                reservation.FleetVehicleId));
        }

        foreach (var document in _dashboard?.Fleet?.ExpiringSoon ?? [])
        {
            Attention.Add(new AttentionItem(
                document.PlateNumber,
                document.DocumentType,
                document.IsExpired
                    ? $"Vencido hace {Math.Abs(document.DaysRemaining)} d"
                    : $"Vence en {document.DaysRemaining} d",
                document.IsExpired,
                document.FleetVehicleId));
        }
    }

    private void NotifyAll()
    {
        foreach (var name in new[]
        {
            nameof(ActiveVehicles), nameof(TotalVehicles), nameof(InWorkshop), nameof(ActiveReservations),
            nameof(ExpiredDocuments), nameof(CoverageRatio), nameof(FleetUnavailable), nameof(WorkshopUnavailable),
            nameof(HasAgenda), nameof(HasAttention), nameof(PendingCount), nameof(HasPending)
        })
        {
            OnPropertyChanged(name);
        }
    }
}
