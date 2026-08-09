using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;

namespace AstraSystemsRental.Maintenance.Api.Services;

public interface IMaintenanceContextGuard
{
    Task<OperationResult?> EnsureCompanyMembershipAsync(CancellationToken cancellationToken);
    Task<OperationResult?> EnsureVehicleAccessibleAsync(long fleetVehicleId, CancellationToken cancellationToken);
    Task<FleetVehicleSummary?> GetVehicleAsync(long fleetVehicleId, CancellationToken cancellationToken);
}

public sealed class MaintenanceContextGuard(
    IUsersApiClient usersApiClient,
    IVehiclesApiClient vehiclesApiClient,
    IAstraRequestContext requestContext) : IMaintenanceContextGuard
{
    public async Task<OperationResult?> EnsureCompanyMembershipAsync(CancellationToken cancellationToken)
    {
        var owner = requestContext.Owner;
        if (owner.OwnerType != OwnerType.Company)
            return null;

        var result = await usersApiClient.IsActiveCompanyMemberAsync(owner.OwnerId, requestContext.UserId, cancellationToken);
        if (result.Success)
            return null;

        return result.Unreachable
            ? OperationResult.Fail("UpstreamUnavailable", System.Net.HttpStatusCode.ServiceUnavailable)
            : OperationResult.Forbidden("CompanyContextForbidden");
    }

    public async Task<OperationResult?> EnsureVehicleAccessibleAsync(long fleetVehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await vehiclesApiClient.GetFleetVehicleAsync(fleetVehicleId, cancellationToken);
        if (vehicle is null)
            return OperationResult.NotFound("Vehicle not found.");

        if (string.Equals(vehicle.Status, "Blocked", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vehicle.Status, "Sold", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Fail("VehicleNotOperational", System.Net.HttpStatusCode.Conflict);
        }

        return null;
    }

    public Task<FleetVehicleSummary?> GetVehicleAsync(long fleetVehicleId, CancellationToken cancellationToken)
        => vehiclesApiClient.GetFleetVehicleAsync(fleetVehicleId, cancellationToken);
}
