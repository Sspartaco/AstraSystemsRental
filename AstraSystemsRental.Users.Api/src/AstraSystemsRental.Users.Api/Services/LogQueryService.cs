using System.Data;
using AstraSystemsRental.Base.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AstraSystemsRental.Users.Api.Services;

public sealed record LogEntryDto
{
    public long Id { get; init; }
    public DateTime TimestampUtc { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ExceptionType { get; init; }
    public string? ExceptionDetail { get; init; }
    public string? TraceId { get; init; }
    public string? RequestMethod { get; init; }
    public string? RequestPath { get; init; }
    public int? StatusCode { get; init; }
    public string? UserEmail { get; init; }
}

public interface ILogQueryService
{
    Task<OperationResult> GetLogsAsync(int page, int pageSize, string? level, string? service, string? search, CancellationToken cancellationToken);
    Task<OperationResult> GetServicesAsync(CancellationToken cancellationToken);
}

public sealed class LogQueryService(IConfiguration configuration) : ILogQueryService
{
    private string ConnectionString => configuration.GetConnectionString("Default") ?? string.Empty;

    public async Task<OperationResult> GetLogsAsync(
        int page, int pageSize, string? level, string? service, string? search, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(level))
            filters.Add("Level = @Level");

        if (!string.IsNullOrWhiteSpace(service))
            filters.Add("Service = @Service");

        if (!string.IsNullOrWhiteSpace(search))
            filters.Add("(Message LIKE @Search OR TraceId LIKE @Search OR RequestPath LIKE @Search)");

        var where = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = $"SELECT COUNT(*) FROM logs.ApplicationLogs {where}";
        AddFilters(countCommand, level, service, search);

        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT Id, TimestampUtc, Level, Service, Message, ExceptionType, ExceptionDetail,
                   TraceId, RequestMethod, RequestPath, StatusCode, UserEmail
            FROM logs.ApplicationLogs
            {where}
            ORDER BY Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        AddFilters(command, level, service, search);
        command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        command.Parameters.AddWithValue("@PageSize", pageSize);

        var items = new List<LogEntryDto>();

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new LogEntryDto
                {
                    Id = reader.GetInt64(0),
                    TimestampUtc = reader.GetDateTime(1),
                    Level = reader.GetString(2),
                    Service = reader.GetString(3),
                    Message = reader.GetString(4),
                    ExceptionType = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ExceptionDetail = reader.IsDBNull(6) ? null : reader.GetString(6),
                    TraceId = reader.IsDBNull(7) ? null : reader.GetString(7),
                    RequestMethod = reader.IsDBNull(8) ? null : reader.GetString(8),
                    RequestPath = reader.IsDBNull(9) ? null : reader.GetString(9),
                    StatusCode = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    UserEmail = reader.IsDBNull(11) ? null : reader.GetString(11)
                });
            }
        }

        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return OperationResult.Ok(new
        {
            items,
            totalCount,
            pageNumber = page,
            pageSize,
            totalPages
        });
    }

    public async Task<OperationResult> GetServicesAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT Service FROM logs.ApplicationLogs ORDER BY Service";

        var services = new List<string>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            services.Add(reader.GetString(0));

        return OperationResult.Ok(services);
    }

    private static void AddFilters(SqlCommand command, string? level, string? service, string? search)
    {
        if (!string.IsNullOrWhiteSpace(level))
            command.Parameters.Add("@Level", SqlDbType.VarChar, 16).Value = level;

        if (!string.IsNullOrWhiteSpace(service))
            command.Parameters.Add("@Service", SqlDbType.VarChar, 64).Value = service;

        if (!string.IsNullOrWhiteSpace(search))
            command.Parameters.Add("@Search", SqlDbType.NVarChar, 512).Value = $"%{search}%";
    }
}
