using AstraSystemsRental.Front.Services;
using AstraSystemsRental.Front.Shared;
using AstraSystemsRental.Front.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddRazorOptions(options => options.ViewLocationExpanders.Add(new FeatureViewLocationExpander()));

builder.Services.AddDataProtection();
builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<INodeCatalogService, NodeCatalogService>();

var gatewayBaseUrl = builder.Configuration["Gateway:BaseUrl"] ?? "https://localhost:8443";
builder.Services.AddHttpClient<IGatewayClient, GatewayClient>(client =>
    {
        client.BaseAddress = new Uri(gatewayBaseUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
    .AddStandardResilienceHandler();

var app = builder.Build();

var pathBase = builder.Configuration["BehaviorSettings:PathBase"];
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseMiddleware<AstraSessionMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program;
