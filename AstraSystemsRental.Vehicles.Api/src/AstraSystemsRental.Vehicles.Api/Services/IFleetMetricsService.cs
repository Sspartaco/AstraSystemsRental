using AstraSystemsRental.Base.Contracts;

namespace AstraSystemsRental.Vehicles.Api.Services;

public interface IFleetMetricsService
{
    Task<OperationResult> GetMetricsAsync(CancellationToken cancellationToken);
}
