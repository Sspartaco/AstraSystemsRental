using System.Security.Cryptography;
using System.Text;
using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Dtos;
using AstraSystemsRental.Users.Api.Persistence;

namespace AstraSystemsRental.Users.Api.Services;

public sealed class CompanySelfService(
    ICompanyRepository companyRepository,
    ICompanyInvitationRepository invitationRepository,
    IAstraRequestContext requestContext) : ICompanySelfService
{
    private static readonly TimeSpan InvitationTtl = TimeSpan.FromDays(7);

    public async Task<OperationResult> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard()
            .NotEmpty(request.Name, "Name")
            .NotEmpty(request.DocumentNumber, "DocumentNumber");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        if (await companyRepository.DocumentExistsAsync(request.DocumentNumber.Trim(), cancellationToken))
            return OperationResult.Conflict("A company with this document number already exists.");

        var companyId = await companyRepository.CreateAsync(request.Name.Trim(), request.DocumentNumber.Trim(), request.Email?.Trim(), cancellationToken);
        await companyRepository.AssignMemberAsync(companyId, requestContext.UserId, isOwner: true, cancellationToken);

        return OperationResult.Created(new { companyId, name = request.Name.Trim() });
    }

    public async Task<OperationResult> GetMyCompaniesAsync(CancellationToken cancellationToken)
    {
        var companies = await companyRepository.GetCompaniesForUserAsync(requestContext.UserId, cancellationToken);
        return OperationResult.Ok(companies);
    }

    public async Task<OperationResult> GetMembersAsync(long companyId, CancellationToken cancellationToken)
    {
        if (!await companyRepository.IsMemberAsync(companyId, requestContext.UserId, cancellationToken))
            return OperationResult.NotFound("Company not found.");

        var members = await companyRepository.GetMembersAsync(companyId, cancellationToken);
        return OperationResult.Ok(members);
    }

    public async Task<OperationResult> InviteAsync(long companyId, InviteCompanyMemberRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard().Email(request.Email, "Email");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        if (!await companyRepository.IsOwnerAsync(companyId, requestContext.UserId, cancellationToken))
            return OperationResult.NotFound("Company not found.");

        var email = request.Email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var existing = await invitationRepository.GetPendingByCompanyAndEmailAsync(companyId, email, cancellationToken);
        var (token, tokenHash) = GenerateToken();

        if (existing is not null)
        {
            existing.TokenHash = tokenHash;
            existing.SentCount += 1;
            existing.LastSentAtUtc = now;
            existing.ExpiresAtUtc = now.Add(InvitationTtl);
            await invitationRepository.UpdateAsync(existing, cancellationToken);

            return OperationResult.Ok(new { invitationId = existing.Id, token });
        }

        var invitation = new CompanyInvitation
        {
            CompanyId = companyId,
            Email = email,
            TokenHash = tokenHash,
            InvitedByUserId = requestContext.UserId,
            Status = CompanyInvitationStatus.Pending,
            ExpiresAtUtc = now.Add(InvitationTtl),
            SentCount = 1,
            LastSentAtUtc = now,
            CreatedAtUtc = now
        };

        var invitationId = await invitationRepository.CreateAsync(invitation, cancellationToken);
        return OperationResult.Created(new { invitationId, token });
    }

    public async Task<OperationResult> RevokeInvitationAsync(long companyId, long invitationId, CancellationToken cancellationToken)
    {
        if (!await companyRepository.IsOwnerAsync(companyId, requestContext.UserId, cancellationToken))
            return OperationResult.NotFound("Company not found.");

        var invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation is null || invitation.CompanyId != companyId || invitation.Status != CompanyInvitationStatus.Pending)
            return OperationResult.NotFound("Invitation not found.");

        invitation.Status = CompanyInvitationStatus.Revoked;
        invitation.RespondedAtUtc = DateTime.UtcNow;
        await invitationRepository.UpdateAsync(invitation, cancellationToken);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> AcceptInvitationAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);
        var invitation = await invitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (invitation is null || invitation.Status != CompanyInvitationStatus.Pending)
            return OperationResult.Fail("Invalid or expired invitation.");

        if (invitation.ExpiresAtUtc <= DateTime.UtcNow)
        {
            invitation.Status = CompanyInvitationStatus.Expired;
            await invitationRepository.UpdateAsync(invitation, cancellationToken);
            return OperationResult.Fail("Invalid or expired invitation.");
        }

        var invitedUserId = await companyRepository.FindUserIdByEmailAsync(invitation.Email, cancellationToken);
        if (invitedUserId is null)
            return OperationResult.Fail("No account exists for the invited email yet.");

        if (invitedUserId.Value != requestContext.UserId)
            return OperationResult.Forbidden("This invitation was not issued for the current account.");

        if (await companyRepository.IsMemberAsync(invitation.CompanyId, requestContext.UserId, cancellationToken))
            return OperationResult.Conflict("Already a member of this company.");

        await companyRepository.AssignMemberAsync(invitation.CompanyId, requestContext.UserId, isOwner: false, cancellationToken);

        invitation.Status = CompanyInvitationStatus.Accepted;
        invitation.RespondedAtUtc = DateTime.UtcNow;
        await invitationRepository.UpdateAsync(invitation, cancellationToken);

        return OperationResult.Ok(new { companyId = invitation.CompanyId });
    }

    public async Task<OperationResult> RemoveMemberAsync(long companyId, long userId, CancellationToken cancellationToken)
    {
        if (!await companyRepository.IsOwnerAsync(companyId, requestContext.UserId, cancellationToken))
            return OperationResult.NotFound("Company not found.");

        var isTargetOwner = await companyRepository.IsOwnerAsync(companyId, userId, cancellationToken);
        if (isTargetOwner)
        {
            var ownerCount = await companyRepository.CountOwnersAsync(companyId, cancellationToken);
            if (ownerCount <= 1)
                return OperationResult.Conflict("Cannot remove the last owner. Transfer ownership first.");
        }

        var removed = await companyRepository.RemoveMemberAsync(companyId, userId, cancellationToken);
        return removed
            ? OperationResult.Ok(new { companyId, userId, removed = true })
            : OperationResult.NotFound("Membership not found.");
    }

    public async Task<OperationResult> TransferOwnershipAsync(long companyId, long newOwnerUserId, CancellationToken cancellationToken)
    {
        if (!await companyRepository.IsOwnerAsync(companyId, requestContext.UserId, cancellationToken))
            return OperationResult.NotFound("Company not found.");

        var transferred = await companyRepository.TransferOwnershipAsync(companyId, requestContext.UserId, newOwnerUserId, cancellationToken);
        return transferred
            ? OperationResult.Ok()
            : OperationResult.NotFound("New owner must already be a member of this company.");
    }

    public async Task<OperationResult> CheckActiveMembershipAsync(long companyId, long userId, CancellationToken cancellationToken)
    {
        var isMember = await companyRepository.IsMemberAsync(companyId, userId, cancellationToken);
        return OperationResult.Ok(new { isActive = isMember });
    }

    private static (string Token, string TokenHash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (token, HashToken(token));
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
