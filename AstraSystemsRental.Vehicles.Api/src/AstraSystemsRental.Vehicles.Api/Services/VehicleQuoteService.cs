using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Vehicles.Api.Domain;
using AstraSystemsRental.Vehicles.Api.Dtos;
using AstraSystemsRental.Vehicles.Api.Persistence;
using System.Text.RegularExpressions;

namespace AstraSystemsRental.Vehicles.Api.Services;

public sealed partial class VehicleQuoteService(
    IVehicleRepository vehicleRepository,
    IQuoteRepository quoteRepository,
    IQuoteOrchestrator orchestrator,
    IVehicleImageSource imageSource) : IVehicleQuoteService
{
    private static readonly TimeSpan ImageCacheTtl = TimeSpan.FromDays(30);

    // El catálogo real incluye maquinaria/equipo con placas numéricas ("1122", "121600"),
    // no solo placas estándar ABC123 — validación laxa a propósito.
    [GeneratedRegex("^[A-Z0-9]{3,10}$")]
    private static partial Regex PlateFormat();

    public async Task<OperationResult> GetVehicleAsync(string plateNumber, CancellationToken cancellationToken)
    {
        var plate = NormalizePlate(plateNumber);
        var vehicle = await vehicleRepository.GetByPlateAsync(plate, cancellationToken);
        if (vehicle is null)
            return OperationResult.NotFound("Vehicle not found for the given plate.");

        vehicle = await EnsureImageAsync(vehicle, cancellationToken);
        return OperationResult.Ok(ToResponse(vehicle));
    }

    public async Task<OperationResult> CreateVehicleAsync(CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var plate = NormalizePlate(request.PlateNumber);
        var guard = new Guard()
            .Must(PlateFormat().IsMatch(plate), "PlateNumber must be a valid Colombian plate (e.g. ABC123).")
            .NotEmpty(request.Brand, "Brand")
            .NotEmpty(request.Line, "Line");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var now = DateTime.UtcNow;
        var vehicle = new VehicleCatalog
        {
            PlateNumber = plate,
            VehicleClass = request.VehicleClass,
            Brand = request.Brand,
            Line = request.Line,
            ModelYear = request.ModelYear,
            FullLine = request.FullLine,
            Engine = request.Engine,
            IsUserAdded = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await vehicleRepository.UpsertBatchAsync([vehicle], cancellationToken);
        vehicle = await EnsureImageAsync(vehicle, cancellationToken);
        return OperationResult.Created(ToResponse(vehicle));
    }

    public async Task<OperationResult> CreateQuoteAsync(string plateNumber, long requestedByUserId, CancellationToken cancellationToken)
    {
        var plate = NormalizePlate(plateNumber);
        if (!PlateFormat().IsMatch(plate))
            return OperationResult.Fail("PlateNumber must be a valid Colombian plate (e.g. ABC123).");

        var vehicle = await vehicleRepository.GetByPlateAsync(plate, cancellationToken);
        if (vehicle is null)
            return OperationResult.NotFound("VehicleNotFound");

        var request = await quoteRepository.CreateQuoteRequestAsync(plate, requestedByUserId, cancellationToken);

        var query = new VehicleQuery(vehicle.PlateNumber, vehicle.Brand, vehicle.Line, vehicle.ModelYear, vehicle.Engine, vehicle.FullLine);
        orchestrator.Enqueue(request.Id, query);

        return OperationResult.Created(new { requestId = request.RequestId });
    }

    public async Task<OperationResult> GetQuoteStatusAsync(Guid requestId, long userId, CancellationToken cancellationToken)
    {
        var request = await quoteRepository.GetOwnedQuoteRequestAsync(requestId, userId, cancellationToken);
        if (request is null)
            return OperationResult.NotFound("Quote request not found.");

        var entries = await quoteRepository.GetCacheEntriesAsync(request.PlateNumber, cancellationToken);
        var activeSources = await quoteRepository.GetActiveSourcesAsync(cancellationToken);

        var sources = activeSources.Select(source =>
        {
            var entry = entries.FirstOrDefault(e => e.SourceCode == source.Code);
            return new QuoteSourceStatus(
                source.Code,
                entry?.Status.ToString() ?? ValuationStatus.Pending.ToString(),
                entry?.ValueMin,
                entry?.ValueMax,
                entry?.ValueAvg,
                entry?.Currency ?? "COP",
                entry?.FetchedAtUtc,
                entry?.ErrorMessage);
        }).ToList();

        var response = new QuoteStatusResponse(request.RequestId, request.PlateNumber, request.Status.ToString(), sources);
        return OperationResult.Ok(response);
    }

    private async Task<VehicleCatalog> EnsureImageAsync(VehicleCatalog vehicle, CancellationToken cancellationToken)
    {
        var isStale = vehicle.ImageFetchedAtUtc is null || DateTime.UtcNow - vehicle.ImageFetchedAtUtc > ImageCacheTtl;
        if (!isStale)
            return vehicle;

        var result = await imageSource.FindAsync(vehicle.Brand, vehicle.Line, vehicle.ModelYear, cancellationToken);
        await vehicleRepository.SetImageAsync(vehicle.PlateNumber, result.ImageUrl, result.Attribution, cancellationToken);

        vehicle.ImageUrl = result.ImageUrl;
        vehicle.ImageAttribution = result.Attribution;
        vehicle.ImageFetchedAtUtc = DateTime.UtcNow;
        return vehicle;
    }

    private static VehicleResponse ToResponse(VehicleCatalog vehicle) => new(
        vehicle.PlateNumber, vehicle.VehicleClass, vehicle.Brand, vehicle.Line,
        vehicle.ModelYear, vehicle.FullLine, vehicle.Engine, vehicle.IsUserAdded,
        vehicle.ImageUrl, vehicle.ImageAttribution);

    private static string NormalizePlate(string plateNumber) => plateNumber.Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");
}
