using System.Globalization;
using System.Net;
using AstraSystemsRental.Vehicles.Api.Domain;
using HtmlAgilityPack;

namespace AstraSystemsRental.Vehicles.Api.Services.ValuationSources;

// URL confirmada por el usuario contra el sitio real: https://carros.tucarro.com.co/{marca}/{linea}/
// Listado de MercadoLibre/TuCarro, precio en <span class="andes-money-amount__fraction">,
// título de la card en <h3 class="poly-component__title"> (contiene el año, ej. "Toyota Corolla 2023 1.8 Xe-i").
public sealed class TucarroValuationSource(HttpClient httpClient, ILogger<TucarroValuationSource> logger) : IValuationSource
{
    public string SourceCode => ValuationSourceCode.Tucarro;

    public async Task<ValuationSourceResult> FetchAsync(VehicleQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Brand) || string.IsNullOrWhiteSpace(query.Line))
            return new ValuationSourceResult(ValuationStatus.NotFound, null, null, null, null, "Marca/línea insuficientes para buscar.");

        var url = $"https://carros.tucarro.com.co/{Slug(query.Brand)}/{Slug(query.Line)}/";

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new ValuationSourceResult(ValuationStatus.Failed, null, null, null, null, $"HTTP {(int)response.StatusCode}");

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var cards = doc.DocumentNode.SelectNodes("//div[contains(@class,'ui-search-result__wrapper')]")
                ?? doc.DocumentNode.SelectNodes("//li[contains(@class,'ui-search-layout__item')]");

            var listings = ExtractListings(cards, query.ModelYear);
            return ListingAggregator.Aggregate(listings, TrimPayload(html));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "TuCarro valuation fetch failed for {Plate}", query.PlateNumber);
            return new ValuationSourceResult(ValuationStatus.Failed, null, null, null, null, ex.Message);
        }
    }

    internal static List<ListingPrice> ExtractListings(HtmlNodeCollection? cards, short? modelYear)
    {
        var listings = new List<ListingPrice>();
        if (cards is null)
            return listings;

        foreach (var card in cards)
        {
            var titleNode = card.SelectSingleNode(".//h3") ?? card.SelectSingleNode(".//*[contains(@class,'poly-component__title')]");
            var title = titleNode is null ? string.Empty : WebUtility.HtmlDecode(titleNode.InnerText).Trim();

            if (modelYear is not null && title.Length > 0 && !title.Contains(modelYear.Value.ToString(), StringComparison.Ordinal))
                continue;

            var priceNode = card.SelectSingleNode(".//span[contains(@class,'andes-money-amount__fraction')]");
            if (priceNode is null)
                continue;

            var text = WebUtility.HtmlDecode(priceNode.InnerText).Replace(".", "").Replace(",", "").Trim();
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) && price > 0)
                listings.Add(new ListingPrice(price, title));
        }

        return listings;
    }

    private static string Slug(string value) => Uri.EscapeDataString(value.Trim().ToLowerInvariant().Replace(' ', '-'));

    private static string TrimPayload(string html) => html.Length > 4000 ? html[..4000] : html;
}
