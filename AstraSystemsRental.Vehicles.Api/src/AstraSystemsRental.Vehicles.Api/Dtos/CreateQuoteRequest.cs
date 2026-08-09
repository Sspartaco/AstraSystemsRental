namespace AstraSystemsRental.Vehicles.Api.Dtos;

public sealed record CreateQuoteRequest(string PlateNumber);

public sealed record CreateVehicleRequest(
    string PlateNumber,
    string? VehicleClass,
    string Brand,
    string Line,
    short? ModelYear,
    string? FullLine,
    string? Engine);

public sealed record QuoteStatusResponse(
    Guid RequestId,
    string PlateNumber,
    string Status,
    IReadOnlyList<QuoteSourceStatus> Sources);

public sealed record QuoteSourceStatus(
    string SourceCode,
    string Status,
    decimal? ValueMin,
    decimal? ValueMax,
    decimal? ValueAvg,
    string Currency,
    DateTime? FetchedAtUtc,
    string? ErrorMessage);

public sealed record VehicleResponse(
    string PlateNumber,
    string? VehicleClass,
    string? Brand,
    string? Line,
    short? ModelYear,
    string? FullLine,
    string? Engine,
    bool IsUserAdded,
    string? ImageUrl,
    string? ImageAttribution);
