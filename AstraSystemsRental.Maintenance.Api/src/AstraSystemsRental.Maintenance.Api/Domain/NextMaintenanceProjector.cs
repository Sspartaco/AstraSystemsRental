namespace AstraSystemsRental.Maintenance.Api.Domain;

public sealed record NextMaintenanceProjection
{
    public required MeasurementUnit Unit { get; init; }
    public required int CurrentValue { get; init; }
    public required int NextThreshold { get; init; }
    public int? PreviousThreshold { get; init; }
    public required int Remaining { get; init; }
    public required int Overdue { get; init; }
    public required bool IsOverdue { get; init; }
}

public static class NextMaintenanceProjector
{
    public static NextMaintenanceProjection? Project(
        MaintenanceRoutinePeriodicity? periodicity,
        int currentValue)
    {
        if (periodicity is null || periodicity.RepeatsEvery <= 0)
            return null;

        var startsAt = periodicity.StartsAt;
        var repeats = periodicity.RepeatsEvery;

        if (currentValue < startsAt)
        {
            return new NextMaintenanceProjection
            {
                Unit = periodicity.Unit,
                CurrentValue = currentValue,
                NextThreshold = startsAt,
                PreviousThreshold = null,
                Remaining = startsAt - currentValue,
                Overdue = 0,
                IsOverdue = false
            };
        }

        var elapsed = currentValue - startsAt;
        var completedCycles = elapsed / repeats;
        var previousThreshold = startsAt + (completedCycles * repeats);
        var nextThreshold = previousThreshold + repeats;

        var overdue = currentValue - previousThreshold;

        return new NextMaintenanceProjection
        {
            Unit = periodicity.Unit,
            CurrentValue = currentValue,
            NextThreshold = nextThreshold,
            PreviousThreshold = previousThreshold,
            Remaining = nextThreshold - currentValue,
            Overdue = overdue,
            IsOverdue = overdue > 0
        };
    }
}
