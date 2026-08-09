using Microsoft.AspNetCore.DataProtection;

namespace AstraSystemsRental.Front.Services;

public interface ISessionService
{
    void SignIn(string jwt, bool remember);
    string? GetToken();
    void SignOut();
    long? GetActiveCompanyId();
    void SetActiveCompanyId(long? companyId);
}

public sealed class SessionService(IHttpContextAccessor accessor, IDataProtectionProvider protectionProvider) : ISessionService
{
    private const string CookieName = "astra.session";
    private const string CompanyCookieName = "astra.company";
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("astra.session.v1");

    public void SignIn(string jwt, bool remember)
    {
        var context = accessor.HttpContext!;
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = remember ? DateTimeOffset.UtcNow.AddDays(30) : null
        };
        context.Response.Cookies.Append(CookieName, _protector.Protect(jwt), options);
    }

    public string? GetToken()
    {
        var context = accessor.HttpContext!;
        if (!context.Request.Cookies.TryGetValue(CookieName, out var protectedValue) || string.IsNullOrEmpty(protectedValue))
            return null;

        try
        {
            return _protector.Unprotect(protectedValue);
        }
        catch
        {
            return null;
        }
    }

    public void SignOut()
    {
        accessor.HttpContext!.Response.Cookies.Delete(CookieName);
        accessor.HttpContext!.Response.Cookies.Delete(CompanyCookieName);
    }

    public long? GetActiveCompanyId()
    {
        var context = accessor.HttpContext!;
        if (!context.Request.Cookies.TryGetValue(CompanyCookieName, out var value) || !long.TryParse(value, out var companyId))
            return null;

        return companyId;
    }

    public void SetActiveCompanyId(long? companyId)
    {
        var context = accessor.HttpContext!;
        if (companyId is null)
        {
            context.Response.Cookies.Delete(CompanyCookieName);
            return;
        }

        context.Response.Cookies.Append(CompanyCookieName, companyId.Value.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true
        });
    }
}
