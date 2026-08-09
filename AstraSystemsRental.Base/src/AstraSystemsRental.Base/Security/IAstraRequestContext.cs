namespace AstraSystemsRental.Base.Security;

public interface IAstraRequestContext
{
    long UserId { get; }
    string RoleCode { get; }
    string Email { get; }
    OwnerContext Owner { get; }
}
