using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Users.Api.Endpoints;

public static class CompanySelfEndpoints
{
    public static void CompanySelfEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/companies/self").WithTags("CompaniesSelf").RequireAuthorization();

        group.MapPost("/", async (
                [FromBody] CreateCompanyRequest request,
                [FromServices] ICompanySelfService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.CreateAsync(request, cancellationToken)).ToResult(context))
            .WithName("CreateOwnCompany").WithSummary("Creates a company owned by the caller")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        group.MapGet("/", async (
                [FromServices] ICompanySelfService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.GetMyCompaniesAsync(cancellationToken)).ToResult(context))
            .WithName("GetMyCompanies").WithSummary("Lists companies where the caller is a member")
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        group.MapGet("/{companyId:long}/members", async (
                long companyId,
                [FromServices] ICompanySelfService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.GetMembersAsync(companyId, cancellationToken)).ToResult(context))
            .WithName("GetOwnCompanyMembers").WithSummary("Lists members of a company the caller belongs to")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{companyId:long}/invitations", async (
                long companyId,
                [FromBody] InviteCompanyMemberRequest request,
                [FromServices] ICompanySelfService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.InviteAsync(companyId, request, cancellationToken)).ToResult(context))
            .WithName("InviteCompanyMember").WithSummary("Invites a user by email (owner only)")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{companyId:long}/invitations/{invitationId:long}/revoke", async (
                long companyId,
                long invitationId,
                [FromServices] ICompanySelfService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.RevokeInvitationAsync(companyId, invitationId, cancellationToken)).ToResult(context))
            .WithName("RevokeCompanyInvitation").WithSummary("Revokes a pending invitation (owner only)")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapDelete("/{companyId:long}/members/{userId:long}", async (
                long companyId,
                long userId,
                [FromServices] ICompanySelfService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.RemoveMemberAsync(companyId, userId, cancellationToken)).ToResult(context))
            .WithName("RemoveOwnCompanyMember").WithSummary("Removes a member (owner only)")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{companyId:long}/owner-transfer/{newOwnerUserId:long}", async (
                long companyId,
                long newOwnerUserId,
                [FromServices] ICompanySelfService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.TransferOwnershipAsync(companyId, newOwnerUserId, cancellationToken)).ToResult(context))
            .WithName("TransferCompanyOwnership").WithSummary("Transfers company ownership to another member (current owner only)")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        app.MapGet("/companies/{companyId:long}/members/{userId:long}/is-active", async (
                long companyId,
                long userId,
                [FromServices] ICompanySelfService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.CheckActiveMembershipAsync(companyId, userId, cancellationToken)).ToResult(context))
            .WithTags("CompaniesSelf")
            .WithName("CheckActiveCompanyMembership").WithSummary("Internal check used to revalidate membership on sensitive writes")
            .RequireAuthorization()
            .Produces<ApiResponse>(StatusCodes.Status200OK);

        app.MapPost("/companies/invitations/{token}/accept", async (
                string token,
                [FromServices] ICompanySelfService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
                (await service.AcceptInvitationAsync(token, cancellationToken)).ToResult(context))
            .WithTags("CompaniesSelf")
            .WithName("AcceptCompanyInvitation").WithSummary("Accepts a company invitation using its token")
            .RequireAuthorization()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest);
    }
}
