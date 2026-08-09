using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Users.Api.Dtos;

namespace AstraSystemsRental.Users.Api.Services;

public interface IUserService
{
    Task<OperationResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<OperationResult> ConfirmAsync(ConfirmEmailRequest request, CancellationToken cancellationToken);
    Task<OperationResult> GetOverviewAsync(int page, int pageSize, string? search, CancellationToken cancellationToken);
    Task<OperationResult> GetProfileAsync(CancellationToken cancellationToken);
}

public interface IAuthService
{
    Task<OperationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<OperationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<OperationResult> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}
