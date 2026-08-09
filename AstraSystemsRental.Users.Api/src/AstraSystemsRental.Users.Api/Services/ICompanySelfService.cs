using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Users.Api.Dtos;

namespace AstraSystemsRental.Users.Api.Services;

public interface ICompanySelfService
{
    Task<OperationResult> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken);
    Task<OperationResult> GetMyCompaniesAsync(CancellationToken cancellationToken);
    Task<OperationResult> GetMembersAsync(long companyId, CancellationToken cancellationToken);
    Task<OperationResult> InviteAsync(long companyId, InviteCompanyMemberRequest request, CancellationToken cancellationToken);
    Task<OperationResult> RevokeInvitationAsync(long companyId, long invitationId, CancellationToken cancellationToken);
    Task<OperationResult> AcceptInvitationAsync(string token, CancellationToken cancellationToken);
    Task<OperationResult> RemoveMemberAsync(long companyId, long userId, CancellationToken cancellationToken);
    Task<OperationResult> TransferOwnershipAsync(long companyId, long newOwnerUserId, CancellationToken cancellationToken);
    Task<OperationResult> CheckActiveMembershipAsync(long companyId, long userId, CancellationToken cancellationToken);
}
