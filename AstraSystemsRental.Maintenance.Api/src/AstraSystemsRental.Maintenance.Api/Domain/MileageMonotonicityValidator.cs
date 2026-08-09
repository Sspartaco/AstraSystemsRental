namespace AstraSystemsRental.Maintenance.Api.Domain;

public readonly record struct MileageBounds(int Minimum, int Maximum);

public readonly record struct MileageValidationResult(bool IsValid, MileageBounds Bounds, string? Error)
{
    public static MileageValidationResult Ok(MileageBounds bounds) => new(true, bounds, null);
    public static MileageValidationResult Invalid(MileageBounds bounds, string error) => new(false, bounds, error);
}

public static class MileageMonotonicityValidator
{
    public static MileageValidationResult Validate(
        int value,
        DateOnly readingDate,
        IReadOnlyList<MileageReading> existingReadings,
        int absoluteMaximum,
        int dailyProjection)
    {
        var previous = existingReadings
            .Where(r => r.ReadingDate <= readingDate)
            .OrderByDescending(r => r.ReadingDate)
            .ThenByDescending(r => r.Value)
            .FirstOrDefault();

        var next = existingReadings
            .Where(r => r.ReadingDate > readingDate)
            .OrderBy(r => r.ReadingDate)
            .ThenBy(r => r.Value)
            .FirstOrDefault();

        var minimum = previous?.Value ?? 0;

        int maximum;
        if (next is not null)
        {
            maximum = next.Value;
        }
        else if (previous is not null)
        {
            var elapsedDays = readingDate.DayNumber - previous.ReadingDate.DayNumber;
            var projected = previous.Value + (Math.Max(elapsedDays, 1) * (long)dailyProjection);
            maximum = (int)Math.Min(projected, absoluteMaximum);
        }
        else
        {
            maximum = absoluteMaximum;
        }

        var bounds = new MileageBounds(minimum, maximum);

        if (value < minimum)
            return MileageValidationResult.Invalid(bounds, $"The reading must be at least {minimum} to keep the history consistent.");

        if (value > maximum)
            return MileageValidationResult.Invalid(bounds, $"The reading cannot exceed {maximum} for this date.");

        return MileageValidationResult.Ok(bounds);
    }
}
