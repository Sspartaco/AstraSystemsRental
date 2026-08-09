namespace AstraSystemsRental.Contracts;

public sealed record ApiEnvelope<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public string? TraceId { get; init; }
}

public sealed record ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }

    public bool Unauthorized => StatusCode == 401;
    public bool Offline => StatusCode == 0;

    public static ApiResult<T> Ok(T? data, int statusCode = 200)
        => new() { Success = true, Data = data, StatusCode = statusCode };

    public static ApiResult<T> Fail(string? error, int statusCode)
        => new() { Success = false, Error = error, StatusCode = statusCode };

    public static ApiResult<T> NoConnection()
        => new() { Success = false, Error = "Sin conexión.", StatusCode = 0 };
}
