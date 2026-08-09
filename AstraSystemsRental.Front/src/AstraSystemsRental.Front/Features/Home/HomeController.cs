using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.Home;

public sealed class HomeController(ICurrentUser currentUser, ISessionService session, IGatewayClient gateway) : Controller
{
    private const string FleetNode = "vehicle-registry";
    private const string TrackingNode = "maintenance-tracking";

    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return Redirect("/auth/login");

        var principal = currentUser.Principal;
        var canSeeFleet = principal.HasNode(FleetNode);
        var canSeeWorkshop = principal.HasNode(TrackingNode);

        if (!canSeeFleet && !canSeeWorkshop)
        {
            return View(new HomeViewModel
            {
                Principal = principal,
                CanSeeFleet = false,
                CanSeeWorkshop = false
            });
        }

        var token = session.GetToken();
        DashboardDto? dashboard = null;
        string? error = null;

        if (!string.IsNullOrEmpty(token))
        {
            dashboard = await gateway.GetTypedAsync<DashboardDto>(
                "/apiReports/dashboard", token, FleetNode, session.GetActiveCompanyId(), cancellationToken);

            if (dashboard is null)
                error = "No se pudieron cargar los indicadores en este momento.";
        }

        return View(new HomeViewModel
        {
            Principal = principal,
            Dashboard = dashboard,
            CanSeeFleet = canSeeFleet,
            CanSeeWorkshop = canSeeWorkshop,
            Error = error
        });
    }

    [HttpGet("/subscription/expired")]
    public IActionResult Expired()
    {
        return View(currentUser.Principal);
    }
}
