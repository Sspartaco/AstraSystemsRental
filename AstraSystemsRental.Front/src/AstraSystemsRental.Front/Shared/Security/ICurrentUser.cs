namespace AstraSystemsRental.Front.Shared.Security;

public interface ICurrentUser
{
    AstraPrincipal Principal { get; }
    bool IsAuthenticated { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    public AstraPrincipal Principal { get; set; } = AstraPrincipal.Anonymous;
    public bool IsAuthenticated => !string.IsNullOrEmpty(Principal.UserId);
}
