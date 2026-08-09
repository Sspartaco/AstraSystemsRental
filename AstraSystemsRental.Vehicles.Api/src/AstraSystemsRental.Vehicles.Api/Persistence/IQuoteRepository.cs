using AstraSystemsRental.Vehicles.Api.Domain;

namespace AstraSystemsRental.Vehicles.Api.Persistence;

public interface IQuoteRepository
{
    Task<QuoteRequest> CreateQuoteRequestAsync(string plateNumber, long requestedByUserId, CancellationToken cancellationToken);
    Task<QuoteRequest?> GetQuoteRequestAsync(Guid requestId, CancellationToken cancellationToken);
    Task<QuoteRequest?> GetOwnedQuoteRequestAsync(Guid requestId, long userId, CancellationToken cancellationToken);
    Task UpdateQuoteRequestStatusAsync(long id, QuoteRequestStatus status, DateTime? completedAtUtc, CancellationToken cancellationToken);

    Task<ValuationCacheEntry?> GetCacheEntryAsync(string plateNumber, string sourceCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<ValuationCacheEntry>> GetCacheEntriesAsync(string plateNumber, CancellationToken cancellationToken);
    Task UpsertCacheEntryAsync(ValuationCacheEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<ValuationSourceCatalog>> GetActiveSourcesAsync(CancellationToken cancellationToken);

    Task<int> PurgeCompletedRequestsOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken);
}
