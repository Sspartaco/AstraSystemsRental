using AstraSystemsRental.Base.Http;

namespace AstraSystemsRental.Vehicles.Api.Services;

public sealed record QuotaLookupResult(string NodeKey, int MaxCount, string PlanCode);

public interface IUsersApiClient
{
    Task<QuotaLookupResult?> GetQuotaAsync(string nodeKey, CancellationToken cancellationToken);
    Task<CrossApiResult> TryReserveQuotaAsync(string nodeKey, CancellationToken cancellationToken);
    Task<CrossApiResult> ReleaseQuotaAsync(string nodeKey, CancellationToken cancellationToken);
    Task<CrossApiResult> IsActiveCompanyMemberAsync(long companyId, long userId, CancellationToken cancellationToken);
}
