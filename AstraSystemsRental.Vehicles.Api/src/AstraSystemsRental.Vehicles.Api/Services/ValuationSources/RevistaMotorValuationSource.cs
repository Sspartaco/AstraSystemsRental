using System.Text;
using AstraSystemsRental.Vehicles.Api.Domain;
using ExcelDataReader;
using Microsoft.Extensions.Caching.Memory;

namespace AstraSystemsRental.Vehicles.Api.Services.ValuationSources;

// Fuente confirmada por el usuario: motor.com.co carga toda su tabla de precios desde 3 archivos
// .xls públicos (sin necesidad de simular el formulario AJAX), consumidos directamente por el
// funcion.js del sitio:
//   usados_importados.xls / usados_nacionales.xls / nuevos.xls
// Estructura de fila (deducida del propio JS del sitio):
//   Usados: { marca, buscador, tipo, USADOS_IMPORTADOS|__EMPTY, "15".."25": precio en millones (decimal) }
//     -> columna de 2 dígitos = año (20+dígitos), valor multiplicado por 10 y redondeado = millones de pesos.
//   Nuevos: { marca, buscador, categoria, nuevos, precio: valor en pesos completos, nomenclatura? }
public sealed class RevistaMotorValuationSource(HttpClient httpClient, IMemoryCache cache, ILogger<RevistaMotorValuationSource> logger) : IValuationSource
{
    private const string ImportedUrl = "https://www.eltiempo.com/infografias/2024/05/revista_motor_entrega_final/xls/usados_importados.xls";
    private const string NationalUrl = "https://www.eltiempo.com/infografias/2024/05/revista_motor_entrega_final/xls/usados_nacionales.xls";
    private const string NewUrl = "https://www.eltiempo.com/infografias/2024/05/revista_motor_entrega_final/xls/nuevos.xls";
    private static readonly TimeSpan SheetCacheTtl = TimeSpan.FromHours(6);

    static RevistaMotorValuationSource()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public string SourceCode => ValuationSourceCode.RevistaMotor;

    public async Task<ValuationSourceResult> FetchAsync(VehicleQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Brand) || string.IsNullOrWhiteSpace(query.Line))
            return new ValuationSourceResult(ValuationStatus.NotFound, null, null, null, null, "Marca/línea insuficientes para buscar.");

        try
        {
            var usedMatch = await FindInUsedSheetAsync(ImportedUrl, "importados", query, cancellationToken)
                ?? await FindInUsedSheetAsync(NationalUrl, "nacionales", query, cancellationToken);

            if (usedMatch is { } value)
                return new ValuationSourceResult(ValuationStatus.Success, value, value, value, null, null);

            var newValue = await FindInNewSheetAsync(query, cancellationToken);
            if (newValue is { } nv)
                return new ValuationSourceResult(ValuationStatus.Success, nv, nv, nv, null, null);

            return new ValuationSourceResult(ValuationStatus.NotFound, null, null, null, null, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Revista Motor valuation fetch failed for {Plate}", query.PlateNumber);
            return new ValuationSourceResult(ValuationStatus.Failed, null, null, null, null, ex.Message);
        }
    }

    private async Task<decimal?> FindInUsedSheetAsync(string url, string cacheKey, VehicleQuery query, CancellationToken cancellationToken)
    {
        var rows = await GetSheetAsync(cacheKey, url, cancellationToken);
        if (rows is null || query.ModelYear is null)
            return null;

        var yearKey = (query.ModelYear.Value % 100).ToString("D2");
        var brandTokens = Tokenize(query.Brand!);
        var lineTokens = Tokenize(query.Line!);

        foreach (var row in rows)
        {
            var brandCell = FirstNonEmpty(row, "marca");
            var modelCell = FirstNonEmpty(row, "USADOS_IMPORTADOS", "__EMPTY", "buscador");
            if (brandCell is null || modelCell is null)
                continue;

            if (!ContainsAllTokens(brandCell, brandTokens) || !ContainsAllTokens(modelCell, lineTokens))
                continue;

            if (row.TryGetValue(yearKey, out var priceRaw) && decimal.TryParse(priceRaw, out var millions) && millions > 0)
                return Math.Round(millions * 1_000_000m, 0);
        }

        return null;
    }

    private async Task<decimal?> FindInNewSheetAsync(VehicleQuery query, CancellationToken cancellationToken)
    {
        var rows = await GetSheetAsync("nuevos", NewUrl, cancellationToken);
        if (rows is null)
            return null;

        var brandTokens = Tokenize(query.Brand!);
        var lineTokens = Tokenize(query.Line!);

        foreach (var row in rows)
        {
            var brandCell = FirstNonEmpty(row, "marca");
            var modelCell = FirstNonEmpty(row, "nuevos");
            if (brandCell is null || modelCell is null)
                continue;

            if (!ContainsAllTokens(brandCell, brandTokens) || !ContainsAllTokens(modelCell, lineTokens))
                continue;

            if (row.TryGetValue("precio", out var priceRaw) && decimal.TryParse(priceRaw, out var price) && price > 0)
                return price;
        }

        return null;
    }

    private async Task<List<Dictionary<string, string>>?> GetSheetAsync(string cacheKey, string url, CancellationToken cancellationToken)
    {
        var key = $"revistamotor:{cacheKey}";
        if (cache.TryGetValue(key, out List<Dictionary<string, string>>? cached))
            return cached;

        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var result = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });

        var table = result.Tables[0];
        var headers = table.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).ToArray();
        var rows = new List<Dictionary<string, string>>(table.Rows.Count);

        foreach (System.Data.DataRow dataRow in table.Rows)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
                dict[header] = dataRow[header]?.ToString() ?? string.Empty;
            rows.Add(dict);
        }

        cache.Set(key, rows, SheetCacheTtl);
        return rows;
    }

    private static string? FirstNonEmpty(Dictionary<string, string> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }

    private static string[] Tokenize(string value)
        => value.ToLowerInvariant().Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool ContainsAllTokens(string haystack, string[] tokens)
    {
        var normalized = haystack.ToLowerInvariant();
        return tokens.All(t => normalized.Contains(t, StringComparison.Ordinal));
    }
}
