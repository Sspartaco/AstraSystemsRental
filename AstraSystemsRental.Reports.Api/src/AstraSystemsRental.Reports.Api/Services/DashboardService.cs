using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Reports.Api.Dtos;

namespace AstraSystemsRental.Reports.Api.Services;

public interface IDashboardService
{
    Task<OperationResult> GetDashboardAsync(CancellationToken cancellationToken);
}

public sealed class DashboardService(
    IFleetMetricsSource fleetSource,
    IWorkshopMetricsSource workshopSource) : IDashboardService
{
    public async Task<OperationResult> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var fleetTask = fleetSource.GetAsync(cancellationToken);
        var workshopTask = workshopSource.GetAsync(cancellationToken);

        await Task.WhenAll(fleetTask, workshopTask);

        var fleet = fleetTask.Result;
        var workshop = workshopTask.Result;

        var response = new DashboardResponse
        {
            Fleet = fleet,
            Workshop = workshop,
            FleetAvailable = fleet is not null,
            WorkshopAvailable = workshop is not null,
            CoverageRatio = Ratio(workshop?.VehiclesWithRoutine ?? 0, fleet?.TotalVehicles ?? 0),
            UtilizationRatio = Ratio(fleet?.ActiveVehicles ?? 0, fleet?.TotalVehicles ?? 0),
            GeneratedAtUtc = DateTime.UtcNow
        };

        return OperationResult.Ok(response);
    }

    private static int Ratio(int part, int total)
        => total <= 0 ? 0 : (int)Math.Round(part * 100d / total);
}
