using AstraSystemsRental.Users.Api.Domain;

namespace AstraSystemsRental.Users.Api.Persistence;

public interface ICompanyInvitationRepository
{
    Task<CompanyInvitation?> GetPendingByCompanyAndEmailAsync(long companyId, string email, CancellationToken cancellationToken);
    Task<CompanyInvitation?> GetByIdAsync(long invitationId, CancellationToken cancellationToken);
    Task<CompanyInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<long> CreateAsync(CompanyInvitation invitation, CancellationToken cancellationToken);
    Task UpdateAsync(CompanyInvitation invitation, CancellationToken cancellationToken);
}
