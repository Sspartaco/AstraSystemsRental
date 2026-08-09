using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.Maintenance;

public sealed class MaintenanceController(IGatewayClient gateway, ISessionService session, ICurrentUser currentUser) : Controller
{
    private const string TrackingNode = "maintenance-tracking";
    private const string RoutinesNode = "maintenance-routines";
    private const string ReservationsNode = "workshop-reservations";

    [HttpGet("/maintenance")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(TrackingNode))
            return Redirect("/");

        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var query = "/apiVehicles/fleet-vehicles?page=1&pageSize=50";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        var page = await gateway.GetTypedAsync<PagedDto<FleetVehicleOptionVm>>(query, token, "vehicle-registry", companyId, cancellationToken);

        var vm = new MaintenanceIndexVm
        {
            Vehicles = page?.Items ?? [],
            Search = search,
            Error = page is null ? "No se pudo cargar la lista de vehículos." : null
        };

        return Request.Headers.ContainsKey("HX-Request")
            ? PartialView("_VehiclePicker", vm)
            : View(vm);
    }

    [HttpGet("/maintenance/vehicle/{fleetVehicleId:long}")]
    public async Task<IActionResult> Vehicle(long fleetVehicleId, string? tab, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(TrackingNode))
            return Redirect("/");

        var vm = await LoadVehicleAsync(fleetVehicleId, null, cancellationToken, tab);
        if (vm is null)
            return NotFound();

        ViewData["Error"] = TempData["MaintenanceError"];
        return View(vm);
    }

    [HttpPost("/maintenance/vehicle/{fleetVehicleId:long}/assign-routine")]
    public async Task<IActionResult> AssignRoutine(long fleetVehicleId, [FromForm] AssignRoutineForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Put,
            $"/apiMaintenance/fleet-vehicles/{fleetVehicleId}/routine-assignment", token,
            new { routineId = form.RoutineId }, RoutinesNode, companyId, cancellationToken);

        var error = response.IsSuccess ? null : Translate(response.Errors.FirstOrDefault());
        return PartialView("_VehicleMaintenanceBody", await LoadVehicleAsync(fleetVehicleId, error, cancellationToken));
    }

    [HttpPost("/maintenance/vehicle/{fleetVehicleId:long}/readings")]
    public async Task<IActionResult> AddReading(long fleetVehicleId, [FromForm] AddReadingForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Post,
            $"/apiMaintenance/fleet-vehicles/{fleetVehicleId}/mileage-readings", token,
            new { value = form.Value, readingDate = form.ReadingDate, notes = form.Notes }, TrackingNode, companyId, cancellationToken);

        var error = response.IsSuccess ? null : Translate(response.Errors.FirstOrDefault());
        return PartialView("_VehicleMaintenanceBody", await LoadVehicleAsync(fleetVehicleId, error, cancellationToken, "readings"));
    }

    [HttpPost("/maintenance/vehicle/{fleetVehicleId:long}/reservations")]
    public async Task<IActionResult> CreateReservation(long fleetVehicleId, [FromForm] CreateReservationForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Post,
            "/apiMaintenance/workshop-reservations", token,
            new
            {
                fleetVehicleId,
                providerId = form.ProviderId,
                scheduledAtUtc = form.ScheduledAtUtc,
                expectedEndAtUtc = form.ExpectedEndAtUtc,
                mileageAtReservation = form.MileageAtReservation,
                isWashOnly = form.IsWashOnly,
                notes = form.Notes
            }, ReservationsNode, companyId, cancellationToken);

        var error = response.IsSuccess ? null : Translate(response.Errors.FirstOrDefault());
        return PartialView("_VehicleMaintenanceBody", await LoadVehicleAsync(fleetVehicleId, error, cancellationToken, "reservations"));
    }

    [HttpPost("/maintenance/vehicle/{fleetVehicleId:long}/reservations/{reservationId:long}/status")]
    public async Task<IActionResult> ChangeReservationStatus(long fleetVehicleId, long reservationId, [FromForm] ChangeReservationStatusForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Post,
            $"/apiMaintenance/workshop-reservations/{reservationId}/status-transitions", token,
            new { newStatus = form.NewStatus }, ReservationsNode, companyId, cancellationToken);

        var error = response.IsSuccess ? null : Translate(response.Errors.FirstOrDefault());
        return PartialView("_VehicleMaintenanceBody", await LoadVehicleAsync(fleetVehicleId, error, cancellationToken, "reservations"));
    }

    [HttpPost("/maintenance/vehicle/{fleetVehicleId:long}/reservations/{reservationId:long}/photos")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> AddReservationPhoto(
        long fleetVehicleId, long reservationId, IFormFile? file, string? notes, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        string? error = null;

        if (file is null || file.Length == 0)
        {
            error = "Tenés que adjuntar una imagen.";
        }
        else
        {
            await using var stream = file.OpenReadStream();
            var response = await gateway.PostFileAsync(
                $"/apiMaintenance/workshop-reservations/{reservationId}/photos", token,
                stream, file.FileName, file.ContentType, notes,
                ReservationsNode, session.GetActiveCompanyId(), cancellationToken);

            error = response.IsSuccess ? null : Translate(response.Errors.FirstOrDefault());
        }

        return PartialView("_VehicleMaintenanceBody", await LoadVehicleAsync(fleetVehicleId, error, cancellationToken, "reservations"));
    }

    [HttpGet("/maintenance/reservations/{reservationId:long}/photos/{photoId:long}")]
    public async Task<IActionResult> ReservationPhoto(long reservationId, long photoId, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var result = await gateway.GetBinaryAsync(
            $"/apiMaintenance/workshop-reservations/{reservationId}/photos/{photoId}/content",
            token, ReservationsNode, session.GetActiveCompanyId(), cancellationToken);

        return result is null
            ? NotFound()
            : File(result.Value.Content, result.Value.ContentType);
    }

    [HttpGet("/maintenance/reservations")]
    public async Task<IActionResult> Reservations(int page, string? status, long? providerId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(ReservationsNode))
            return Redirect("/");

        var isPartial = Request.Headers.ContainsKey("HX-Request");
        var vm = await LoadReservationsAsync(page == 0 ? 1 : page, status, providerId, from, to, null, includeFormData: !isPartial, cancellationToken);
        ViewData["Error"] = TempData["ReservationError"];

        return isPartial
            ? PartialView("_ReservationsGrid", vm)
            : View(vm);
    }

    [HttpPost("/maintenance/reservations")]
    public async Task<IActionResult> CreateGlobalReservation([FromForm] CreateGlobalReservationForm form, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(ReservationsNode))
            return Redirect("/");

        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Post,
            "/apiMaintenance/workshop-reservations", token,
            new
            {
                fleetVehicleId = form.FleetVehicleId,
                providerId = form.ProviderId,
                scheduledAtUtc = form.ScheduledAtUtc,
                expectedEndAtUtc = form.ExpectedEndAtUtc,
                mileageAtReservation = form.MileageAtReservation,
                isWashOnly = form.IsWashOnly,
                notes = form.Notes
            }, ReservationsNode, companyId, cancellationToken);

        if (!response.IsSuccess)
            TempData["ReservationError"] = Translate(response.Errors.FirstOrDefault());

        return Redirect("/maintenance/reservations");
    }

    [HttpGet("/maintenance/routines")]
    public async Task<IActionResult> Routines(int page, string? search, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(RoutinesNode))
            return Redirect("/");

        var vm = await LoadRoutinesAsync(page == 0 ? 1 : page, search, null, cancellationToken);
        ViewData["Error"] = TempData["RoutineError"];

        return Request.Headers.ContainsKey("HX-Request")
            ? PartialView("_RoutinesGrid", vm)
            : View(vm);
    }

    [HttpGet("/maintenance/routines/{id:long}")]
    public async Task<IActionResult> RoutineDetail(long id, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(RoutinesNode))
            return Redirect("/");

        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var routine = await gateway.GetTypedAsync<RoutineDto>($"/apiMaintenance/maintenance-routines/{id}", token, RoutinesNode, companyId, cancellationToken);
        if (routine is null)
            return NotFound();

        ViewData["Error"] = TempData["RoutineError"];
        return View(routine);
    }

    [HttpPost("/maintenance/routines")]
    public async Task<IActionResult> CreateRoutine([FromForm] CreateRoutineForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Post,
            "/apiMaintenance/maintenance-routines", token,
            new { name = form.Name, description = form.Description }, RoutinesNode, companyId, cancellationToken);

        if (!response.IsSuccess)
        {
            TempData["RoutineError"] = Translate(response.Errors.FirstOrDefault());
            return Redirect("/maintenance/routines");
        }

        var id = response.Data is { } data && data.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : (long?)null;
        return Redirect(id is not null ? $"/maintenance/routines/{id}" : "/maintenance/routines");
    }

    [HttpPost("/maintenance/routines/{id:long}/periodicities")]
    public async Task<IActionResult> AddPeriodicity(long id, [FromForm] AddPeriodicityForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Post,
            $"/apiMaintenance/maintenance-routines/{id}/periodicities", token,
            new { unit = form.Unit, startsAt = form.StartsAt, repeatsEvery = form.RepeatsEvery }, RoutinesNode, companyId, cancellationToken);

        if (!response.IsSuccess)
            TempData["RoutineError"] = Translate(response.Errors.FirstOrDefault());

        return Redirect($"/maintenance/routines/{id}");
    }

    [HttpPost("/maintenance/routines/{id:long}/concepts")]
    public async Task<IActionResult> AddConcept(long id, [FromForm] AddConceptForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Post,
            $"/apiMaintenance/maintenance-routines/{id}/concepts", token,
            new { periodicityId = form.PeriodicityId, name = form.Name, quantity = form.Quantity, notes = form.Notes }, RoutinesNode, companyId, cancellationToken);

        if (!response.IsSuccess)
            TempData["RoutineError"] = Translate(response.Errors.FirstOrDefault());

        return Redirect($"/maintenance/routines/{id}");
    }

    [HttpPost("/maintenance/routines/{id:long}/copy")]
    public async Task<IActionResult> CopyRoutine(long id, [FromForm] string newName, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Post,
            $"/apiMaintenance/maintenance-routines/{id}/copy", token,
            new { newName }, RoutinesNode, companyId, cancellationToken);

        if (!response.IsSuccess)
            TempData["RoutineError"] = Translate(response.Errors.FirstOrDefault());

        return Redirect("/maintenance/routines");
    }

    [HttpPost("/maintenance/routines/{id:long}/delete")]
    public async Task<IActionResult> DeleteRoutine(long id, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Delete,
            $"/apiMaintenance/maintenance-routines/{id}", token, null, RoutinesNode, companyId, cancellationToken);

        if (!response.IsSuccess)
            TempData["RoutineError"] = Translate(response.Errors.FirstOrDefault());

        return Redirect("/maintenance/routines");
    }

    private async Task<ReservationsIndexVm> LoadReservationsAsync(
        int page, string? status, long? providerId, DateOnly? from, DateOnly? to, string? error, bool includeFormData, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return new ReservationsIndexVm { Status = status, Error = error };

        var companyId = session.GetActiveCompanyId();
        var query = $"/apiMaintenance/workshop-reservations?page={page}&pageSize=50";

        if (!string.IsNullOrWhiteSpace(status))
            query += $"&status={Uri.EscapeDataString(status)}";

        if (providerId is { } providerFilter)
            query += $"&providerId={providerFilter}";

        if (from is { } fromFilter)
            query += $"&from={fromFilter:yyyy-MM-dd}";

        if (to is { } toFilter)
            query += $"&to={toFilter:yyyy-MM-dd}";

        var result = await gateway.GetTypedAsync<PagedDto<ReservationDto>>(query, token, ReservationsNode, companyId, cancellationToken);

        var vehicles = await gateway.GetTypedAsync<PagedDto<FleetVehicleOptionVm>>(
            "/apiVehicles/fleet-vehicles?page=1&pageSize=200", token, "vehicle-registry", companyId, cancellationToken);

        IReadOnlyList<ProviderDto> providers = includeFormData
            ? await gateway.GetTypedAsync<List<ProviderDto>>(
                "/apiMaintenance/workshop-providers", token, ReservationsNode, companyId, cancellationToken) ?? []
            : [];

        if (result is null)
        {
            return new ReservationsIndexVm
            {
                Status = status,
                ProviderId = providerId,
                From = from,
                To = to,
                Vehicles = vehicles?.Items ?? [],
                Providers = providers,
                Error = error ?? "No se pudo cargar el listado de reservas."
            };
        }

        var byId = (vehicles?.Items ?? []).ToDictionary(v => v.Id);

        var items = result.Items.Select(r => new ReservationListItemVm
        {
            Reservation = r,
            PlateNumber = byId.TryGetValue(r.FleetVehicleId, out var vehicle) ? vehicle.PlateNumber : null,
            Brand = byId.TryGetValue(r.FleetVehicleId, out var v2) ? v2.Brand : null,
            Line = byId.TryGetValue(r.FleetVehicleId, out var v3) ? v3.Line : null
        }).ToList();

        return new ReservationsIndexVm
        {
            Items = items,
            PageNumber = result.PageNumber,
            TotalPages = result.TotalPages,
            Status = status,
            ProviderId = providerId,
            From = from,
            To = to,
            Vehicles = vehicles?.Items ?? [],
            Providers = providers,
            Error = error
        };
    }

    private async Task<RoutinesIndexVm> LoadRoutinesAsync(int page, string? search, string? error, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return new RoutinesIndexVm { Error = error };

        var companyId = session.GetActiveCompanyId();
        var query = $"/apiMaintenance/maintenance-routines?page={page}&pageSize=20";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        var result = await gateway.GetTypedAsync<PagedDto<RoutineDto>>(query, token, RoutinesNode, companyId, cancellationToken);

        return new RoutinesIndexVm
        {
            Items = result?.Items ?? [],
            PageNumber = result?.PageNumber ?? 1,
            TotalPages = result?.TotalPages ?? 1,
            Search = search,
            Error = error ?? (result is null ? "No se pudo cargar el listado de rutinas." : null)
        };
    }

    private async Task<VehicleMaintenanceVm?> LoadVehicleAsync(long fleetVehicleId, string? error, CancellationToken cancellationToken, string? initialTab = null)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return null;

        var companyId = session.GetActiveCompanyId();

        var vehicle = await gateway.GetTypedAsync<FleetVehicleOptionVm>($"/apiVehicles/fleet-vehicles/{fleetVehicleId}", token, "vehicle-registry", companyId, cancellationToken);
        if (vehicle is null)
            return null;

        var assignmentTask = gateway.GetTypedAsync<RoutineAssignmentDto>($"/apiMaintenance/fleet-vehicles/{fleetVehicleId}/routine-assignment", token, TrackingNode, companyId, cancellationToken);
        var nextTask = gateway.GetTypedAsync<NextMaintenanceDto>($"/apiMaintenance/fleet-vehicles/{fleetVehicleId}/mileage-readings/next-maintenance", token, TrackingNode, companyId, cancellationToken);
        var readingsTask = gateway.GetTypedAsync<PagedDto<MileageReadingDto>>($"/apiMaintenance/fleet-vehicles/{fleetVehicleId}/mileage-readings?page=1&pageSize=20", token, TrackingNode, companyId, cancellationToken);
        var historyTask = gateway.GetTypedAsync<List<AssignmentHistoryDto>>($"/apiMaintenance/fleet-vehicles/{fleetVehicleId}/routine-assignment/history", token, TrackingNode, companyId, cancellationToken);

        await Task.WhenAll(assignmentTask, nextTask, readingsTask, historyTask);

        var assignment = assignmentTask.Result;
        var next = nextTask.Result;
        var readings = readingsTask.Result;
        var history = historyTask.Result;

        var canManageRoutines = currentUser.Principal.HasNode(RoutinesNode);
        var canManageReservations = currentUser.Principal.HasNode(ReservationsNode);

        var routinesTask = canManageRoutines
            ? gateway.GetTypedAsync<PagedDto<RoutineDto>>("/apiMaintenance/maintenance-routines?page=1&pageSize=100&isActive=true", token, RoutinesNode, companyId, cancellationToken)
            : Task.FromResult<PagedDto<RoutineDto>?>(null);

        var reservationsTask = canManageReservations
            ? gateway.GetTypedAsync<PagedDto<ReservationDto>>($"/apiMaintenance/workshop-reservations?page=1&pageSize=20&fleetVehicleId={fleetVehicleId}", token, ReservationsNode, companyId, cancellationToken)
            : Task.FromResult<PagedDto<ReservationDto>?>(null);

        var providersTask = canManageReservations
            ? gateway.GetTypedAsync<List<ProviderDto>>("/apiMaintenance/workshop-providers", token, ReservationsNode, companyId, cancellationToken)
            : Task.FromResult<List<ProviderDto>?>(null);

        await Task.WhenAll(routinesTask, reservationsTask, providersTask);

        IReadOnlyList<RoutineDto> routines = routinesTask.Result?.Items ?? [];
        IReadOnlyList<ReservationDto> reservations = reservationsTask.Result?.Items ?? [];
        IReadOnlyList<ProviderDto> providers = providersTask.Result ?? [];

        return new VehicleMaintenanceVm
        {
            Vehicle = vehicle,
            Assignment = assignment,
            NextMaintenance = next,
            Readings = readings?.Items ?? [],
            Reservations = reservations,
            History = history ?? [],
            AvailableRoutines = routines,
            Providers = providers,
            CanManageRoutines = canManageRoutines,
            CanManageReservations = canManageReservations,
            InitialTab = NormalizeTab(initialTab),
            Error = error
        };
    }

    private static string NormalizeTab(string? tab) => tab switch
    {
        "readings" or "reservations" or "history" => tab,
        _ => "routine"
    };

    private static string Translate(string? error)
        => AstraSystemsRental.Contracts.Display.ErrorText.Translate(error);
}
