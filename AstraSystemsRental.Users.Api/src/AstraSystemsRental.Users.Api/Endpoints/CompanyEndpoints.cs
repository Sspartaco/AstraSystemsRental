using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Http;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Security;
using AstraSystemsRental.Users.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Users.Api.Endpoints;

public static class CompanyEndpoints
{
    public static void CompanyEndpoints_Map(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/companies").WithTags("Companies").RequireAuthorization(AstraPolicies.SuperUser);

        group.MapPost("/", async (
                [FromBody] CreateCompanyRequest request,
                [FromServices] ICompanyService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateAsync(request, cancellationToken);
                return result.ToResult(context);
            })
            .WithName("CreateCompany")
            .WithSummary("Creates a company")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        group.MapPost("/{companyId:long}/members", async (
                long companyId,
                [FromBody] AssignMemberRequest request,
                [FromServices] ICompanyService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.AssignMemberAsync(companyId, request, cancellationToken);
                return result.ToResult(context);
            })
            .WithName("AssignCompanyMember")
            .WithSummary("Assigns a user to a company")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict);

        group.MapDelete("/{companyId:long}/members/{userId:long}", async (
                long companyId,
                long userId,
                [FromServices] ICompanyService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.RemoveMemberAsync(companyId, userId, cancellationToken);
                return result.ToResult(context);
            })
            .WithName("RemoveCompanyMember")
            .WithSummary("Removes a user from a company")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/{companyId:long}/subscription", async (
                long companyId,
                [FromBody] CreateCompanySubscriptionRequest request,
                [FromServices] ICompanyService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateSubscriptionAsync(companyId, request, cancellationToken);
                return result.ToResult(context);
            })
            .WithName("CreateCompanySubscription")
            .WithSummary("Creates a subscription owned by a company")
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);
    }
}
