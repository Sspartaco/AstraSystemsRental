using System.Text.Json;

namespace AstraSystemsRental.Mobile.Services;

public sealed record StoredSession
{
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public string? Role { get; init; }
    public string? Plan { get; init; }
    public string? Email { get; init; }
    public long? ActiveCompanyId { get; init; }
}

public interface ISessionStore
{
    StoredSession? Current { get; }
    Task LoadAsync();
    Task SaveAsync(StoredSession session);
    Task SetActiveCompanyAsync(long? companyId);
    Task ClearAsync();
}

public sealed class SessionStore : ISessionStore
{
    private const string Key = "astra.session";

    private StoredSession? _current;

    public StoredSession? Current => _current;

    public async Task LoadAsync()
    {
        try
        {
            var raw = await SecureStorage.Default.GetAsync(Key);
            _current = string.IsNullOrWhiteSpace(raw) ? null : JsonSerializer.Deserialize<StoredSession>(raw);
        }
        catch
        {
            _current = null;
        }
    }

    public async Task SaveAsync(StoredSession session)
    {
        _current = session;
        await SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(session));
    }

    public Task SetActiveCompanyAsync(long? companyId)
        => _current is null
            ? Task.CompletedTask
            : SaveAsync(_current with { ActiveCompanyId = companyId });

    public Task ClearAsync()
    {
        _current = null;
        SecureStorage.Default.Remove(Key);
        return Task.CompletedTask;
    }
}
