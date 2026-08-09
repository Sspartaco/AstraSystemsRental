using System.Text.Json;
using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.VehicleRegistry;

public sealed record FleetVehicleListItemVm(long Id, string PlateNumber, string? Brand, string? Line, short? ModelYear, string Status);
public sealed record FleetVehiclesPageVm(IReadOnlyList<FleetVehicleListItemVm> Items, int PageNumber, int TotalPages, string? Status, string? Search, string? Error);

public sealed record FleetVehicleDetailVm(
    long Id, string PlateNumber, string? VehicleClass, string? Brand, string? Line, short? ModelYear,
    string? BodyType, string? ServiceType, string? FuelType, string? Transmission, string? Color,
    string? Vin, string? EngineNumber, string? SerialNumber, string? ChassisNumber, string? TransitLicenseNumber,
    string? PurchaseInvoiceNumber, DateOnly? PurchaseDate, decimal? PurchaseValue,
    string Status, string? Notes, string RowVersion,
    IReadOnlyList<FleetVehicleDocumentVm> Documents,
    IReadOnlyList<StatusHistoryVm> StatusHistory,
    string? Error);

public sealed record FleetVehicleDocumentVm(long Id, string DocumentType, string? DocumentNumber, DateOnly? ExpiresDate, string Status);
public sealed record StatusHistoryVm(string? PreviousStatus, string NewStatus, string? Reason, DateTime ChangedAtUtc);

public sealed record CreateVehicleForm(
    string PlateNumber, string? VehicleClass, string? Brand, string? Line, short? ModelYear,
    string? BodyType, string? ServiceType, string? FuelType, string? Transmission, string? Color,
    string? Vin, string? EngineNumber, string? SerialNumber, string? ChassisNumber, string? TransitLicenseNumber,
    string? PurchaseInvoiceNumber, DateOnly? PurchaseDate, decimal? PurchaseValue, string? Notes);

public sealed record ChangeStatusForm(string NewStatus, string? Reason);
public sealed record DocumentForm(string DocumentType, string? DocumentNumber, DateOnly? IssuedDate, DateOnly? ExpiresDate, string? Notes);

public sealed class VehicleRegistryController(IGatewayClient gateway, ISessionService session, ICurrentUser currentUser) : Controller
{
    private const string NodeKey = "vehicle-registry";

    [HttpGet("/vehicle-registry")]
    public async Task<IActionResult> Index(int page, string? status, string? search, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(NodeKey))
            return Redirect("/");

        var vm = await LoadPage(page == 0 ? 1 : page, status, search, cancellationToken);

        return Request.Headers.ContainsKey("HX-Request")
            ? PartialView("_FleetTable", vm)
            : View(vm);
    }

    [HttpGet("/vehicle-registry/create")]
    public IActionResult Create()
    {
        if (!currentUser.Principal.HasNode(NodeKey))
            return Redirect("/");

        ViewData["Error"] = TempData["CreateError"];
        return View();
    }

    [HttpPost("/vehicle-registry")]
    public async Task<IActionResult> Store([FromForm] CreateVehicleForm form, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(NodeKey))
            return Redirect("/");

        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var companyId = session.GetActiveCompanyId();
        var response = await gateway.SendForDataAsync(HttpMethod.Post, "/apiVehicles/fleet-vehicles", token, form, NodeKey, companyId, cancellationToken);

        if (!response.IsSuccess)
        {
            TempData["CreateError"] = TranslateError(response.Errors.FirstOrDefault());
            return Redirect("/vehicle-registry/create");
        }

        long? id = response.Data is { } data && data.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : null;
        return id is not null ? Redirect($"/vehicle-registry/{id}") : Redirect("/vehicle-registry");
    }

    [HttpGet("/vehicle-registry/{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(NodeKey))
            return Redirect("/");

        var detail = await LoadDetail(id, null, cancellationToken);
        if (detail is null)
            return NotFound();

        return View(detail);
    }

    [HttpPost("/vehicle-registry/{id:long}/status")]
    public async Task<IActionResult> ChangeStatus(long id, [FromForm] ChangeStatusForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            var companyId = session.GetActiveCompanyId();
            await gateway.SendForDataAsync(HttpMethod.Post, $"/apiVehicles/fleet-vehicles/{id}/status-transitions", token,
                new { newStatus = form.NewStatus, reason = form.Reason }, NodeKey, companyId, cancellationToken);
        }

        return PartialView("_VehicleDetail", await LoadDetail(id, null, cancellationToken));
    }

    [HttpPost("/vehicle-registry/{id:long}/documents")]
    public async Task<IActionResult> AddDocument(long id, [FromForm] DocumentForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        string? error = null;
        if (!string.IsNullOrEmpty(token))
        {
            var companyId = session.GetActiveCompanyId();
            var response = await gateway.SendForDataAsync(HttpMethod.Post, $"/apiVehicles/fleet-vehicles/{id}/documents", token,
                new { documentType = form.DocumentType, documentNumber = form.DocumentNumber, issuedDate = form.IssuedDate, expiresDate = form.ExpiresDate, notes = form.Notes },
                NodeKey, companyId, cancellationToken);
            if (!response.IsSuccess)
                error = TranslateError(response.Errors.FirstOrDefault());
        }

        return PartialView("_VehicleDetail", await LoadDetail(id, error, cancellationToken));
    }

    private async Task<FleetVehiclesPageVm> LoadPage(int page, string? status, string? search, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return new FleetVehiclesPageVm([], 1, 1, status, search, null);

        var companyId = session.GetActiveCompanyId();
        var query = $"/apiVehicles/fleet-vehicles?page={page}&pageSize=20";
        if (!string.IsNullOrWhiteSpace(status))
            query += $"&status={Uri.EscapeDataString(status)}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        var data = await gateway.GetAsync(query, token, NodeKey, companyId, cancellationToken);
        if (data is not { } d)
            return new FleetVehiclesPageVm([], 1, 1, status, search, "No se pudo cargar el listado.");

        var items = new List<FleetVehicleListItemVm>();
        if (d.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                items.Add(new FleetVehicleListItemVm(
                    item.GetProperty("id").GetInt64(),
                    item.GetProperty("plateNumber").GetString() ?? string.Empty,
                    GetString(item, "brand"),
                    GetString(item, "line"),
                    item.TryGetProperty("modelYear", out var y) && y.ValueKind == JsonValueKind.Number ? y.GetInt16() : null,
                    item.GetProperty("status").GetString() ?? "Draft"));
            }
        }

        return new FleetVehiclesPageVm(
            items,
            d.TryGetProperty("pageNumber", out var pn) ? pn.GetInt32() : 1,
            d.TryGetProperty("totalPages", out var tp) ? tp.GetInt32() : 1,
            status, search, null);
    }

    private async Task<FleetVehicleDetailVm?> LoadDetail(long id, string? error, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return null;

        var companyId = session.GetActiveCompanyId();
        var data = await gateway.GetAsync($"/apiVehicles/fleet-vehicles/{id}", token, NodeKey, companyId, cancellationToken);
        if (data is not { } v)
            return null;

        var documentsData = await gateway.GetAsync($"/apiVehicles/fleet-vehicles/{id}/documents", token, NodeKey, companyId, cancellationToken);
        var documents = new List<FleetVehicleDocumentVm>();
        if (documentsData is { ValueKind: JsonValueKind.Array } documentsArr)
        {
            foreach (var doc in documentsArr.EnumerateArray())
                documents.Add(new FleetVehicleDocumentVm(
                    doc.GetProperty("id").GetInt64(), doc.GetProperty("documentType").GetString() ?? string.Empty,
                    GetString(doc, "documentNumber"),
                    doc.TryGetProperty("expiresDate", out var ed) && ed.ValueKind == JsonValueKind.String ? DateOnly.Parse(ed.GetString()!) : null,
                    doc.GetProperty("status").GetString() ?? "Pending"));
        }

        var historyData = await gateway.GetAsync($"/apiVehicles/fleet-vehicles/{id}/status-history", token, NodeKey, companyId, cancellationToken);
        var history = new List<StatusHistoryVm>();
        if (historyData is { ValueKind: JsonValueKind.Array } historyArr)
        {
            foreach (var h in historyArr.EnumerateArray())
                history.Add(new StatusHistoryVm(
                    GetString(h, "previousStatus"), h.GetProperty("newStatus").GetString() ?? string.Empty,
                    GetString(h, "reason"), h.GetProperty("changedAtUtc").GetDateTime()));
        }

        return new FleetVehicleDetailVm(
            v.GetProperty("id").GetInt64(), v.GetProperty("plateNumber").GetString() ?? string.Empty,
            GetString(v, "vehicleClass"), GetString(v, "brand"), GetString(v, "line"),
            v.TryGetProperty("modelYear", out var my) && my.ValueKind == JsonValueKind.Number ? my.GetInt16() : null,
            GetString(v, "bodyType"), GetString(v, "serviceType"), GetString(v, "fuelType"), GetString(v, "transmission"), GetString(v, "color"),
            GetString(v, "vin"), GetString(v, "engineNumber"), GetString(v, "serialNumber"), GetString(v, "chassisNumber"), GetString(v, "transitLicenseNumber"),
            GetString(v, "purchaseInvoiceNumber"),
            v.TryGetProperty("purchaseDate", out var pd) && pd.ValueKind == JsonValueKind.String ? DateOnly.Parse(pd.GetString()!) : null,
            v.TryGetProperty("purchaseValue", out var pv) && pv.ValueKind == JsonValueKind.Number ? pv.GetDecimal() : null,
            v.GetProperty("status").GetString() ?? "Draft", GetString(v, "notes"), v.GetProperty("rowVersion").GetString() ?? string.Empty,
            documents, history, error);
    }

    private static string? GetString(JsonElement element, string prop)
        => element.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string? TranslateError(string? error) => error switch
    {
        "QuotaExceeded" => "Alcanzaste el límite de vehículos de tu plan actual.",
        _ => AstraSystemsRental.Contracts.Display.ErrorText.Translate(error)
    };
}
