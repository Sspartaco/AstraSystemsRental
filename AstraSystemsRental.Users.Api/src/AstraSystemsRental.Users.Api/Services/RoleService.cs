using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Persistence;

namespace AstraSystemsRental.Users.Api.Services;

public interface IRoleService
{
    Task<OperationResult> AssignRoleAsync(long userId, string roleCode, CancellationToken cancellationToken);
}

public sealed class RoleService(IUserRepository repository) : IRoleService
{
    private static readonly HashSet<string> AssignableRoles =
        new(StringComparer.Ordinal) { RoleCode.Standard, RoleCode.Demo, RoleCode.SuperUser, RoleCode.SysAdmin };

    public async Task<OperationResult> AssignRoleAsync(long userId, string roleCode, CancellationToken cancellationToken)
    {
        var guard = new Guard()
            .Must(userId > 0, "A valid user id is required.")
            .Must(AssignableRoles.Contains(roleCode), $"Role must be one of: {string.Join(", ", AssignableRoles)}.");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var updated = await repository.SetRoleAsync(userId, roleCode, cancellationToken);
        if (!updated)
            return OperationResult.NotFound("User not found.");

        return OperationResult.Ok(new { userId, role = roleCode });
    }
}
