using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Vehicles.Api.Dtos;

namespace AstraSystemsRental.Vehicles.Api.Services;

public interface IFleetVehicleService
{
    Task<OperationResult> GetPagedAsync(int pageNumber, int pageSize, string? status, string? search, CancellationToken cancellationToken);
    Task<OperationResult> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<OperationResult> CreateAsync(CreateFleetVehicleRequest request, CancellationToken cancellationToken);
    Task<OperationResult> UpdateAsync(long id, UpdateFleetVehicleRequest request, CancellationToken cancellationToken);
    Task<OperationResult> DeleteAsync(long id, CancellationToken cancellationToken);
    Task<OperationResult> ChangeStatusAsync(long id, ChangeFleetVehicleStatusRequest request, CancellationToken cancellationToken);
    Task<OperationResult> GetStatusHistoryAsync(long id, CancellationToken cancellationToken);
    Task<OperationResult> GetDocumentsAsync(long id, CancellationToken cancellationToken);
    Task<OperationResult> AddDocumentAsync(long id, CreateFleetVehicleDocumentRequest request, CancellationToken cancellationToken);
    Task<OperationResult> UpdateDocumentAsync(long id, long documentId, UpdateFleetVehicleDocumentRequest request, CancellationToken cancellationToken);
}
