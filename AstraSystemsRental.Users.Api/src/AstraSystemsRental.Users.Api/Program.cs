using AstraSystemsRental.Base.Api;
using AstraSystemsRental.Base.Persistence;
using AstraSystemsRental.Base.Security;
using AstraSystemsRental.Users.Api.Cli;
using AstraSystemsRental.Users.Api.Configuration;
using AstraSystemsRental.Users.Api.Domain;
using AstraSystemsRental.Users.Api.Endpoints;
using AstraSystemsRental.Users.Api.Persistence;
using AstraSystemsRental.Users.Api.Security;
using AstraSystemsRental.Users.Api.Services;

if (args.Length > 0 && args[0] == SeedSuperUserCommand.CommandName)
    return await SeedSuperUserCommand.RunAsync(args);

var builder = WebApplication.CreateBuilder(args);

builder.AddAstraApi(new AstraApiOptions
{
    ServiceName = "AstraSystemsRental.Users",
    PathBase = "/apiUsers"
});

builder.Services.AddAstraDbContext<AstraUsersDbContext>(builder.Configuration);
builder.Services.Configure<ConfirmationOptions>(builder.Configuration.GetSection(ConfirmationOptions.SectionName));
builder.Services.Configure<MailClientOptions>(builder.Configuration.GetSection(MailClientOptions.SectionName));

builder.Services.Configure<BootstrapOptions>(builder.Configuration.GetSection(BootstrapOptions.SectionName));

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILogQueryService, LogQueryService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IQuotaRepository, QuotaRepository>();
builder.Services.AddScoped<IQuotaService, QuotaService>();
builder.Services.AddScoped<ICompanyInvitationRepository, CompanyInvitationRepository>();
builder.Services.AddScoped<ICompanySelfService, CompanySelfService>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AstraPolicies.SuperUser, policy => policy.RequireClaim(AstraClaims.Role, RoleCode.SuperUser))
    .AddPolicy(AstraPolicies.Diagnostics, policy => policy.RequireClaim(AstraClaims.Role, RoleCode.SuperUser, RoleCode.SysAdmin));

builder.Services.AddHttpClient<IMailClient, MailClient>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MailClientOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        client.BaseAddress = new Uri(options.BaseUrl);
});

var app = builder.Build();

app.UseAstraPipeline();

app.UserEndpoints_Map();
app.LogEndpoints_Map();
app.AuthEndpoints_Map();
app.CompanyEndpoints_Map();
app.CatalogEndpoints_Map();
app.QuotaEndpoints_Map();
app.CompanySelfEndpoints_Map();

app.Run();

return 0;

public partial class Program;
