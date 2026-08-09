using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AstraSystemsRental.Base.Security;

public sealed class AstraRequestContext(IHttpContextAccessor accessor) : IAstraRequestContext
{
    private const string CompanyHeaderName = "X-Astra-Company";

    public long UserId
    {
        get
        {
            var value = User?.FindFirstValue(AstraClaims.UserId);
            return long.TryParse(value, out var id) ? id : 0;
        }
    }

    public string RoleCode => User?.FindFirstValue(AstraClaims.Role) ?? string.Empty;

    public string Email => User?.FindFirstValue(AstraClaims.Email) ?? string.Empty;

    public OwnerContext Owner => ResolveOwner();

    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    private OwnerContext ResolveOwner()
    {
        var context = accessor.HttpContext;
        var requestedCompanyId = context?.Request.Headers[CompanyHeaderName].ToString();

        if (string.IsNullOrWhiteSpace(requestedCompanyId))
            return new OwnerContext(Security.OwnerType.User, UserId);

        if (!long.TryParse(requestedCompanyId, out var companyId))
            throw new CompanyContextForbiddenException();

        var allowedCompanyIds = User?.FindAll(AstraClaims.Company).Select(c => c.Value) ?? [];
        var isMember = allowedCompanyIds.Any(id => long.TryParse(id, out var parsed) && parsed == companyId);

        if (!isMember)
            throw new CompanyContextForbiddenException();

        return new OwnerContext(Security.OwnerType.Company, companyId);
    }
}

public sealed class CompanyContextForbiddenException : Exception
{
    public CompanyContextForbiddenException()
        : base("The requested company context is not allowed for the current user.")
    {
    }
}
