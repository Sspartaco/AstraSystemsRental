using AstraSystemsRental.Users.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Users.Api.Persistence;

public sealed class CompanyInvitationRepository(AstraUsersDbContext context) : ICompanyInvitationRepository
{
    public Task<CompanyInvitation?> GetPendingByCompanyAndEmailAsync(long companyId, string email, CancellationToken cancellationToken)
        => context.CompanyInvitations.FirstOrDefaultAsync(
            i => i.CompanyId == companyId && i.Email == email && i.Status == CompanyInvitationStatus.Pending,
            cancellationToken);

    public Task<CompanyInvitation?> GetByIdAsync(long invitationId, CancellationToken cancellationToken)
        => context.CompanyInvitations.FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

    public Task<CompanyInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        => context.CompanyInvitations.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

    public async Task<long> CreateAsync(CompanyInvitation invitation, CancellationToken cancellationToken)
    {
        context.CompanyInvitations.Add(invitation);
        await context.SaveChangesAsync(cancellationToken);
        return invitation.Id;
    }

    public async Task UpdateAsync(CompanyInvitation invitation, CancellationToken cancellationToken)
        => await context.SaveChangesAsync(cancellationToken);
}
