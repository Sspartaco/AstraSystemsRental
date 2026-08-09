using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AstraSystemsRental.Base.Diagnostics;

public sealed record ApplicationLogEntry
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string Level { get; init; } = "Error";
    public string Service { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ExceptionType { get; init; }
    public string? ExceptionDetail { get; init; }
    public string? TraceId { get; init; }
    public string? RequestMethod { get; init; }
    public string? RequestPath { get; init; }
    public int? StatusCode { get; init; }
    public long? UserId { get; init; }
    public string? UserEmail { get; init; }
}

public interface IApplicationLogWriter
{
    void Enqueue(ApplicationLogEntry entry);
}

public sealed class ApplicationLogWriter(
    ApplicationLogOptions options,
    ILogger<ApplicationLogWriter> logger) : BackgroundService, IApplicationLogWriter
{
    private const int MaxQueued = 500;

    private readonly ConcurrentQueue<ApplicationLogEntry> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public void Enqueue(ApplicationLogEntry entry)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ConnectionString))
            return;

        if (_queue.Count >= MaxQueued)
            return;

        _queue.Enqueue(entry);
        _signal.Release();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ConnectionString))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!_queue.TryDequeue(out var entry))
                continue;

            await WriteAsync(entry, stoppingToken);
        }
    }

    private async Task WriteAsync(ApplicationLogEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO logs.ApplicationLogs
                    (TimestampUtc, Level, Service, Message, ExceptionType, ExceptionDetail,
                     TraceId, RequestMethod, RequestPath, StatusCode, UserId, UserEmail)
                VALUES
                    (@TimestampUtc, @Level, @Service, @Message, @ExceptionType, @ExceptionDetail,
                     @TraceId, @RequestMethod, @RequestPath, @StatusCode, @UserId, @UserEmail);
                """;

            command.Parameters.AddWithValue("@TimestampUtc", entry.TimestampUtc);
            command.Parameters.AddWithValue("@Level", Truncate(entry.Level, 16));
            command.Parameters.AddWithValue("@Service", Truncate(entry.Service, 64));
            command.Parameters.AddWithValue("@Message", Truncate(entry.Message, 2000));
            command.Parameters.AddWithValue("@ExceptionType", Value(Truncate(entry.ExceptionType, 256)));
            command.Parameters.AddWithValue("@ExceptionDetail", Value(entry.ExceptionDetail));
            command.Parameters.AddWithValue("@TraceId", Value(Truncate(entry.TraceId, 64)));
            command.Parameters.AddWithValue("@RequestMethod", Value(Truncate(entry.RequestMethod, 10)));
            command.Parameters.AddWithValue("@RequestPath", Value(Truncate(entry.RequestPath, 512)));
            command.Parameters.AddWithValue("@StatusCode", Value(entry.StatusCode));
            command.Parameters.AddWithValue("@UserId", Value(entry.UserId));
            command.Parameters.AddWithValue("@UserEmail", Value(Truncate(entry.UserEmail, 256)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not persist application log entry");
        }
    }

    private static object Value(object? value) => value ?? DBNull.Value;

    private static string? Truncate(string? value, int max)
        => value is null || value.Length <= max ? value : value[..max];
}
