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

    private UserProfileDto? _profile;

    public ProfileViewModel(IAstraApiClient api, IAuthService auth, ISyncService sync, IOfflineQueue queue)
    {
        _api = api;
        _auth = auth;
        _sync = sync;
        _queue = queue;
        LoadCommand = new Command(async () => await LoadAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
        SyncCommand = new Command(async () => await SyncAsync());
        DiscardCommand = new Command<PendingOperation>(async op => await DiscardAsync(op));
    }

    public ObservableCollection<NodeDto> Nodes { get; } = [];
    public ObservableCollection<PendingOperation> Conflicts { get; } = [];

    public ICommand LoadCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand SyncCommand { get; }
    public ICommand DiscardCommand { get; }

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

    private async Task LogoutAsync()
    {
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

            foreach (var name in new[] { nameof(Detail), nameof(Status), nameof(CurrentMileage), nameof(RoutineName), nameof(NextThreshold), nameof(IsOverdue) })
                OnPropertyChanged(name);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
