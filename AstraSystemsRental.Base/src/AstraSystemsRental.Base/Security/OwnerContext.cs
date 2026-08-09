namespace AstraSystemsRental.Base.Security;

public static class OwnerType
{
    public const string User = "User";
    public const string Company = "Company";
}

public readonly record struct OwnerContext(string OwnerType, long OwnerId);
