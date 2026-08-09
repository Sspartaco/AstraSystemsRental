namespace AstraSystemsRental.Users.Api.Dtos;

public sealed record CreateUserRequest(
    string FirstNames,
    string LastNames,
    string? Address,
    string PersonType,
    string DocumentNumber,
    string? CompanySize,
    string Email);

public sealed record ConfirmEmailRequest(string Token, string Password);

public sealed record LoginRequest(string Email, string Password, string? DeviceInfo = null);

public sealed record RefreshTokenRequest(string? RefreshToken, string? DeviceInfo = null);

public sealed record AssignRoleRequest(string RoleCode);

public sealed record CreateCompanyRequest(string Name, string DocumentNumber, string? Email);

public sealed record AssignMemberRequest(long UserId, bool IsOwner);

public sealed record CreateCompanySubscriptionRequest(string PlanCode);

public sealed record CreateNodeRequest(string Key, string Label, string? Icon, string? Route, int SortOrder);

public sealed record CreatePlanRequest(string Code, string Name, int DurationDays);

public sealed record UpdatePlanRequest(string Name, int DurationDays, bool IsActive);

public sealed record SetNodeRequest(string NodeKey, bool Assign);

public sealed record InviteCompanyMemberRequest(string Email);

public sealed record SetActiveRequest(bool IsActive);

public sealed record AssignPlanRequest(string PlanCode, int? DurationDays);
