using Microsoft.Maui.Controls.Shapes;

namespace AstraSystemsRental.Mobile;

/// <summary>
/// Geometrias portadas de los mismos SVG que usa el sidebar de la web (access.Nodes.Icon),
/// para que los iconos sean identicos en ambas plataformas.
/// Se exponen como Geometry (no string) porque XamlC no convierte string -> Geometry
/// cuando el valor viene de x:Static.
/// </summary>
public static class Icons
{
    private static readonly PathGeometryConverter Converter = new();

    private static Geometry Parse(string data) => (Geometry)Converter.ConvertFromInvariantString(data)!;

    public static readonly Geometry Dashboard = Parse("M3 4.5 A1.5 1.5 0 0 1 4.5 3 H9 A1.5 1.5 0 0 1 10.5 4.5 V9 A1.5 1.5 0 0 1 9 10.5 H4.5 A1.5 1.5 0 0 1 3 9 Z M13.5 4.5 A1.5 1.5 0 0 1 15 3 H19.5 A1.5 1.5 0 0 1 21 4.5 V9 A1.5 1.5 0 0 1 19.5 10.5 H15 A1.5 1.5 0 0 1 13.5 9 Z M3 15 A1.5 1.5 0 0 1 4.5 13.5 H9 A1.5 1.5 0 0 1 10.5 15 V19.5 A1.5 1.5 0 0 1 9 21 H4.5 A1.5 1.5 0 0 1 3 19.5 Z M13.5 15 A1.5 1.5 0 0 1 15 13.5 H19.5 A1.5 1.5 0 0 1 21 15 V19.5 A1.5 1.5 0 0 1 19.5 21 H15 A1.5 1.5 0 0 1 13.5 19.5 Z");

    public static readonly Geometry Truck = Parse("M2.75 6.75A1.75 1.75 0 0 1 4.5 5h8.75a1.75 1.75 0 0 1 1.75 1.75V15H3.5a.75.75 0 0 1-.75-.75V6.75Z M15 8.5h2.94a1.75 1.75 0 0 1 1.55.94l1.56 3a1.75 1.75 0 0 1 .2.81V15H15V8.5Z M5.25 17a1.75 1.75 0 1 1 3.5 0 1.75 1.75 0 0 1-3.5 0Z M15.75 17a1.75 1.75 0 1 1 3.5 0 1.75 1.75 0 0 1-3.5 0Z");

    public static readonly Geometry Fleet = Parse("M3 9 A1.5 1.5 0 0 1 4.5 7.5 H19.5 A1.5 1.5 0 0 1 21 9 V18 A1.5 1.5 0 0 1 19.5 19.5 H4.5 A1.5 1.5 0 0 1 3 18 Z M7.5 7.5V6a1.5 1.5 0 0 1 1.5-1.5h6A1.5 1.5 0 0 1 16.5 6v1.5");

    public static readonly Geometry Clock = Parse("M12 3.75 A8.25 8.25 0 1 1 11.99 3.75 Z M12 7.5 V12 L15 13.75");

    public static readonly Geometry Calendar = Parse("M3.75 7 A1.75 1.75 0 0 1 5.5 5.25 H18.5 A1.75 1.75 0 0 1 20.25 7 V18.5 A1.75 1.75 0 0 1 18.5 20.25 H5.5 A1.75 1.75 0 0 1 3.75 18.5 Z M3.75 10.5 H20.25 M8.25 3 V7.5 M15.75 3 V7.5");

    public static readonly Geometry Routine = Parse("M4.5 6.75h15 M4.5 12h15 M4.5 17.25h9 M15.75 17.25 a2.25 2.25 0 1 1 4.5 0 a2.25 2.25 0 0 1 -4.5 0Z");

    public static readonly Geometry Users = Parse("M17.5 20.25v-1.5a4 4 0 0 0-4-4h-4a4 4 0 0 0-4 4v1.5 M8.25 7.5 a3.25 3.25 0 1 1 6.5 0 a3.25 3.25 0 0 1 -6.5 0Z");

    public static readonly Geometry User = Parse("M12 4 a4 4 0 1 1 0 8 a4 4 0 0 1 0-8Z M4 21v-1a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v1");

    public static readonly Geometry Camera = Parse("M3 8.5A1.5 1.5 0 0 1 4.5 7h2l1.2-2h8.6L17.5 7h2A1.5 1.5 0 0 1 21 8.5v9A1.5 1.5 0 0 1 19.5 19h-15A1.5 1.5 0 0 1 3 17.5Z M8.75 13 a3.25 3.25 0 1 1 6.5 0 a3.25 3.25 0 0 1 -6.5 0Z");

    public static readonly Geometry Alert = Parse("M12 9v4 M12 17h.01 M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z");

    public static readonly Geometry Check = Parse("M20 6 9 17l-5-5");

    public static readonly Geometry Search = Parse("M11 4 a7 7 0 1 1 0 14 a7 7 0 0 1 0-14Z M20 20 l-4.35-4.35");

    public static readonly Geometry Plus = Parse("M12 5v14 M5 12h14");

    public static readonly Geometry Logout = Parse("M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4 M16 17l5-5-5-5 M21 12H9");

    public static readonly Geometry Offline = Parse("M1 1l22 22 M16.72 11.06A10.94 10.94 0 0 1 19 12.55 M5 12.55a10.94 10.94 0 0 1 5.17-2.39 M10.71 5.05A16 16 0 0 1 22.58 9 M1.42 9a15.91 15.91 0 0 1 4.7-2.88 M8.53 16.11a6 6 0 0 1 6.95 0 M12 20h.01");

    public static readonly Geometry Wrench = Parse("M14.7 6.3a4 4 0 0 0 5 5l-9.4 9.4a2.1 2.1 0 0 1-3-3Z");


    /// <summary>
    /// Monograma de marca: la "A" de Astra cruzada por un anillo orbital.
    /// Reemplaza al camion generico, que no decia nada de la marca.
    /// </summary>
    public static readonly Geometry Monogram = Parse("M12 4.6 L18.4 19.2 M12 4.6 L5.6 19.2 M8.4 14.4 H15.6");

    /// <summary>Anillo orbital del monograma; va detras de la "A", inclinado.</summary>
    public static readonly Geometry MonogramRing = Parse("M2 13 a10 4.2 0 1 0 20 0 a10 4.2 0 1 0 -20 0 Z");

    /// <summary>Dos personas: distingue "Usuarios" (gestion) de "Mi cuenta" (perfil propio).</summary>
    public static readonly Geometry Group = Parse("M9 11.5 a3.2 3.2 0 1 0 0-6.4 a3.2 3.2 0 0 0 0 6.4 Z M2.8 19.6 v-1.1 a4.6 4.6 0 0 1 4.6-4.6 h3.2 a4.6 4.6 0 0 1 4.6 4.6 v1.1 M16.4 5.4 a3.1 3.1 0 0 1 0 6 M17.6 13.9 a4.6 4.6 0 0 1 3.6 4.5 v1.2");
}