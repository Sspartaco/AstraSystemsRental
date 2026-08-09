using System.Globalization;
using System.Text.RegularExpressions;

namespace AstraSystemsRental.Contracts.Display;

public static partial class ErrorText
{
    public static string Translate(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return "No se pudo completar la operación.";

        if (Codes.TryGetValue(error, out var byCode))
            return byCode;

        if (Exact.TryGetValue(error, out var exact))
            return exact;

        foreach (var (pattern, format) in Patterns)
        {
            var match = pattern.Match(error);

            if (!match.Success)
                continue;

            var groups = match.Groups.Cast<Group>()
                .Skip(1)
                .Select(g => (object)Humanize(g.Value))
                .ToArray();

            return string.Format(CultureInfo.InvariantCulture, format, groups);
        }

        return error;
    }

    private static string Humanize(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return number.ToString("N0", new CultureInfo("es-CO"));

        var translated = StatusText.Reservation(value);

        return translated == value ? value : translated.ToLowerInvariant();
    }

    private static readonly Dictionary<string, string> Codes = new(StringComparer.Ordinal)
    {
        ["QuotaExceeded"] = "Alcanzaste el límite de tu plan actual.",
        ["CompanyContextForbidden"] = "Ya no tenés acceso a esta compañía. Cambiá de contexto e intentá de nuevo.",
        ["UpstreamUnavailable"] = "El servicio no está disponible temporalmente. Intentá de nuevo.",
        ["VehicleNotOperational"] = "El vehículo no está operativo y no admite esta operación."
    };

    private static readonly Dictionary<string, string> Exact = new(StringComparer.Ordinal)
    {
        ["A reading with the same date and value already exists."] =
            "Ya existe una lectura con la misma fecha y el mismo valor.",
        ["The reading is not consistent with the vehicle history."] =
            "La lectura no es coherente con el historial del vehículo.",
        ["The mileage is not consistent with the vehicle history."] =
            "El kilometraje no es coherente con el historial del vehículo.",
        ["Invalid reading type. Use Kilometers or Hours."] =
            "Tipo de lectura inválido. Usá Kilómetros u Horas.",
        ["Reading not found."] = "No se encontró la lectura.",
        ["ExpectedEndAtUtc must be after ScheduledAtUtc."] =
            "La fecha de fin estimado debe ser posterior a la fecha programada.",
        ["Reservation not found."] = "No se encontró la reserva.",
        ["Provider not found."] = "No se encontró el taller.",
        ["Invalid provider type. Use Individual or Company."] =
            "Tipo de proveedor inválido. Usá Persona o Empresa.",
        ["The reservation was modified by someone else. Reload and try again."] =
            "Alguien más modificó la reserva. Recargá e intentá de nuevo.",
        ["Could not save the reservation. Try again."] =
            "No se pudo guardar la reserva. Intentá de nuevo.",
        ["A routine with this name already exists."] = "Ya existe una rutina con ese nombre.",
        ["Routine not found."] = "No se encontró la rutina.",
        ["The routine is inactive and cannot be assigned."] =
            "La rutina está inactiva y no se puede asignar.",
        ["The routine was modified by someone else. Reload and try again."] =
            "Alguien más modificó la rutina. Recargá e intentá de nuevo.",
        ["Could not save the routine. Try again."] =
            "No se pudo guardar la rutina. Intentá de nuevo.",
        ["Periodicity not found for this routine."] =
            "No se encontró la periodicidad para esta rutina.",
        ["Invalid unit. Use Kilometers, Hours or Days."] =
            "Unidad inválida. Usá Kilómetros, Horas o Días.",
        ["Invalid quantity unit."] = "Unidad de cantidad inválida.",
        ["Vehicle not found."] = "No se encontró el vehículo.",
        ["A vehicle with this plate is already registered."] =
            "Ya hay un vehículo registrado con esa placa.",
        ["The vehicle was modified by someone else. Reload and try again."] =
            "Alguien más modificó el vehículo. Recargá e intentá de nuevo.",
        ["Could not save the vehicle. Try again."] =
            "No se pudo guardar el vehículo. Intentá de nuevo.",
        ["Document not found."] = "No se encontró el documento.",
        ["Invalid document type."] = "Tipo de documento inválido.",
        ["A file is required."] = "Tenés que adjuntar un archivo.",
        ["Unsupported file type. Use JPG, PNG or WEBP."] =
            "Formato no admitido. Usá JPG, PNG o WEBP (máximo 8 MB).",
        ["You cannot deactivate your own account."] = "No podés desactivar tu propia cuenta."
    };

    private static readonly (Regex Pattern, string Format)[] Patterns =
    [
        (MinimumReadingRegex(), "La lectura debe ser al menos {0} para mantener el histórico."),
        (MaximumReadingRegex(), "La lectura no puede superar {0} para esa fecha."),
        (OverlapRegex(), "El vehículo ya tiene una reserva activa que se cruza con esas fechas (#{0})."),
        (TransitionRegex(), "No se puede pasar la reserva de {0} a {1}.")
    ];

    [GeneratedRegex(@"^The reading must be at least (\d+) to keep the history consistent\.$")]
    private static partial Regex MinimumReadingRegex();

    [GeneratedRegex(@"^The reading cannot exceed (\d+) for this date\.$")]
    private static partial Regex MaximumReadingRegex();

    [GeneratedRegex(@"^The vehicle already has an active reservation overlapping these dates \(#(\d+)\)\.$")]
    private static partial Regex OverlapRegex();

    [GeneratedRegex(@"^Cannot transition a reservation from (\w+) to (\w+)\.$")]
    private static partial Regex TransitionRegex();
}
