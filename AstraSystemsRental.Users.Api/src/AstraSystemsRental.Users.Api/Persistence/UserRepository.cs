using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Users.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Users.Api.Persistence;

public sealed class UserRepository(AstraUsersDbContext context)
    : BaseRepository<AstraUsersDbContext, User>(context), IUserRepository
{
    private static readonly Func<AstraUsersDbContext, string, Task<UserCredentials?>> CredentialsQuery =
        EF.CompileAsyncQuery((AstraUsersDbContext db, string email) =>
            db.Users
                .Where(u => u.Email == email)
                .Select(u => new UserCredentials(
                    u.Id,
                    u.Email,
                    u.PasswordHash,
                    u.PasswordSalt,
                    u.IsConfirmed,
                    u.Role!.Code))
                .FirstOrDefault());

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        => await AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken)
        => await DbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Code == code && r.IsActive, cancellationToken);

    public async Task<Plan?> GetPlanByCodeAsync(string code, CancellationToken cancellationToken)
        => await DbContext.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Code == code && p.IsActive, cancellationToken);

    public async Task<long> CreateUserWithConfirmationAsync(
        CreateUserData data, int roleId, Plan plan, string token, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = data.Email,
            RoleId = roleId,
            IsActive = false,
            IsConfirmed = false,
            Person = new Person
            {
                FirstNames = data.FirstNames,
                LastNames = data.LastNames,
                Address = data.Address,
                PersonType = data.PersonType,
                DocumentNumber = data.DocumentNumber,
                CompanySize = data.CompanySize,
                Email = data.Email
            },
            EmailConfirmations =
            [
                new EmailConfirmation { Token = token, ExpiresAtUtc = expiresAtUtc }
            ]
        };

        await AddAsync(user, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        await DbContext.Subscriptions.AddAsync(new Subscription
        {
            OwnerType = SubscriptionOwnerType.User,
            OwnerId = user.Id,
            PlanId = plan.Id,
            StartsAtUtc = now,
            EndsAtUtc = now.AddDays(plan.DurationDays),
            Status = "Active"
        }, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    public async Task<long?> ConfirmAsync(string token, byte[] passwordHash, byte[] passwordSalt, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var confirmation = await DbContext.EmailConfirmations
            .FirstOrDefaultAsync(c => c.Token == token && c.ConsumedAtUtc == null && c.ExpiresAtUtc > nowUtc, cancellationToken);

        if (confirmation is null)
            return null;

        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == confirmation.UserId, cancellationToken);
        if (user is null)
            return null;

        confirmation.ConsumedAtUtc = nowUtc;
        user.IsConfirmed = true;
        user.IsActive = true;
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;

        await SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task<LoginProjection?> GetLoginProjectionAsync(string email, CancellationToken cancellationToken)
    {
        var credentials = await CredentialsQuery(DbContext, email);
        if (credentials is null)
            return null;

        var effective = await DbContext.EffectiveForUser(credentials.UserId)
            .Select(s => new { s.Plan!.Code, s.EndsAtUtc })
            .FirstOrDefaultAsync(cancellationToken);

        return new LoginProjection(
            credentials.UserId,
            credentials.Email,
            credentials.PasswordHash,
            credentials.PasswordSalt,
            credentials.IsConfirmed,
            credentials.RoleCode,
            effective?.Code,
            effective is null ? null : effective.EndsAtUtc);
    }

    public async Task<LoginProjection?> GetLoginProjectionByIdAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await DbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new { u.Id, u.Email, u.IsConfirmed, RoleCode = u.Role!.Code })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        var effective = await DbContext.EffectiveForUser(userId)
            .Select(s => new { s.Plan!.Code, s.EndsAtUtc })
            .FirstOrDefaultAsync(cancellationToken);

        return new LoginProjection(
            user.Id,
            user.Email,
            null,
            null,
            user.IsConfirmed,
            user.RoleCode,
            effective?.Code,
            effective is null ? null : effective.EndsAtUtc);
    }

    public async Task<IReadOnlyList<string>> GetAllowedNodesAsync(long userId, CancellationToken cancellationToken)
    {
        var roleNodes = DbContext.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Role!.RoleNodes.Select(rn => rn.NodeKey));

        var planNodes = DbContext.ActiveForUser(userId)
            .SelectMany(s => s.Plan!.PlanNodes.Select(pn => pn.NodeKey));

        return await roleNodes.Union(planNodes).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<long>> GetMemberCompanyIdsAsync(long userId, CancellationToken cancellationToken)
        => await DbContext.CompanyMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.CompanyId)
            .ToListAsync(cancellationToken);

    public async Task<bool> SuperUserExistsAsync(CancellationToken cancellationToken)
        => await DbContext.Users.AnyAsync(u => u.Role!.Code == RoleCode.SuperUser, cancellationToken);

    public async Task<long> CreateSuperUserAsync(CreateUserData data, byte[] passwordHash, byte[] passwordSalt, CancellationToken cancellationToken)
    {
        var role = await DbContext.Roles.FirstAsync(r => r.Code == RoleCode.SuperUser, cancellationToken);
        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = data.Email,
            RoleId = role.Id,
            IsActive = true,
            IsConfirmed = true,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            CreatedAtUtc = now,
            Person = new Person
            {
                FirstNames = data.FirstNames,
                LastNames = data.LastNames,
                Address = data.Address,
                PersonType = data.PersonType,
                DocumentNumber = data.DocumentNumber,
                CompanySize = data.CompanySize,
                Email = data.Email,
                CreatedAtUtc = now
            }
        };

        await AddAsync(user, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task<bool> SetRoleAsync(long userId, string roleCode, CancellationToken cancellationToken)
    {
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return false;

        var role = await DbContext.Roles.FirstOrDefaultAsync(r => r.Code == roleCode, cancellationToken);
        if (role is null)
            return false;

        user.RoleId = role.Id;
        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetActiveAsync(long userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return false;

        user.IsActive = isActive;
        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AssignPlanAsync(long userId, Plan plan, DateTime endsAtUtc, CancellationToken cancellationToken)
    {
        var exists = await DbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);

        if (!exists)
            return false;

        var current = await DbContext.Subscriptions
            .Where(s => s.OwnerType == SubscriptionOwnerType.User && s.OwnerId == userId && s.Status == "Active")
            .ToListAsync(cancellationToken);

        foreach (var subscription in current)
            subscription.Status = "Cancelled";

        await DbContext.Subscriptions.AddAsync(new Subscription
        {
            OwnerType = SubscriptionOwnerType.User,
            OwnerId = userId,
            PlanId = plan.Id,
            StartsAtUtc = DateTime.UtcNow,
            EndsAtUtc = endsAtUtc,
            Status = "Active"
        }, cancellationToken);

        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserProfile?> GetProfileAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await DbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.IsActive,
                u.IsConfirmed,
                u.CreatedAtUtc,
                FirstNames = u.Person!.FirstNames,
                LastNames = u.Person.LastNames,
                u.Person.Address,
                u.Person.PersonType,
                u.Person.DocumentNumber,
                RoleCode = u.Role!.Code,
                RoleName = u.Role.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        var subscription = await DbContext.EffectiveForUser(userId)
            .AsNoTracking()
            .Select(s => new { s.EndsAtUtc, PlanCode = s.Plan!.Code, PlanName = s.Plan.Name })
            .FirstOrDefaultAsync(cancellationToken);

        var companyCount = await DbContext.CompanyMembers
            .AsNoTracking()
            .CountAsync(m => m.UserId == userId, cancellationToken);

        return new UserProfile(
            user.Id,
            user.Email,
            user.FirstNames,
            user.LastNames,
            user.Address,
            user.PersonType,
            user.DocumentNumber,
            user.RoleCode,
            user.RoleName,
            user.IsActive,
            user.IsConfirmed,
            user.CreatedAtUtc,
            subscription?.PlanCode,
            subscription?.PlanName,
            subscription?.EndsAtUtc,
            companyCount);
    }

    public Task<PagedResult<UserOverview>> GetUsersOverviewAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        => GetPagedAsync(
            pageNumber,
            pageSize,
            projection: query => query.Select(u => new UserOverview(
                u.Id,
                u.Person!.FirstNames,
                u.Person.LastNames,
                u.Email,
                u.Role!.Code,
                u.IsActive,
                u.IsConfirmed,
                DbContext.CompanyMembers.Where(m => m.UserId == u.Id).Select(m => m.Company!.Name).FirstOrDefault(),
                DbContext.Subscriptions
                    .Where(s => s.Status == "Active" &&
                        ((s.OwnerType == SubscriptionOwnerType.User && s.OwnerId == u.Id) ||
                         (s.OwnerType == SubscriptionOwnerType.Company && DbContext.CompanyMembers.Any(m => m.UserId == u.Id && m.CompanyId == s.OwnerId))))
                    .OrderByDescending(s => s.EndsAtUtc)
                    .Select(s => s.Plan!.Code).FirstOrDefault(),
                DbContext.Subscriptions
                    .Where(s => s.Status == "Active" &&
                        ((s.OwnerType == SubscriptionOwnerType.User && s.OwnerId == u.Id) ||
                         (s.OwnerType == SubscriptionOwnerType.Company && DbContext.CompanyMembers.Any(m => m.UserId == u.Id && m.CompanyId == s.OwnerId))))
                    .OrderByDescending(s => s.EndsAtUtc)
                    .Select(s => (DateTime?)s.EndsAtUtc).FirstOrDefault())),
            queryBuilder: query =>
            {
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    query = query.Where(u =>
                        u.Email.Contains(term) ||
                        u.Person!.FirstNames.Contains(term) ||
                        u.Person.LastNames.Contains(term));
                }
                return query.OrderByDescending(u => u.Id);
            },
            cancellationToken: cancellationToken);
}
