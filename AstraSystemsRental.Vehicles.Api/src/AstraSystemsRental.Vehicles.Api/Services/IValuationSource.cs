using AstraSystemsRental.Vehicles.Api.Domain;

namespace AstraSystemsRental.Vehicles.Api.Services;

public sealed record VehicleQuery(string PlateNumber, string? Brand, string? Line, short? ModelYear, string? Engine, string? FullLine);

public sealed record ValuationSourceResult(
    ValuationStatus Status,
    decimal? ValueMin,
    decimal? ValueMax,
    decimal? ValueAvg,
    string? RawPayload,
    string? ErrorMessage);

public interface IValuationSource
{
    string SourceCode { get; }
    Task<ValuationSourceResult> FetchAsync(VehicleQuery query, CancellationToken cancellationToken);
}
