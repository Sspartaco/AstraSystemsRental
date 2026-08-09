using System.Net;

namespace AstraSystemsRental.Base.Contracts;

public sealed class OperationResult
{
    public bool Success { get; private init; }
    public object? Data { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];
    public string? TraceId { get; set; }
    public HttpStatusCode StatusCode { get; private init; } = HttpStatusCode.OK;

    public static OperationResult Ok(object? data = null) => new()
    {
        Success = true,
        Data = data,
        StatusCode = HttpStatusCode.OK
    };

    public static OperationResult Created(object? data = null) => new()
    {
        Success = true,
        Data = data,
        StatusCode = HttpStatusCode.Created
    };

    public static OperationResult NoContent() => new()
    {
        Success = true,
        StatusCode = HttpStatusCode.NoContent
    };

    public static OperationResult Fail(string error, HttpStatusCode statusCode = HttpStatusCode.BadRequest) => new()
    {
        Success = false,
        Errors = string.IsNullOrWhiteSpace(error) ? [] : [error],
        StatusCode = statusCode
    };

    public static OperationResult Fail(IEnumerable<string> errors, HttpStatusCode statusCode = HttpStatusCode.BadRequest) => new()
    {
        Success = false,
        Errors = errors.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray(),
        StatusCode = statusCode
    };

    public static OperationResult NotFound(string error = "Resource not found") =>
        Fail(error, HttpStatusCode.NotFound);

    public static OperationResult Conflict(string error) =>
        Fail(error, HttpStatusCode.Conflict);

    public static OperationResult Unauthorized(string error = "Unauthorized") =>
        Fail(error, HttpStatusCode.Unauthorized);

    public static OperationResult Forbidden(string error = "Forbidden") =>
        Fail(error, HttpStatusCode.Forbidden);
}
