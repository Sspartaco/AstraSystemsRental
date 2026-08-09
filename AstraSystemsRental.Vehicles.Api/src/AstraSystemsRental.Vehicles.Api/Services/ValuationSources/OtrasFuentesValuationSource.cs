using AstraSystemsRental.Vehicles.Api.Domain;

namespace AstraSystemsRental.Vehicles.Api.Services.ValuationSources;

// Extension point genérico: sumar otros clasificados públicos colombianos de "segunda"
// (Carro Ya, El Carro Colombia, Encuentra24, etc.) implementando IValuationSource y
// registrándolo en DI, sin tocar el orquestador.
public sealed class OtrasFuentesValuationSource : IValuationSource
{
    public string SourceCode => ValuationSourceCode.OtrasFuentes;

    public Task<ValuationSourceResult> FetchAsync(VehicleQuery query, CancellationToken cancellationToken)
        => Task.FromResult(new ValuationSourceResult(
            ValuationStatus.NotFound, null, null, null, null,
            "Sin fuente concreta configurada todavía."));
}
