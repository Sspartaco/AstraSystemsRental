using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Maintenance.Api.Domain;
using AstraSystemsRental.Maintenance.Api.Dtos;
using AstraSystemsRental.Maintenance.Api.Persistence;

namespace AstraSystemsRental.Maintenance.Api.Services;

public interface IWorkshopReservationService
{
    Task<OperationResult> GetPagedAsync(int pageNumber, int pageSize, long? fleetVehicleId, string? status, long? providerId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<OperationResult> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<OperationResult> CreateAsync(CreateWorkshopReservationRequest request, CancellationToken cancellationToken);
    Task<OperationResult> UpdateAsync(long id, UpdateWorkshopReservationRequest request, CancellationToken cancellationToken);
    Task<OperationResult> ChangeStatusAsync(long id, ChangeReservationStatusRequest request, CancellationToken cancellationToken);
    Task<OperationResult> AddPhotoAsync(long id, string fileName, string storagePath, string? contentType, long sizeBytes, string? notes, CancellationToken cancellationToken);
    Task<string?> GetPhotoStoragePathAsync(long reservationId, long photoId, CancellationToken cancellationToken);
    Task<OperationResult> GetProvidersAsync(CancellationToken cancellationToken);
    Task<OperationResult> CreateProviderAsync(CreateWorkshopProviderRequest request, CancellationToken cancellationToken);
}

public sealed class WorkshopReservationService(
    IWorkshopReservationRepository repository,
    IMileageReadingService mileageReadingService,
    IUsersApiClient usersApiClient,
    IMailApiClient mailApiClient,
    IMaintenanceContextGuard contextGuard,
    IAstraRequestContext requestContext,
    ILogger<WorkshopReservationService> logger) : IWorkshopReservationService
{
    private const string NodeKey = "workshop-reservations";

    public async Task<OperationResult> GetPagedAsync(int pageNumber, int pageSize, long? fleetVehicleId, string? status, long? providerId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var page = await repository.GetPagedAsync(requestContext.Owner, pageNumber, pageSize, fleetVehicleId, status, providerId, from, to, cancellationToken);
        var providers = await repository.GetProvidersAsync(requestContext.Owner, cancellationToken);
        var photos = await repository.GetPhotosForReservationsAsync(page.Items.Select(r => r.Id).ToList(), cancellationToken);

        var items = page.Items.Select(r => ToResponse(r, providers, photos)).ToList();
        return OperationResult.Ok(new { items, page.TotalCount, page.PageNumber, page.PageSize, page.TotalPages });
    }

    public async Task<OperationResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var reservation = await repository.GetOwnedAsync(id, requestContext.Owner, cancellationToken);
        if (reservation is null)
            return OperationResult.NotFound("Reservation not found.");

        var providers = await repository.GetProvidersAsync(requestContext.Owner, cancellationToken);
        var photos = await repository.GetPhotosAsync(id, cancellationToken);

        return OperationResult.Ok(ToResponse(reservation, providers, photos));
    }

    public async Task<OperationResult> CreateAsync(CreateWorkshopReservationRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard().MaxLength(request.Notes, 1000, "Notes");
        if (request.MileageAtReservation is { } mileage)
            guard.NotNegative(mileage, "MileageAtReservation");
        if (request.ExpectedEndAtUtc is { } expectedEnd && expectedEnd < request.ScheduledAtUtc)
            guard.Must(false, "ExpectedEndAtUtc must be after ScheduledAtUtc.");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var vehicleCheck = await contextGuard.EnsureVehicleAccessibleAsync(request.FleetVehicleId, cancellationToken);
        if (vehicleCheck is not null)
            return vehicleCheck;

        var owner = requestContext.Owner;

        if (request.ProviderId is { } providerId)
        {
            var provider = await repository.GetOwnedProviderAsync(providerId, owner, cancellationToken);
            if (provider is null)
                return OperationResult.NotFound("Provider not found.");
        }

        var existing = await repository.GetForVehicleAsync(request.FleetVehicleId, owner, cancellationToken);
        var conflict = WorkshopReservationOverlapValidator.FindConflict(existing, request.ScheduledAtUtc, request.ExpectedEndAtUtc, null);
        if (conflict is not null)
            return OperationResult.Conflict($"The vehicle already has an active reservation overlapping these dates (#{conflict.Id}).");

        var reservation = await usersApiClient.TryReserveQuotaAsync(NodeKey, cancellationToken);
        if (!reservation.Success)
        {
            return reservation.Unreachable
                ? OperationResult.Fail("UpstreamUnavailable", System.Net.HttpStatusCode.ServiceUnavailable)
                : OperationResult.Fail("QuotaExceeded", System.Net.HttpStatusCode.Conflict);
        }

        var now = DateTime.UtcNow;
        var entity = new WorkshopReservation
        {
            OwnerType = owner.OwnerType,
            OwnerId = owner.OwnerId,
            FleetVehicleId = request.FleetVehicleId,
            ProviderId = request.ProviderId,
            Status = WorkshopReservationStatus.Pending,
            ScheduledAtUtc = request.ScheduledAtUtc,
            ExpectedEndAtUtc = request.ExpectedEndAtUtc,
            MileageAtReservation = request.MileageAtReservation,
            IsWashOnly = request.IsWashOnly,
            Notes = request.Notes?.Trim(),
            ReservedByUserId = requestContext.UserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        try
        {
            await repository.AddAsync(entity, cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            await usersApiClient.ReleaseQuotaAsync(NodeKey, cancellationToken);
            return OperationResult.Fail("Could not save the reservation. Try again.");
        }

        if (request.MileageAtReservation is { } capturedMileage)
        {
            var readingResult = await mileageReadingService.RecordFromWorkshopReservationAsync(
                request.FleetVehicleId, capturedMileage, entity.Id, cancellationToken);

            if (!readingResult.Success)
                logger.LogWarning("Reservation {ReservationId} created but mileage reading was rejected: {Errors}", entity.Id, string.Join("; ", readingResult.Errors));
        }

        var providers = await repository.GetProvidersAsync(owner, cancellationToken);
        return OperationResult.Created(ToResponse(entity, providers, []));
    }

    public async Task<OperationResult> UpdateAsync(long id, UpdateWorkshopReservationRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard().MaxLength(request.Notes, 1000, "Notes");
        if (request.ExpectedEndAtUtc is { } expectedEnd && expectedEnd < request.ScheduledAtUtc)
            guard.Must(false, "ExpectedEndAtUtc must be after ScheduledAtUtc.");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var owner = requestContext.Owner;
        var entity = await repository.GetOwnedAsync(id, owner, cancellationToken);
        if (entity is null)
            return OperationResult.NotFound("Reservation not found.");

        if (!TryParseRowVersion(request.RowVersion, out var rowVersion) || !rowVersion.SequenceEqual(entity.RowVersion ?? []))
            return OperationResult.Fail("The reservation was modified by someone else. Reload and try again.", System.Net.HttpStatusCode.PreconditionFailed);

        var existing = await repository.GetForVehicleAsync(entity.FleetVehicleId, owner, cancellationToken);
        var conflict = WorkshopReservationOverlapValidator.FindConflict(existing, request.ScheduledAtUtc, request.ExpectedEndAtUtc, id);
        if (conflict is not null)
            return OperationResult.Conflict($"The vehicle already has an active reservation overlapping these dates (#{conflict.Id}).");

        entity.ProviderId = request.ProviderId;
        entity.ScheduledAtUtc = request.ScheduledAtUtc;
        entity.ExpectedEndAtUtc = request.ExpectedEndAtUtc;
        entity.IsWashOnly = request.IsWashOnly;
        entity.Notes = request.Notes?.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);

        var providers = await repository.GetProvidersAsync(owner, cancellationToken);
        var photos = await repository.GetPhotosAsync(id, cancellationToken);
        return OperationResult.Ok(ToResponse(entity, providers, photos));
    }

    public async Task<OperationResult> ChangeStatusAsync(long id, ChangeReservationStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<WorkshopReservationStatus>(request.NewStatus, ignoreCase: true, out var newStatus))
            return OperationResult.Fail("Invalid status.");

        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var owner = requestContext.Owner;
        var entity = await repository.GetOwnedAsync(id, owner, cancellationToken);
        if (entity is null)
            return OperationResult.NotFound("Reservation not found.");

        var wasActive = entity.IsActive;

        try
        {
            entity.TransitionTo(newStatus, DateTime.UtcNow);
        }
        catch (InvalidStatusTransitionException ex)
        {
            return OperationResult.Fail(ex.Message, System.Net.HttpStatusCode.Conflict);
        }

        await repository.SaveChangesAsync(cancellationToken);

        if (wasActive && !entity.IsActive)
            await usersApiClient.ReleaseQuotaAsync(NodeKey, cancellationToken);

        if (newStatus == WorkshopReservationStatus.Ready)
            await NotifyReadyAsync(entity, cancellationToken);

        var providers = await repository.GetProvidersAsync(owner, cancellationToken);
        var photos = await repository.GetPhotosAsync(id, cancellationToken);
        return OperationResult.Ok(ToResponse(entity, providers, photos));
    }

    public async Task<OperationResult> AddPhotoAsync(long id, string fileName, string storagePath, string? contentType, long sizeBytes, string? notes, CancellationToken cancellationToken)
    {
        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var entity = await repository.GetOwnedAsync(id, requestContext.Owner, cancellationToken);
        if (entity is null)
            return OperationResult.NotFound("Reservation not found.");

        var photo = new WorkshopReservationPhoto
        {
            WorkshopReservationId = id,
            Status = entity.Status,
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Notes = notes?.Trim(),
            UploadedByUserId = requestContext.UserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await repository.AddPhotoAsync(photo, cancellationToken);
        return OperationResult.Created(new ReservationPhotoResponse(photo.Id, photo.Status.ToString(), photo.FileName, photo.StoragePath, photo.Notes, photo.CreatedAtUtc));
    }

    public async Task<string?> GetPhotoStoragePathAsync(long reservationId, long photoId, CancellationToken cancellationToken)
    {
        var entity = await repository.GetOwnedAsync(reservationId, requestContext.Owner, cancellationToken);

        if (entity is null)
            return null;

        var photos = await repository.GetPhotosAsync(reservationId, cancellationToken);

        return photos.FirstOrDefault(p => p.Id == photoId)?.StoragePath;
    }

    public async Task<OperationResult> GetProvidersAsync(CancellationToken cancellationToken)
    {
        var providers = await repository.GetProvidersAsync(requestContext.Owner, cancellationToken);
        return OperationResult.Ok(providers.Select(ToProviderResponse));
    }

    public async Task<OperationResult> CreateProviderAsync(CreateWorkshopProviderRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var guard = new Guard()
            .NotEmpty(name, "Name")
            .MaxLength(name, 160, "Name")
            .MaxLength(request.DocumentNumber, 40, "DocumentNumber")
            .MaxLength(request.ContactPhone, 40, "ContactPhone")
            .MaxLength(request.Address, 250, "Address");

        if (!string.IsNullOrWhiteSpace(request.ContactEmail))
            guard.Email(request.ContactEmail, "ContactEmail");

        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var providerType = WorkshopProviderType.Company;
        if (!string.IsNullOrWhiteSpace(request.ProviderType) &&
            !Enum.TryParse(request.ProviderType, ignoreCase: true, out providerType))
        {
            return OperationResult.Fail("Invalid provider type. Use Individual or Company.");
        }

        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var owner = requestContext.Owner;
        var now = DateTime.UtcNow;
        var provider = new WorkshopProvider
        {
            OwnerType = owner.OwnerType,
            OwnerId = owner.OwnerId,
            ProviderType = providerType,
            Name = name,
            DocumentNumber = request.DocumentNumber?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            ContactEmail = request.ContactEmail?.Trim(),
            Address = request.Address?.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await repository.AddProviderAsync(provider, cancellationToken);
        return OperationResult.Created(ToProviderResponse(provider));
    }

    private async Task NotifyReadyAsync(WorkshopReservation reservation, CancellationToken cancellationToken)
    {
        var vehicle = await contextGuard.GetVehicleAsync(reservation.FleetVehicleId, cancellationToken);
        var plate = vehicle?.PlateNumber ?? reservation.FleetVehicleId.ToString();

        var email = requestContext.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogInformation("Reservation {ReservationId} is ready but no email is available for notification.", reservation.Id);
            return;
        }

        var result = await mailApiClient.SendReservationReadyAsync(email, plate, cancellationToken);
        if (!result.Success)
            logger.LogWarning("Could not notify reservation {ReservationId} ready: {Error}", reservation.Id, result.Error);
    }

    private static WorkshopReservationResponse ToResponse(
        WorkshopReservation reservation,
        IReadOnlyList<WorkshopProvider> providers,
        IReadOnlyList<WorkshopReservationPhoto> photos) => new()
    {
        Id = reservation.Id,
        FleetVehicleId = reservation.FleetVehicleId,
        ProviderId = reservation.ProviderId,
        ProviderName = providers.FirstOrDefault(p => p.Id == reservation.ProviderId)?.Name,
        Status = reservation.Status.ToString(),
        ScheduledAtUtc = reservation.ScheduledAtUtc,
        ExpectedEndAtUtc = reservation.ExpectedEndAtUtc,
        PickedUpAtUtc = reservation.PickedUpAtUtc,
        ReadyAtUtc = reservation.ReadyAtUtc,
        CollectedAtUtc = reservation.CollectedAtUtc,
        MileageAtReservation = reservation.MileageAtReservation,
        IsWashOnly = reservation.IsWashOnly,
        Notes = reservation.Notes,
        RowVersion = Convert.ToBase64String(reservation.RowVersion ?? []),
        CreatedAtUtc = reservation.CreatedAtUtc,
        Photos = photos
            .Where(p => p.WorkshopReservationId == reservation.Id)
            .Select(p => new ReservationPhotoResponse(p.Id, p.Status.ToString(), p.FileName, p.StoragePath, p.Notes, p.CreatedAtUtc))
            .ToList()
    };

    private static WorkshopProviderResponse ToProviderResponse(WorkshopProvider provider) => new()
    {
        Id = provider.Id,
        Name = provider.Name,
        ProviderType = provider.ProviderType.ToString(),
        DocumentNumber = provider.DocumentNumber,
        ContactPhone = provider.ContactPhone,
        ContactEmail = provider.ContactEmail,
        Address = provider.Address,
        IsActive = provider.IsActive
    };

    private static bool TryParseRowVersion(string value, out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }
}
