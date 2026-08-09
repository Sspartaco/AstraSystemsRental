using AstraSystemsRental.Base.Contracts;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Base.Validation;
using AstraSystemsRental.Maintenance.Api.Domain;
using AstraSystemsRental.Maintenance.Api.Dtos;
using AstraSystemsRental.Maintenance.Api.Persistence;

namespace AstraSystemsRental.Maintenance.Api.Services;

public interface IRoutineService
{
    Task<OperationResult> GetPagedAsync(int pageNumber, int pageSize, string? search, bool? isActive, CancellationToken cancellationToken);
    Task<OperationResult> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<OperationResult> CreateAsync(CreateRoutineRequest request, CancellationToken cancellationToken);
    Task<OperationResult> UpdateAsync(long id, UpdateRoutineRequest request, CancellationToken cancellationToken);
    Task<OperationResult> DeleteAsync(long id, CancellationToken cancellationToken);
    Task<OperationResult> CopyAsync(long id, CopyRoutineRequest request, CancellationToken cancellationToken);
    Task<OperationResult> AddPeriodicityAsync(long routineId, CreatePeriodicityRequest request, CancellationToken cancellationToken);
    Task<OperationResult> AddConceptAsync(long routineId, CreateConceptRequest request, CancellationToken cancellationToken);
}

public sealed class RoutineService(
    IRoutineRepository repository,
    IUsersApiClient usersApiClient,
    IMaintenanceContextGuard contextGuard,
    IAstraRequestContext requestContext) : IRoutineService
{
    private const string NodeKey = "maintenance-routines";

    public async Task<OperationResult> GetPagedAsync(int pageNumber, int pageSize, string? search, bool? isActive, CancellationToken cancellationToken)
    {
        var page = await repository.GetPagedAsync(requestContext.Owner, pageNumber, pageSize, search, isActive, cancellationToken);
        var items = new List<RoutineResponse>();

        foreach (var routine in page.Items)
            items.Add(await BuildResponseAsync(routine, includeChildren: false, cancellationToken));

        return OperationResult.Ok(new { items, page.TotalCount, page.PageNumber, page.PageSize, page.TotalPages });
    }

    public async Task<OperationResult> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var routine = await repository.GetOwnedAsync(id, requestContext.Owner, cancellationToken);
        return routine is null
            ? OperationResult.NotFound("Routine not found.")
            : OperationResult.Ok(await BuildResponseAsync(routine, includeChildren: true, cancellationToken));
    }

    public async Task<OperationResult> CreateAsync(CreateRoutineRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var guard = new Guard().NotEmpty(name, "Name").MaxLength(name, 150, "Name").MaxLength(request.Description, 400, "Description");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var owner = requestContext.Owner;

        if (await repository.NameExistsAsync(owner, name, null, cancellationToken))
            return OperationResult.Conflict("A routine with this name already exists.");

        var reservation = await usersApiClient.TryReserveQuotaAsync(NodeKey, cancellationToken);
        if (!reservation.Success)
        {
            return reservation.Unreachable
                ? OperationResult.Fail("UpstreamUnavailable", System.Net.HttpStatusCode.ServiceUnavailable)
                : OperationResult.Fail("QuotaExceeded", System.Net.HttpStatusCode.Conflict);
        }

        var now = DateTime.UtcNow;
        var routine = new MaintenanceRoutine
        {
            OwnerType = owner.OwnerType,
            OwnerId = owner.OwnerId,
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedByUserId = requestContext.UserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        try
        {
            await repository.AddAsync(routine, cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            await usersApiClient.ReleaseQuotaAsync(NodeKey, cancellationToken);
            return OperationResult.Fail("Could not save the routine. Try again.");
        }

        return OperationResult.Created(await BuildResponseAsync(routine, includeChildren: false, cancellationToken));
    }

    public async Task<OperationResult> UpdateAsync(long id, UpdateRoutineRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var guard = new Guard().NotEmpty(name, "Name").MaxLength(name, 150, "Name").MaxLength(request.Description, 400, "Description");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var owner = requestContext.Owner;
        var routine = await repository.GetOwnedAsync(id, owner, cancellationToken);
        if (routine is null)
            return OperationResult.NotFound("Routine not found.");

        if (!TryParseRowVersion(request.RowVersion, out var rowVersion) || !rowVersion.SequenceEqual(routine.RowVersion ?? []))
            return OperationResult.Fail("The routine was modified by someone else. Reload and try again.", System.Net.HttpStatusCode.PreconditionFailed);

        if (await repository.NameExistsAsync(owner, name, id, cancellationToken))
            return OperationResult.Conflict("A routine with this name already exists.");

        routine.Name = name;
        routine.Description = request.Description?.Trim();
        routine.IsActive = request.IsActive;
        routine.UpdatedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        return OperationResult.Ok(await BuildResponseAsync(routine, includeChildren: true, cancellationToken));
    }

    public async Task<OperationResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var removed = await repository.RemoveOwnedAsync(id, requestContext.Owner, cancellationToken);
        if (!removed)
            return OperationResult.NotFound("Routine not found.");

        await usersApiClient.ReleaseQuotaAsync(NodeKey, cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> CopyAsync(long id, CopyRoutineRequest request, CancellationToken cancellationToken)
    {
        var newName = request.NewName?.Trim() ?? string.Empty;
        var guard = new Guard().NotEmpty(newName, "NewName").MaxLength(newName, 150, "NewName");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var owner = requestContext.Owner;
        var source = await repository.GetOwnedAsync(id, owner, cancellationToken);
        if (source is null)
            return OperationResult.NotFound("Routine not found.");

        if (await repository.NameExistsAsync(owner, newName, null, cancellationToken))
            return OperationResult.Conflict("A routine with this name already exists.");

        var reservation = await usersApiClient.TryReserveQuotaAsync(NodeKey, cancellationToken);
        if (!reservation.Success)
        {
            return reservation.Unreachable
                ? OperationResult.Fail("UpstreamUnavailable", System.Net.HttpStatusCode.ServiceUnavailable)
                : OperationResult.Fail("QuotaExceeded", System.Net.HttpStatusCode.Conflict);
        }

        var now = DateTime.UtcNow;
        var copy = new MaintenanceRoutine
        {
            OwnerType = owner.OwnerType,
            OwnerId = owner.OwnerId,
            Name = newName,
            Description = source.Description,
            IsActive = true,
            CreatedByUserId = requestContext.UserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await repository.AddAsync(copy, cancellationToken);

        var periodicities = await repository.GetPeriodicitiesAsync(source.Id, cancellationToken);
        var concepts = await repository.GetConceptsAsync(periodicities.Select(p => p.Id).ToList(), cancellationToken);

        foreach (var periodicity in periodicities)
        {
            var newPeriodicity = new MaintenanceRoutinePeriodicity
            {
                RoutineId = copy.Id,
                Unit = periodicity.Unit,
                StartsAt = periodicity.StartsAt,
                RepeatsEvery = periodicity.RepeatsEvery
            };
            await repository.AddPeriodicityAsync(newPeriodicity, cancellationToken);

            foreach (var concept in concepts.Where(c => c.PeriodicityId == periodicity.Id))
            {
                await repository.AddConceptAsync(new MaintenanceRoutineConcept
                {
                    PeriodicityId = newPeriodicity.Id,
                    Name = concept.Name,
                    Quantity = concept.Quantity,
                    QuantityUnit = concept.QuantityUnit,
                    Notes = concept.Notes
                }, cancellationToken);
            }
        }

        return OperationResult.Created(await BuildResponseAsync(copy, includeChildren: true, cancellationToken));
    }

    public async Task<OperationResult> AddPeriodicityAsync(long routineId, CreatePeriodicityRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MeasurementUnit>(request.Unit, ignoreCase: true, out var unit))
            return OperationResult.Fail("Invalid unit. Use Kilometers, Hours or Days.");

        var guard = new Guard()
            .Positive(request.RepeatsEvery, "RepeatsEvery")
            .NotNegative(request.StartsAt, "StartsAt");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var routine = await repository.GetOwnedAsync(routineId, requestContext.Owner, cancellationToken);
        if (routine is null)
            return OperationResult.NotFound("Routine not found.");

        var periodicity = new MaintenanceRoutinePeriodicity
        {
            RoutineId = routineId,
            Unit = unit,
            StartsAt = request.StartsAt,
            RepeatsEvery = request.RepeatsEvery
        };

        await repository.AddPeriodicityAsync(periodicity, cancellationToken);
        return OperationResult.Created(new PeriodicityResponse
        {
            Id = periodicity.Id,
            Unit = periodicity.Unit.ToString(),
            StartsAt = periodicity.StartsAt,
            RepeatsEvery = periodicity.RepeatsEvery
        });
    }

    public async Task<OperationResult> AddConceptAsync(long routineId, CreateConceptRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var guard = new Guard().NotEmpty(name, "Name").MaxLength(name, 150, "Name").MaxLength(request.Notes, 400, "Notes");
        if (guard.HasErrors)
            return OperationResult.Fail(guard.Errors);

        MeasurementUnit? quantityUnit = null;
        if (!string.IsNullOrWhiteSpace(request.QuantityUnit))
        {
            if (!Enum.TryParse<MeasurementUnit>(request.QuantityUnit, ignoreCase: true, out var parsedUnit))
                return OperationResult.Fail("Invalid quantity unit.");
            quantityUnit = parsedUnit;
        }

        var membershipCheck = await contextGuard.EnsureCompanyMembershipAsync(cancellationToken);
        if (membershipCheck is not null)
            return membershipCheck;

        var routine = await repository.GetOwnedAsync(routineId, requestContext.Owner, cancellationToken);
        if (routine is null)
            return OperationResult.NotFound("Routine not found.");

        var periodicity = await repository.GetPeriodicityAsync(request.PeriodicityId, routineId, cancellationToken);
        if (periodicity is null)
            return OperationResult.NotFound("Periodicity not found for this routine.");

        var concept = new MaintenanceRoutineConcept
        {
            PeriodicityId = periodicity.Id,
            Name = name,
            Quantity = request.Quantity <= 0 ? 1 : request.Quantity,
            QuantityUnit = quantityUnit,
            Notes = request.Notes?.Trim()
        };

        await repository.AddConceptAsync(concept, cancellationToken);
        return OperationResult.Created(new ConceptResponse
        {
            Id = concept.Id,
            Name = concept.Name,
            Quantity = concept.Quantity,
            QuantityUnit = concept.QuantityUnit?.ToString(),
            Notes = concept.Notes
        });
    }

    private async Task<RoutineResponse> BuildResponseAsync(MaintenanceRoutine routine, bool includeChildren, CancellationToken cancellationToken)
    {
        var periodicityResponses = new List<PeriodicityResponse>();

        if (includeChildren)
        {
            var periodicities = await repository.GetPeriodicitiesAsync(routine.Id, cancellationToken);
            var concepts = await repository.GetConceptsAsync(periodicities.Select(p => p.Id).ToList(), cancellationToken);

            foreach (var periodicity in periodicities)
            {
                periodicityResponses.Add(new PeriodicityResponse
                {
                    Id = periodicity.Id,
                    Unit = periodicity.Unit.ToString(),
                    StartsAt = periodicity.StartsAt,
                    RepeatsEvery = periodicity.RepeatsEvery,
                    Concepts = concepts
                        .Where(c => c.PeriodicityId == periodicity.Id)
                        .Select(c => new ConceptResponse
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Quantity = c.Quantity,
                            QuantityUnit = c.QuantityUnit?.ToString(),
                            Notes = c.Notes
                        })
                        .ToList()
                });
            }
        }

        return new RoutineResponse
        {
            Id = routine.Id,
            Name = routine.Name,
            Description = routine.Description,
            IsActive = routine.IsActive,
            RowVersion = Convert.ToBase64String(routine.RowVersion ?? []),
            CreatedAtUtc = routine.CreatedAtUtc,
            UpdatedAtUtc = routine.UpdatedAtUtc,
            Periodicities = periodicityResponses
        };
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
}
