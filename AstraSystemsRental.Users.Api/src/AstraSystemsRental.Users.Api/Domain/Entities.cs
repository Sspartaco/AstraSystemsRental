namespace AstraSystemsRental.Users.Api.Domain;

public class Person
{
    public long Id { get; set; }
    public string FirstNames { get; set; } = string.Empty;
    public string LastNames { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string PersonType { get; set; } = Domain.PersonType.Natural;
    public string DocumentNumber { get; set; } = string.Empty;
    public string? CompanySize { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public User? User { get; set; }
}

public class User
{
    public long Id { get; set; }
    public long PersonId { get; set; }
    public string Email { get; set; } = string.Empty;
    public byte[]? PasswordHash { get; set; }
    public byte[]? PasswordSalt { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Person? Person { get; set; }
    public Role? Role { get; set; }
    public ICollection<EmailConfirmation> EmailConfirmations { get; set; } = [];
    public ICollection<CompanyMember> CompanyMemberships { get; set; } = [];
}

public class EmailConfirmation
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public User? User { get; set; }
}

public class Role
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<RoleNode> RoleNodes { get; set; } = [];
}

public class RoleNode
{
    public int RoleId { get; set; }
    public string NodeKey { get; set; } = string.Empty;
    public Role? Role { get; set; }
}

public class Node
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}

public class Plan
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<PlanNode> PlanNodes { get; set; } = [];
}

public class PlanNode
{
    public int PlanId { get; set; }
    public string NodeKey { get; set; } = string.Empty;
    public Plan? Plan { get; set; }
}

public class PlanNodeQuota
{
    public int PlanId { get; set; }
    public string NodeKey { get; set; } = string.Empty;
    public int MaxCount { get; set; }
}

public class PlanNodeUsageCounter
{
    public string OwnerType { get; set; } = SubscriptionOwnerType.User;
    public long OwnerId { get; set; }
    public string NodeKey { get; set; } = string.Empty;
    public int CurrentCount { get; set; }
}

public static class SubscriptionOwnerType
{
    public const string User = "User";
    public const string Company = "Company";
}

public class Subscription
{
    public long Id { get; set; }
    public string OwnerType { get; set; } = SubscriptionOwnerType.User;
    public long OwnerId { get; set; }
    public int PlanId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAtUtc { get; set; }
    public Plan? Plan { get; set; }
}

public class Company
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<CompanyMember> Members { get; set; } = [];
}

public class CompanyMember
{
    public long CompanyId { get; set; }
    public long UserId { get; set; }
    public bool IsOwner { get; set; }
    public DateTime JoinedAtUtc { get; set; }
    public Company? Company { get; set; }
    public User? User { get; set; }
}

public static class CompanyInvitationStatus
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string Revoked = "Revoked";
    public const string Expired = "Expired";
}

public class CompanyInvitation
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public long InvitedByUserId { get; set; }
    public string Status { get; set; } = CompanyInvitationStatus.Pending;
    public DateTime ExpiresAtUtc { get; set; }
    public int SentCount { get; set; } = 1;
    public DateTime LastSentAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public Company? Company { get; set; }
}

public class RefreshToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public byte[]? ReplacedByTokenHash { get; set; }
    public string? DeviceInfo { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public User? User { get; set; }

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;
}
