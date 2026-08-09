using AstraSystemsRental.Base.Http;

namespace AstraSystemsRental.Maintenance.Api.Services;

public sealed record QuotaLookupResult(string NodeKey, int MaxCount, string PlanCode);

public sealed record FleetVehicleSummary(long Id, string PlateNumber, string? Brand, string? Line, string Status);

public interface IUsersApiClient
{
    Task<QuotaLookupResult?> GetQuotaAsync(string nodeKey, CancellationToken cancellationToken);
    Task<CrossApiResult> TryReserveQuotaAsync(string nodeKey, CancellationToken cancellationToken);
    Task<CrossApiResult> ReleaseQuotaAsync(string nodeKey, CancellationToken cancellationToken);
    Task<CrossApiResult> IsActiveCompanyMemberAsync(long companyId, long userId, CancellationToken cancellationToken);
}

public interface IVehiclesApiClient
{
    Task<FleetVehicleSummary?> GetFleetVehicleAsync(long fleetVehicleId, CancellationToken cancellationToken);
}

public interface IMailApiClient
{
    Task<CrossApiResult> SendReservationReadyAsync(string toEmail, string plateNumber, CancellationToken cancellationToken);
}
