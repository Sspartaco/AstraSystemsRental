using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Vehicles.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Vehicles.Api.Persistence;

public sealed class QuoteRepository(AstraVehiclesDbContext context)
    : BaseRepository<AstraVehiclesDbContext, QuoteRequest>(context), IQuoteRepository
{
    public async Task<QuoteRequest> CreateQuoteRequestAsync(string plateNumber, long requestedByUserId, CancellationToken cancellationToken)
    {
        var request = new QuoteRequest
        {
            RequestId = Guid.NewGuid(),
            PlateNumber = plateNumber,
            RequestedByUserId = requestedByUserId,
            Status = QuoteRequestStatus.Running,
            CreatedAtUtc = DateTime.UtcNow
        };

        await AddAsync(request, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return request;
    }

    public Task<QuoteRequest?> GetQuoteRequestAsync(Guid requestId, CancellationToken cancellationToken)
        => GetFirstOrDefaultAsync(r => r.RequestId == requestId, cancellationToken);

    public Task<QuoteRequest?> GetOwnedQuoteRequestAsync(Guid requestId, long userId, CancellationToken cancellationToken)
        => GetFirstOrDefaultAsync(r => r.RequestId == requestId && r.RequestedByUserId == userId, cancellationToken);

    public async Task UpdateQuoteRequestStatusAsync(long id, QuoteRequestStatus status, DateTime? completedAtUtc, CancellationToken cancellationToken)
    {
        var request = await DbContext.QuoteRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request is null)
            return;

        request.Status = status;
        request.CompletedAtUtc = completedAtUtc;
        await SaveChangesAsync(cancellationToken);
    }

    public Task<ValuationCacheEntry?> GetCacheEntryAsync(string plateNumber, string sourceCode, CancellationToken cancellationToken)
        => DbContext.ValuationCache.AsNoTracking()
            .FirstOrDefaultAsync(e => e.PlateNumber == plateNumber && e.SourceCode == sourceCode, cancellationToken);

    public async Task<IReadOnlyList<ValuationCacheEntry>> GetCacheEntriesAsync(string plateNumber, CancellationToken cancellationToken)
        => await DbContext.ValuationCache.AsNoTracking()
            .Where(e => e.PlateNumber == plateNumber)
            .ToListAsync(cancellationToken);

    public async Task UpsertCacheEntryAsync(ValuationCacheEntry entry, CancellationToken cancellationToken)
    {
        var existing = await DbContext.ValuationCache
            .FirstOrDefaultAsync(e => e.PlateNumber == entry.PlateNumber && e.SourceCode == entry.SourceCode, cancellationToken);

        if (existing is null)
        {
            DbContext.ValuationCache.Add(entry);
        }
        else
        {
            existing.Status = entry.Status;
            existing.ValueMin = entry.ValueMin;
            existing.ValueMax = entry.ValueMax;
            existing.ValueAvg = entry.ValueAvg;
            existing.Currency = entry.Currency;
            existing.RawPayload = entry.RawPayload;
            existing.FetchedAtUtc = entry.FetchedAtUtc;
            existing.ExpiresAtUtc = entry.ExpiresAtUtc;
            existing.ErrorMessage = entry.ErrorMessage;
        }

        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ValuationSourceCatalog>> GetActiveSourcesAsync(CancellationToken cancellationToken)
        => await DbContext.ValuationSources.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<int> PurgeCompletedRequestsOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        var stale = await DbContext.QuoteRequests
            .Where(r => r.Status != QuoteRequestStatus.Running && r.CompletedAtUtc != null && r.CompletedAtUtc < cutoffUtc)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
            return 0;

        RemoveRange(stale);
        await SaveChangesAsync(cancellationToken);
        return stale.Count;
    }
}
