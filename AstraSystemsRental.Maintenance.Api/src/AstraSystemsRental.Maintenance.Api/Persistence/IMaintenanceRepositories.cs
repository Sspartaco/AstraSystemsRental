using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Maintenance.Api.Domain;

namespace AstraSystemsRental.Maintenance.Api.Persistence;

public interface IRoutineRepository
{
    Task<PagedResult<MaintenanceRoutine>> GetPagedAsync(OwnerContext owner, int pageNumber, int pageSize, string? search, bool? isActive, CancellationToken cancellationToken);
    Task<MaintenanceRoutine?> GetOwnedAsync(long id, OwnerContext owner, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(OwnerContext owner, string name, long? excludeId, CancellationToken cancellationToken);
    Task<int> CountActiveAsync(OwnerContext owner, CancellationToken cancellationToken);
    Task AddAsync(MaintenanceRoutine routine, CancellationToken cancellationToken);
    Task<bool> RemoveOwnedAsync(long id, OwnerContext owner, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenanceRoutinePeriodicity>> GetPeriodicitiesAsync(long routineId, CancellationToken cancellationToken);
    Task<MaintenanceRoutinePeriodicity?> GetPeriodicityAsync(long periodicityId, long routineId, CancellationToken cancellationToken);
    Task AddPeriodicityAsync(MaintenanceRoutinePeriodicity periodicity, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceRoutineConcept>> GetConceptsAsync(IReadOnlyList<long> periodicityIds, CancellationToken cancellationToken);
    Task AddConceptAsync(MaintenanceRoutineConcept concept, CancellationToken cancellationToken);
    Task<MaintenanceRoutinePeriodicity?> GetPeriodicityForUnitAsync(long routineId, MeasurementUnit unit, CancellationToken cancellationToken);
}

public interface IRoutineAssignmentRepository
{
    Task<RoutineAssignment?> GetForVehicleAsync(long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken);
    Task AddAsync(RoutineAssignment assignment, CancellationToken cancellationToken);
    Task AddHistoryAsync(RoutineAssignmentHistory history, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoutineAssignmentHistory>> GetHistoryAsync(long fleetVehicleId, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IMileageReadingRepository
{
    Task<PagedResult<MileageReading>> GetPagedAsync(long fleetVehicleId, OwnerContext owner, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<MileageReading>> GetAllForVehicleAsync(long fleetVehicleId, OwnerContext owner, ReadingType readingType, CancellationToken cancellationToken);
    Task<MileageReading?> GetOwnedAsync(long id, long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(long fleetVehicleId, OwnerContext owner, DateOnly readingDate, int value, CancellationToken cancellationToken);
    Task AddAsync(MileageReading reading, CancellationToken cancellationToken);
    Task<bool> RemoveOwnedAsync(long id, long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IWorkshopReservationRepository
{
    Task<PagedResult<WorkshopReservation>> GetPagedAsync(OwnerContext owner, int pageNumber, int pageSize, long? fleetVehicleId, string? status, long? providerId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<WorkshopReservation?> GetOwnedAsync(long id, OwnerContext owner, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkshopReservation>> GetForVehicleAsync(long fleetVehicleId, OwnerContext owner, CancellationToken cancellationToken);
    Task<int> CountActiveAsync(OwnerContext owner, CancellationToken cancellationToken);
    Task AddAsync(WorkshopReservation reservation, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopReservationPhoto>> GetPhotosAsync(long reservationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkshopReservationPhoto>> GetPhotosForReservationsAsync(IReadOnlyList<long> reservationIds, CancellationToken cancellationToken);
    Task AddPhotoAsync(WorkshopReservationPhoto photo, CancellationToken cancellationToken);

    Task<WorkshopProvider?> GetOwnedProviderAsync(long id, OwnerContext owner, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkshopProvider>> GetProvidersAsync(OwnerContext owner, CancellationToken cancellationToken);
    Task AddProviderAsync(WorkshopProvider provider, CancellationToken cancellationToken);
}
