using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Vehicles.Api.Domain;
using AstraSystemsRental.Vehicles.Api.Dtos;
using AstraSystemsRental.Vehicles.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AstraSystemsRental.Vehicles.Api.Services;

public sealed class FleetVehicleService(
    IFleetVehicleRepository repository,
    IUsersApiClient usersApiClient,
    IAstraRequestContext requestContext) : IFleetVehicleService
{
    private const string NodeKey = "vehicle-registry";
    private const string DemoPlanCode = "Demo";

    public async Task<OperationResult> GetPagedAsync(int pageNumber, int pageSize, string? status, string? search, CancellationToken cancellationToken)
    {
        var owner = requestContext.Owner;
        var page = await repository.GetPagedAsync(owner, pageNumber, pageSize, status, search, cancellationToken);
        var items = page.Items.Select(ToResponse).ToList();

        return OperationResult.Ok(new { items, page.TotalCount, page.PageNumber, page.PageSize, page.TotalPages });
    }

    public async Task<OperationResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var vehicle = await repository.GetOwnedAsync(id, requestContext.Owner, cancellationToken);
        return vehicle is null
            ? OperationResult.NotFound("Vehicle not found.")
            : OperationResult.Ok(ToResponse(vehicle));
    }

    private static readonly short MinModelYear = 1980;

    public async Task<OperationResult> CreateAsync(CreateFleetVehicleRequest request, CancellationToken cancellationToken)
    {
        var plate = NormalizePlate(request.PlateNumber);
        var guard = new Guard().NotEmpty(plate, "PlateNumber").MaxLength(plate, 10, "PlateNumber");
        ValidateOptionalFields(guard, request.ModelYear, request.PurchaseDate);
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var membershipCheck = await EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var owner = requestContext.Owner;

        if (await repository.PlateExistsForOwnerAsync(owner, plate, cancellationToken))
            return OperationResult.Conflict("A vehicle with this plate is already registered.");

        var reservation = await usersApiClient.TryReserveQuotaAsync(NodeKey, cancellationToken);
        if (!reservation.Success)
        {
            return reservation.Unreachable
                ? OperationResult.Fail("UpstreamUnavailable", System.Net.HttpStatusCode.ServiceUnavailable)
                : OperationResult.Fail("QuotaExceeded", System.Net.HttpStatusCode.Conflict);
        }

        var isDemo = await IsCurrentPlanDemoAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var vehicle = new FleetVehicle
        {
            OwnerType = owner.OwnerType,
            OwnerId = owner.OwnerId,
            PlateNumber = plate,
            VehicleClass = request.VehicleClass,
            Brand = request.Brand,
            Line = request.Line,
            ModelYear = request.ModelYear,
            Color = request.Color,
            Status = FleetVehicleStatus.Draft,
            CreatedByUserId = requestContext.UserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        if (!isDemo)
        {
            vehicle.BodyType = request.BodyType;
            vehicle.ServiceType = request.ServiceType;
            vehicle.FuelType = request.FuelType;
            vehicle.Transmission = request.Transmission;
            vehicle.Vin = request.Vin;
            vehicle.EngineNumber = request.EngineNumber;
            vehicle.SerialNumber = request.SerialNumber;
            vehicle.ChassisNumber = request.ChassisNumber;
            vehicle.TransitLicenseNumber = request.TransitLicenseNumber;
            vehicle.PurchaseInvoiceNumber = request.PurchaseInvoiceNumber;
            vehicle.PurchaseDate = request.PurchaseDate;
            vehicle.PurchaseValue = request.PurchaseValue;
            vehicle.Notes = request.Notes;
        }

        try
        {
            await repository.AddAsync(vehicle, cancellationToken);
        }
        catch (DbUpdateException)
        {
            var compensation = await usersApiClient.ReleaseQuotaAsync(NodeKey, cancellationToken);
            if (!compensation.Success)
                await repository.RecordPendingQuotaCompensationAsync(NodeKey, owner, compensation.Error, cancellationToken);

            return OperationResult.Fail("Could not save the vehicle. Try again.");
        }

        return OperationResult.Created(ToResponse(vehicle));
    }

    public async Task<OperationResult> UpdateAsync(long id, UpdateFleetVehicleRequest request, CancellationToken cancellationToken)
    {
        var guard = new Guard();
        ValidateOptionalFields(guard, request.ModelYear, request.PurchaseDate);
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var membershipCheck = await EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var vehicle = await repository.GetOwnedAsync(id, requestContext.Owner, cancellationToken);
        if (vehicle is null)
            return OperationResult.NotFound("Vehicle not found.");

        if (!TryParseRowVersion(request.RowVersion, out var rowVersion) || !rowVersion.SequenceEqual(vehicle.RowVersion ?? []))
            return OperationResult.Fail("The vehicle was modified by someone else. Reload and try again.", System.Net.HttpStatusCode.PreconditionFailed);

        var isDemo = await IsCurrentPlanDemoAsync(cancellationToken);

        vehicle.VehicleClass = request.VehicleClass;
        vehicle.Brand = request.Brand;
        vehicle.Line = request.Line;
        vehicle.ModelYear = request.ModelYear;
        vehicle.Color = request.Color;

        if (!isDemo)
        {
            vehicle.BodyType = request.BodyType;
            vehicle.ServiceType = request.ServiceType;
            vehicle.FuelType = request.FuelType;
            vehicle.Transmission = request.Transmission;
            vehicle.Vin = request.Vin;
            vehicle.EngineNumber = request.EngineNumber;
            vehicle.SerialNumber = request.SerialNumber;
            vehicle.ChassisNumber = request.ChassisNumber;
            vehicle.TransitLicenseNumber = request.TransitLicenseNumber;
            vehicle.PurchaseInvoiceNumber = request.PurchaseInvoiceNumber;
            vehicle.PurchaseDate = request.PurchaseDate;
            vehicle.PurchaseValue = request.PurchaseValue;
            vehicle.Notes = request.Notes;
        }

        vehicle.UpdatedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        return OperationResult.Ok(ToResponse(vehicle));
    }

    public async Task<OperationResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var membershipCheck = await EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var removed = await repository.RemoveOwnedAsync(id, requestContext.Owner, cancellationToken);
        if (!removed)
            return OperationResult.NotFound("Vehicle not found.");

        await usersApiClient.ReleaseQuotaAsync(NodeKey, cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> ChangeStatusAsync(long id, ChangeFleetVehicleStatusRequest request, CancellationToken cancellationToken)
    {
        var membershipCheck = await EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        if (!Enum.TryParse<FleetVehicleStatus>(request.NewStatus, ignoreCase: true, out var newStatus))
            return OperationResult.Fail("Invalid status.");

        var vehicle = await repository.GetOwnedAsync(id, requestContext.Owner, cancellationToken);
        if (vehicle is null)
            return OperationResult.NotFound("Vehicle not found.");

        var previousStatus = vehicle.Status;
        vehicle.Status = newStatus;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        await repository.AddStatusHistoryAsync(new FleetVehicleStatusHistory
        {
            FleetVehicleId = vehicle.Id,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Reason = request.Reason,
            ChangedByUserId = requestContext.UserId,
            ChangedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return OperationResult.Ok(ToResponse(vehicle));
    }

    public async Task<OperationResult> GetStatusHistoryAsync(long id, CancellationToken cancellationToken)
    {
        var history = await repository.GetStatusHistoryAsync(id, requestContext.Owner, cancellationToken);
        return OperationResult.Ok(history.Select(h => new FleetVehicleStatusHistoryResponse(
            h.PreviousStatus?.ToString(), h.NewStatus.ToString(), h.Reason, h.ChangedByUserId, h.ChangedAtUtc)));
    }

    public async Task<OperationResult> GetDocumentsAsync(long id, CancellationToken cancellationToken)
    {
        var documents = await repository.GetDocumentsAsync(id, requestContext.Owner, cancellationToken);
        return OperationResult.Ok(documents.Select(ToDocumentResponse));
    }

    public async Task<OperationResult> AddDocumentAsync(long id, CreateFleetVehicleDocumentRequest request, CancellationToken cancellationToken)
    {
        var membershipCheck = await EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        if (!FleetDocumentType.All.Contains(request.DocumentType))
            return OperationResult.Fail("Invalid document type.");

        var vehicle = await repository.GetOwnedAsync(id, requestContext.Owner, cancellationToken);
        if (vehicle is null)
            return OperationResult.NotFound("Vehicle not found.");

        var now = DateTime.UtcNow;
        var document = new FleetVehicleDocument
        {
            FleetVehicleId = id,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber,
            IssuedDate = request.IssuedDate,
            ExpiresDate = request.ExpiresDate,
            Status = ResolveDocumentStatus(request.ExpiresDate),
            Notes = request.Notes,
            CreatedByUserId = requestContext.UserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await repository.AddDocumentAsync(document, cancellationToken);
        return OperationResult.Created(ToDocumentResponse(document));
    }

    public async Task<OperationResult> UpdateDocumentAsync(long id, long documentId, UpdateFleetVehicleDocumentRequest request, CancellationToken cancellationToken)
    {
        var membershipCheck = await EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var document = await repository.GetOwnedDocumentAsync(documentId, id, requestContext.Owner, cancellationToken);
        if (document is null)
            return OperationResult.NotFound("Document not found.");

        document.DocumentNumber = request.DocumentNumber;
        document.IssuedDate = request.IssuedDate;
        document.ExpiresDate = request.ExpiresDate;
        document.Status = ResolveDocumentStatus(request.ExpiresDate);
        document.Notes = request.Notes;
        document.UpdatedAtUtc = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok(ToDocumentResponse(document));
    }

    private async Task<OperationResult?> EnsureCompanyMembershipAsync(CancellationToken cancellationToken)
    {
        var owner = requestContext.Owner;
        if (owner.OwnerType != AstraSystemsRental.Base.Security.OwnerType.Company)
            return null;

        var result = await usersApiClient.IsActiveCompanyMemberAsync(owner.OwnerId, requestContext.UserId, cancellationToken);
        if (result.Success)
            return null;

        return result.Unreachable
            ? OperationResult.Fail("UpstreamUnavailable", System.Net.HttpStatusCode.ServiceUnavailable)
            : OperationResult.Forbidden("CompanyContextForbidden");
    }

    private async Task<bool> IsCurrentPlanDemoAsync(CancellationToken cancellationToken)
    {
        var quota = await usersApiClient.GetQuotaAsync(NodeKey, cancellationToken);
        return string.Equals(quota?.PlanCode, DemoPlanCode, StringComparison.OrdinalIgnoreCase);
    }

    private static FleetDocumentStatus ResolveDocumentStatus(DateOnly? expiresDate)
    {
        if (expiresDate is null)
            return FleetDocumentStatus.Pending;

        return expiresDate.Value < DateOnly.FromDateTime(DateTime.UtcNow) ? FleetDocumentStatus.Expired : FleetDocumentStatus.Valid;
    }

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

    private static string NormalizePlate(string plateNumber) => plateNumber.Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");

    private static void ValidateOptionalFields(Guard guard, short? modelYear, DateOnly? purchaseDate)
    {
        if (modelYear is { } year)
            guard.Range(year, MinModelYear, (short)(DateTime.UtcNow.Year + 1), "ModelYear");

        if (purchaseDate is { } date)
            guard.NotInFuture(date, "PurchaseDate");
    }

    private static FleetVehicleResponse ToResponse(FleetVehicle v) => new()
    {
        Id = v.Id,
        PlateNumber = v.PlateNumber,
        VehicleClass = v.VehicleClass,
        Brand = v.Brand,
        Line = v.Line,
        ModelYear = v.ModelYear,
        BodyType = v.BodyType,
        ServiceType = v.ServiceType,
        FuelType = v.FuelType,
        Transmission = v.Transmission,
        Color = v.Color,
        Vin = v.Vin,
        EngineNumber = v.EngineNumber,
        SerialNumber = v.SerialNumber,
        ChassisNumber = v.ChassisNumber,
        TransitLicenseNumber = v.TransitLicenseNumber,
        PurchaseInvoiceNumber = v.PurchaseInvoiceNumber,
        PurchaseDate = v.PurchaseDate,
        PurchaseValue = v.PurchaseValue,
        Status = v.Status.ToString(),
        Notes = v.Notes,
        RowVersion = Convert.ToBase64String(v.RowVersion ?? []),
        CreatedAtUtc = v.CreatedAtUtc,
        UpdatedAtUtc = v.UpdatedAtUtc
    };

    private static FleetVehicleDocumentResponse ToDocumentResponse(FleetVehicleDocument d) => new(
        d.Id, d.DocumentType, d.DocumentNumber, d.IssuedDate, d.ExpiresDate, d.Status.ToString(), d.Notes);
}
