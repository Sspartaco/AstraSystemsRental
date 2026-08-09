using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Users.Api.Domain;

namespace AstraSystemsRental.Users.Api.Persistence;

public interface ICompanyRepository : IBaseRepository<Company>
{
    Task<bool> DocumentExistsAsync(string documentNumber, CancellationToken cancellationToken);
    Task<long> CreateAsync(string name, string documentNumber, string? email, CancellationToken cancellationToken);
    Task<Company?> GetByIdWithMembersAsync(long companyId, CancellationToken cancellationToken);
    Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken);
    Task<bool> IsMemberAsync(long companyId, long userId, CancellationToken cancellationToken);
    Task AssignMemberAsync(long companyId, long userId, bool isOwner, CancellationToken cancellationToken);
    Task<bool> RemoveMemberAsync(long companyId, long userId, CancellationToken cancellationToken);
    Task<long> CreateSubscriptionAsync(long companyId, int planId, DateTime startsAtUtc, DateTime endsAtUtc, CancellationToken cancellationToken);
    Task<Plan?> GetPlanByCodeAsync(string code, CancellationToken cancellationToken);
    Task<bool> CompanyExistsAsync(long companyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompanyMembershipRow>> GetCompaniesForUserAsync(long userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompanyMemberRow>> GetMembersAsync(long companyId, CancellationToken cancellationToken);
    Task<bool> IsOwnerAsync(long companyId, long userId, CancellationToken cancellationToken);
    Task<int> CountOwnersAsync(long companyId, CancellationToken cancellationToken);
    Task<bool> TransferOwnershipAsync(long companyId, long currentOwnerUserId, long newOwnerUserId, CancellationToken cancellationToken);
    Task<long?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken);
}

public sealed record CompanyMembershipRow(long CompanyId, string CompanyName, bool IsOwner);
public sealed record CompanyMemberRow(long UserId, string Email, string FullName, bool IsOwner, DateTime JoinedAtUtc);
