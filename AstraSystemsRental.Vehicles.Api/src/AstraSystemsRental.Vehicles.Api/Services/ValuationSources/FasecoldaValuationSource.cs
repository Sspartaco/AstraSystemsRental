using AstraSystemsRental.Vehicles.Api.Domain;

namespace AstraSystemsRental.Vehicles.Api.Services.ValuationSources;

// Stub v1: la guía comercial Fasecolda es un servicio de pago sin scraping público confiable.
// Activar cuando se cuente con acceso oficial (API/convenio) — mientras tanto retorna NotFound
// sin bloquear al resto de fuentes.
public sealed class FasecoldaValuationSource : IValuationSource
{
    public string SourceCode => ValuationSourceCode.Fasecolda;

    public Task<ValuationSourceResult> FetchAsync(VehicleQuery query, CancellationToken cancellationToken)
        => Task.FromResult(new ValuationSourceResult(
            ValuationStatus.NotFound, null, null, null, null,
            "Fuente no disponible: requiere acceso oficial a la guía comercial Fasecolda."));
}
