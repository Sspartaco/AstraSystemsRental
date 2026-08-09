using System.Text.Json;
using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.Admin;

public sealed record UserRow(
    long UserId, string FullName, string Email, string Role,
    bool IsActive, bool IsConfirmed, string? Company, string? Plan, DateTime? SubscriptionEndsAtUtc);

public sealed record UsersPage(IReadOnlyList<UserRow> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, string? Search);

public sealed record PlanRow(int Id, string Code, string Name, int DurationDays, bool IsActive, string[] Nodes);

public sealed record CatalogViewModel(IReadOnlyList<PlanRow> Plans, IReadOnlyList<CatalogNode> Nodes);

public sealed record AssignRoleForm(long UserId, string RoleCode);
public sealed record SetActiveForm(long UserId, bool IsActive);
public sealed record AssignPlanForm(long UserId, string PlanCode);

public sealed record CreateUserForm
{
    public string FirstNames { get; init; } = string.Empty;
    public string LastNames { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string PersonType { get; init; } = "Natural";
    public string DocumentNumber { get; init; } = string.Empty;
    public string? CompanySize { get; init; }
    public string Email { get; init; } = string.Empty;
}
public sealed record SetPlanNodeForm(int PlanId, string NodeKey, bool Assign);

public sealed class AdminController(IGatewayClient gateway, ISessionService session, ICurrentUser currentUser) : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("/admin/users")]
    public async Task<IActionResult> Users(int page = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default)
    {
        if (!currentUser.Principal.IsSuperUser)
            return Redirect("/");

        var vm = await LoadUsers(page, pageSize, search, cancellationToken);

        return Request.Headers.ContainsKey("HX-Request")
            ? PartialView("_UsersTable", vm)
            : View(vm);
    }

    [HttpPost("/admin/users/active")]
    public async Task<IActionResult> SetActive([FromForm] SetActiveForm form, int page = 1, string? search = null, CancellationToken cancellationToken = default)
    {
        if (!currentUser.Principal.IsSuperUser)
            return Redirect("/");

        var token = session.GetToken();
        if (!string.IsNullOrEmpty(token))
            await gateway.SendAsync(HttpMethod.Post, $"/apiUsers/users/{form.UserId}/active", token, new { isActive = form.IsActive }, cancellationToken);

        return PartialView("_UsersTable", await LoadUsers(page, 10, search, cancellationToken));
    }

    [HttpPost("/admin/users/plan")]
    public async Task<IActionResult> AssignPlan([FromForm] AssignPlanForm form, int page = 1, string? search = null, CancellationToken cancellationToken = default)
    {
        if (!currentUser.Principal.IsSuperUser)
            return Redirect("/");

        var token = session.GetToken();
        if (!string.IsNullOrEmpty(token))
            await gateway.SendAsync(HttpMethod.Post, $"/apiUsers/users/{form.UserId}/plan", token, new { planCode = form.PlanCode }, cancellationToken);

        return PartialView("_UsersTable", await LoadUsers(page, 10, search, cancellationToken));
    }

    [HttpPost("/admin/users/create")]
    public async Task<IActionResult> CreateUser([FromForm] CreateUserForm form, CancellationToken cancellationToken = default)
    {
        if (!currentUser.Principal.IsSuperUser)
            return Redirect("/");

        var token = session.GetToken();

        if (!string.IsNullOrEmpty(token))
        {
            var response = await gateway.SendForDataAsync(
                HttpMethod.Post, "/apiUsers/users", token,
                new
                {
                    firstNames = form.FirstNames,
                    lastNames = form.LastNames,
                    address = form.Address,
                    personType = form.PersonType,
                    documentNumber = form.DocumentNumber,
                    companySize = form.CompanySize,
                    email = form.Email
                },
                null, cancellationToken);

            TempData[response.IsSuccess ? "UserCreated" : "UserError"] = response.IsSuccess
                ? $"Usuario {form.Email} creado. Se envió el correo de confirmación."
                : response.Errors.FirstOrDefault() ?? "No se pudo crear el usuario.";
        }

        return Redirect("/admin/users");
    }

    [HttpPost("/admin/users/role")]
    public async Task<IActionResult> AssignRole([FromForm] AssignRoleForm form, int page = 1, string? search = null, CancellationToken cancellationToken = default)
    {
        if (!currentUser.Principal.IsSuperUser)
            return Redirect("/");

        var token = session.GetToken();
        if (!string.IsNullOrEmpty(token))
            await gateway.SendAsync(HttpMethod.Post, $"/apiUsers/users/{form.UserId}/role", token, new { roleCode = form.RoleCode }, cancellationToken);

        return PartialView("_UsersTable", await LoadUsers(page, 10, search, cancellationToken));
    }

    [HttpGet("/admin/catalog")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken = default)
    {
        if (!currentUser.Principal.IsSuperUser)
            return Redirect("/");

        return View(await LoadCatalog(cancellationToken));
    }

    [HttpPost("/admin/catalog/plan-node")]
    public async Task<IActionResult> SetPlanNode([FromForm] SetPlanNodeForm form, CancellationToken cancellationToken = default)
    {
        if (!currentUser.Principal.IsSuperUser)
            return Redirect("/");

        var token = session.GetToken();
        if (!string.IsNullOrEmpty(token))
            await gateway.SendAsync(HttpMethod.Post, $"/apiUsers/plans/{form.PlanId}/nodes", token, new { nodeKey = form.NodeKey, assign = form.Assign }, cancellationToken);

        return PartialView("_PlansGrid", await LoadCatalog(cancellationToken));
    }

    private async Task<UsersPage> LoadUsers(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return new UsersPage([], 1, pageSize, 0, 0, search);

        var query = $"/apiUsers/users/overview?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        var data = await gateway.GetAsync(query, token, cancellationToken);
        if (data is null)
            return new UsersPage([], 1, pageSize, 0, 0, search);

        var d = data.Value;
        var rows = new List<UserRow>();
        if (d.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                rows.Add(new UserRow(
                    item.GetProperty("userId").GetInt64(),
                    $"{item.GetProperty("firstNames").GetString()} {item.GetProperty("lastNames").GetString()}".Trim(),
                    item.GetProperty("email").GetString() ?? string.Empty,
                    item.GetProperty("roleCode").GetString() ?? string.Empty,
                    item.GetProperty("isActive").GetBoolean(),
                    item.GetProperty("isConfirmed").GetBoolean(),
                    Nullable(item, "companyName"),
                    Nullable(item, "planCode"),
                    item.TryGetProperty("subscriptionEndsAtUtc", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetDateTime() : null));
            }
        }

        return new UsersPage(
            rows,
            d.TryGetProperty("pageNumber", out var pn) ? pn.GetInt32() : 1,
            d.TryGetProperty("pageSize", out var ps) ? ps.GetInt32() : pageSize,
            d.TryGetProperty("totalCount", out var tc) ? tc.GetInt32() : rows.Count,
            d.TryGetProperty("totalPages", out var tp) ? tp.GetInt32() : 1,
            search);
    }

    private async Task<CatalogViewModel> LoadCatalog(CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return new CatalogViewModel([], []);

        var plans = new List<PlanRow>();
        var plansData = await gateway.GetAsync("/apiUsers/plans", token, cancellationToken);
        if (plansData is { ValueKind: JsonValueKind.Array })
        {
            foreach (var p in plansData.Value.EnumerateArray())
            {
                var nodes = p.TryGetProperty("nodes", out var n) && n.ValueKind == JsonValueKind.Array
                    ? n.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray()
                    : [];
                plans.Add(new PlanRow(
                    p.GetProperty("id").GetInt32(),
                    p.GetProperty("code").GetString() ?? string.Empty,
                    p.GetProperty("name").GetString() ?? string.Empty,
                    p.GetProperty("durationDays").GetInt32(),
                    p.GetProperty("isActive").GetBoolean(),
                    nodes));
            }
        }

        var nodesCatalog = new List<CatalogNode>();
        var nodesData = await gateway.GetAsync("/apiUsers/nodes", token, cancellationToken);
        if (nodesData is { ValueKind: JsonValueKind.Array })
        {
            foreach (var item in nodesData.Value.EnumerateArray())
            {
                nodesCatalog.Add(new CatalogNode(
                    item.GetProperty("key").GetString() ?? string.Empty,
                    item.GetProperty("label").GetString() ?? string.Empty,
                    item.GetProperty("icon").GetString() ?? string.Empty,
                    item.GetProperty("route").GetString() ?? string.Empty,
                    false));
            }
        }

        return new CatalogViewModel(plans, nodesCatalog);
    }

    private static string? Nullable(JsonElement item, string prop)
        => item.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null;
}
