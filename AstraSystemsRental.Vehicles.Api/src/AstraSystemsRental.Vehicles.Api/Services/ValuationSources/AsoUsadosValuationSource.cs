using System.Globalization;
using System.Net;
using AstraSystemsRental.Vehicles.Api.Domain;
using HtmlAgilityPack;

namespace AstraSystemsRental.Vehicles.Api.Services.ValuationSources;

// El código/clave de la fuente sigue siendo "AsoUsados" (así está sembrado en la BD), pero el
// sitio real confirmado por el usuario es AutoExpo: https://autoexpo.com.co/concesionario-carros-usados-bogota/{linea}/{marca}/
// Precio visible directo en el listado, sin necesidad de abrir cada aviso.
public sealed class AsoUsadosValuationSource(HttpClient httpClient, ILogger<AsoUsadosValuationSource> logger) : IValuationSource
{
    public string SourceCode => ValuationSourceCode.AsoUsados;

    public async Task<ValuationSourceResult> FetchAsync(VehicleQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Brand) || string.IsNullOrWhiteSpace(query.Line))
            return new ValuationSourceResult(ValuationStatus.NotFound, null, null, null, null, "Marca/línea insuficientes para buscar.");

        var url = $"https://autoexpo.com.co/concesionario-carros-usados-bogota/{Slug(query.Line)}/{Slug(query.Brand)}/" +
                  "?condition=usado&type=automovil";

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new ValuationSourceResult(ValuationStatus.Failed, null, null, null, null, $"HTTP {(int)response.StatusCode}");

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // TODO: confirmar clase real de la tarjeta de resultado (referencia visual: título + precio en negrita rojo, sin card wrapper claro en la captura).
            var priceNodes = doc.DocumentNode.SelectNodes("//*[contains(text(),'$') and (contains(@class,'price') or contains(@class,'precio'))]")
                ?? doc.DocumentNode.SelectNodes("//strong[contains(text(),'$')] | //b[contains(text(),'$')]");
            var listings = ExtractPrices(priceNodes);

            return ListingAggregator.Aggregate(listings, TrimPayload(html));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "AutoExpo valuation fetch failed for {Plate}", query.PlateNumber);
            return new ValuationSourceResult(ValuationStatus.Failed, null, null, null, null, ex.Message);
        }
    }

    private static List<ListingPrice> ExtractPrices(HtmlNodeCollection? nodes)
    {
        var listings = new List<ListingPrice>();
        if (nodes is null)
            return listings;

        foreach (var node in nodes)
        {
            var digits = new string(WebUtility.HtmlDecode(node.InnerText).Where(char.IsDigit).ToArray());
            if (decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) && price > 100000)
                listings.Add(new ListingPrice(price, string.Empty));
        }

        return listings;
    }

    private static string Slug(string value) => Uri.EscapeDataString(value.Trim().ToLowerInvariant().Replace(' ', '-'));

    private static string TrimPayload(string html) => html.Length > 4000 ? html[..4000] : html;
}
