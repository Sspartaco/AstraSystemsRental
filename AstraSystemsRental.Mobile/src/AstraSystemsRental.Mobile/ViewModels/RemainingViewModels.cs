using System.Collections.ObjectModel;
using System.Windows.Input;
using AstraSystemsRental.Contracts.Auth;
using AstraSystemsRental.Contracts.Display;
using AstraSystemsRental.Contracts.Fleet;
using AstraSystemsRental.Contracts.Maintenance;
using AstraSystemsRental.Mobile.Services;

namespace AstraSystemsRental.Mobile.ViewModels;

public sealed class RoutinesViewModel : BaseViewModel
{
    private readonly IAstraApiClient _api;

    public RoutinesViewModel(IAstraApiClient api)
    {
        _api = api;
        LoadCommand = new Command(async () => await LoadAsync());
    }

    public ObservableCollection<MaintenanceRoutineDto> Items { get; } = [];
    public ICommand LoadCommand { get; }
    public bool IsEmpty => !IsBusy && Items.Count == 0;

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        Error = null;

        try
        {
            var result = await _api.GetAsync<PagedDto<MaintenanceRoutineDto>>(
                "/apiMaintenance/maintenance-routines?page=1&pageSize=50", AppConfig.NodeRoutines);

            if (!result.Success)
            {
                Error = result.Offline ? "Sin conexión." : ErrorText.Translate(result.Error);
                return;
            }

            Items.Clear();

            foreach (var routine in result.Data?.Items ?? [])
                Items.Add(routine);

            OnPropertyChanged(nameof(IsEmpty));
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed class ProfileViewModel : BaseViewModel
{
    private readonly IAstraApiClient _api;
    private readonly IAuthService _auth;
    private readonly ISyncService _sync;
    private readonly IOfflineQueue _queue;
    private readonly IBiometricService _biometric;

    private UserProfileDto? _profile;

    public ProfileViewModel(IAstraApiClient api, IAuthService auth, ISyncService sync, IOfflineQueue queue, IBiometricService biometric)
    {
        _api = api;
        _auth = auth;
        _sync = sync;
        _queue = queue;
        _biometric = biometric;
        LoadCommand = new Command(async () => await LoadAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
        SaveServerCommand = new Command(SaveServer);
        ResetServerCommand = new Command(ResetServer);
        _serverUrl = AppConfig.GatewayBaseUrl;
        SyncCommand = new Command(async () => await SyncAsync());
        DiscardCommand = new Command<PendingOperation>(async op => await DiscardAsync(op));
    }

    public ObservableCollection<NodeDto> Nodes { get; } = [];
    public ObservableCollection<PendingOperation> Conflicts { get; } = [];

    public ICommand LoadCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand SyncCommand { get; }
    public ICommand DiscardCommand { get; }
    public ICommand SaveServerCommand { get; }
    public ICommand ResetServerCommand { get; }

    private string _serverUrl = string.Empty;
    private string? _serverMessage;

    /// <summary>
    /// Permite apuntar la app a otro Gateway sin recompilar: util al cambiar de red
    /// o al pasar de la IP local a una URL publica.
    /// </summary>
    public string ServerUrl
    {
        get => _serverUrl;
        set => Set(ref _serverUrl, value);
    }

    public string? ServerMessage
    {
        get => _serverMessage;
        private set
        {
            if (Set(ref _serverMessage, value))
                OnPropertyChanged(nameof(HasServerMessage));
        }
    }

    public bool HasServerMessage => !string.IsNullOrWhiteSpace(ServerMessage);

    private bool _biometricAvailable;
    private bool _biometricEnabled;
    private string _biometricLabel = "Face ID";
    private bool _applyingBiometric;

    public bool BiometricAvailable
    {
        get => _biometricAvailable;
        private set
        {
            if (Set(ref _biometricAvailable, value))
            {
                OnPropertyChanged(nameof(BiometricUnavailable));
                OnPropertyChanged(nameof(BiometricDescription));
                OnPropertyChanged(nameof(BiometricTitle));
            }
        }
    }

    public string BiometricTitle => BiometricAvailable ? BiometricLabel : "Desbloqueo biométrico";

    public string BiometricLabel
    {
        get => _biometricLabel;
        private set
        {
            if (Set(ref _biometricLabel, value))
            {
                OnPropertyChanged(nameof(BiometricDescription));
                OnPropertyChanged(nameof(BiometricTitle));
            }
        }
    }

    public string BiometricDescription => BiometricAvailable
        ? $"Entrá con {BiometricLabel} sin escribir tu contraseña."
        : "Este dispositivo no tiene huella ni reconocimiento facial configurado. Registralo en Ajustes del sistema para poder activarlo.";

    /// <summary>
    /// La seccion se muestra SIEMPRE, aunque no haya sensor: si se ocultara por
    /// completo el usuario no tendria forma de saber que la funcion existe ni
    /// por que no aparece.
    /// </summary>
    public bool BiometricUnavailable => !BiometricAvailable;

    /// <summary>
    /// Activarlo exige superar el prompt: si el usuario no puede autenticarse
    /// ahora, tampoco podria entrar despues y quedaria fuera de la app.
    /// </summary>
    public bool BiometricEnabled
    {
        get => _biometricEnabled;
        set
        {
            if (_applyingBiometric || value == _biometricEnabled)
                return;

            _ = ApplyBiometricAsync(value);
        }
    }

    private async Task ApplyBiometricAsync(bool enabled)
    {
        if (enabled && !await _biometric.AuthenticateAsync($"Confirmá tu identidad para activar {BiometricLabel}"))
        {
            // Revierte el switch en la vista sin volver a disparar el setter.
            _applyingBiometric = true;
            OnPropertyChanged(nameof(BiometricEnabled));
            _applyingBiometric = false;
            return;
        }

        await _biometric.SetEnabledAsync(enabled);

        _applyingBiometric = true;
        Set(ref _biometricEnabled, enabled, nameof(BiometricEnabled));
        _applyingBiometric = false;
    }

    public string FullName => _profile?.FullName ?? "—";
    public string Email => _profile?.Email ?? "—";
    public string Role => StatusText.Role(_profile?.RoleCode);
    public string PlanName => _profile?.PlanName ?? "Sin plan";
    public string Document => _profile?.DocumentNumber ?? "—";
    public string Expiry => _profile?.SubscriptionEndsAtUtc?.ToLocalTime().ToString("dd/MM/yyyy") ?? "—";
    public int PendingCount => _sync.PendingCount;
    public bool HasPending => PendingCount > 0;
    public bool HasConflicts => Conflicts.Count > 0;

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        Error = null;

        try
        {
            var result = await _api.GetAsync<UserProfileDto>("/apiUsers/users/me");

            if (result.Success)
                _profile = result.Data;
            else if (!result.Offline)
                Error = ErrorText.Translate(result.Error);

            Nodes.Clear();

            foreach (var node in await _auth.GetNodesAsync())
                Nodes.Add(node);

            var kind = await _biometric.GetAvailableAsync();
            BiometricAvailable = kind != BiometricKind.None;
            BiometricLabel = _biometric.Label(kind);

            _applyingBiometric = true;
            Set(ref _biometricEnabled, _biometric.IsEnabled, nameof(BiometricEnabled));
            _applyingBiometric = false;

            await ReloadConflictsAsync();
            NotifyAll();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadConflictsAsync()
    {
        Conflicts.Clear();

        foreach (var conflict in await _queue.GetConflictsAsync())
            Conflicts.Add(conflict);

        OnPropertyChanged(nameof(HasConflicts));
    }

    private async Task SyncAsync()
    {
        var outcome = await _sync.SyncAsync();
        await ReloadConflictsAsync();

        Error = outcome.Conflicts > 0
            ? $"{outcome.Synced} sincronizados, {outcome.Conflicts} con conflicto."
            : null;

        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(HasPending));
    }

    private async Task DiscardAsync(PendingOperation? operation)
    {
        if (operation is null)
            return;

        await _queue.DiscardAsync(operation.Id);
        await ReloadConflictsAsync();
    }

    private void SaveServer()
    {
        var url = ServerUrl?.Trim() ?? string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            ServerMessage = "Ingresá una URL válida, por ejemplo http://192.168.1.50:8080";
            return;
        }

        Preferences.Default.Set(AppConfig.GatewayOverrideKey, url.TrimEnd('/'));
        ServerMessage = "Servidor guardado. Cerrá y volvé a abrir la app para aplicarlo.";
    }

    private void ResetServer()
    {
        Preferences.Default.Remove(AppConfig.GatewayOverrideKey);
        ServerUrl = AppConfig.DefaultGatewayUrl;
        ServerMessage = "Restablecido al servidor por defecto.";
    }

    private async Task LogoutAsync()
    {
        // Cerrar sesion invalida el refresh token: dejar la biometria activa
        // haria que el proximo arranque intente desbloquear una sesion muerta.
        await _biometric.SetEnabledAsync(false);
        await _auth.LogoutAsync();

        if (Shell.Current is AppShell shell)
            await shell.GoToLoginAsync();
    }

    private void NotifyAll()
    {
        foreach (var name in new[]
        {
            nameof(FullName), nameof(Email), nameof(Role), nameof(PlanName),
            nameof(Document), nameof(Expiry), nameof(PendingCount), nameof(HasPending)
        })
        {
            OnPropertyChanged(name);
        }
    }
}

public sealed record UserRow(long UserId, string Name, string Email, string Role, string State);

public sealed class UsersViewModel : BaseViewModel
{
    private readonly IAstraApiClient _api;

    public UsersViewModel(IAstraApiClient api)
    {
        _api = api;
        LoadCommand = new Command(async () => await LoadAsync());
    }

    public ObservableCollection<UserRow> Items { get; } = [];
    public ICommand LoadCommand { get; }
    public bool IsEmpty => !IsBusy && Items.Count == 0;

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        Error = null;

        try
        {
            var result = await _api.GetAsync<PagedDto<UserOverviewDto>>("/apiUsers/users/overview?page=1&pageSize=50");

            if (!result.Success)
            {
                Error = result.Offline ? "Sin conexión." : ErrorText.Translate(result.Error);
                return;
            }

            Items.Clear();

            foreach (var user in result.Data?.Items ?? [])
            {
                Items.Add(new UserRow(
                    user.UserId,
                    $"{user.FirstNames} {user.LastNames}".Trim(),
                    user.Email,
                    StatusText.Role(user.RoleCode),
                    user.IsActive ? "Activo" : "Inactivo"));
            }

            OnPropertyChanged(nameof(IsEmpty));
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed record UserOverviewDto
{
    public long UserId { get; init; }
    public string FirstNames { get; init; } = string.Empty;
    public string LastNames { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string RoleCode { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsConfirmed { get; init; }
    public string? PlanCode { get; init; }
}

[QueryProperty(nameof(VehicleId), "id")]
[QueryProperty(nameof(Plate), "plate")]
public sealed class VehicleDetailViewModel : BaseViewModel
{
    private readonly IAstraApiClient _api;

    private string _vehicleId = string.Empty;
    private string _plate = string.Empty;
    private FleetVehicleDto? _vehicle;
    private NextMaintenanceDto? _next;

    public VehicleDetailViewModel(IAstraApiClient api)
    {
        _api = api;
        LoadCommand = new Command(async () => await LoadAsync());
        SaveCommand = new Command(async () => await SaveAsync());
        ToggleEditCommand = new Command(() =>
        {
            if (!ShowEdit)
                FillEditFields();

            SaveMessage = null;
            ShowEdit = !ShowEdit;
        });
    }

    public ICommand LoadCommand { get; }

    public string VehicleId
    {
        get => _vehicleId;
        set
        {
            if (Set(ref _vehicleId, value))
                _ = LoadAsync();
        }
    }

    public string Plate
    {
        get => _plate;
        set => Set(ref _plate, Uri.UnescapeDataString(value ?? string.Empty));
    }

    public ObservableCollection<MileageReadingDto> Readings { get; } = [];

    public string Detail => _vehicle?.Display ?? "—";
    public string Status => StatusText.Vehicle(_vehicle?.Status);
    public string CurrentMileage => _next?.CurrentValue is { } value ? $"{value:N0} km" : "Sin lecturas";
    public string RoutineName => _next?.RoutineName ?? "Sin rutina asignada";
    public string NextThreshold => _next?.NextThreshold is { } t ? $"{t:N0}" : "—";
    public bool IsOverdue => _next?.IsOverdue ?? false;

    private bool _showEdit;
    private string? _brand, _line, _modelYear, _vehicleClass, _bodyType, _color;
    private string? _serviceType, _fuelType, _transmission, _vin, _engineNumber, _notes;
    private string? _saveMessage;

    public bool ShowEdit
    {
        get => _showEdit;
        private set
        {
            if (Set(ref _showEdit, value))
                OnPropertyChanged(nameof(EditButtonText));
        }
    }

    public string EditButtonText => ShowEdit ? "Cancelar" : "Completar ficha";

    public string? Brand { get => _brand; set => Set(ref _brand, value); }
    public string? Line { get => _line; set => Set(ref _line, value); }
    public string? ModelYear { get => _modelYear; set => Set(ref _modelYear, value); }
    public string? VehicleClass { get => _vehicleClass; set => Set(ref _vehicleClass, value); }
    public string? BodyType { get => _bodyType; set => Set(ref _bodyType, value); }
    public string? Color { get => _color; set => Set(ref _color, value); }
    public string? ServiceType { get => _serviceType; set => Set(ref _serviceType, value); }
    public string? FuelType { get => _fuelType; set => Set(ref _fuelType, value); }
    public string? Transmission { get => _transmission; set => Set(ref _transmission, value); }
    public string? Vin { get => _vin; set => Set(ref _vin, value); }
    public string? EngineNumber { get => _engineNumber; set => Set(ref _engineNumber, value); }
    public string? Notes { get => _notes; set => Set(ref _notes, value); }

    public string? SaveMessage
    {
        get => _saveMessage;
        private set
        {
            if (Set(ref _saveMessage, value))
                OnPropertyChanged(nameof(HasSaveMessage));
        }
    }

    public bool HasSaveMessage => !string.IsNullOrWhiteSpace(SaveMessage);

    /// <summary>
    /// Cuantos de los 12 campos de la ficha estan cargados. Da una razon visible
    /// para completarla: sin esto, "Completar ficha" no dice cuanto falta ni si
    /// vale la pena abrirlo.
    /// </summary>
    public int FilledCount => new[]
    {
        _vehicle?.Brand, _vehicle?.Line, _vehicle?.ModelYear?.ToString(), _vehicle?.VehicleClass,
        _vehicle?.BodyType, _vehicle?.Color, _vehicle?.ServiceType, _vehicle?.FuelType,
        _vehicle?.Transmission, _vehicle?.Vin, _vehicle?.EngineNumber, _vehicle?.Notes
    }.Count(v => !string.IsNullOrWhiteSpace(v));

    public const int TotalFields = 12;

    public double CompletionProgress => (double)FilledCount / TotalFields;
    public string CompletionLabel => $"{FilledCount} de {TotalFields} datos";
    public bool IsComplete => FilledCount == TotalFields;

    public string CompletionHint => FilledCount switch
    {
        0 => "Todavía no cargaste ningún dato.",
        TotalFields => "Ficha completa.",
        _ => $"Faltan {TotalFields - FilledCount} por completar."
    };

    // Con "=>" cada acceso devolvia un Command NUEVO: el binding se quedaba con
    // una instancia distinta de la que reaccionaba, y el boton no hacia nada.
    public ICommand ToggleEditCommand { get; }
    public ICommand SaveCommand { get; }

    private void FillEditFields()
    {
        Brand = _vehicle?.Brand;
        Line = _vehicle?.Line;
        ModelYear = _vehicle?.ModelYear?.ToString();
        VehicleClass = _vehicle?.VehicleClass;
        BodyType = _vehicle?.BodyType;
        Color = _vehicle?.Color;
        ServiceType = _vehicle?.ServiceType;
        FuelType = _vehicle?.FuelType;
        Transmission = _vehicle?.Transmission;
        Vin = _vehicle?.Vin;
        EngineNumber = _vehicle?.EngineNumber;
        Notes = _vehicle?.Notes;
    }

    private async Task SaveAsync()
    {
        if (IsBusy || _vehicle is null)
            return;

        short? year = null;

        if (!string.IsNullOrWhiteSpace(ModelYear))
        {
            // El servidor acepta 1980..anio+1; avisar aca evita un viaje perdido.
            if (!short.TryParse(ModelYear, out var parsed) || parsed < 1980 || parsed > DateTime.Now.Year + 1)
            {
                Error = $"El modelo debe estar entre 1980 y {DateTime.Now.Year + 1}.";
                return;
            }

            year = parsed;
        }

        IsBusy = true;
        Error = null;
        SaveMessage = null;

        try
        {
            var payload = new UpdateFleetVehicleDto
            {
                Brand = Trim(Brand),
                Line = Trim(Line),
                ModelYear = year,
                VehicleClass = Trim(VehicleClass),
                BodyType = Trim(BodyType),
                Color = Trim(Color),
                ServiceType = Trim(ServiceType),
                FuelType = Trim(FuelType),
                Transmission = Trim(Transmission),
                Vin = Trim(Vin),
                EngineNumber = Trim(EngineNumber),
                Notes = Trim(Notes),
                RowVersion = _vehicle.RowVersion ?? string.Empty
            };

            var result = await _api.PutAsync<FleetVehicleDto>(
                $"/apiVehicles/fleet-vehicles/{_vehicle.Id}", payload, AppConfig.NodeFleet);

            if (!result.Success)
            {
                Error = result.Offline
                    ? "Sin conexión. Completar la ficha necesita red."
                    : ErrorText.Translate(result.Error);
                return;
            }

            ShowEdit = false;
            SaveMessage = "Ficha actualizada.";
        }
        finally
        {
            IsBusy = false;
        }

        // Fuera del finally: LoadAsync aborta mientras IsBusy siga en true.
        if (Error is null)
            await LoadAsync();
    }

    private static string? Trim(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task LoadAsync()
    {
        if (IsBusy || !long.TryParse(VehicleId, out var id))
            return;

        IsBusy = true;
        Error = null;

        try
        {
            var vehicleResult = await _api.GetAsync<FleetVehicleDto>($"/apiVehicles/fleet-vehicles/{id}", AppConfig.NodeFleet);

            if (!vehicleResult.Success)
            {
                Error = vehicleResult.Offline ? "Sin conexión." : ErrorText.Translate(vehicleResult.Error);
                return;
            }

            _vehicle = vehicleResult.Data;

            var nextResult = await _api.GetAsync<NextMaintenanceDto>(
                $"/apiMaintenance/fleet-vehicles/{id}/mileage-readings/next-maintenance", AppConfig.NodeTracking);

            _next = nextResult.Data;

            var readingsResult = await _api.GetAsync<PagedDto<MileageReadingDto>>(
                $"/apiMaintenance/fleet-vehicles/{id}/mileage-readings?page=1&pageSize=20", AppConfig.NodeTracking);

            Readings.Clear();

            foreach (var reading in readingsResult.Data?.Items ?? [])
                Readings.Add(reading);

            foreach (var name in new[]
                     {
                         nameof(Detail), nameof(Status), nameof(CurrentMileage), nameof(RoutineName),
                         nameof(NextThreshold), nameof(IsOverdue), nameof(FilledCount),
                         nameof(CompletionProgress), nameof(CompletionLabel), nameof(CompletionHint), nameof(IsComplete)
                     })
                OnPropertyChanged(name);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
