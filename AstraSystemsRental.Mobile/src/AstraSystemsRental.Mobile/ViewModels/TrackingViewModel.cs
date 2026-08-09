using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using AstraSystemsRental.Contracts.Display;
using AstraSystemsRental.Contracts.Fleet;
using AstraSystemsRental.Contracts.Maintenance;
using AstraSystemsRental.Mobile.Services;

namespace AstraSystemsRental.Mobile.ViewModels;

/// <summary>
/// Registrar kilometraje en 2 toques: elegir vehiculo y escribir el valor.
/// Calcula la ventana valida en cliente replicando MileageMonotonicityValidator,
/// para avisar antes de enviar en vez de que el servidor rechace.
/// </summary>
public sealed class TrackingViewModel : BaseViewModel
{
    private const int DailyProjectionKm = 600;
    private const int MaxKilometers = 1_000_000;

    private readonly IAstraApiClient _api;
    private readonly ISyncService _sync;

    private VehicleListItem? _selectedVehicle;
    private string _value = string.Empty;
    private DateTime _readingDate = DateTime.Today;
    private string? _notes;
    private string _boundsHint = string.Empty;
    private NextMaintenanceDto? _next;

    public TrackingViewModel(IAstraApiClient api, ISyncService sync)
    {
        _api = api;
        _sync = sync;
        LoadCommand = new Command(async () => await LoadAsync());
        SaveCommand = new Command(async () => await SaveAsync());
    }

    public ObservableCollection<VehicleListItem> Vehicles { get; } = [];
    public ObservableCollection<MileageReadingDto> Readings { get; } = [];

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }

    public VehicleListItem? SelectedVehicle
    {
        get => _selectedVehicle;
        set
        {
            if (Set(ref _selectedVehicle, value))
                _ = LoadVehicleContextAsync();
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (Set(ref _value, value))
                ValidateLocally();
        }
    }

    public DateTime ReadingDate
    {
        get => _readingDate;
        set => Set(ref _readingDate, value);
    }

    public DateTime MaxDate => DateTime.Today;

    public string? Notes
    {
        get => _notes;
        set => Set(ref _notes, value);
    }

    public string BoundsHint
    {
        get => _boundsHint;
        private set => Set(ref _boundsHint, value);
    }

    public bool HasVehicle => SelectedVehicle is not null;
    public bool HasReadings => Readings.Count > 0;

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        Error = null;

        try
        {
            IsOffline = !_sync.IsOnline;

            var result = await _api.GetAsync<PagedDto<FleetVehicleDto>>(
                "/apiVehicles/fleet-vehicles?page=1&pageSize=100", AppConfig.NodeFleet);

            if (!result.Success)
            {
                Error = result.Offline
                    ? "Sin conexión. Podés registrar igual: se enviará al recuperar la red."
                    : ErrorText.Translate(result.Error);
                return;
            }

            Vehicles.Clear();

            foreach (var vehicle in result.Data?.Items ?? [])
            {
                Vehicles.Add(new VehicleListItem(
                    vehicle.Id, vehicle.PlateNumber, vehicle.Display, StatusText.Vehicle(vehicle.Status)));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadVehicleContextAsync()
    {
        OnPropertyChanged(nameof(HasVehicle));

        if (SelectedVehicle is null)
            return;

        Readings.Clear();

        var nextResult = await _api.GetAsync<NextMaintenanceDto>(
            $"/apiMaintenance/fleet-vehicles/{SelectedVehicle.Id}/mileage-readings/next-maintenance", AppConfig.NodeTracking);

        _next = nextResult.Data;

        var readingsResult = await _api.GetAsync<PagedDto<MileageReadingDto>>(
            $"/apiMaintenance/fleet-vehicles/{SelectedVehicle.Id}/mileage-readings?page=1&pageSize=10", AppConfig.NodeTracking);

        foreach (var reading in readingsResult.Data?.Items ?? [])
            Readings.Add(reading);

        OnPropertyChanged(nameof(HasReadings));
        ValidateLocally();
    }

    /// <summary>
    /// Replica la ventana [minimo, maximo] del validador del servidor.
    /// </summary>
    private void ValidateLocally()
    {
        if (_next?.CurrentValue is not { } current)
        {
            BoundsHint = "Sin lecturas previas: cualquier valor coherente es válido.";
            Error = null;
            return;
        }

        var elapsedDays = _next.LastReadingDate is { } last
            ? Math.Max(DateOnly.FromDateTime(ReadingDate).DayNumber - last.DayNumber, 1)
            : 1;

        var maximum = Math.Min(current + elapsedDays * DailyProjectionKm, MaxKilometers);

        BoundsHint = $"Entre {current:N0} y {maximum:N0}";

        if (!int.TryParse(Value, out var parsed))
        {
            Error = null;
            return;
        }

        Error = parsed < current
            ? $"La lectura debe ser al menos {current:N0} para mantener el histórico."
            : parsed > maximum
                ? $"La lectura no puede superar {maximum:N0} para esa fecha."
                : null;
    }

    private async Task SaveAsync()
    {
        if (SelectedVehicle is null)
        {
            Error = "Elegí un vehículo.";
            return;
        }

        if (!int.TryParse(Value, out var parsed) || parsed < 0)
        {
            Error = "Ingresá un kilometraje válido.";
            return;
        }

        ValidateLocally();

        if (HasError)
            return;

        IsBusy = true;

        try
        {
            var payload = new CreateMileageReadingDto
            {
                Value = parsed,
                ReadingDate = DateOnly.FromDateTime(ReadingDate),
                Notes = Notes
            };

            var operation = new PendingOperation
            {
                Kind = PendingKind.MileageReading,
                Path = $"/apiMaintenance/fleet-vehicles/{SelectedVehicle.Id}/mileage-readings",
                PayloadJson = JsonSerializer.Serialize(payload),
                NodeKey = AppConfig.NodeTracking,
                Label = $"{SelectedVehicle.Plate}: {parsed:N0} km"
            };

            await _sync.EnqueueOrSendAsync(operation);

            Value = string.Empty;
            Notes = null;

            Error = _sync.IsOnline ? null : "Guardado sin conexión. Se enviará al recuperar la red.";

            if (_sync.IsOnline)
                await LoadVehicleContextAsync();
        }
        catch (InvalidOperationException ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
