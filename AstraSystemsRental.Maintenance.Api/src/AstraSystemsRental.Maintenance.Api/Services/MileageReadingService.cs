using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Maintenance.Api.Configuration;
using AstraSystemsRental.Maintenance.Api.Domain;
using AstraSystemsRental.Maintenance.Api.Dtos;
using AstraSystemsRental.Maintenance.Api.Persistence;
using Microsoft.Extensions.Options;

namespace AstraSystemsRental.Maintenance.Api.Services;

public interface IMileageReadingService
{
    Task<OperationResult> GetPagedAsync(long fleetVehicleId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<OperationResult> AddAsync(long fleetVehicleId, CreateMileageReadingRequest request, CancellationToken cancellationToken);
    Task<OperationResult> DeleteAsync(long fleetVehicleId, long readingId, CancellationToken cancellationToken);
    Task<OperationResult> GetNextMaintenanceAsync(long fleetVehicleId, CancellationToken cancellationToken);
    Task<OperationResult> RecordFromWorkshopReservationAsync(long fleetVehicleId, int value, long reservationId, CancellationToken cancellationToken);
}

public sealed class MileageReadingService(
    IMileageReadingRepository repository,
    IRoutineAssignmentRepository assignmentRepository,
    IRoutineRepository routineRepository,
    IMaintenanceContextGuard contextGuard,
    IAstraRequestContext requestContext,
    IOptions<MaintenanceOptions> options) : IMileageReadingService
{
    private readonly MaintenanceOptions _options = options.Value;

    public async Task<OperationResult> GetPagedAsync(long fleetVehicleId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var page = await repository.GetPagedAsync(fleetVehicleId, requestContext.Owner, pageNumber, pageSize, cancellationToken);
        var items = page.Items.Select(ToResponse).ToList();

        return OperationResult.Ok(new { items, page.TotalCount, page.PageNumber, page.PageSize, page.TotalPages });
    }

    public async Task<OperationResult> AddAsync(long fleetVehicleId, CreateMileageReadingRequest request, CancellationToken cancellationToken)
    {
        var readingType = ReadingType.Kilometers;
        if (!string.IsNullOrWhiteSpace(request.ReadingType) &&
            !Enum.TryParse(request.ReadingType, ignoreCase: true, out readingType))
        {
            return OperationResult.Fail("Invalid reading type. Use Kilometers or Hours.");
        }

        var absoluteMaximum = readingType == ReadingType.Hours ? _options.MaxHours : _options.MaxKilometers;

        var guard = new Guard()
            .NotNegative(request.Value, "Value")
            .Range(request.Value, 0, absoluteMaximum, "Value")
            .NotInFuture(request.ReadingDate, "ReadingDate")
            .MaxLength(request.Notes, 400, "Notes");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var vehicleCheck = await contextGuard.EnsureVehicleAccessibleAsync(fleetVehicleId, cancellationToken);
        if (vehicleCheck is not null)
            return vehicleCheck;

        var owner = requestContext.Owner;

        if (await repository.ExistsAsync(fleetVehicleId, owner, request.ReadingDate, request.Value, cancellationToken))
            return OperationResult.Conflict("A reading with the same date and value already exists.");

        var existingReadings = await repository.GetAllForVehicleAsync(fleetVehicleId, owner, readingType, cancellationToken);
        var validation = MileageMonotonicityValidator.Validate(
            request.Value, request.ReadingDate, existingReadings, absoluteMaximum, _options.DailyProjectionKm);

        if (!validation.IsValid)
            return OperationResult.Fail(validation.Error ?? "The reading is not consistent with the vehicle history.");

        var reading = new MileageReading
        {
            OwnerType = owner.OwnerType,
            OwnerId = owner.OwnerId,
            FleetVehicleId = fleetVehicleId,
            ReadingType = readingType,
            ReadingDate = request.ReadingDate,
            Value = request.Value,
            Source = MileageReadingSource.Manual,
            Notes = request.Notes?.Trim(),
            RecordedByUserId = requestContext.UserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await repository.AddAsync(reading, cancellationToken);
        return OperationResult.Created(ToResponse(reading));
    }

    public async Task<OperationResult> RecordFromWorkshopReservationAsync(long fleetVehicleId, int value, long reservationId, CancellationToken cancellationToken)
    {
        var owner = requestContext.Owner;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (await repository.ExistsAsync(fleetVehicleId, owner, today, value, cancellationToken))
            return OperationResult.Ok();

        var existingReadings = await repository.GetAllForVehicleAsync(fleetVehicleId, owner, ReadingType.Kilometers, cancellationToken);
        var validation = MileageMonotonicityValidator.Validate(
            value, today, existingReadings, _options.MaxKilometers, _options.DailyProjectionKm);

        if (!validation.IsValid)
            return OperationResult.Fail(validation.Error ?? "The mileage is not consistent with the vehicle history.");

        var reading = new MileageReading
        {
            OwnerType = owner.OwnerType,
            OwnerId = owner.OwnerId,
            FleetVehicleId = fleetVehicleId,
            ReadingType = ReadingType.Kilometers,
            ReadingDate = today,
            Value = value,
            Source = MileageReadingSource.Workshop,
            SourceReservationId = reservationId,
            RecordedByUserId = requestContext.UserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await repository.AddAsync(reading, cancellationToken);
        return OperationResult.Created(ToResponse(reading));
    }

    public async Task<OperationResult> DeleteAsync(long fleetVehicleId, long readingId, CancellationToken cancellationToken)
    {
        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var removed = await repository.RemoveOwnedAsync(readingId, fleetVehicleId, requestContext.Owner, cancellationToken);
        return removed ? OperationResult.Ok() : OperationResult.NotFound("Reading not found.");
    }

    public async Task<OperationResult> GetNextMaintenanceAsync(long fleetVehicleId, CancellationToken cancellationToken)
    {
        var owner = requestContext.Owner;
        var assignment = await assignmentRepository.GetForVehicleAsync(fleetVehicleId, owner, cancellationToken);

        if (assignment is null)
            return OperationResult.Ok(new NextMaintenanceResponse { HasRoutine = false });

        var routine = await routineRepository.GetOwnedAsync(assignment.RoutineId, owner, cancellationToken);
        var readings = await repository.GetAllForVehicleAsync(fleetVehicleId, owner, ReadingType.Kilometers, cancellationToken);
        var latest = readings.OrderByDescending(r => r.ReadingDate).ThenByDescending(r => r.Value).FirstOrDefault();

        if (latest is null)
        {
            return OperationResult.Ok(new NextMaintenanceResponse
            {
                HasRoutine = true,
                RoutineName = routine?.Name
            });
        }

        var periodicity = await routineRepository.GetPeriodicityForUnitAsync(assignment.RoutineId, MeasurementUnit.Kilometers, cancellationToken);
        var projection = NextMaintenanceProjector.Project(periodicity, latest.Value);

        if (projection is null)
        {
            return OperationResult.Ok(new NextMaintenanceResponse
            {
                HasRoutine = true,
                RoutineName = routine?.Name,
                CurrentValue = latest.Value,
                LastReadingDate = latest.ReadingDate
            });
        }

        return OperationResult.Ok(new NextMaintenanceResponse
        {
            HasRoutine = true,
            RoutineName = routine?.Name,
            Unit = projection.Unit.ToString(),
            CurrentValue = projection.CurrentValue,
            NextThreshold = projection.NextThreshold,
            Remaining = projection.Remaining,
            Overdue = projection.Overdue,
            IsOverdue = projection.IsOverdue,
            LastReadingDate = latest.ReadingDate
        });
    }

    private static MileageReadingResponse ToResponse(MileageReading reading) => new()
    {
        Id = reading.Id,
        FleetVehicleId = reading.FleetVehicleId,
        ReadingType = reading.ReadingType.ToString(),
        ReadingDate = reading.ReadingDate,
        Value = reading.Value,
        Source = reading.Source,
        SourceReservationId = reading.SourceReservationId,
        Notes = reading.Notes,
        CreatedAtUtc = reading.CreatedAtUtc
    };
}
