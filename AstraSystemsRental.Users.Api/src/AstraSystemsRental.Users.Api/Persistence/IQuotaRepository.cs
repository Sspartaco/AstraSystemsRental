namespace AstraSystemsRental.Users.Api.Persistence;

public sealed record QuotaLookup(string NodeKey, int MaxCount, string PlanCode);

public interface IQuotaRepository
{
    Task<QuotaLookup?> GetQuotaAsync(string ownerType, long ownerId, string nodeKey, CancellationToken cancellationToken);
    Task<bool> TryReserveAsync(string ownerType, long ownerId, string nodeKey, CancellationToken cancellationToken);
    Task ReleaseAsync(string ownerType, long ownerId, string nodeKey, CancellationToken cancellationToken);
}
