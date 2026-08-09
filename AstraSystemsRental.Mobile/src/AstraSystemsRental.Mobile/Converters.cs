using System.Globalization;

namespace AstraSystemsRental.Mobile;

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool flag && !flag;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool flag && !flag;
}

public sealed class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string;

        return status switch
        {
            "Listo" or "Activo" or "Vigente" => Color.FromArgb("#3ddc97"),
            "En taller" or "Pendiente" or "Borrador" => Color.FromArgb("#ffb454"),
            "Cancelada" or "Vencido" or "Bloqueado" => Color.FromArgb("#ff6b6b"),
            _ => Color.FromArgb("#9aa3bd")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
