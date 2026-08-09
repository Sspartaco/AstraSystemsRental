using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Users.Api.Domain;

namespace AstraSystemsRental.Users.Api.Persistence;

public interface IUserRepository : IBaseRepository<User>
{
    Task<PagedResult<UserOverview>> GetUsersOverviewAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken);
    Task<Plan?> GetPlanByCodeAsync(string code, CancellationToken cancellationToken);
    Task<long> CreateUserWithConfirmationAsync(CreateUserData person, int roleId, Plan plan, string token, DateTime expiresAtUtc, CancellationToken cancellationToken);
    Task<long?> ConfirmAsync(string token, byte[] passwordHash, byte[] passwordSalt, DateTime nowUtc, CancellationToken cancellationToken);
    Task<LoginProjection?> GetLoginProjectionAsync(string email, CancellationToken cancellationToken);
    Task<LoginProjection?> GetLoginProjectionByIdAsync(long userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetAllowedNodesAsync(long userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<long>> GetMemberCompanyIdsAsync(long userId, CancellationToken cancellationToken);
    Task<bool> SuperUserExistsAsync(CancellationToken cancellationToken);
    Task<long> CreateSuperUserAsync(CreateUserData person, byte[] passwordHash, byte[] passwordSalt, CancellationToken cancellationToken);
    Task<bool> SetRoleAsync(long userId, string roleCode, CancellationToken cancellationToken);
    Task<UserProfile?> GetProfileAsync(long userId, CancellationToken cancellationToken);
    Task<bool> SetActiveAsync(long userId, bool isActive, CancellationToken cancellationToken);
    Task<bool> AssignPlanAsync(long userId, Plan plan, DateTime endsAtUtc, CancellationToken cancellationToken);
}

public sealed record UserProfile(
    long UserId,
    string Email,
    string FirstNames,
    string LastNames,
    string? Address,
    string PersonType,
    string DocumentNumber,
    string RoleCode,
    string RoleName,
    bool IsActive,
    bool IsConfirmed,
    DateTime CreatedAtUtc,
    string? PlanCode,
    string? PlanName,
    DateTime? SubscriptionEndsAtUtc,
    int CompanyCount);

public sealed record CreateUserData(
    string FirstNames,
    string LastNames,
    string? Address,
    string PersonType,
    string DocumentNumber,
    string? CompanySize,
    string Email);
