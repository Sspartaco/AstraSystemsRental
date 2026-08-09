using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.Profile;

public sealed record ProfileDto
{
    public long UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstNames { get; init; } = string.Empty;
    public string LastNames { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string PersonType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public string RoleCode { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsConfirmed { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string? PlanCode { get; init; }
    public string? PlanName { get; init; }
    public DateTime? SubscriptionEndsAtUtc { get; init; }
    public int CompanyCount { get; init; }

    public string FullName => $"{FirstNames} {LastNames}".Trim();
}

public sealed record ProfileIndexVm
{
    public required AstraPrincipal Principal { get; init; }
    public ProfileDto? Profile { get; init; }
    public IReadOnlyList<CatalogNode> Nodes { get; init; } = [];
    public string? Error { get; init; }

    public int? DaysToExpiry => Profile?.SubscriptionEndsAtUtc is { } end
        ? (int)Math.Ceiling((end - DateTime.UtcNow).TotalDays)
        : null;
}

public sealed class ProfileController(
    ICurrentUser currentUser,
    ISessionService session,
    IGatewayClient gateway,
    INodeCatalogService catalog) : Controller
{
    [HttpGet("/profile")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return Redirect("/auth/login");

        var token = session.GetToken();

        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var profile = await gateway.GetTypedAsync<ProfileDto>(
            "/apiUsers/users/me", token, null, null, cancellationToken);

        var allNodes = await catalog.GetAsync(token, cancellationToken);

        var visible = allNodes
            .Where(n => n.DiagnosticsOnly ? currentUser.Principal.CanSeeDiagnostics : n.SuperUserOnly ? currentUser.Principal.IsSuperUser : currentUser.Principal.HasNode(n.Key))
            .ToList();

        return View(new ProfileIndexVm
        {
            Principal = currentUser.Principal,
            Profile = profile,
            Nodes = visible,
            Error = profile is null ? "No se pudo cargar tu información en este momento." : null
        });
    }
}
