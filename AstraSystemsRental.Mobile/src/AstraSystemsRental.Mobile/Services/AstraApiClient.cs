using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AstraSystemsRental.Contracts;
using AstraSystemsRental.Contracts.Auth;

namespace AstraSystemsRental.Mobile.Services;

public interface IAstraApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string path, string? nodeKey = null, CancellationToken cancellationToken = default);
    Task<ApiResult<T>> PostAsync<T>(string path, object? body, string? nodeKey = null, CancellationToken cancellationToken = default);
    Task<ApiResult<T>> PutAsync<T>(string path, object? body, string? nodeKey = null, CancellationToken cancellationToken = default);
    Task<ApiResult<T>> PostFileAsync<T>(string path, Stream content, string fileName, string contentType, string? notes, string? nodeKey = null, CancellationToken cancellationToken = default);
    Task<byte[]?> GetBytesAsync(string path, string? nodeKey = null, CancellationToken cancellationToken = default);
    event EventHandler? SessionExpired;
}

public sealed class AstraApiClient : IAstraApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ISessionStore _session;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public event EventHandler? SessionExpired;

    private readonly IClientLogReporter _reporter;

    public AstraApiClient(HttpClient http, ISessionStore session, IClientLogReporter reporter)
    {
        _http = http;
        _session = session;
        _reporter = reporter;
    }

    public Task<ApiResult<T>> GetAsync<T>(string path, string? nodeKey = null, CancellationToken cancellationToken = default)
        => SendAsync<T>(() => new HttpRequestMessage(HttpMethod.Get, path), nodeKey, cancellationToken);

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body, string? nodeKey = null, CancellationToken cancellationToken = default)
        => SendAsync<T>(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            if (body is not null)
                // JsonContent.Create(body) con body declarado como object? serializa
                // segun el tipo ESTATICO y produce "{}". Hay que pasar el tipo real.
                request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
            return request;
        }, nodeKey, cancellationToken);

    public Task<ApiResult<T>> PutAsync<T>(string path, object? body, string? nodeKey = null, CancellationToken cancellationToken = default)
        => SendAsync<T>(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Put, path);
            if (body is not null)
                // JsonContent.Create(body) con body declarado como object? serializa
                // segun el tipo ESTATICO y produce "{}". Hay que pasar el tipo real.
                request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
            return request;
        }, nodeKey, cancellationToken);

    public Task<ApiResult<T>> PostFileAsync<T>(
        string path, Stream content, string fileName, string contentType, string? notes,
        string? nodeKey = null, CancellationToken cancellationToken = default)
    {
        var buffer = new MemoryStream();
        content.CopyTo(buffer);

        return SendAsync<T>(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            var form = new MultipartFormDataContent();

            var copy = new MemoryStream(buffer.ToArray());
            var fileContent = new StreamContent(copy);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);

            if (!string.IsNullOrWhiteSpace(notes))
                form.Add(new StringContent(notes), "notes");

            request.Content = form;
            return request;
        }, nodeKey, cancellationToken);
    }

    public async Task<byte[]?> GetBytesAsync(string path, string? nodeKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = BuildRequest(new HttpRequestMessage(HttpMethod.Get, path), nodeKey);
            using var response = await _http.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshAsync(cancellationToken))
            {
                using var retry = BuildRequest(new HttpRequestMessage(HttpMethod.Get, path), nodeKey);
                using var retryResponse = await _http.SendAsync(retry, cancellationToken);
                return retryResponse.IsSuccessStatusCode ? await retryResponse.Content.ReadAsByteArrayAsync(cancellationToken) : null;
            }

            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(cancellationToken) : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        Func<HttpRequestMessage> factory, string? nodeKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = BuildRequest(factory(), nodeKey);
            using var response = await _http.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (!await TryRefreshAsync(cancellationToken))
                {
                    SessionExpired?.Invoke(this, EventArgs.Empty);
                    return ApiResult<T>.Fail("La sesión expiró. Iniciá sesión de nuevo.", 401);
                }

                using var retry = BuildRequest(factory(), nodeKey);
                using var retryResponse = await _http.SendAsync(retry, cancellationToken);
                return Track(await ReadAsync<T>(retryResponse, cancellationToken), retry);
            }

            return Track(await ReadAsync<T>(response, cancellationToken), request);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.NoConnection();
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.NoConnection();
        }
    }

    /// <summary>
    /// Manda a la API cualquier respuesta fallida para que quede en la vista de
    /// Logs. Devuelve el resultado sin tocarlo: es solo un punto de observacion.
    /// </summary>
    private ApiResult<T> Track<T>(ApiResult<T> result, HttpRequestMessage request)
    {
        if (!result.Success)
        {
            _reporter.ReportApiFailure(
                request.Method.Method,
                request.RequestUri?.PathAndQuery ?? "?",
                result.StatusCode,
                result.Error);
        }

        return result;
    }

    private HttpRequestMessage BuildRequest(HttpRequestMessage request, string? nodeKey)
    {
        var session = _session.Current;

        if (session is not null && !string.IsNullOrEmpty(session.AccessToken))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);

        if (!string.IsNullOrEmpty(nodeKey))
            request.Headers.TryAddWithoutValidation("X-Astra-Node", nodeKey);

        if (session?.ActiveCompanyId is { } companyId)
            request.Headers.TryAddWithoutValidation("X-Astra-Company", companyId.ToString());

        return request;
    }

    private static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;

        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(JsonOptions, cancellationToken);

            if (envelope is null)
                return response.IsSuccessStatusCode
                    ? ApiResult<T>.Ok(default, status)
                    : ApiResult<T>.Fail("Respuesta inesperada del servidor.", status);

            return envelope.Success
                ? ApiResult<T>.Ok(envelope.Data, status)
                : ApiResult<T>.Fail(envelope.Errors.FirstOrDefault(), status);
        }
        catch
        {
            return response.IsSuccessStatusCode
                ? ApiResult<T>.Ok(default, status)
                : ApiResult<T>.Fail("Respuesta inesperada del servidor.", status);
        }
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        var before = _session.Current?.AccessToken;

        await _refreshLock.WaitAsync(cancellationToken);

        try
        {
            var session = _session.Current;

            if (session is null || string.IsNullOrWhiteSpace(session.RefreshToken))
                return false;

            if (!string.Equals(before, session.AccessToken, StringComparison.Ordinal))
                return true;

            using var request = new HttpRequestMessage(HttpMethod.Post, "/apiUsers/auth/refresh")
            {
                Content = JsonContent.Create(
                    new RefreshRequestDto { RefreshToken = session.RefreshToken, DeviceInfo = DeviceInfo.Current.Platform.ToString() },
                    options: JsonOptions)
            };

            using var response = await _http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await _session.ClearAsync();
                return false;
            }

            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokensDto>>(JsonOptions, cancellationToken);

            if (envelope?.Data is not { } tokens || string.IsNullOrWhiteSpace(tokens.AccessToken))
            {
                await _session.ClearAsync();
                return false;
            }

            await _session.SaveAsync(session with
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken ?? session.RefreshToken,
                Role = tokens.Role ?? session.Role,
                Plan = tokens.Plan ?? session.Plan
            });

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
