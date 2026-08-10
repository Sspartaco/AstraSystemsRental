using AstraSystemsRental.Mobile.ViewModels;

namespace AstraSystemsRental.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private LoginParticles? _particles;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Las particulas se crean cuando la pagina ya tiene tamano: antes de eso
    /// Width y Height son -1 y quedarian todas apiladas en el origen.
    /// </summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (_particles is not null || width <= 0 || height <= 0)
            return;

        _particles = new LoginParticles(ParticleLayer);
        _particles.Start(width, height);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Se detienen al salir: dejar animaciones vivas en una pagina oculta
        // gasta bateria sin que nadie las vea.
        _particles?.Stop();
        _particles = null;
    }

    /// <summary>
    /// Entrada escalonada: el logo aparece, luego el titulo y por ultimo la tarjeta.
    /// Son ~600ms en total, suficientes para que se sienta vivo sin demorar al usuario.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Al volver desde logout la pagina ya tiene tamano y OnSizeAllocated no
        // se vuelve a disparar, asi que hay que rearrancarlas aca.
        if (_particles is null && Width > 0 && Height > 0)
        {
            _particles = new LoginParticles(ParticleLayer);
            _particles.Start(Width, Height);
        }

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
