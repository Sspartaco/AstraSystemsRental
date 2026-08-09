namespace AstraSystemsRental.Base.Contracts;

public sealed record ApiResponse(bool Success, object? Data, IReadOnlyList<string> Errors, string? TraceId);
