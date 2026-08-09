using System.Net.Http.Json;
using System.Text.Json;
using AstraSystemsRental.Reports.Api.Dtos;

namespace AstraSystemsRental.Reports.Api.Services;

public interface IFleetMetricsSource
{
    Task<FleetSectionDto?> GetAsync(CancellationToken cancellationToken);
}

public interface IWorkshopMetricsSource
{
    Task<WorkshopSectionDto?> GetAsync(CancellationToken cancellationToken);
}

public abstract class ForwardingApiClient(HttpClient httpClient, IHttpContextAccessor accessor)
{
    private const string CompanyHeaderName = "X-Astra-Company";
    private const string NodeHeaderName = "X-Astra-Node";

    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected HttpClient Client { get; } = httpClient;

    protected HttpRequestMessage BuildRequest(HttpMethod method, string path, string nodeKey)
    {
        var request = new HttpRequestMessage(method, path);

        var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        var companyHeader = accessor.HttpContext?.Request.Headers[CompanyHeaderName].ToString();
        if (!string.IsNullOrEmpty(companyHeader))
            request.Headers.TryAddWithoutValidation(CompanyHeaderName, companyHeader);

        request.Headers.TryAddWithoutValidation(NodeHeaderName, nodeKey);

        return request;
    }

    protected async Task<T?> ReadDataAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            return default;

        var envelope = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);

        if (!envelope.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            return default;

        return data.Deserialize<T>(JsonOptions);
    }
}

public sealed class FleetMetricsSource(HttpClient httpClient, IHttpContextAccessor accessor, ILogger<FleetMetricsSource> logger)
    : ForwardingApiClient(httpClient, accessor), IFleetMetricsSource
{
    public async Task<FleetSectionDto?> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, "/apiVehicles/fleet-metrics", "vehicle-registry");
            using var response = await Client.SendAsync(request, cancellationToken);
            return await ReadDataAsync<FleetSectionDto>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fleet metrics source unavailable");
            return null;
        }
    }
}

public sealed class WorkshopMetricsSource(HttpClient httpClient, IHttpContextAccessor accessor, ILogger<WorkshopMetricsSource> logger)
    : ForwardingApiClient(httpClient, accessor), IWorkshopMetricsSource
{
    public async Task<WorkshopSectionDto?> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, "/apiMaintenance/maintenance-metrics", "maintenance-tracking");
            using var response = await Client.SendAsync(request, cancellationToken);
            return await ReadDataAsync<WorkshopSectionDto>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Workshop metrics source unavailable");
            return null;
        }
    }
}
