using AstraSystemsRental.Base.Contracts;

namespace AstraSystemsRental.Maintenance.Api.Services;

public interface IMaintenanceMetricsService
{
    Task<OperationResult> GetMetricsAsync(CancellationToken cancellationToken);
}
