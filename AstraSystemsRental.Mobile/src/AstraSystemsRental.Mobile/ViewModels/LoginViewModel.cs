using AstraSystemsRental.Contracts.Display;
using System.Windows.Input;
using AstraSystemsRental.Mobile.Services;

namespace AstraSystemsRental.Mobile.ViewModels;

public sealed class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _auth;
    private readonly ISyncService _sync;
    private readonly ISessionStore _session;
    private readonly IBiometricService _biometric;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _canUseBiometric;
    private string _biometricLabel = "Face ID";
    private bool _promptShown;

    public LoginViewModel(IAuthService auth, ISyncService sync, ISessionStore session, IBiometricService biometric)
    {
        _auth = auth;
        _sync = sync;
        _session = session;
        _biometric = biometric;
        LoginCommand = new Command(async () => await LoginAsync(), () => IsNotBusy);
        BiometricCommand = new Command(async () => await UnlockAsync(), () => IsNotBusy);
    }

    public string Email
    {
        get => _email;
        set => Set(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => Set(ref _password, value);
    }

    /// <summary>
    /// Solo se ofrece si el dispositivo tiene sensor, el usuario lo activo y hay
    /// una sesion guardada que desbloquear.
    /// </summary>
    public bool CanUseBiometric
    {
        get => _canUseBiometric;
        private set => Set(ref _canUseBiometric, value);
    }

    public string BiometricLabel
    {
        get => _biometricLabel;
        private set => Set(ref _biometricLabel, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand BiometricCommand { get; }

    /// <summary>
    /// Al abrir la app se intenta el desbloqueo una sola vez: si el usuario cancela
    /// el prompt, queda el login normal y no se le insiste.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _session.LoadAsync();

        var kind = await _biometric.GetAvailableAsync();
        var hasSession = !string.IsNullOrWhiteSpace(_session.Current?.RefreshToken);

        BiometricLabel = _biometric.Label(kind);
        CanUseBiometric = kind != BiometricKind.None && _biometric.IsEnabled && hasSession;

        if (!CanUseBiometric || _promptShown)
            return;

        _promptShown = true;
        await UnlockAsync();
    }

    private async Task UnlockAsync()
    {
        if (IsBusy || !CanUseBiometric)
            return;

        Error = null;
        IsBusy = true;

        try
        {
            if (!await _biometric.AuthenticateAsync($"Desbloqueá AstraSystems con {BiometricLabel}"))
                return;

            // La biometria desbloquea la sesion guardada; el refresh confirma
            // contra el servidor que sigue siendo valida.
            if (!await _auth.RestoreSessionAsync())
            {
                CanUseBiometric = false;
                Error = "Tu sesión expiró. Ingresá con tu correo y contraseña.";
                return;
            }

            await _sync.RefreshPendingCountAsync();

            if (Shell.Current is AppShell shell)
                await shell.GoToMainAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        Error = null;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            Error = "Ingresá tu correo y contraseña.";
            return;
        }

        IsBusy = true;

        try
        {
            var result = await _auth.LoginAsync(Email, Password);

            if (!result.Success)
            {
                Error = result.Offline
                    ? "Sin conexión con el servidor. Revisá tu red."
                    // Sin Translate, mensajes como "Invalid credentials." llegaban
                    // en ingles crudo justo en la primera pantalla de la app.
                    : ErrorText.Translate(result.Error);
                return;
            }

            Password = string.Empty;
            await _sync.RefreshPendingCountAsync();

            if (Shell.Current is AppShell shell)
                await shell.GoToMainAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
