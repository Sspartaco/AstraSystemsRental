using System.Net.Mail;

namespace AstraSystemsRental.Base.Validation;

public sealed class Guard
{
    private readonly List<string> _errors = [];

    public IReadOnlyList<string> Errors => _errors;
    public bool HasErrors => _errors.Count > 0;

    public Guard NotEmpty(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            _errors.Add($"{field} is required.");
        return this;
    }

    public Guard MaxLength(string? value, int max, string field)
    {
        if (value is not null && value.Length > max)
            _errors.Add($"{field} must not exceed {max} characters.");
        return this;
    }

    public Guard Email(string? value, string field)
    {
        if (!string.IsNullOrWhiteSpace(value) && !MailAddress.TryCreate(value, out _))
            _errors.Add($"{field} must be a valid email address.");
        return this;
    }

    public Guard Must(bool condition, string error)
    {
        if (!condition)
            _errors.Add(error);
        return this;
    }

    public Guard Range<T>(T value, T min, T max, string field) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            _errors.Add($"{field} must be between {min} and {max}.");
        return this;
    }

    public Guard Positive(int value, string field)
    {
        if (value <= 0)
            _errors.Add($"{field} must be greater than zero.");
        return this;
    }

    public Guard NotNegative(int value, string field)
    {
        if (value < 0)
            _errors.Add($"{field} must be zero or greater.");
        return this;
    }

    public Guard NotInFuture(DateOnly value, string field)
    {
        if (value > DateOnly.FromDateTime(DateTime.UtcNow))
            _errors.Add($"{field} cannot be in the future.");
        return this;
    }

    public Guard NotInFuture(DateTime value, string field)
    {
        if (value > DateTime.UtcNow)
            _errors.Add($"{field} cannot be in the future.");
        return this;
    }

    public Guard Before(DateOnly value, DateOnly reference, string field)
    {
        if (value >= reference)
            _errors.Add($"{field} must be before {reference}.");
        return this;
    }

    public Guard After(DateOnly value, DateOnly reference, string field)
    {
        if (value <= reference)
            _errors.Add($"{field} must be after {reference}.");
        return this;
    }
}
