using System.Text.Json;
using AstraSystemsRental.Contracts.Display;

namespace AstraSystemsRental.Mobile.Services;

public sealed record SyncOutcome(int Synced, int Conflicts, int Remaining);

public interface ISyncService
{
    bool IsOnline { get; }
    int PendingCount { get; }
    event EventHandler? PendingChanged;
    Task RefreshPendingCountAsync();
    Task<SyncOutcome> SyncAsync(CancellationToken cancellationToken = default);
    Task EnqueueOrSendAsync(PendingOperation operation, CancellationToken cancellationToken = default);
}

public sealed class SyncService(
    IAstraApiClient api,
    IOfflineQueue queue) : ISyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private int _pendingCount;

    public event EventHandler? PendingChanged;

    public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public int PendingCount => _pendingCount;

    public async Task RefreshPendingCountAsync()
    {
        _pendingCount = await queue.CountPendingAsync();
        PendingChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Intenta enviar la operacion; si no hay red o el servidor no responde, la encola.
    /// Un rechazo del servidor (4xx) NO se encola: es un error de negocio que el usuario debe ver.
    /// </summary>
    public async Task EnqueueOrSendAsync(PendingOperation operation, CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
        {
            await queue.EnqueueAsync(operation);
            await RefreshPendingCountAsync();
            return;
        }

        var result = await SendAsync(operation, cancellationToken);

        if (result.Sent)
            return;

        if (result.IsBusinessError)
            throw new InvalidOperationException(result.Error ?? "No se pudo completar la operación.");

        await queue.EnqueueAsync(operation);
        await RefreshPendingCountAsync();
    }

    public async Task<SyncOutcome> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
            return new SyncOutcome(0, 0, await queue.CountPendingAsync());

        var pending = await queue.GetPendingAsync();
        var synced = 0;
        var conflicts = 0;

        foreach (var operation in pending)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await SendAsync(operation, cancellationToken);

            if (result.Sent)
            {
                await queue.MarkSyncedAsync(operation);
                synced++;
            }
            else if (result.IsBusinessError)
            {
                await queue.MarkConflictAsync(operation, result.Error ?? "Rechazado por el servidor.");
                conflicts++;
            }
            else
            {
                await queue.MarkRetryAsync(operation, result.Error ?? "Sin conexión.");
                break;
            }
        }

        await RefreshPendingCountAsync();

        return new SyncOutcome(synced, conflicts, _pendingCount);
    }

    private async Task<SendResult> SendAsync(PendingOperation operation, CancellationToken cancellationToken)
    {
        if (operation.Kind == PendingKind.ReservationPhoto)
        {
            if (string.IsNullOrWhiteSpace(operation.FilePath) || !File.Exists(operation.FilePath))
                return new SendResult(false, true, "La imagen ya no está disponible en el dispositivo.");

            await using var stream = File.OpenRead(operation.FilePath);

            var photoResult = await api.PostFileAsync<object>(
                operation.Path, stream, operation.FileName ?? "foto.jpg",
                operation.ContentType ?? "image/jpeg", operation.Notes, operation.NodeKey, cancellationToken);

            return Interpret(photoResult.Success, photoResult.StatusCode, photoResult.Error);
        }

        object? body = string.IsNullOrWhiteSpace(operation.PayloadJson)
            ? null
            : JsonSerializer.Deserialize<JsonElement>(operation.PayloadJson, JsonOptions);

        var result = await api.PostAsync<object>(operation.Path, body, operation.NodeKey, cancellationToken);

        return Interpret(result.Success, result.StatusCode, result.Error);
    }

    private static SendResult Interpret(bool success, int statusCode, string? error)
    {
        if (success)
            return new SendResult(true, false, null);

        // 0 = sin conexion, 5xx = servidor caido: reintentar.
        // 4xx = el servidor evaluo y rechazo: es conflicto, reintentarlo no cambia nada.
        var isBusinessError = statusCode is >= 400 and < 500 and not 401 and not 408 and not 429;

        return new SendResult(false, isBusinessError, ErrorText.Translate(error));
    }

    private sealed record SendResult(bool Sent, bool IsBusinessError, string? Error);
}
