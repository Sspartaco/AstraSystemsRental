using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Users.Api.Persistence;

namespace AstraSystemsRental.Users.Api.Services;

public interface IUserAdminService
{
    Task<OperationResult> SetActiveAsync(long userId, bool isActive, CancellationToken cancellationToken);
    Task<OperationResult> AssignPlanAsync(long userId, string planCode, int? durationDays, CancellationToken cancellationToken);
}

public sealed class UserAdminService(
    IUserRepository repository,
    IAstraRequestContext requestContext) : IUserAdminService
{
    public async Task<OperationResult> SetActiveAsync(long userId, bool isActive, CancellationToken cancellationToken)
    {
        var guard = new Guard().Must(userId > 0, "A valid user id is required.");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        if (!isActive && userId == requestContext.UserId)
            return OperationResult.Fail("You cannot deactivate your own account.");

        var updated = await repository.SetActiveAsync(userId, isActive, cancellationToken);

        return updated
            ? OperationResult.Ok(new { userId, isActive })
            : OperationResult.NotFound("User not found.");
    }

    public async Task<OperationResult> AssignPlanAsync(long userId, string planCode, int? durationDays, CancellationToken cancellationToken)
    {
        var guard = new Guard()
            .Must(userId > 0, "A valid user id is required.")
            .NotEmpty(planCode, "planCode");

        if (durationDays is { } days)
            guard.Range(days, 1, 3650, "Duration must be between 1 and 3650 days.");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var plan = await repository.GetPlanByCodeAsync(planCode, cancellationToken);

        if (plan is null)
            return OperationResult.NotFound($"Plan '{planCode}' not found or inactive.");

        var endsAtUtc = DateTime.UtcNow.AddDays(durationDays ?? plan.DurationDays);
        var updated = await repository.AssignPlanAsync(userId, plan, endsAtUtc, cancellationToken);

        return updated
            ? OperationResult.Ok(new { userId, planCode = plan.Code, endsAtUtc })
            : OperationResult.NotFound("User not found.");
    }
}
