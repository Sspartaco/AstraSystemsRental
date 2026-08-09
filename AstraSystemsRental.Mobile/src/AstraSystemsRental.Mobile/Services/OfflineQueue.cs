using SQLite;

namespace AstraSystemsRental.Mobile.Services;

public enum PendingKind
{
    MileageReading = 0,
    WorkshopReservation = 1,
    ReservationPhoto = 2
}

public enum PendingState
{
    Pending = 0,
    Synced = 1,
    Conflict = 2
}

[Table("PendingOperations")]
public sealed class PendingOperation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public PendingKind Kind { get; set; }
    public PendingState State { get; set; } = PendingState.Pending;

    public string Path { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }

    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? Notes { get; set; }

    public string? NodeKey { get; set; }
    public long? CompanyId { get; set; }

    public string Label { get; set; } = string.Empty;
    public string? LastError { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public interface IOfflineQueue
{
    Task InitializeAsync();
    Task EnqueueAsync(PendingOperation operation);
    Task<IReadOnlyList<PendingOperation>> GetPendingAsync();
    Task<IReadOnlyList<PendingOperation>> GetConflictsAsync();
    Task<int> CountPendingAsync();
    Task MarkSyncedAsync(PendingOperation operation);
    Task MarkConflictAsync(PendingOperation operation, string error);
    Task MarkRetryAsync(PendingOperation operation, string error);
    Task DiscardAsync(int id);
}

public sealed class OfflineQueue : IOfflineQueue
{
    private SQLiteAsyncConnection? _connection;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        var path = Path.Combine(FileSystem.AppDataDirectory, "astra-offline.db3");
        _connection = new SQLiteAsyncConnection(path);
        await _connection.CreateTableAsync<PendingOperation>();

        return _connection;
    }

    public async Task InitializeAsync() => await GetConnectionAsync();

    public async Task EnqueueAsync(PendingOperation operation)
    {
        var connection = await GetConnectionAsync();
        await connection.InsertAsync(operation);
    }

    public async Task<IReadOnlyList<PendingOperation>> GetPendingAsync()
    {
        var connection = await GetConnectionAsync();

        return await connection.Table<PendingOperation>()
            .Where(o => o.State == PendingState.Pending)
            .OrderBy(o => o.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PendingOperation>> GetConflictsAsync()
    {
        var connection = await GetConnectionAsync();

        return await connection.Table<PendingOperation>()
            .Where(o => o.State == PendingState.Conflict)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<int> CountPendingAsync()
    {
        var connection = await GetConnectionAsync();

        return await connection.Table<PendingOperation>()
            .Where(o => o.State == PendingState.Pending)
            .CountAsync();
    }

    public async Task MarkSyncedAsync(PendingOperation operation)
    {
        var connection = await GetConnectionAsync();

        if (!string.IsNullOrWhiteSpace(operation.FilePath) && File.Exists(operation.FilePath))
        {
            try { File.Delete(operation.FilePath); } catch { }
        }

        await connection.DeleteAsync(operation);
    }

    public async Task MarkConflictAsync(PendingOperation operation, string error)
    {
        var connection = await GetConnectionAsync();
        operation.State = PendingState.Conflict;
        operation.LastError = error;
        await connection.UpdateAsync(operation);
    }

    public async Task MarkRetryAsync(PendingOperation operation, string error)
    {
        var connection = await GetConnectionAsync();
        operation.Attempts++;
        operation.LastError = error;
        await connection.UpdateAsync(operation);
    }

    public async Task DiscardAsync(int id)
    {
        var connection = await GetConnectionAsync();
        await connection.DeleteAsync<PendingOperation>(id);
    }
}
