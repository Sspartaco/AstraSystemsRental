using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Mail.Api.Dtos;

namespace AstraSystemsRental.Mail.Api.Services;

public interface IMailService
{
    Task<OperationResult> SendWelcomeAsync(SendWelcomeRequest request, CancellationToken cancellationToken = default);
}
