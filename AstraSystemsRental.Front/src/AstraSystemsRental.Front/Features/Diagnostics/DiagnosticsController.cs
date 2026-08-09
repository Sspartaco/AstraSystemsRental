using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.Diagnostics;

public sealed record LogEntryDto
{
    public long Id { get; init; }
    public DateTime TimestampUtc { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ExceptionType { get; init; }
    public string? ExceptionDetail { get; init; }
    public string? TraceId { get; init; }
    public string? RequestMethod { get; init; }
    public string? RequestPath { get; init; }
    public int? StatusCode { get; init; }
    public string? UserEmail { get; init; }
}

public sealed record LogsPageDto
{
    public IReadOnlyList<LogEntryDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
}

public sealed record LogsIndexVm
{
    public IReadOnlyList<LogEntryDto> Items { get; init; } = [];
    public IReadOnlyList<string> Services { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public string? Level { get; init; }
    public string? Service { get; init; }
    public string? Search { get; init; }
    public string? Error { get; init; }
}

public sealed class DiagnosticsController(ICurrentUser currentUser, ISessionService session, IGatewayClient gateway) : Controller
{
    [HttpGet("/diagnostics/logs")]
    public async Task<IActionResult> Logs(
        int page, string? level, string? service, string? search, CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.CanSeeDiagnostics)
            return Redirect("/");

        var token = session.GetToken();

        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        page = page == 0 ? 1 : page;

        var query = $"/apiUsers/logs?page={page}&pageSize=50";

        if (!string.IsNullOrWhiteSpace(level))
            query += $"&level={Uri.EscapeDataString(level)}";

        if (!string.IsNullOrWhiteSpace(service))
            query += $"&service={Uri.EscapeDataString(service)}";

        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        var result = await gateway.GetTypedAsync<LogsPageDto>(query, token, null, null, cancellationToken);

        var isPartial = Request.Headers.ContainsKey("HX-Request");

        var services = isPartial
            ? []
            : await gateway.GetTypedAsync<List<string>>("/apiUsers/logs/services", token, null, null, cancellationToken) ?? [];

        var vm = new LogsIndexVm
        {
            Items = result?.Items ?? [],
            Services = services,
            TotalCount = result?.TotalCount ?? 0,
            PageNumber = result?.PageNumber ?? page,
            TotalPages = result?.TotalPages ?? 1,
            Level = level,
            Service = service,
            Search = search,
            Error = result is null ? "No se pudieron cargar los logs." : null
        };

        return isPartial ? PartialView("_LogsTable", vm) : View(vm);
    }
}
