using System.Windows.Input;
using AstraSystemsRental.Mobile.Services;

namespace AstraSystemsRental.Mobile.ViewModels;

public sealed class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _auth;
    private readonly ISyncService _sync;

    private string _email = string.Empty;
    private string _password = string.Empty;

    public LoginViewModel(IAuthService auth, ISyncService sync)
    {
        _auth = auth;
        _sync = sync;
        LoginCommand = new Command(async () => await LoginAsync(), () => IsNotBusy);
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

    public ICommand LoginCommand { get; }

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
                    : result.Error ?? "No se pudo iniciar sesión.";
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
