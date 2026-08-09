using System.Net.Http.Json;
using System.Text.Json;
using AstraSystemsRental.Base.Http;

namespace AstraSystemsRental.Vehicles.Api.Services;

public sealed class UsersApiClient(HttpClient httpClient, IHttpContextAccessor accessor, ILogger<UsersApiClient> logger) : IUsersApiClient
{
    private const string CompanyHeaderName = "X-Astra-Company";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<QuotaLookupResult?> GetQuotaAsync(string nodeKey, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, $"/apiUsers/quotas/{nodeKey}");
        using var response = await httpClient.SendAsync(request, cancellationToken);

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

    public async Task<CrossApiResult> TryReserveQuotaAsync(string nodeKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Post, $"/apiUsers/quotas/{nodeKey}/reserve");
            using var response = await httpClient.SendAsync(request, cancellationToken);
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
            using var response = await httpClient.SendAsync(request, cancellationToken);
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
            using var response = await httpClient.SendAsync(request, cancellationToken);

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

    private HttpRequestMessage BuildRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);

        var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        var companyHeader = accessor.HttpContext?.Request.Headers[CompanyHeaderName].ToString();
        if (!string.IsNullOrEmpty(companyHeader))
            request.Headers.TryAddWithoutValidation(CompanyHeaderName, companyHeader);

        return request;
    }
}
