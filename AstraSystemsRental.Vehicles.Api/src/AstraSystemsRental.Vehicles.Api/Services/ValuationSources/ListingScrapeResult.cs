namespace AstraSystemsRental.Vehicles.Api.Services.ValuationSources;

public sealed record ListingPrice(decimal Price, string Title);

public static class ListingAggregator
{
    public static ValuationSourceResult Aggregate(IReadOnlyCollection<ListingPrice> listings, string rawPayload)
    {
        if (listings.Count == 0)
            return new ValuationSourceResult(Domain.ValuationStatus.NotFound, null, null, null, rawPayload, null);

        var prices = listings.Select(l => l.Price).OrderBy(p => p).ToArray();
        var min = prices.First();
        var max = prices.Last();
        var avg = Math.Round(prices.Average(), 2);

        return new ValuationSourceResult(Domain.ValuationStatus.Success, min, max, avg, rawPayload, null);
    }
}
