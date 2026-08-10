using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using AstraSystemsRental.Contracts.Display;
using AstraSystemsRental.Contracts.Fleet;
using AstraSystemsRental.Contracts.Maintenance;
using AstraSystemsRental.Mobile.Services;

namespace AstraSystemsRental.Mobile.ViewModels;

public sealed class ReservationItem
{
    public long Id { get; init; }
    public long FleetVehicleId { get; init; }
    public string Plate { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string RawStatus { get; init; } = string.Empty;
    public string When { get; init; } = string.Empty;
    public int PhotoCount { get; init; }
    public bool CanAdvance { get; init; }
    public bool CanCancel { get; init; }
    public string NextStatus { get; init; } = string.Empty;
    public string NextLabel { get; init; } = string.Empty;
    public string PhotoLabel => PhotoCount > 0 ? $"{PhotoCount} foto(s)" : "Sin fotos";
}

public sealed class ReservationsViewModel : BaseViewModel
{
    private readonly IAstraApiClient _api;
    private readonly ISyncService _sync;

    private VehicleListItem? _newVehicle;
    private DateTime _scheduledDate = DateTime.Today.AddDays(1);
    private TimeSpan _scheduledTime = new(9, 0, 0);
    private DateTime _expectedEndDate = DateTime.Today.AddDays(2);
    private bool _showCreate;
    private string? _mileage;

    public ReservationsViewModel(IAstraApiClient api, ISyncService sync)
    {
        _api = api;
        _sync = sync;
        LoadCommand = new Command(async () => await LoadAsync());
        ToggleCreateCommand = new Command(() => ShowCreate = !ShowCreate);
        CreateCommand = new Command(async () => await CreateAsync());
        AdvanceCommand = new Command<ReservationItem>(async item => await ChangeStatusAsync(item, item?.NextStatus));
        CancelCommand = new Command<ReservationItem>(async item => await CancelAsync(item));
        TakePhotoCommand = new Command<ReservationItem>(async item => await TakePhotoAsync(item));
        PickPhotoCommand = new Command<ReservationItem>(async item => await PickPhotoAsync(item));
    }

    public ObservableCollection<ReservationItem> Items { get; } = [];
    public ObservableCollection<VehicleListItem> Vehicles { get; } = [];

    public ICommand LoadCommand { get; }
    public ICommand ToggleCreateCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand AdvanceCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand TakePhotoCommand { get; }
    public ICommand PickPhotoCommand { get; }

    public VehicleListItem? NewVehicle
    {
        get => _newVehicle;
        set => Set(ref _newVehicle, value);
    }

    public DateTime ScheduledDate
    {
        get => _scheduledDate;
        set => Set(ref _scheduledDate, value);
    }

    public TimeSpan ScheduledTime
    {
        get => _scheduledTime;
        set => Set(ref _scheduledTime, value);
    }

    /// <summary>
    /// Una reserva activa sin fecha de fin bloquea cualquier reserva posterior del mismo vehiculo
    /// (el validador la trata como intervalo abierto). Por eso la app siempre manda fin estimado.
    /// </summary>
    public DateTime ExpectedEndDate
    {
        get => _expectedEndDate;
        set => Set(ref _expectedEndDate, value);
    }

    public string? Mileage
    {
        get => _mileage;
        set => Set(ref _mileage, value);
    }

    public bool ShowCreate
    {
        get => _showCreate;
        set => Set(ref _showCreate, value);
    }

    public override bool IsEmpty => !IsBusy && Items.Count == 0;

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        Error = null;

        try
        {
            IsOffline = !_sync.IsOnline;

            var vehiclesResult = await _api.GetAsync<PagedDto<FleetVehicleDto>>(
                "/apiVehicles/fleet-vehicles?page=1&pageSize=100", AppConfig.NodeFleet);

            var plates = new Dictionary<long, string>();
            Vehicles.Clear();

            foreach (var vehicle in vehiclesResult.Data?.Items ?? [])
            {
                plates[vehicle.Id] = vehicle.PlateNumber;
                Vehicles.Add(new VehicleListItem(vehicle.Id, vehicle.PlateNumber, vehicle.Display, StatusText.Vehicle(vehicle.Status)));
            }

            var result = await _api.GetAsync<PagedDto<WorkshopReservationDto>>(
                "/apiMaintenance/workshop-reservations?page=1&pageSize=50", AppConfig.NodeReservations);

            if (!result.Success)
            {
                Error = result.Offline ? "Sin conexión. La agenda necesita red." : ErrorText.Translate(result.Error);
                return;
            }

            Items.Clear();

            foreach (var reservation in result.Data?.Items ?? [])
                Items.Add(Map(reservation, plates));

            OnPropertyChanged(nameof(IsEmpty));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static ReservationItem Map(WorkshopReservationDto dto, IReadOnlyDictionary<long, string> plates)
    {
        var (next, label) = dto.Status switch
        {
            "Pending" => ("InWorkshop", "Marcar en taller"),
            "InWorkshop" => ("Ready", "Marcar listo"),
            "Ready" => ("Collected", "Marcar retirado"),
            _ => (string.Empty, string.Empty)
        };

        return new ReservationItem
        {
            Id = dto.Id,
            FleetVehicleId = dto.FleetVehicleId,
            Plate = plates.TryGetValue(dto.FleetVehicleId, out var plate) ? plate : $"#{dto.FleetVehicleId}",
            Provider = dto.ProviderName ?? "Taller sin asignar",
            Status = StatusText.Reservation(dto.Status),
            RawStatus = dto.Status,
            When = dto.ScheduledAtUtc.ToLocalTime().ToString("dd MMM HH:mm"),
            PhotoCount = dto.Photos.Count,
            CanAdvance = next.Length > 0,
            CanCancel = dto.Status is "Pending" or "InWorkshop",
            NextStatus = next,
            NextLabel = label
        };
    }

    private async Task CreateAsync()
    {
        if (NewVehicle is null)
        {
            Error = "Elegí un vehículo.";
            return;
        }

        IsBusy = true;
        Error = null;

        try
        {
            var scheduled = ScheduledDate.Date.Add(ScheduledTime);

            var payload = new CreateWorkshopReservationDto
            {
                FleetVehicleId = NewVehicle.Id,
                ScheduledAtUtc = scheduled.ToUniversalTime(),
                ExpectedEndAtUtc = ExpectedEndDate.Date.AddHours(18).ToUniversalTime(),
                MileageAtReservation = int.TryParse(Mileage, out var km) ? km : null
            };

            var operation = new PendingOperation
            {
                Kind = PendingKind.WorkshopReservation,
                Path = "/apiMaintenance/workshop-reservations",
                PayloadJson = JsonSerializer.Serialize(payload),
                NodeKey = AppConfig.NodeReservations,
                Label = $"Reserva {NewVehicle.Plate} {scheduled:dd MMM HH:mm}"
            };

            await _sync.EnqueueOrSendAsync(operation);

            ShowCreate = false;
            Mileage = null;

            if (!_sync.IsOnline)
                Error = "Guardado sin conexión. Se enviará al recuperar la red.";
        }
        catch (InvalidOperationException ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }

        // Fuera del try por la misma razon que en FleetViewModel: LoadAsync
        // aborta si IsBusy sigue en true y la recarga se perdia en silencio.
        if (Error is null && _sync.IsOnline)
            await LoadAsync();
    }

    private async Task ChangeStatusAsync(ReservationItem? item, string? newStatus)
    {
        if (item is null || string.IsNullOrEmpty(newStatus))
            return;

        var result = await _api.PostAsync<WorkshopReservationDto>(
            $"/apiMaintenance/workshop-reservations/{item.Id}/status-transitions",
            new { newStatus }, AppConfig.NodeReservations);

        Error = result.Success ? null : ErrorText.Translate(result.Error);

        if (result.Success)
            await LoadAsync();
    }

    private async Task CancelAsync(ReservationItem? item)
    {
        if (item is null)
            return;

        var confirmed = await Application.Current!.Windows[0].Page!.DisplayAlert(
            "Cancelar reserva", $"¿Cancelar la reserva de {item.Plate}? No se puede deshacer.", "Sí, cancelar", "No");

        if (confirmed)
            await ChangeStatusAsync(item, "Cancelled");
    }

    /// <summary>
    /// La camara es lo que la web no puede dar. El backend estampa el estado de la reserva
    /// en cada foto, asi que se obtiene un antes/despues automatico.
    /// </summary>
    private async Task TakePhotoAsync(ReservationItem? item)
    {
        if (item is null)
            return;

        if (!MediaPicker.Default.IsCaptureSupported)
        {
            Error = "Este dispositivo no tiene cámara disponible.";
            return;
        }

        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();

            if (photo is not null)
                await QueuePhotoAsync(item, photo);
        }
        catch (PermissionException)
        {
            Error = "Falta permiso de cámara.";
        }
    }

    private async Task PickPhotoAsync(ReservationItem? item)
    {
        if (item is null)
            return;

        var photo = await MediaPicker.Default.PickPhotoAsync();

        if (photo is not null)
            await QueuePhotoAsync(item, photo);
    }

    private async Task QueuePhotoAsync(ReservationItem item, FileResult photo)
    {
        var localPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}.jpg");

        await using (var source = await photo.OpenReadAsync())
        await using (var destination = File.Create(localPath))
        {
            await source.CopyToAsync(destination);
        }

        var operation = new PendingOperation
        {
            Kind = PendingKind.ReservationPhoto,
            Path = $"/apiMaintenance/workshop-reservations/{item.Id}/photos",
            FilePath = localPath,
            FileName = photo.FileName ?? "foto.jpg",
            ContentType = photo.ContentType ?? "image/jpeg",
            Notes = $"Estado: {item.Status}",
            NodeKey = AppConfig.NodeReservations,
            Label = $"Foto {item.Plate} ({item.Status})"
        };

        try
        {
            await _sync.EnqueueOrSendAsync(operation);

            Error = _sync.IsOnline ? null : "Foto guardada. Se enviará al recuperar la red.";

            if (_sync.IsOnline)
                await LoadAsync();
        }
        catch (InvalidOperationException ex)
        {
            Error = ex.Message;
        }
    }
}
