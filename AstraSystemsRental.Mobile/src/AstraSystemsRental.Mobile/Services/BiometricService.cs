using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace AstraSystemsRental.Mobile.Services;

public enum BiometricKind
{
    None,
    Face,
    Fingerprint,
    Generic
}

public interface IBiometricService
{
    Task<BiometricKind> GetAvailableAsync();
    bool IsEnabled { get; }
    Task SetEnabledAsync(bool enabled);
    Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken = default);
    string Label(BiometricKind kind);
}

public sealed class BiometricService : IBiometricService
{
    private const string EnabledKey = "astra.biometric.enabled";

    private BiometricKind? _cached;

    public bool IsEnabled => Preferences.Default.Get(EnabledKey, false);

    public Task SetEnabledAsync(bool enabled)
    {
        Preferences.Default.Set(EnabledKey, enabled);
        return Task.CompletedTask;
    }

    public async Task<BiometricKind> GetAvailableAsync()
    {
        if (_cached is { } cached)
            return cached;

        try
        {
            var availability = await CrossFingerprint.Current.GetAvailabilityAsync();

            if (availability != FingerprintAvailability.Available)
            {
                _cached = BiometricKind.None;
                return _cached.Value;
            }

            var type = await CrossFingerprint.Current.GetAuthenticationTypeAsync();

            _cached = type switch
            {
                AuthenticationType.Face => BiometricKind.Face,
                AuthenticationType.Fingerprint => BiometricKind.Fingerprint,
                _ => BiometricKind.Generic
            };
        }
        catch
        {
            // Un dispositivo sin sensor, o con el modulo no disponible, no es un
            // error: simplemente no ofrecemos la opcion.
            _cached = BiometricKind.None;
        }

        return _cached.Value;
    }

    public async Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new AuthenticationRequestConfiguration("AstraSystems", reason)
            {
                CancelTitle = "Cancelar",
                FallbackTitle = "Usar contraseña",
                AllowAlternativeAuthentication = true
            };

            var result = await CrossFingerprint.Current.AuthenticateAsync(request, cancellationToken);
            return result.Authenticated;
        }
        catch
        {
            return false;
        }
    }

    public string Label(BiometricKind kind) => kind switch
    {
        BiometricKind.Face => "Face ID",
        BiometricKind.Fingerprint => DeviceInfo.Current.Platform == DevicePlatform.iOS ? "Touch ID" : "Huella",
        BiometricKind.Generic => "Biometría",
        _ => "Biometría"
    };
}
