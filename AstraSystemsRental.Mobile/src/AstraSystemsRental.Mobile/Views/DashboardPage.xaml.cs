using AstraSystemsRental.Mobile.ViewModels;

namespace AstraSystemsRental.Mobile.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private bool _animated;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_animated)
        {
            _animated = true;
            await AnimateEntranceAsync();
        }

        await _viewModel.LoadAsync();
    }

    /// <summary>
    /// Las tarjetas entran escalonadas en vez de todas a la vez: el desfase de
    /// 70ms es lo que hace que se lea como una secuencia y no como un salto.
    /// </summary>
    private async Task AnimateEntranceAsync()
    {
        Root.Opacity = 0;
        Root.TranslationY = 14;

        var cards = new View[] { Kpi1, Kpi2, Kpi3, Kpi4 };

        foreach (var card in cards)
        {
            card.Opacity = 0;
            card.TranslationY = 16;
        }

        await Task.WhenAll(
            Root.FadeTo(1, 260, Easing.CubicOut),
            Root.TranslateTo(0, 0, 300, Easing.CubicOut));

        foreach (var card in cards)
        {
            _ = Task.WhenAll(
                card.FadeTo(1, 260, Easing.CubicOut),
                card.TranslateTo(0, 0, 320, Easing.CubicOut));

            await Task.Delay(70);
        }
    }
}
