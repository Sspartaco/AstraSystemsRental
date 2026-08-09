using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Vehicles.Api.Persistence;
using AstraSystemsRental.Vehicles.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AstraSystemsRental.Vehicles.Api.Cli;

public static class ImportVehicleCatalogCommand
{
    public const string CommandName = "import-catalog";

    public static async Task<int> RunAsync(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var filePath = config["file"];
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Console.Error.WriteLine("Usage: dotnet run -- import-catalog --file <ruta-al-excel.xlsx>");
            return 1;
        }

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddAstraDbContext<AstraVehiclesDbContext>(config);
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IVehicleImportService, VehicleImportService>();

        await using var provider = services.BuildServiceProvider();
        var importService = provider.GetRequiredService<IVehicleImportService>();

        Console.WriteLine($"Importando catálogo desde {filePath} ...");
        var result = await importService.ImportFromExcelAsync(filePath, CancellationToken.None);

        Console.WriteLine("==================================================");
        Console.WriteLine(" VEHICLE CATALOG IMPORT COMPLETE");
        Console.WriteLine("==================================================");
        Console.WriteLine($" Filas leídas:    {result.TotalRows}");
        Console.WriteLine($" Placas importadas: {result.Imported}");
        Console.WriteLine("==================================================");

        return 0;
    }
}
