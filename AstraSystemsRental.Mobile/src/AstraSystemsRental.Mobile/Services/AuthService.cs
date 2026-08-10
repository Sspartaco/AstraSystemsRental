using System.Net.Http.Json;
using System.Text.Json;
using AstraSystemsRental.Contracts;
using AstraSystemsRental.Contracts.Auth;

namespace AstraSystemsRental.Mobile.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    Task<ApiResult<AuthTokensDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<bool> RestoreSessionAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NodeDto>> GetNodesAsync(CancellationToken cancellationToken = default);
    bool HasNode(string nodeKey);
    bool IsSuperUser { get; }
    bool IsSysAdmin { get; }
    bool CanSeeDiagnostics { get; }
    void RestoreClaims();
}

public sealed class AuthService(
    HttpClient http,
    IAstraApiClient api,
    ISessionStore session) : IAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private IReadOnlyList<NodeDto> _nodes = [];
    private HashSet<string> _allowedNodes = new(StringComparer.Ordinal);

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(session.Current?.AccessToken);

    public bool IsSuperUser => string.Equals(session.Current?.Role, "SuperUser", StringComparison.Ordinal);

    public bool IsSysAdmin => string.Equals(session.Current?.Role, "SysAdmin", StringComparison.Ordinal);

    public bool CanSeeDiagnostics => IsSuperUser || IsSysAdmin;

    public bool HasNode(string nodeKey)
        => IsSuperUser || IsSysAdmin || _allowedNodes.Contains("*") || _allowedNodes.Contains(nodeKey);

    public async Task<ApiResult<AuthTokensDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/apiUsers/auth/login")
            {
                Content = JsonContent.Create(
                    new LoginRequestDto
                    {
                        Email = email.Trim(),
                        Password = password,
                        DeviceInfo = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model} ({DeviceInfo.Current.Platform})"
                    },
                    options: JsonOptions)
            };

            using var response = await http.SendAsync(request, cancellationToken);
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthTokensDto>>(JsonOptions, cancellationToken);

            if (envelope?.Data is not { } tokens || string.IsNullOrWhiteSpace(tokens.AccessToken))
                return ApiResult<AuthTokensDto>.Fail(
                    envelope?.Errors.FirstOrDefault() ?? "No se pudo iniciar sesión.", (int)response.StatusCode);

            await session.SaveAsync(new StoredSession
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                Role = tokens.Role,
                Plan = tokens.Plan,
                Email = email.Trim()
            });

            ReadClaims(tokens.AccessToken);

            return ApiResult<AuthTokensDto>.Ok(tokens, (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return ApiResult<AuthTokensDto>.NoConnection();
        }
        catch (TaskCanceledException)
        {
            return ApiResult<AuthTokensDto>.NoConnection();
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var refreshToken = session.Current?.RefreshToken;

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/apiUsers/auth/logout")
                {
                    Content = JsonContent.Create(new RefreshRequestDto { RefreshToken = refreshToken }, options: JsonOptions)
                };

                using var _ = await http.SendAsync(request, cancellationToken);
            }
            catch
            {
                // cerrar sesion local aunque el servidor no responda
            }
        }

        _nodes = [];
        _allowedNodes = new HashSet<string>(StringComparer.Ordinal);
        await session.ClearAsync();
    }

    /// <summary>
    /// Reanuda una sesion guardada sin pedir credenciales. Se apoya en /users/me:
    /// si el access token expiro, el propio AstraApiClient lo renueva con el
    /// refresh token antes de reintentar, asi que un 200 confirma que la sesion
    /// sigue viva del lado del servidor.
    /// </summary>
    public async Task<bool> RestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(session.Current?.RefreshToken))
            return false;

        var result = await api.GetAsync<UserProfileDto>("/apiUsers/users/me", cancellationToken: cancellationToken);

        // Sin red no se invalida la sesion: se deja entrar y la app opera en
        // modo offline, igual que si la conexion se cayera estando dentro.
        if (!result.Success && !result.Offline)
            return false;

        RestoreClaims();
        return true;
    }

    public async Task<IReadOnlyList<NodeDto>> GetNodesAsync(CancellationToken cancellationToken = default)
    {
        if (_nodes.Count > 0)
            return _nodes;

        var result = await api.GetAsync<List<NodeDto>>("/apiUsers/nodes", cancellationToken: cancellationToken);

        if (result.Success && result.Data is { } nodes)
            _nodes = nodes;

        return _nodes;
    }

    public void RestoreClaims()
    {
        if (session.Current?.AccessToken is { Length: > 0 } token)
            ReadClaims(token);
    }

    private void ReadClaims(string accessToken)
    {
        _allowedNodes = new HashSet<string>(StringComparer.Ordinal);

        var parts = accessToken.Split('.');

        if (parts.Length < 2)
            return;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));

            if (!document.RootElement.TryGetProperty("node", out var nodeClaim))
                return;

            if (nodeClaim.ValueKind == JsonValueKind.Array)
            {
                foreach (var node in nodeClaim.EnumerateArray())
                {
                    if (node.GetString() is { } value)
                        _allowedNodes.Add(value);
                }
            }
            else if (nodeClaim.GetString() is { } single)
            {
                _allowedNodes.Add(single);
            }
        }
        catch
        {
            // token ilegible: se queda sin nodos y el gating cae a lo mas restrictivo
        }
    }
}
