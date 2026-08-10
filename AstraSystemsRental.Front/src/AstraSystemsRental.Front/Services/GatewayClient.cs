using AstraSystemsRental.Contracts.Display;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AstraSystemsRental.Front.Services;

public sealed record LoginResult(bool Success, string? AccessToken, string? Error);

public sealed record GatewayResponse(int StatusCode, JsonElement? Data, IReadOnlyList<string> Errors)
{
    public bool IsSuccess => StatusCode is >= 200 and < 300;
}

public interface IGatewayClient
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<bool> RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task<JsonElement?> GetAsync(string path, string accessToken, CancellationToken cancellationToken);
    Task<JsonElement?> GetAsync(string path, string accessToken, string? nodeKey, CancellationToken cancellationToken);
    Task<JsonElement?> GetAsync(string path, string accessToken, string? nodeKey, long? companyId, CancellationToken cancellationToken);
    Task<T?> GetTypedAsync<T>(string path, string accessToken, string? nodeKey, long? companyId, CancellationToken cancellationToken);
    Task<bool> SendAsync(HttpMethod method, string path, string accessToken, object? body, CancellationToken cancellationToken);
    Task<GatewayResponse> SendForDataAsync(HttpMethod method, string path, string accessToken, object? body, string? nodeKey, CancellationToken cancellationToken);
    Task<GatewayResponse> SendForDataAsync(HttpMethod method, string path, string accessToken, object? body, string? nodeKey, long? companyId, CancellationToken cancellationToken);
    Task<GatewayResponse> PostFileAsync(string path, string accessToken, Stream content, string fileName, string contentType, string? notes, string? nodeKey, long? companyId, CancellationToken cancellationToken);
    Task<(Stream Content, string ContentType)?> GetBinaryAsync(string path, string accessToken, string? nodeKey, long? companyId, CancellationToken cancellationToken);
}

public sealed class GatewayClient(HttpClient httpClient, ILogger<GatewayClient> logger) : IGatewayClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync("/apiUsers/auth/login", new { email, password }, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                var envelope = await response.Content.ReadFromJsonAsync<Envelope>(JsonOptions, cancellationToken);
                return new LoginResult(false, null, Translate(envelope?.Errors?.FirstOrDefault()));
            }

            response.EnsureSuccessStatusCode();
            var ok = await response.Content.ReadFromJsonAsync<Envelope>(JsonOptions, cancellationToken);
            var token = ok?.Data?.AccessToken;
            return string.IsNullOrEmpty(token)
                ? new LoginResult(false, null, "No se pudo iniciar sesión.")
                : new LoginResult(true, token, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login request to gateway failed for {Email}", email);
            return new LoginResult(false, null, "El servicio no está disponible temporalmente.");
        }
    }

    public async Task<bool> RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync("/apiUsers/auth/forgot-password", new { email }, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Password reset request to gateway failed for {Email}", email);
            return false;
        }
    }

    public Task<JsonElement?> GetAsync(string path, string accessToken, CancellationToken cancellationToken)
        => GetAsync(path, accessToken, null, null, cancellationToken);

    public Task<JsonElement?> GetAsync(string path, string accessToken, string? nodeKey, CancellationToken cancellationToken)
        => GetAsync(path, accessToken, nodeKey, null, cancellationToken);

    public async Task<JsonElement?> GetAsync(string path, string accessToken, string? nodeKey, long? companyId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            if (!string.IsNullOrEmpty(nodeKey))
                request.Headers.Add("X-Astra-Node", nodeKey);
            if (companyId is not null)
                request.Headers.Add("X-Astra-Company", companyId.Value.ToString());
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var envelope = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
            return envelope.TryGetProperty("data", out var data) ? data : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GET {Path} through gateway failed", path);
            return null;
        }
    }

    public async Task<T?> GetTypedAsync<T>(string path, string accessToken, string? nodeKey, long? companyId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            if (!string.IsNullOrEmpty(nodeKey))
                request.Headers.Add("X-Astra-Node", nodeKey);
            if (companyId is not null)
                request.Headers.Add("X-Astra-Company", companyId.Value.ToString());

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return default;

            var envelope = await response.Content.ReadFromJsonAsync<TypedEnvelope<T>>(JsonOptions, cancellationToken);
            return envelope is not null ? envelope.Data : default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GET {Path} through gateway failed", path);
            return default;
        }
    }

    public Task<GatewayResponse> SendForDataAsync(HttpMethod method, string path, string accessToken, object? body, string? nodeKey, CancellationToken cancellationToken)
        => SendForDataAsync(method, path, accessToken, body, nodeKey, null, cancellationToken);

    public async Task<GatewayResponse> SendForDataAsync(HttpMethod method, string path, string accessToken, object? body, string? nodeKey, long? companyId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            if (!string.IsNullOrEmpty(nodeKey))
                request.Headers.Add("X-Astra-Node", nodeKey);
            if (companyId is not null)
                request.Headers.Add("X-Astra-Company", companyId.Value.ToString());
            if (body is not null)
                request.Content = JsonContent.Create(body);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var envelope = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);

            JsonElement? data = envelope.ValueKind == JsonValueKind.Object && envelope.TryGetProperty("data", out var d) && d.ValueKind != JsonValueKind.Null ? d : null;
            var errors = envelope.ValueKind == JsonValueKind.Object && envelope.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Array
                ? e.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToArray()
                : [];

            return new GatewayResponse((int)response.StatusCode, data, errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Method} {Path} through gateway failed", method, path);
            return new GatewayResponse(503, null, ["El servicio no está disponible temporalmente."]);
        }
    }

    public async Task<GatewayResponse> PostFileAsync(
        string path, string accessToken, Stream content, string fileName, string contentType,
        string? notes, string? nodeKey, long? companyId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            if (!string.IsNullOrEmpty(nodeKey))
                request.Headers.Add("X-Astra-Node", nodeKey);

            if (companyId is not null)
                request.Headers.Add("X-Astra-Company", companyId.Value.ToString());

            var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);

            if (!string.IsNullOrWhiteSpace(notes))
                form.Add(new StringContent(notes), "notes");

            request.Content = form;

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var envelope = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);

            JsonElement? data = envelope.ValueKind == JsonValueKind.Object && envelope.TryGetProperty("data", out var d) && d.ValueKind != JsonValueKind.Null ? d : null;
            var errors = envelope.ValueKind == JsonValueKind.Object && envelope.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Array
                ? e.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToArray()
                : [];

            return new GatewayResponse((int)response.StatusCode, data, errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "POST {Path} (multipart) through gateway failed", path);
            return new GatewayResponse(503, null, ["El servicio no está disponible temporalmente."]);
        }
    }

    public async Task<(Stream Content, string ContentType)?> GetBinaryAsync(
        string path, string accessToken, string? nodeKey, long? companyId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            if (!string.IsNullOrEmpty(nodeKey))
                request.Headers.Add("X-Astra-Node", nodeKey);

            if (companyId is not null)
                request.Headers.Add("X-Astra-Company", companyId.Value.ToString());

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            return (stream, contentType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GET {Path} (binary) through gateway failed", path);
            return null;
        }
    }

    public async Task<bool> SendAsync(HttpMethod method, string path, string accessToken, object? body, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            if (body is not null)
                request.Content = JsonContent.Create(body);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Method} {Path} through gateway failed", method, path);
            return false;
        }
    }

    // Antes tenia 3 casos propios y un "_ => error" que dejaba pasar en ingles
    // los otros 65 mensajes de las APIs. Ahora usa el mismo diccionario que la
    // app: una sola fuente de verdad para las traducciones.
    private static string Translate(string? error)
        => string.IsNullOrWhiteSpace(error)
            ? "Correo o contraseña incorrectos."
            : ErrorText.Translate(error);

    private sealed record Envelope(bool Success, EnvelopeData? Data, string[]? Errors);
    private sealed record EnvelopeData(string? AccessToken, string? TokenType, string? Role, string? Plan);
    private sealed record TypedEnvelope<T>(bool Success, T? Data, string[]? Errors);
}
