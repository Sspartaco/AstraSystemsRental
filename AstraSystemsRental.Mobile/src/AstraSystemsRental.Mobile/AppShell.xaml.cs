using AstraSystemsRental.Mobile.Services;
using AstraSystemsRental.Mobile.Views;

namespace AstraSystemsRental.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("vehicle-detail", typeof(VehicleDetailPage));

        ApplyVisibility(authenticated: false);
    }

    /// <summary>
    /// Mismo gating que la web: nodos del plan + rol. Sin sesion solo se ve el login.
    /// </summary>
    public void ApplyVisibility(bool authenticated)
    {
        LoginItem.IsVisible = !authenticated;
        MainItem.IsVisible = authenticated;

        var auth = Handler?.MauiContext?.Services.GetService<IAuthService>();

        RoutinesItem.IsVisible = authenticated && (auth?.HasNode(AppConfig.NodeRoutines) ?? false);
        UsersItem.IsVisible = authenticated && (auth?.IsSuperUser ?? false);
        ProfileItem.IsVisible = authenticated;

        FlyoutBehavior = authenticated ? FlyoutBehavior.Flyout : FlyoutBehavior.Disabled;
    }

    public async Task GoToLoginAsync()
    {
        ApplyVisibility(authenticated: false);
        await GoToAsync("//login");
    }

    public async Task GoToMainAsync()
    {
        ApplyVisibility(authenticated: true);
        await GoToAsync("//main/dashboard");
    }
}
