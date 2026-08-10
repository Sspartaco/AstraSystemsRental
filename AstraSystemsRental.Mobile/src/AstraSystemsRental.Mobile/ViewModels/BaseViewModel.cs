using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AstraSystemsRental.Mobile.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool _isBusy;
    private string? _error;
    private bool _isOffline;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (Set(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Estado del gesto "tirar para refrescar", separado de IsBusy a proposito.
    /// Con RefreshView.IsRefreshing enlazado directo a IsBusy el spinner se
    /// quedaba girando para siempre: el gesto ponia IsBusy en true, y entonces
    /// el "if (IsBusy) return" del inicio de LoadAsync abortaba la carga sin
    /// llegar nunca al finally que lo vuelve a poner en false.
    ///
    /// El setter ignora el true que manda el control (la carga la dispara su
    /// Command) y solo honra el false, para poder cortar la animacion.
    /// </summary>
    public bool IsRefreshing
    {
        get => _isBusy;
        set
        {
            if (!value && _isBusy)
                IsBusy = false;
            else
                OnPropertyChanged();
        }
    }

    public string? Error
    {
        get => _error;
        set
        {
            if (Set(ref _error, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    public bool IsOffline
    {
        get => _isOffline;
        set => Set(ref _isOffline, value);
    }

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
