using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Shared.ViewComponents;

public sealed class SidebarViewComponent(ICurrentUser currentUser, ISessionService session, INodeCatalogService catalog) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var principal = currentUser.Principal;
        var token = session.GetToken();

        var nodes = string.IsNullOrEmpty(token)
            ? []
            : await catalog.GetAsync(token, HttpContext.RequestAborted);

        var visible = nodes
            .Where(node => node.DiagnosticsOnly ? principal.CanSeeDiagnostics : node.SuperUserOnly ? principal.IsSuperUser : principal.HasNode(node.Key))
            .ToList();

        return View(visible);
    }
}
