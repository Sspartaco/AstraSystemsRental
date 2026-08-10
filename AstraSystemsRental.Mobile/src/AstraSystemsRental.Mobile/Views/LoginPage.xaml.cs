using AstraSystemsRental.Mobile.ViewModels;

namespace AstraSystemsRental.Mobile.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Entrada escalonada: el logo aparece, luego el titulo y por ultimo la tarjeta.
    /// Son ~600ms en total, suficientes para que se sienta vivo sin demorar al usuario.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        LogoOrb.Opacity = 0;
        LogoOrb.Scale = 0.8;
        LogoRing.Rotation = -160;
        BrandTitle.Opacity = 0;
        BrandSub.Opacity = 0;
        LoginCard.Opacity = 0;
        LoginCard.TranslationY = 18;
        Footer.Opacity = 0;

        // El anillo se acomoda girando mientras el orbe aparece: da la sensacion
        // de que el monograma se "arma" en vez de aparecer de golpe.
        await Task.WhenAll(
            LogoOrb.FadeTo(1, 320, Easing.CubicOut),
            LogoOrb.ScaleTo(1, 380, Easing.SpringOut),
            LogoRing.RotateTo(-20, 620, Easing.CubicOut));

        await Task.WhenAll(
            BrandTitle.FadeTo(1, 220, Easing.CubicOut),
            BrandSub.FadeTo(1, 260, Easing.CubicOut));

        await Task.WhenAll(
            LoginCard.FadeTo(1, 280, Easing.CubicOut),
            LoginCard.TranslateTo(0, 0, 320, Easing.CubicOut));

        await Footer.FadeTo(1, 300, Easing.CubicOut);

        // Despues de la animacion: el prompt del sistema tapa la pantalla, y
        // lanzarlo antes deja la tarjeta a medio aparecer al volver de el.
        if (BindingContext is LoginViewModel vm)
            await vm.InitializeAsync();
    }
}
