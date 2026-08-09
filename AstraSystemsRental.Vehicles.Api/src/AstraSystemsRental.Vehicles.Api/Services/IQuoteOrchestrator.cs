namespace AstraSystemsRental.Vehicles.Api.Services;

public interface IQuoteOrchestrator
{
    void Enqueue(long quoteRequestId, VehicleQuery query);
}
