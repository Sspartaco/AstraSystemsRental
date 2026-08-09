namespace AstraSystemsRental.Contracts.Display;

public static class StatusText
{
    public static string Vehicle(string? status) => status switch
    {
        "Draft" => "Borrador",
        "Active" => "Activo",
        "Maintenance" => "En mantenimiento",
        "Blocked" => "Bloqueado",
        "Inactive" => "Inactivo",
        "Sold" => "Vendido",
        _ => status ?? "—"
    };

    public static string Reservation(string? status) => status switch
    {
        "Pending" => "Pendiente",
        "InWorkshop" => "En taller",
        "Ready" => "Listo",
        "Collected" => "Retirado",
        "Cancelled" => "Cancelada",
        _ => status ?? "—"
    };

    public static string Document(string? status) => status switch
    {
        "Valid" => "Vigente",
        "Expired" => "Vencido",
        "Pending" => "Pendiente",
        _ => status ?? "—"
    };

    public static string MeasurementUnit(string? unit) => unit switch
    {
        "Kilometers" => "Kilómetros",
        "Hours" => "Horas",
        "Days" => "Días",
        _ => unit ?? "—"
    };

    public static string ReadingSource(string? source) => source switch
    {
        "Manual" => "Manual",
        "Workshop" => "Taller",
        "Import" => "Importado",
        _ => source ?? "—"
    };

    public static string ProviderType(string? type) => type switch
    {
        "Individual" => "Persona",
        "Company" => "Empresa",
        _ => type ?? "—"
    };

    public static string Role(string? role) => role switch
    {
        "SuperUser" => "Super usuario",
        "Standard" => "Estándar",
        "Demo" => "Demo",
        "SysAdmin" => "Admin sistemas",
        _ => role ?? "—"
    };
}
