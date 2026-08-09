namespace AstraSystemsRental.Vehicles.Api.Domain;

public class ValuationSourceCatalog
{
    public required string Code { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public static class ValuationSourceCode
{
    public const string Fasecolda = "Fasecolda";
    public const string AsoUsados = "AsoUsados";
    public const string Tucarro = "Tucarro";
    public const string RevistaMotor = "RevistaMotor";
    public const string OtrasFuentes = "OtrasFuentes";
}
