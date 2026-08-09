using System.Text.Json;
using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.Fleet;

public sealed record VehicleVm(string PlateNumber, string? VehicleClass, string? Brand, string? Line, short? ModelYear, string? FullLine, string? Engine, string? ImageUrl, string? ImageAttribution);

public sealed record SourceCardVm(
    string SourceCode, string DisplayName, string Status,
    decimal? ValueMin, decimal? ValueMax, decimal? ValueAvg,
    string Currency, DateTime? FetchedAtUtc, string? ErrorMessage);

public sealed record QuoteSummaryVm(decimal? Min, decimal? Max, decimal? Avg, int SuccessCount, int TotalSources);

public sealed record QuoteResultVm(Guid RequestId, string PlateNumber, string Status, VehicleVm? Vehicle, IReadOnlyList<SourceCardVm> Sources)
{
    public bool IsRunning => Status == "Running";

    public QuoteSummaryVm Summary
    {
        get
        {
            var ok = Sources.Where(s => s.Status == "Success" && s.ValueAvg is not null).ToList();
            if (ok.Count == 0)
                return new QuoteSummaryVm(null, null, null, 0, Sources.Count);
            return new QuoteSummaryVm(
                ok.Min(s => s.ValueMin ?? s.ValueAvg),
                ok.Max(s => s.ValueMax ?? s.ValueAvg),
                Math.Round(ok.Average(s => s.ValueAvg!.Value), 0),
                ok.Count,
                Sources.Count);
        }
    }
}

public sealed record ManualVehicleVm(string PlateNumber, string? Error);

public sealed record SearchForm(string PlateNumber);
public sealed record ManualVehicleForm(string PlateNumber, string? VehicleClass, string Brand, string Line, short? ModelYear, string? Engine);

public sealed class FleetController(IGatewayClient gateway, ISessionService session, ICurrentUser currentUser) : Controller
{
    private const string NodeKey = "fleet";

    private static readonly Dictionary<string, string> SourceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Fasecolda"] = "Fasecolda",
        ["RevistaMotor"] = "Revista Motor",
        ["Tucarro"] = "TuCarro",
        ["AsoUsados"] = "AutoExpo",
        ["OtrasFuentes"] = "Otras Fuentes"
    };

    [HttpGet("/fleet")]
    public IActionResult Index()
    {
        if (!currentUser.Principal.HasNode(NodeKey))
            return Redirect("/");

        return View();
    }

    [HttpPost("/fleet/quotes")]
    public async Task<IActionResult> Search([FromForm] SearchForm form, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(NodeKey))
            return Redirect("/");

        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var plate = form.PlateNumber.Trim().ToUpperInvariant();
        var response = await gateway.SendForDataAsync(HttpMethod.Post, "/apiVehicles/quotes", token, new { plateNumber = plate }, NodeKey, cancellationToken);

        if (response.StatusCode == 404)
            return PartialView("_ManualVehicleForm", new ManualVehicleVm(plate, null));

        if (!response.IsSuccess || response.Data is not { } data || !data.TryGetProperty("requestId", out var rid))
            return PartialView("_ManualVehicleForm", new ManualVehicleVm(plate, response.Errors.FirstOrDefault() ?? "No se pudo iniciar la cotización."));

        var requestId = rid.GetGuid();
        var model = await LoadQuoteResult(requestId, token, cancellationToken);
        return PartialView("_QuoteResult", model);
    }

    [HttpGet("/fleet/quotes/{requestId:guid}")]
    public async Task<IActionResult> QuoteStatus(Guid requestId, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(NodeKey))
            return Redirect("/");

        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var model = await LoadQuoteResult(requestId, token, cancellationToken);
        return PartialView("_QuoteResult", model);
    }

    [HttpPost("/fleet/vehicles")]
    public async Task<IActionResult> CreateVehicle([FromForm] ManualVehicleForm form, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(NodeKey))
            return Redirect("/");

        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var plate = form.PlateNumber.Trim().ToUpperInvariant();
        var create = await gateway.SendForDataAsync(HttpMethod.Post, "/apiVehicles/vehicles", token, new
        {
            plateNumber = plate,
            vehicleClass = form.VehicleClass,
            brand = form.Brand,
            line = form.Line,
            modelYear = form.ModelYear,
            engine = form.Engine
        }, NodeKey, cancellationToken);

        if (!create.IsSuccess)
            return PartialView("_ManualVehicleForm", new ManualVehicleVm(plate, create.Errors.FirstOrDefault() ?? "No se pudo registrar el vehículo."));

        var quote = await gateway.SendForDataAsync(HttpMethod.Post, "/apiVehicles/quotes", token, new { plateNumber = plate }, NodeKey, cancellationToken);
        if (!quote.IsSuccess || quote.Data is not { } data || !data.TryGetProperty("requestId", out var rid))
            return PartialView("_ManualVehicleForm", new ManualVehicleVm(plate, quote.Errors.FirstOrDefault() ?? "No se pudo iniciar la cotización."));

        var model = await LoadQuoteResult(rid.GetGuid(), token, cancellationToken);
        return PartialView("_QuoteResult", model);
    }

    private async Task<QuoteResultVm?> LoadQuoteResult(Guid requestId, string token, CancellationToken cancellationToken)
    {
        var data = await gateway.GetAsync($"/apiVehicles/quotes/{requestId}", token, NodeKey, cancellationToken);
        if (data is not { } d)
            return null;

        var plate = d.GetProperty("plateNumber").GetString() ?? string.Empty;
        var status = d.GetProperty("status").GetString() ?? "Running";

        var sources = new List<SourceCardVm>();
        if (d.TryGetProperty("sources", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in arr.EnumerateArray())
            {
                var code = s.GetProperty("sourceCode").GetString() ?? string.Empty;
                sources.Add(new SourceCardVm(
                    code,
                    SourceNames.GetValueOrDefault(code, code),
                    s.GetProperty("status").GetString() ?? "Pending",
                    GetDecimal(s, "valueMin"),
                    GetDecimal(s, "valueMax"),
                    GetDecimal(s, "valueAvg"),
                    s.TryGetProperty("currency", out var c) ? c.GetString() ?? "COP" : "COP",
                    s.TryGetProperty("fetchedAtUtc", out var f) && f.ValueKind != JsonValueKind.Null ? f.GetDateTime() : null,
                    s.TryGetProperty("errorMessage", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetString() : null));
            }
        }

        var vehicle = await LoadVehicle(plate, token, cancellationToken);
        return new QuoteResultVm(requestId, plate, status, vehicle, sources);
    }

    private async Task<VehicleVm?> LoadVehicle(string plate, string token, CancellationToken cancellationToken)
    {
        var data = await gateway.GetAsync($"/apiVehicles/vehicles/{Uri.EscapeDataString(plate)}", token, NodeKey, cancellationToken);
        if (data is not { } v)
            return null;

        return new VehicleVm(
            v.GetProperty("plateNumber").GetString() ?? plate,
            GetString(v, "vehicleClass"),
            GetString(v, "brand"),
            GetString(v, "line"),
            v.TryGetProperty("modelYear", out var y) && y.ValueKind == JsonValueKind.Number ? y.GetInt16() : null,
            GetString(v, "fullLine"),
            GetString(v, "engine"),
            GetString(v, "imageUrl"),
            GetString(v, "imageAttribution"));
    }

    private static decimal? GetDecimal(JsonElement element, string prop)
        => element.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;

    private static string? GetString(JsonElement element, string prop)
        => element.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
