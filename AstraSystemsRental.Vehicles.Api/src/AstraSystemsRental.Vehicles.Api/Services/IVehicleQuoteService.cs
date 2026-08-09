using AstraSystemsRental.Base.Contracts;

namespace AstraSystemsRental.Vehicles.Api.Services;

public interface IVehicleQuoteService
{
    Task<OperationResult> GetVehicleAsync(string plateNumber, CancellationToken cancellationToken);
    Task<OperationResult> CreateVehicleAsync(Dtos.CreateVehicleRequest request, CancellationToken cancellationToken);
    Task<OperationResult> CreateQuoteAsync(string plateNumber, long requestedByUserId, CancellationToken cancellationToken);
    Task<OperationResult> GetQuoteStatusAsync(Guid requestId, long userId, CancellationToken cancellationToken);
}
