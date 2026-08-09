using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace AstraSystemsRental.Front.Services;

public sealed record CatalogNode(string Key, string Label, string Icon, string Route, bool SuperUserOnly, bool DiagnosticsOnly = false);

public interface INodeCatalogService
{
    Task<IReadOnlyList<CatalogNode>> GetAsync(string accessToken, CancellationToken cancellationToken);
}

public sealed class NodeCatalogService(IGatewayClient gateway, IMemoryCache cache) : INodeCatalogService
{
    private const string CacheKey = "astra.nodes.catalog";

    private static readonly CatalogNode AdminUsers = new(
        "admin-users", "Usuarios", """<path d="M17.5 20.25v-1.5a4 4 0 0 0-4-4h-4a4 4 0 0 0-4 4v1.5" /><circle cx="11.5" cy="7.5" r="3.25" /><path d="M20.5 20.25v-1.5a4 4 0 0 0-3-3.87" /><path d="M16.75 4.5a3.25 3.25 0 0 1 0 6" />""", "Admin/Users", true);

    private static readonly CatalogNode DiagnosticsLogs = new(
        "diagnostics-logs", "Logs del sistema", """<path d="M4 6.75h16" /><path d="M4 12h16" /><path d="M4 17.25h10" /><circle cx="18.5" cy="17.25" r="2" />""", "Diagnostics/Logs", false, true);

    private static readonly CatalogNode AdminCatalog = new(
        "admin-catalog", "Planes y nodos", """<path d="M3.75 3.75h6.5v6.5h-6.5zM13.75 3.75h6.5v6.5h-6.5zM3.75 13.75h6.5v6.5h-6.5zM13.75 13.75h6.5v6.5h-6.5z" />""", "Admin/Catalog", true);

    public async Task<IReadOnlyList<CatalogNode>> GetAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<IReadOnlyList<CatalogNode>>(CacheKey, out var cached) && cached is not null)
            return cached;

        var nodes = new List<CatalogNode>();
        var data = await gateway.GetAsync("/apiUsers/nodes", accessToken, cancellationToken);
        if (data is { ValueKind: JsonValueKind.Array })
        {
            foreach (var item in data.Value.EnumerateArray())
            {
                nodes.Add(new CatalogNode(
                    item.GetProperty("key").GetString() ?? string.Empty,
                    item.GetProperty("label").GetString() ?? string.Empty,
                    item.GetProperty("icon").GetString() ?? string.Empty,
                    item.GetProperty("route").GetString() ?? string.Empty,
                    false));
            }
        }

        nodes.Add(AdminUsers);
        nodes.Add(AdminCatalog);
        nodes.Add(DiagnosticsLogs);

        cache.Set(CacheKey, (IReadOnlyList<CatalogNode>)nodes, TimeSpan.FromMinutes(5));
        return nodes;
    }
}
