using System.Collections.ObjectModel;
using System.Windows.Input;
using AstraSystemsRental.Contracts.Display;
using AstraSystemsRental.Contracts.Fleet;
using AstraSystemsRental.Mobile.Services;

namespace AstraSystemsRental.Mobile.ViewModels;

public sealed record VehicleListItem(long Id, string Plate, string Detail, string Status);

public sealed class FleetViewModel : BaseViewModel
{
    private readonly IAstraApiClient _api;
    private readonly ISyncService _sync;

    private string _search = string.Empty;
    private string _newPlate = string.Empty;
    private bool _showCreate;
    private int _page = 1;
    private int _totalPages = 1;

    public FleetViewModel(IAstraApiClient api, ISyncService sync)
    {
        _api = api;
        _sync = sync;
        LoadCommand = new Command(async () => await LoadAsync());
        SearchCommand = new Command(async () => { _page = 1; await LoadAsync(); });
        NextPageCommand = new Command(async () => { if (_page < _totalPages) { _page++; await LoadAsync(); } });
        PreviousPageCommand = new Command(async () => { if (_page > 1) { _page--; await LoadAsync(); } });
        ToggleCreateCommand = new Command(() => ShowCreate = !ShowCreate);
        CreateCommand = new Command(async () => await CreateAsync());
        OpenCommand = new Command<VehicleListItem>(async item => await OpenAsync(item));
    }

    public ObservableCollection<VehicleListItem> Items { get; } = [];

    public ICommand LoadCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand ToggleCreateCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand OpenCommand { get; }

    public string Search
    {
        get => _search;
        set => Set(ref _search, value);
    }

    /// <summary>
    /// El servidor solo exige la placa. La app no pide 18 campos como el asistente web.
    /// </summary>
    public string NewPlate
    {
        get => _newPlate;
        set => Set(ref _newPlate, value);
    }

    public bool ShowCreate
    {
        get => _showCreate;
        set => Set(ref _showCreate, value);
    }

    public string PageLabel => $"Página {_page} de {_totalPages}";
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

            var query = $"/apiVehicles/fleet-vehicles?page={_page}&pageSize=20";

            if (!string.IsNullOrWhiteSpace(Search))
                query += $"&search={Uri.EscapeDataString(Search.Trim())}";

            var result = await _api.GetAsync<PagedDto<FleetVehicleDto>>(query, AppConfig.NodeFleet);

            if (!result.Success)
            {
                Error = result.Offline ? "Sin conexión. El listado necesita red." : ErrorText.Translate(result.Error);
                return;
            }

            Items.Clear();

            foreach (var vehicle in result.Data?.Items ?? [])
            {
                Items.Add(new VehicleListItem(
                    vehicle.Id,
                    vehicle.PlateNumber,
                    string.IsNullOrWhiteSpace(vehicle.Brand) && string.IsNullOrWhiteSpace(vehicle.Line)
                        ? "Sin marca/línea"
                        : $"{vehicle.Brand} {vehicle.Line}".Trim(),
                    StatusText.Vehicle(vehicle.Status)));
            }

            _totalPages = result.Data?.TotalPages ?? 1;
            OnPropertyChanged(nameof(PageLabel));
            OnPropertyChanged(nameof(IsEmpty));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateAsync()
    {
        var plate = NewPlate.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(plate))
        {
            Error = "Ingresá la placa.";
            return;
        }

        IsBusy = true;
        Error = null;

        try
        {
            var result = await _api.PostAsync<FleetVehicleDto>(
                "/apiVehicles/fleet-vehicles", new CreateFleetVehicleDto { PlateNumber = plate }, AppConfig.NodeFleet);

            if (!result.Success)
            {
                Error = result.Offline
                    ? "Sin conexión. Registrar un vehículo necesita red."
                    : ErrorText.Translate(result.Error);
                return;
            }

            NewPlate = string.Empty;
            ShowCreate = false;
            _page = 1;
        }
        finally
        {
            IsBusy = false;
        }

        // Fuera del try: LoadAsync aborta si IsBusy sigue en true, asi que
        // recargar dentro del bloque descartaba la lista en silencio y el
        // vehiculo recien creado no aparecia.
        if (Error is null)
            await LoadAsync();
    }

    private static async Task OpenAsync(VehicleListItem? item)
    {
        if (item is null)
            return;

        await Shell.Current.GoToAsync($"vehicle-detail?id={item.Id}&plate={Uri.EscapeDataString(item.Plate)}");
    }
}
