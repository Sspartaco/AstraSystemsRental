namespace AstraSystemsRental.Base.Http;

public sealed record CrossApiResult(bool Success, bool Unreachable, string? Error)
{
    public static CrossApiResult Ok() => new(true, false, null);

    public static CrossApiResult Denied(string error) => new(false, false, error);

    public static CrossApiResult Unavailable(string error) => new(false, true, error);
}
