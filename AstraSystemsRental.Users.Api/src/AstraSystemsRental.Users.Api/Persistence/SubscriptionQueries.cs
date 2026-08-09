using AstraSystemsRental.Users.Api.Domain;

namespace AstraSystemsRental.Users.Api.Persistence;

public static class SubscriptionQueries
{
    public static IQueryable<Subscription> ActiveForUser(this AstraUsersDbContext db, long userId) =>
        db.Subscriptions.Where(s =>
            s.Status == "Active" &&
            ((s.OwnerType == SubscriptionOwnerType.User && s.OwnerId == userId) ||
             (s.OwnerType == SubscriptionOwnerType.Company &&
              db.CompanyMembers.Any(m => m.UserId == userId && m.CompanyId == s.OwnerId))));

    public static IQueryable<Subscription> EffectiveForUser(this AstraUsersDbContext db, long userId) =>
        db.ActiveForUser(userId).OrderByDescending(s => s.EndsAtUtc);

    public static IQueryable<Subscription> ActiveForOwner(this AstraUsersDbContext db, string ownerType, long ownerId) =>
        db.Subscriptions.Where(s => s.Status == "Active" && s.OwnerType == ownerType && s.OwnerId == ownerId);

    public static IQueryable<Subscription> EffectiveForOwner(this AstraUsersDbContext db, string ownerType, long ownerId) =>
        db.ActiveForOwner(ownerType, ownerId).OrderByDescending(s => s.EndsAtUtc);
}
