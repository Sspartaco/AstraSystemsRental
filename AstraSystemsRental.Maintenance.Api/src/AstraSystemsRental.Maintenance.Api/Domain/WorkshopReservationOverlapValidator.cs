namespace AstraSystemsRental.Maintenance.Api.Domain;

public static class WorkshopReservationOverlapValidator
{
    public static WorkshopReservation? FindConflict(
        IReadOnlyList<WorkshopReservation> existing,
        DateTime candidateStartUtc,
        DateTime? candidateEndUtc,
        long? excludeReservationId)
    {
        return existing.FirstOrDefault(r =>
            r.Id != excludeReservationId &&
            r.IsActive &&
            Overlaps(r.ScheduledAtUtc, EffectiveEnd(r), candidateStartUtc, candidateEndUtc));
    }

    private static DateTime? EffectiveEnd(WorkshopReservation reservation)
        => reservation.CollectedAtUtc ?? reservation.ExpectedEndAtUtc;

    private static bool Overlaps(DateTime startA, DateTime? endA, DateTime startB, DateTime? endB)
    {
        var openA = endA is null;
        var openB = endB is null;

        if (openA && openB)
            return true;

        if (openA)
            return endB!.Value >= startA;

        if (openB)
            return endA!.Value >= startB;

        return startA <= endB!.Value && startB <= endA!.Value;
    }
}
