using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.Fingerprint;

namespace AstraSystemsRental.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // El dialogo biometrico de Android necesita la Activity viva. Sin este
        // resolver, AuthenticateAsync revienta al construir el prompt.
        CrossFingerprint.SetCurrentActivityResolver(() => this);
    }
}
