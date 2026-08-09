using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Maintenance.Api.Domain;
using AstraSystemsRental.Maintenance.Api.Dtos;
using AstraSystemsRental.Maintenance.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Maintenance.Api.Services;

public sealed class MaintenanceMetricsService(
    AstraMaintenanceDbContext dbContext,
    IAstraRequestContext requestContext) : IMaintenanceMetricsService
{
    public async Task<OperationResult> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var owner = requestContext.Owner;
        var nowUtc = DateTime.UtcNow;
        var todayStart = nowUtc.Date;
        var last30 = nowUtc.AddDays(-30);

        var reservations = await dbContext.WorkshopReservations
            .AsNoTracking()
            .Where(r => r.OwnerType == owner.OwnerType && r.OwnerId == owner.OwnerId)
            .Select(r => new
            {
                r.Id,
                r.FleetVehicleId,
                r.ProviderId,
                r.Status,
                r.ScheduledAtUtc,
                r.PickedUpAtUtc,
                r.CollectedAtUtc,
                r.IsWashOnly
            })
            .ToListAsync(cancellationToken);

        var providers = await dbContext.WorkshopProviders
            .AsNoTracking()
            .Where(p => p.OwnerType == owner.OwnerType && p.OwnerId == owner.OwnerId)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);

        var providerNameById = providers.ToDictionary(p => p.Id, p => p.Name);

        var activeStatuses = new[]
        {
            WorkshopReservationStatus.Pending,
            WorkshopReservationStatus.InWorkshop,
            WorkshopReservationStatus.Ready
        };

        var completed = reservations
            .Where(r => r.Status == WorkshopReservationStatus.Collected
                        && r.CollectedAtUtc >= last30
                        && r.PickedUpAtUtc != null)
            .ToList();

        var response = new MaintenanceMetricsResponse
        {
            ActiveReservations = reservations.Count(r => activeStatuses.Contains(r.Status)),
            ReservationsToday = reservations.Count(r =>
                r.ScheduledAtUtc >= todayStart && r.ScheduledAtUtc < todayStart.AddDays(1)),
            ReservationsNext7Days = reservations.Count(r =>
                r.ScheduledAtUtc >= todayStart && r.ScheduledAtUtc < todayStart.AddDays(7)),
            VehiclesInWorkshop = reservations
                .Where(r => r.Status == WorkshopReservationStatus.InWorkshop)
                .Select(r => r.FleetVehicleId)
                .Distinct()
                .Count(),
            VehiclesWithRoutine = await dbContext.RoutineAssignments
                .AsNoTracking()
                .CountAsync(a => a.OwnerType == owner.OwnerType && a.OwnerId == owner.OwnerId, cancellationToken),
            ReadingsLast30Days = await dbContext.MileageReadings
                .AsNoTracking()
                .CountAsync(m => m.OwnerType == owner.OwnerType
                                 && m.OwnerId == owner.OwnerId
                                 && m.CreatedAtUtc >= last30, cancellationToken),
            CompletedLast30Days = completed.Count,
            AverageWorkshopDays = completed.Count == 0
                ? 0
                : Math.Round(completed.Average(r => (r.CollectedAtUtc!.Value - r.PickedUpAtUtc!.Value).TotalDays), 1),
            ByStatus = reservations
                .GroupBy(r => r.Status)
                .Select(g => new ReservationStatusSliceDto { Status = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .ToList(),
            Upcoming = reservations
                .Where(r => activeStatuses.Contains(r.Status) && r.ScheduledAtUtc >= todayStart)
                .OrderBy(r => r.ScheduledAtUtc)
                .Take(8)
                .Select(r => new UpcomingReservationDto
                {
                    Id = r.Id,
                    FleetVehicleId = r.FleetVehicleId,
                    Status = r.Status.ToString(),
                    ScheduledAtUtc = r.ScheduledAtUtc,
                    ProviderName = r.ProviderId.HasValue && providerNameById.TryGetValue(r.ProviderId.Value, out var name)
                        ? name
                        : null,
                    IsWashOnly = r.IsWashOnly
                })
                .ToList(),
            ByWorkshop = reservations
                .Where(r => r.ProviderId.HasValue)
                .GroupBy(r => r.ProviderId!.Value)
                .Select(g => new WorkshopLoadSliceDto
                {
                    ProviderName = providerNameById.TryGetValue(g.Key, out var name) ? name : $"#{g.Key}",
                    Count = g.Count()
                })
                .OrderByDescending(w => w.Count)
                .Take(6)
                .ToList()
        };

        return OperationResult.Ok(response);
    }
}
