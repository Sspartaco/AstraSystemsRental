using Microsoft.Maui.Controls.Shapes;

namespace AstraSystemsRental.Mobile.Views;

/// <summary>
/// Partículas que suben lentamente detrás del login. Son Ellipse animadas con
/// las mismas animaciones de MAUI que el resto de la app: no hay canvas ni
/// timers propios, así que se detienen solas al salir de la página.
///
/// Deliberadamente pocas (14) y lentas: el objetivo es que el fondo respire,
/// no llamar la atención sobre sí mismas ni gastar batería en un login.
/// </summary>
public sealed class LoginParticles
{
    private const int Count = 14;

    private readonly AbsoluteLayout _layer;
    private readonly Random _random = new();
    private readonly List<View> _dots = [];

    private bool _running;

    public LoginParticles(AbsoluteLayout layer) => _layer = layer;

    public void Start(double width, double height)
    {
        if (_running || width <= 0 || height <= 0)
            return;

        _running = true;

        for (var i = 0; i < Count; i++)
        {
            var size = _random.Next(3, 8);

            var dot = new Ellipse
            {
                WidthRequest = size,
                HeightRequest = size,
                Fill = new SolidColorBrush(Color.FromRgba(79, 124, 255, _random.Next(30, 90))),
                InputTransparent = true
            };

            _layer.Add(dot);
            _dots.Add(dot);

            _ = FloatAsync(dot, width, height, size);
        }
    }

    public void Stop()
    {
        _running = false;

        foreach (var dot in _dots)
        {
            dot.AbortAnimation("float");
            _layer.Remove(dot);
        }

        _dots.Clear();
    }

    private async Task FloatAsync(View dot, double width, double height, double size)
    {
        // Cada partícula arranca en un punto distinto del recorrido para que no
        // suban todas en bloque, que es lo que delata una animación artificial.
        var x = _random.NextDouble() * (width - size);
        var startY = _random.NextDouble() * height;

        AbsoluteLayout.SetLayoutBounds(dot, new Rect(x, startY, size, size));

        while (_running)
        {
            var duration = (uint)_random.Next(9000, 18000);
            var drift = (_random.NextDouble() - 0.5) * 60;

            var travelled = await AnimateAsync(dot, x + drift, -size, duration);

            if (!_running || !travelled)
                return;

            // Reaparece abajo, en otra columna: el ciclo no se repite igual.
            x = _random.NextDouble() * (width - size);
            AbsoluteLayout.SetLayoutBounds(dot, new Rect(x, height, size, size));
        }
    }

    private static Task<bool> AnimateAsync(View dot, double toX, double toY, uint duration)
    {
        var completion = new TaskCompletionSource<bool>();
        var from = AbsoluteLayout.GetLayoutBounds(dot);

        var animation = new Animation(v =>
        {
            var y = from.Y + (toY - from.Y) * v;
            var x = from.X + (toX - from.X) * v;
            AbsoluteLayout.SetLayoutBounds(dot, new Rect(x, y, from.Width, from.Height));
        });

        animation.Commit(dot, "float", 16, duration, Easing.Linear,
            finished: (_, cancelled) => completion.TrySetResult(!cancelled));

        return completion.Task;
    }
}
