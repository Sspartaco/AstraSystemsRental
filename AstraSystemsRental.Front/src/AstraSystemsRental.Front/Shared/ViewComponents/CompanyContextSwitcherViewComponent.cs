using System.Text.Json;
using AstraSystemsRental.Front.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Shared.ViewComponents;

public sealed record CompanyOptionVm(long CompanyId, string CompanyName, bool IsOwner, bool IsActive);

public sealed class CompanyContextSwitcherViewComponent(IGatewayClient gateway, ISessionService session) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return View(new List<CompanyOptionVm>());

        var activeCompanyId = session.GetActiveCompanyId();
        var data = await gateway.GetAsync("/apiUsers/companies/self", token, HttpContext.RequestAborted);

        var companies = new List<CompanyOptionVm>();
        if (data is { ValueKind: JsonValueKind.Array } arr)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var companyId = item.GetProperty("companyId").GetInt64();
                companies.Add(new CompanyOptionVm(
                    companyId,
                    item.GetProperty("companyName").GetString() ?? string.Empty,
                    item.GetProperty("isOwner").GetBoolean(),
                    companyId == activeCompanyId));
            }
        }

        return View(companies);
    }
}
