using System.Text.Json;
using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared.Security;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.Companies;

public sealed record CompanyVm(long CompanyId, string CompanyName, bool IsOwner);
public sealed record CompanyMemberVm(long UserId, string Email, string FullName, bool IsOwner, DateTime JoinedAtUtc);
public sealed record CompanyDetailVm(long CompanyId, IReadOnlyList<CompanyMemberVm> Members, long CurrentUserId, string? Error, string? InviteToken);

public sealed record CreateCompanyForm(string Name, string DocumentNumber, string? Email);
public sealed record InviteMemberForm(string Email);

public sealed class CompaniesController(IGatewayClient gateway, ISessionService session, ICurrentUser currentUser) : Controller
{
    [HttpGet("/companies")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await LoadCompanies(cancellationToken));
    }

    [HttpPost("/companies")]
    public async Task<IActionResult> Create([FromForm] CreateCompanyForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        await gateway.SendForDataAsync(HttpMethod.Post, "/apiUsers/companies/self", token,
            new { name = form.Name, documentNumber = form.DocumentNumber, email = form.Email }, null, cancellationToken);

        return PartialView("_CompaniesList", await LoadCompanies(cancellationToken));
    }

    [HttpGet("/companies/{companyId:long}")]
    public async Task<IActionResult> Detail(long companyId, CancellationToken cancellationToken)
    {
        return View(await LoadDetail(companyId, null, null, cancellationToken));
    }

    [HttpPost("/companies/{companyId:long}/invitations")]
    public async Task<IActionResult> Invite(long companyId, [FromForm] InviteMemberForm form, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var response = await gateway.SendForDataAsync(HttpMethod.Post, $"/apiUsers/companies/self/{companyId}/invitations", token,
            new { email = form.Email }, null, cancellationToken);

        string? inviteToken = null;
        if (response.IsSuccess && response.Data is { } data && data.TryGetProperty("token", out var t))
            inviteToken = t.GetString();

        return PartialView("_CompanyDetail", await LoadDetail(companyId, response.Errors.FirstOrDefault(), inviteToken, cancellationToken));
    }

    [HttpPost("/companies/{companyId:long}/members/{userId:long}/remove")]
    public async Task<IActionResult> RemoveMember(long companyId, long userId, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return Redirect("/auth/login");

        var response = await gateway.SendForDataAsync(HttpMethod.Delete, $"/apiUsers/companies/self/{companyId}/members/{userId}", token, null, null, cancellationToken);

        return PartialView("_CompanyDetail", await LoadDetail(companyId, response.IsSuccess ? null : response.Errors.FirstOrDefault(), null, cancellationToken));
    }

    private async Task<IReadOnlyList<CompanyVm>> LoadCompanies(CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        if (string.IsNullOrEmpty(token))
            return [];

        var data = await gateway.GetAsync("/apiUsers/companies/self", token, cancellationToken);
        var companies = new List<CompanyVm>();
        if (data is { ValueKind: JsonValueKind.Array } arr)
        {
            foreach (var item in arr.EnumerateArray())
            {
                companies.Add(new CompanyVm(
                    item.GetProperty("companyId").GetInt64(),
                    item.GetProperty("companyName").GetString() ?? string.Empty,
                    item.GetProperty("isOwner").GetBoolean()));
            }
        }

        return companies;
    }

    private async Task<CompanyDetailVm> LoadDetail(long companyId, string? error, string? inviteToken, CancellationToken cancellationToken)
    {
        var token = session.GetToken();
        var currentUserId = long.TryParse(currentUser.Principal.UserId, out var id) ? id : 0;

        if (string.IsNullOrEmpty(token))
            return new CompanyDetailVm(companyId, [], currentUserId, error, inviteToken);

        var data = await gateway.GetAsync($"/apiUsers/companies/self/{companyId}/members", token, cancellationToken);
        var members = new List<CompanyMemberVm>();
        if (data is { ValueKind: JsonValueKind.Array } arr)
        {
            foreach (var item in arr.EnumerateArray())
            {
                members.Add(new CompanyMemberVm(
                    item.GetProperty("userId").GetInt64(),
                    item.GetProperty("email").GetString() ?? string.Empty,
                    item.GetProperty("fullName").GetString() ?? string.Empty,
                    item.GetProperty("isOwner").GetBoolean(),
                    item.GetProperty("joinedAtUtc").GetDateTime()));
            }
        }

        return new CompanyDetailVm(companyId, members, currentUserId, error, inviteToken);
    }
}
