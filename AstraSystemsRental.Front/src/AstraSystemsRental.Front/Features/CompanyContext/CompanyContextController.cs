using AstraSystemsRental.Front.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.CompanyContext;

public sealed class CompanyContextController(ISessionService session) : Controller
{
    [HttpPost("/context/company")]
    public IActionResult SetActive(long? companyId)
    {
        session.SetActiveCompanyId(companyId);

        var referer = Request.Headers.Referer.ToString();
        return string.IsNullOrEmpty(referer) ? Redirect("/") : Redirect(referer);
    }
}
