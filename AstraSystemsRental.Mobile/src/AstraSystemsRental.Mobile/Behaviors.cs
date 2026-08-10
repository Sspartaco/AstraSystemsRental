namespace AstraSystemsRental.Mobile;

/// <summary>
/// Da un pulso de escala al tocar el elemento. Es el gesto que separa una lista
/// "de datos" de una que se siente nativa: sin esto, tocar una tarjeta no
/// devuelve ninguna senal hasta que termina de navegar.
///
/// Se apoya en TapGestureRecognizer y no en PointerGestureRecognizer: en Android
/// los eventos de puntero solo llegan con mouse o stylus, no con el dedo, asi
/// que un press/release quedaria muerto en el uso real.
/// </summary>
public sealed class TapFeedbackBehavior : Behavior<View>
{
    private TapGestureRecognizer? _recognizer;

    public static readonly BindableProperty ScaleProperty =
        BindableProperty.Create(nameof(Scale), typeof(double), typeof(TapFeedbackBehavior), 0.97);

    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);

        _recognizer = new TapGestureRecognizer();
        _recognizer.Tapped += OnTapped;

        // Se inserta al final: los recognizers que ya tenga el elemento (la
        // navegacion, por ejemplo) siguen recibiendo el toque igual.
        bindable.GestureRecognizers.Add(_recognizer);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        base.OnDetachingFrom(bindable);

        if (_recognizer is null)
            return;

        _recognizer.Tapped -= OnTapped;
        bindable.GestureRecognizers.Remove(_recognizer);
        _recognizer = null;
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not View view)
            return;

        await view.ScaleTo(Scale, 80, Easing.CubicOut);
        await view.ScaleTo(1, 130, Easing.SpringOut);
    }
}
