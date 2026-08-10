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
        // --- Sesion y cuenta ---
        ["Invalid credentials."] = "Correo o contraseña incorrectos.",
        ["Account is not confirmed."] = "Tu cuenta todavía no está confirmada. Revisá el correo de activación.",
        ["The account is no longer active."] = "Esta cuenta está inhabilitada. Contactá al administrador.",
        ["Subscription has expired."] = "Tu suscripción venció. Renovala para seguir usando la plataforma.",
        ["No active subscription for the current context."] = "No hay una suscripción activa para esta cuenta.",
        ["The refresh token is invalid or expired."] = "Tu sesión expiró. Ingresá de nuevo.",
        ["A refresh token is required."] = "Tu sesión expiró. Ingresá de nuevo.",
        ["Invalid or expired confirmation token."] = "El enlace de confirmación no es válido o ya venció.",
        ["Invalid or missing internal API key."] = "Error de configuración interna. Contactá al administrador.",

        // --- Usuarios ---
        ["A user with this email already exists."] = "Ya existe un usuario con ese correo.",
        ["User not found."] = "No se encontró el usuario.",
        ["User profile not found"] = "No se encontró el perfil del usuario.",
        ["Role not found."] = "No se encontró el rol indicado.",
        ["Default role is not configured."] = "Falta configurar el rol por defecto. Contactá al administrador.",

        // --- Companias ---
        ["Company not found."] = "No se encontró la compañía.",
        ["A company with this document number already exists."] = "Ya existe una compañía con ese número de documento.",
        ["Already a member of this company."] = "Ya sos miembro de esta compañía.",
        ["The user is already a member of this company."] = "Ese usuario ya es miembro de la compañía.",
        ["Membership not found."] = "No se encontró la membresía.",
        ["Cannot remove the last owner. Transfer ownership first."] =
            "No podés quitar al último propietario. Transferí la propiedad primero.",
        ["New owner must already be a member of this company."] =
            "El nuevo propietario ya debe ser miembro de la compañía.",
        ["Invitation not found."] = "No se encontró la invitación.",
        ["Invalid or expired invitation."] = "La invitación no es válida o ya venció.",
        ["This invitation was not issued for the current account."] =
            "Esta invitación fue emitida para otra cuenta.",
        ["No account exists for the invited email yet."] =
            "Todavía no existe una cuenta con ese correo. La persona debe registrarse primero.",

        // --- Planes y nodos ---
        ["Plan not found."] = "No se encontró el plan.",
        ["Plan not found or inactive."] = "El plan no existe o está inactivo.",
        ["A plan with this code already exists."] = "Ya existe un plan con ese código.",
        ["Default plan is not configured."] = "Falta configurar el plan por defecto. Contactá al administrador.",
        ["A node with this key already exists."] = "Ya existe un módulo con esa clave.",
        ["Node not found in catalog."] = "No se encontró el módulo en el catálogo.",

        // --- Vehiculos ---
        ["PlateNumber must be a valid Colombian plate (e.g. ABC123)."] =
            "La placa debe tener el formato colombiano, por ejemplo ABC123.",
        ["Vehicle not found for the given plate."] = "No se encontró un vehículo con esa placa.",
        ["VehicleNotFound"] = "No se encontró el vehículo.",
        ["Quote request not found."] = "No se encontró la solicitud de cotización.",
        ["Invalid status."] = "El estado indicado no es válido.",

        // --- Bootstrap (primer superusuario) ---
        ["A SuperUser already exists. Bootstrap is disabled."] =
            "Ya existe un superusuario: el arranque inicial está deshabilitado.",
        ["Provided bootstrap secret does not match."] = "La clave de arranque no coincide.",
        ["Bootstrap secret is not configured or is too short (min 24 chars)."] =
            "La clave de arranque no está configurada o es demasiado corta.",

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
