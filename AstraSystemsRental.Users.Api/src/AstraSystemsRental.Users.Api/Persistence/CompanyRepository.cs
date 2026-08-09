using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Users.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Users.Api.Persistence;

public sealed class CompanyRepository(AstraUsersDbContext context)
    : BaseRepository<AstraUsersDbContext, Company>(context), ICompanyRepository
{
    public async Task<bool> DocumentExistsAsync(string documentNumber, CancellationToken cancellationToken)
        => await AnyAsync(c => c.DocumentNumber == documentNumber, cancellationToken);

    public async Task<long> CreateAsync(string name, string documentNumber, string? email, CancellationToken cancellationToken)
    {
        var company = new Company { Name = name, DocumentNumber = documentNumber, Email = email, IsActive = true };
        await AddAsync(company, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return company.Id;
    }

    public async Task<Company?> GetByIdWithMembersAsync(long companyId, CancellationToken cancellationToken)
        => await GetEntityWithIncludesAsync(c => c.Id == companyId, c => c.Members);

    public async Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken)
        => await DbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);

    public async Task<bool> CompanyExistsAsync(long companyId, CancellationToken cancellationToken)
        => await AnyAsync(c => c.Id == companyId, cancellationToken);

    public async Task<bool> IsMemberAsync(long companyId, long userId, CancellationToken cancellationToken)
        => await DbContext.CompanyMembers.AnyAsync(m => m.CompanyId == companyId && m.UserId == userId, cancellationToken);

    public async Task AssignMemberAsync(long companyId, long userId, bool isOwner, CancellationToken cancellationToken)
    {
        await DbContext.CompanyMembers.AddAsync(new CompanyMember
        {
            CompanyId = companyId,
            UserId = userId,
            IsOwner = isOwner
        }, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveMemberAsync(long companyId, long userId, CancellationToken cancellationToken)
    {
        var member = await DbContext.CompanyMembers.FirstOrDefaultAsync(m => m.CompanyId == companyId && m.UserId == userId, cancellationToken);
        if (member is null)
            return false;

        DbContext.CompanyMembers.Remove(member);
        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Plan?> GetPlanByCodeAsync(string code, CancellationToken cancellationToken)
        => await DbContext.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Code == code && p.IsActive, cancellationToken);

    public async Task<long> CreateSubscriptionAsync(long companyId, int planId, DateTime startsAtUtc, DateTime endsAtUtc, CancellationToken cancellationToken)
    {
        var subscription = new Subscription
        {
            OwnerType = SubscriptionOwnerType.Company,
            OwnerId = companyId,
            PlanId = planId,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            Status = "Active"
        };
        await DbContext.Subscriptions.AddAsync(subscription, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return subscription.Id;
    }

    public async Task<IReadOnlyList<CompanyMembershipRow>> GetCompaniesForUserAsync(long userId, CancellationToken cancellationToken)
        => await DbContext.CompanyMembers
            .Where(m => m.UserId == userId)
            .Select(m => new CompanyMembershipRow(m.CompanyId, m.Company!.Name, m.IsOwner))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CompanyMemberRow>> GetMembersAsync(long companyId, CancellationToken cancellationToken)
        => await DbContext.CompanyMembers
            .Where(m => m.CompanyId == companyId)
            .Select(m => new CompanyMemberRow(
                m.UserId,
                m.User!.Email,
                m.User!.Person!.FirstNames + " " + m.User!.Person!.LastNames,
                m.IsOwner,
                m.JoinedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<bool> IsOwnerAsync(long companyId, long userId, CancellationToken cancellationToken)
        => await DbContext.CompanyMembers.AnyAsync(m => m.CompanyId == companyId && m.UserId == userId && m.IsOwner, cancellationToken);

    public async Task<int> CountOwnersAsync(long companyId, CancellationToken cancellationToken)
        => await DbContext.CompanyMembers.CountAsync(m => m.CompanyId == companyId && m.IsOwner, cancellationToken);

    public async Task<bool> TransferOwnershipAsync(long companyId, long currentOwnerUserId, long newOwnerUserId, CancellationToken cancellationToken)
    {
        var newOwner = await DbContext.CompanyMembers.FirstOrDefaultAsync(m => m.CompanyId == companyId && m.UserId == newOwnerUserId, cancellationToken);
        if (newOwner is null)
            return false;

        var currentOwner = await DbContext.CompanyMembers.FirstOrDefaultAsync(m => m.CompanyId == companyId && m.UserId == currentOwnerUserId, cancellationToken);
        if (currentOwner is null)
            return false;

        newOwner.IsOwner = true;
        currentOwner.IsOwner = false;
        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<long?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken)
        => await DbContext.Users.Where(u => u.Email == email).Select(u => (long?)u.Id).FirstOrDefaultAsync(cancellationToken);
}
