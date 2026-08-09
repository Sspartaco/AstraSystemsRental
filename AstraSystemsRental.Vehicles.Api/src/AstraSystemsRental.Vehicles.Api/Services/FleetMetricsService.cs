using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Vehicles.Api.Domain;
using AstraSystemsRental.Vehicles.Api.Dtos;
using AstraSystemsRental.Vehicles.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Vehicles.Api.Services;

public sealed class FleetMetricsService(
    AstraVehiclesDbContext dbContext,
    IAstraRequestContext requestContext) : IFleetMetricsService
{
    private const int ExpiringWindowDays = 45;

    public async Task<OperationResult> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var owner = requestContext.Owner;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var vehicles = await dbContext.FleetVehicles
            .AsNoTracking()
            .Where(v => v.OwnerType == owner.OwnerType && v.OwnerId == owner.OwnerId)
            .Select(v => new { v.Id, v.PlateNumber, v.Status, v.ModelYear, v.PurchaseValue })
            .ToListAsync(cancellationToken);

        var vehicleIds = vehicles.Select(v => v.Id).ToList();

        var documents = await dbContext.FleetVehicleDocuments
            .AsNoTracking()
            .Where(d => vehicleIds.Contains(d.FleetVehicleId) && d.ExpiresDate != null)
            .Select(d => new { d.FleetVehicleId, d.DocumentType, d.ExpiresDate })
            .ToListAsync(cancellationToken);

        var plateById = vehicles.ToDictionary(v => v.Id, v => v.PlateNumber);

        var expiringSoon = documents
            .Where(d => d.ExpiresDate!.Value <= today.AddDays(ExpiringWindowDays))
            .Select(d => new ExpiringDocumentDto
            {
                FleetVehicleId = d.FleetVehicleId,
                PlateNumber = plateById.TryGetValue(d.FleetVehicleId, out var plate) ? plate : string.Empty,
                DocumentType = d.DocumentType,
                ExpiresDate = d.ExpiresDate,
                DaysRemaining = d.ExpiresDate!.Value.DayNumber - today.DayNumber
            })
            .OrderBy(d => d.DaysRemaining)
            .ToList();

        var currentYear = today.Year;

        var response = new FleetMetricsResponse
        {
            TotalVehicles = vehicles.Count,
            ActiveVehicles = vehicles.Count(v => v.Status == FleetVehicleStatus.Active),
            InMaintenance = vehicles.Count(v => v.Status == FleetVehicleStatus.Maintenance),
            BlockedVehicles = vehicles.Count(v => v.Status == FleetVehicleStatus.Blocked),
            TotalPurchaseValue = vehicles.Sum(v => v.PurchaseValue ?? 0m),
            AverageAgeYears = vehicles.Count(v => v.ModelYear.HasValue) == 0
                ? 0
                : Math.Round(vehicles.Where(v => v.ModelYear.HasValue)
                    .Average(v => currentYear - v.ModelYear!.Value), 1),
            ExpiredDocuments = expiringSoon.Count(d => d.DaysRemaining < 0),
            ByStatus = vehicles
                .GroupBy(v => v.Status)
                .Select(g => new FleetStatusSliceDto { Status = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .ToList(),
            ByAge = BuildAgeBuckets(vehicles.Where(v => v.ModelYear.HasValue).Select(v => currentYear - v.ModelYear!.Value)),
            ExpiringSoon = expiringSoon.Take(10).ToList()
        };

        return OperationResult.Ok(response);
    }

    private static List<FleetAgeSliceDto> BuildAgeBuckets(IEnumerable<int> ages)
    {
        var buckets = new[] { "0-2 años", "3-5 años", "6-10 años", "Más de 10 años" };
        var counts = new int[buckets.Length];

        foreach (var age in ages)
        {
            var index = age switch
            {
                <= 2 => 0,
                <= 5 => 1,
                <= 10 => 2,
                _ => 3
            };
            counts[index]++;
        }

        return buckets
            .Select((label, i) => new FleetAgeSliceDto { Bucket = label, Count = counts[i] })
            .ToList();
    }
}
