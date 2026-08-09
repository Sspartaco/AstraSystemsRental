using AstraSystemsRental.Base.Contracts;

namespace AstraSystemsRental.Users.Api.Services;

public interface IQuotaService
{
    Task<OperationResult> GetQuotaAsync(string nodeKey, CancellationToken cancellationToken);
    Task<OperationResult> ReserveAsync(string nodeKey, CancellationToken cancellationToken);
    Task<OperationResult> ReleaseAsync(string nodeKey, CancellationToken cancellationToken);
}
