using AstraSystemsRental.Front.Features.Home;
using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.Reports;

public sealed record ReportsIndexVm
{
    public DashboardDto? Dashboard { get; init; }
    public string? Error { get; init; }
}

public sealed class ReportsController(ICurrentUser currentUser, ISessionService session, IGatewayClient gateway) : Controller
{
    private const string NodeKey = "reports";

    [HttpGet("/reports")]
    [HttpGet("/reports/index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!currentUser.Principal.HasNode(NodeKey))
            return Redirect("/");

        var token = session.GetToken();

        var dashboard = string.IsNullOrEmpty(token)
            ? null
            : await gateway.GetTypedAsync<DashboardDto>(
                "/apiReports/dashboard", token, NodeKey, session.GetActiveCompanyId(), cancellationToken);

        return View(new ReportsIndexVm
        {
            Dashboard = dashboard,
            Error = dashboard is null ? "No se pudieron cargar los reportes en este momento." : null
        });
    }
}
