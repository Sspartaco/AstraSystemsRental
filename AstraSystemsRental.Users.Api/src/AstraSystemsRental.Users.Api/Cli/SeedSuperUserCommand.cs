using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Users.Api.Configuration;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Security;
using AstraSystemsRental.Users.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AstraSystemsRental.Users.Api.Cli;

public static class SeedSuperUserCommand
{
    public const string CommandName = "seed-superuser";

    public static async Task<int> RunAsync(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var services = new ServiceCollection();
        services.Configure<BootstrapOptions>(config.GetSection(BootstrapOptions.SectionName));
        services.AddAstraDbContext<AstraUsersDbContext>(config);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBootstrapService, BootstrapService>();

        await using var provider = services.BuildServiceProvider();
        var bootstrap = provider.GetRequiredService<IBootstrapService>();

        var data = new SeedSuperUserData(
            FirstNames: config["firstNames"] ?? "System",
            LastNames: config["lastNames"] ?? "Administrator",
            DocumentNumber: config["documentNumber"] ?? "SUPERUSER",
            Email: config["email"] ?? string.Empty,
            Password: config["password"] ?? string.Empty,
            ProvidedSecret: config["secret"] ?? string.Empty);

        var result = await bootstrap.SeedSuperUserAsync(data, CancellationToken.None);

        Console.WriteLine();
        if (result.Success)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" SUPERUSER CREATED");
            Console.WriteLine("==================================================");
            Console.WriteLine($" Email:    {data.Email}");
            Console.WriteLine($" Password: {data.Password}");
            Console.WriteLine($" Role:     SuperUser");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(" Use these credentials to log in via /apiUsers/auth/login");
            Console.WriteLine("==================================================");
            return 0;
        }

        var message = string.Join("; ", result.Errors);
        if (result.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            Console.WriteLine($"[SKIPPED] {message}");
            return 0;
        }

        Console.Error.WriteLine($"[ERROR] {message}");
        return 1;
    }
}
