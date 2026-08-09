using System.Net.Http.Json;
using System.Text.Json;
using AstraSystemsRental.Base.Http;

namespace AstraSystemsRental.Maintenance.Api.Services;

public abstract class ForwardingApiClient(HttpClient httpClient, IHttpContextAccessor accessor)
{
    protected const string CompanyHeaderName = "X-Astra-Company";
    protected const string NodeHeaderName = "X-Astra-Node";
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected HttpClient Client { get; } = httpClient;

    protected HttpRequestMessage BuildRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);

        var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        var companyHeader = accessor.HttpContext?.Request.Headers[CompanyHeaderName].ToString();
        if (!string.IsNullOrEmpty(companyHeader))
            request.Headers.TryAddWithoutValidation(CompanyHeaderName, companyHeader);

        var nodeHeader = accessor.HttpContext?.Request.Headers[NodeHeaderName].ToString();
        if (!string.IsNullOrEmpty(nodeHeader))
            request.Headers.TryAddWithoutValidation(NodeHeaderName, nodeHeader);

        return request;
    }
}

public sealed class UsersApiClient(HttpClient httpClient, IHttpContextAccessor accessor, ILogger<UsersApiClient> logger)
    : ForwardingApiClient(httpClient, accessor), IUsersApiClient
{
    public async Task<QuotaLookupResult?> GetQuotaAsync(string nodeKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, $"/apiUsers/quotas/{nodeKey}");
            using var response = await Client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var envelope = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
            if (!envelope.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                return null;

            return new QuotaLookupResult(
                data.GetProperty("nodeKey").GetString() ?? nodeKey,
                data.GetProperty("maxCount").GetInt32(),
                data.GetProperty("planCode").GetString() ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading quota for node {NodeKey}", nodeKey);
            return null;
        }
    }

    public async Task<CrossApiResult> TryReserveQuotaAsync(string nodeKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Post, $"/apiUsers/quotas/{nodeKey}/reserve");
            using var response = await Client.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? CrossApiResult.Ok()
                : CrossApiResult.Denied($"QuotaReserveFailed:{(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reserving quota for node {NodeKey}", nodeKey);
            return CrossApiResult.Unavailable(ex.Message);
        }
    }

    public async Task<CrossApiResult> ReleaseQuotaAsync(string nodeKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Post, $"/apiUsers/quotas/{nodeKey}/release");
            using var response = await Client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return CrossApiResult.Ok();

            logger.LogWarning("Failed to release quota for node {NodeKey}: {StatusCode}", nodeKey, response.StatusCode);
            return CrossApiResult.Denied($"QuotaReleaseFailed:{(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error releasing quota for node {NodeKey}", nodeKey);
            return CrossApiResult.Unavailable(ex.Message);
        }
    }

    public async Task<CrossApiResult> IsActiveCompanyMemberAsync(long companyId, long userId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, $"/apiUsers/companies/{companyId}/members/{userId}/is-active");
            using var response = await Client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return CrossApiResult.Unavailable($"UpstreamStatus:{(int)response.StatusCode}");

            var envelope = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
            var isActive = envelope.TryGetProperty("data", out var data) &&
                           data.TryGetProperty("isActive", out var isActiveProp) &&
                           isActiveProp.GetBoolean();

            return isActive ? CrossApiResult.Ok() : CrossApiResult.Denied("NotActiveMember");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking company membership for company {CompanyId}", companyId);
            return CrossApiResult.Unavailable(ex.Message);
        }
    }
}

public sealed class VehiclesApiClient(HttpClient httpClient, IHttpContextAccessor accessor, ILogger<VehiclesApiClient> logger)
    : ForwardingApiClient(httpClient, accessor), IVehiclesApiClient
{
    public async Task<FleetVehicleSummary?> GetFleetVehicleAsync(long fleetVehicleId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, $"/apiVehicles/fleet-vehicles/{fleetVehicleId}");
            using var response = await Client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var envelope = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
            if (!envelope.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                return null;

            return new FleetVehicleSummary(
                data.GetProperty("id").GetInt64(),
                data.GetProperty("plateNumber").GetString() ?? string.Empty,
                GetString(data, "brand"),
                GetString(data, "line"),
                data.GetProperty("status").GetString() ?? "Draft");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading fleet vehicle {FleetVehicleId}", fleetVehicleId);
            return null;
        }
    }

    private static string? GetString(JsonElement element, string prop)
        => element.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

public sealed class MailApiClient(HttpClient httpClient, IHttpContextAccessor accessor, ILogger<MailApiClient> logger)
    : ForwardingApiClient(httpClient, accessor), IMailApiClient
{
    public async Task<CrossApiResult> SendReservationReadyAsync(string toEmail, string plateNumber, CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Post, "/apiMail/notifications/workshop-reservation-ready");
            request.Content = JsonContent.Create(new { toEmail, plateNumber }, options: JsonOptions);

            using var response = await Client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return CrossApiResult.Ok();

            logger.LogWarning("Failed to send ready notification for plate {PlateNumber}: {StatusCode}", plateNumber, response.StatusCode);
            return CrossApiResult.Denied($"MailFailed:{(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending ready notification for plate {PlateNumber}", plateNumber);
            return CrossApiResult.Unavailable(ex.Message);
        }
    }
}
