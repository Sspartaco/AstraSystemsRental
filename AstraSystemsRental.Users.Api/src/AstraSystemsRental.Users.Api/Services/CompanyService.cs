using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Persistence;

namespace AstraSystemsRental.Users.Api.Services;

public interface ICompanyService
{
    Task<OperationResult> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken);
    Task<OperationResult> AssignMemberAsync(long companyId, AssignMemberRequest request, CancellationToken cancellationToken);
    Task<OperationResult> RemoveMemberAsync(long companyId, long userId, CancellationToken cancellationToken);
    Task<OperationResult> CreateSubscriptionAsync(long companyId, CreateCompanySubscriptionRequest request, CancellationToken cancellationToken);
}

public sealed class CompanyService(ICompanyRepository repository) : ICompanyService
{
    public async Task<OperationResult> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard()
            .NotEmpty(request.Name, "Name")
            .NotEmpty(request.DocumentNumber, "DocumentNumber");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        if (await repository.DocumentExistsAsync(request.DocumentNumber.Trim(), cancellationToken))
            return OperationResult.Conflict("A company with this document number already exists.");

        var companyId = await repository.CreateAsync(request.Name.Trim(), request.DocumentNumber.Trim(), request.Email?.Trim(), cancellationToken);
        return OperationResult.Created(new { companyId, name = request.Name.Trim() });
    }

    public async Task<OperationResult> AssignMemberAsync(long companyId, AssignMemberRequest request, CancellationToken cancellationToken)
    {
        if (!await repository.CompanyExistsAsync(companyId, cancellationToken))
            return OperationResult.NotFound("Company not found.");

        if (!await repository.UserExistsAsync(request.UserId, cancellationToken))
            return OperationResult.NotFound("User not found.");

        if (await repository.IsMemberAsync(companyId, request.UserId, cancellationToken))
            return OperationResult.Conflict("The user is already a member of this company.");

        await repository.AssignMemberAsync(companyId, request.UserId, request.IsOwner, cancellationToken);
        return OperationResult.Ok(new { companyId, request.UserId, request.IsOwner });
    }

    public async Task<OperationResult> RemoveMemberAsync(long companyId, long userId, CancellationToken cancellationToken)
    {
        var removed = await repository.RemoveMemberAsync(companyId, userId, cancellationToken);
        return removed
            ? OperationResult.Ok(new { companyId, userId, removed = true })
            : OperationResult.NotFound("Membership not found.");
    }

    public async Task<OperationResult> CreateSubscriptionAsync(long companyId, CreateCompanySubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (!await repository.CompanyExistsAsync(companyId, cancellationToken))
            return OperationResult.NotFound("Company not found.");

        var plan = await repository.GetPlanByCodeAsync(request.PlanCode, cancellationToken);
        if (plan is null)
            return OperationResult.Fail("Plan not found or inactive.");

        var startsAt = DateTime.UtcNow;
        var subscriptionId = await repository.CreateSubscriptionAsync(companyId, plan.Id, startsAt, startsAt.AddDays(plan.DurationDays), cancellationToken);
        return OperationResult.Created(new { subscriptionId, companyId, plan = plan.Code });
    }
}
