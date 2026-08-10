using System.ComponentModel;
using AstraSystemsRental.Mobile.ViewModels;

namespace AstraSystemsRental.Mobile.Views;

public partial class VehicleDetailPage : ContentPage
{
    private readonly VehicleDetailViewModel _viewModel;

    public VehicleDetailPage(VehicleDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    /// <summary>
    /// La hoja entra deslizando en vez de aparecer de golpe: al ser larga, un
    /// cambio instantaneo de altura desorienta sobre que acaba de pasar.
    /// </summary>
    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(VehicleDetailViewModel.ShowEdit) || !_viewModel.ShowEdit)
            return;

        EditSheet.Opacity = 0;
        EditSheet.TranslationY = 16;

        await Task.WhenAll(
            EditSheet.FadeTo(1, 240, Easing.CubicOut),
            EditSheet.TranslateTo(0, 0, 280, Easing.CubicOut));
    }
}
