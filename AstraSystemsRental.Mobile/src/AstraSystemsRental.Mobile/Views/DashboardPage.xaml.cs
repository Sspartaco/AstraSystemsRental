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
            Root.Opacity = 0;
            Root.TranslationY = 14;
            await Task.WhenAll(
                Root.FadeTo(1, 300, Easing.CubicOut),
                Root.TranslateTo(0, 0, 340, Easing.CubicOut));
        }

        await _viewModel.LoadAsync();
    }
}
