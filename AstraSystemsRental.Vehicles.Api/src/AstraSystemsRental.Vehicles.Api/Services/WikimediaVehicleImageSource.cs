using System.Text.Json;

namespace AstraSystemsRental.Vehicles.Api.Services;

// Wikimedia Commons: API pública, sin key, licencias libres. Busca un archivo de imagen que
// coincida con "Marca Línea Modelo" en el namespace de archivos (ns=6) y resuelve su URL directa
// vía imageinfo. Cobertura buena para modelos populares, pobre para variantes muy específicas
// o vehículos de nicho — en ese caso simplemente no hay imagen y el Front usa un placeholder.
public sealed class WikimediaVehicleImageSource(HttpClient httpClient, ILogger<WikimediaVehicleImageSource> logger) : IVehicleImageSource
{
    private const string ApiBase = "https://commons.wikimedia.org/w/api.php";

    public async Task<VehicleImageResult> FindAsync(string? brand, string? line, short? modelYear, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(line))
            return new VehicleImageResult(null, null);

        var searchTerm = $"{brand} {line} {modelYear}".Trim();

        try
        {
            var searchUrl = $"{ApiBase}?action=query&generator=search&gsrsearch={Uri.EscapeDataString(searchTerm)}" +
                             "&gsrnamespace=6&gsrlimit=1&prop=imageinfo&iiprop=url|extmetadata&iiurlwidth=800&format=json";

            using var response = await httpClient.GetAsync(searchUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new VehicleImageResult(null, null);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("query", out var query) ||
                !query.TryGetProperty("pages", out var pages))
                return new VehicleImageResult(null, null);

            foreach (var page in pages.EnumerateObject())
            {
                if (!page.Value.TryGetProperty("imageinfo", out var infoArray) || infoArray.GetArrayLength() == 0)
                    continue;

                var info = infoArray[0];
                var url = info.TryGetProperty("thumburl", out var thumb) ? thumb.GetString()
                    : info.TryGetProperty("url", out var full) ? full.GetString() : null;

                if (string.IsNullOrEmpty(url))
                    continue;

                var attribution = ExtractAttribution(info);
                return new VehicleImageResult(url, attribution);
            }

            return new VehicleImageResult(null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Wikimedia image lookup failed for {Term}", searchTerm);
            return new VehicleImageResult(null, null);
        }
    }

    private static string? ExtractAttribution(JsonElement info)
    {
        if (!info.TryGetProperty("extmetadata", out var meta))
            return "Wikimedia Commons";

        if (meta.TryGetProperty("Artist", out var artist) && artist.TryGetProperty("value", out var artistValue))
        {
            var text = System.Text.RegularExpressions.Regex.Replace(artistValue.GetString() ?? string.Empty, "<.*?>", string.Empty).Trim();
            return string.IsNullOrEmpty(text) ? "Wikimedia Commons" : $"{text} · Wikimedia Commons";
        }

        return "Wikimedia Commons";
    }
}
