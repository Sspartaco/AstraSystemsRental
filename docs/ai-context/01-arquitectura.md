# Arquitectura general — AstraSystemsRental

## Qué es este proyecto

Sistema de renta/gestión con acceso por demos temporales (3 días) o planes pagos. El usuario de este repo a veces lo llama de palabra "astraldemo", pero el nombre real del repo y de la solución es **AstraSystemsRental**.

## Mapa de soluciones

Cada carpeta de primer nivel es un proyecto .NET **standalone**, con su propio `.slnx` (no hay una solución raíz que agrupe todo). Se orquestan juntos vía `docker-compose.yml` en la raíz del repo.

| Solución | Puerto host | Rol |
|---|---|---|
| `AstraSystemsRental.Base` | — | Librería NuGet compartida (paquete local, ver más abajo) |
| `AstraSystemsRental.Gateway` | 8080 (HTTP) / 8443 (HTTPS) | YARP reverse proxy — único punto de entrada |
| `AstraSystemsRental.Users.Api` | 5001 / 5443 | Identidad, auth, catálogo de nodos/planes/roles |
| `AstraSystemsRental.Mail.Api` | 5006 / 5446 | Emails transaccionales (Gmail SMTP) |
| `AstraSystemsRental.Vehicles.Api` | 5011 | Cotización de vehículos por placa (ver `03-feature-vehiculos-placas.md`) |
| `AstraSystemsRental.Front` | 5000 (HTTP) / 8444 (HTTPS) | MVC server-rendered, la UI que usa la gente |

**Regla para levantar todo:** `./run.ps1 up` (usa `docker-compose.yml`). Copia `.env.example` a `.env` en el primer uso si no existe.

## `AstraSystemsRental.Base` — la librería compartida

Paquete NuGet **local** (feed en `nuget-local/`, referenciado en `nuget.config`), versión gestionada centralmente en `Directory.Packages.props`. Todas las APIs (Users, Mail, Vehicles) lo referencian. Provee:

- **Bootstrap de API**: `AddAstraApi(AstraApiOptions)` + `UseAstraPipeline()` — configura OpenAPI/Scalar, rate limiting, JWT auth (opcional), health check en `/health`, exception handling middleware, security headers.
- **Envelope de respuesta estándar**: `OperationResult` (server-side, con `.Ok()`, `.NotFound()`, `.Fail()`, etc.) se convierte a `ApiResponse` (`{success, data, errors, traceId}`) vía `.ToResult(HttpContext)`.
- **Persistencia EF Core**: `AddAstraDbContext<TContext>(configuration)` registra el `DbContext` con SQL Server + retry-on-failure. `BaseRepository<TContext, TEntity>` da CRUD genérico (`GetByIdAsync`, `GetPagedAsync`, `AddAsync`, etc.) — **es EF Core, no Dapper**, a pesar de que el README mencione Dapper en algún lugar desactualizado.
- **JWT RS256**: `JwtTokenIssuer`, `RsaKeyProvider` (lee claves de archivos `.pem`), claims estándar en `AstraClaims` (`sub`, `email`, `role`, `plan`, `node` multivaluado, `sub_end`).
- **Validación**: `Guard` — builder fluido de errores de validación (`.NotEmpty()`, `.MaxLength()`, `.Must()`).

**Patrón de arranque de una API nueva** (ya replicado 2 veces: Mail.Api y Vehicles.Api):
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddAstraApi(new AstraApiOptions { ServiceName = "...", PathBase = "/apiXxx" });
builder.Services.AddAstraDbContext<TuDbContext>(builder.Configuration); // si necesita BD
// ... registrar tus servicios propios
var app = builder.Build();
app.UseAstraPipeline();
app.TusEndpoints_Map();
app.Run();
public partial class Program;
```

## Gateway — único punto de entrada

`AstraSystemsRental.Gateway` es YARP puro + un middleware de control de acceso propio (ver `02-sistema-nodos-accesos.md` para el detalle). Rutea por prefijo de path:

```json
"Routes": { "users-route": {"ClusterId": "users-cluster", "Match": {"Path": "/apiUsers/{**catch-all}"}}, ... }
"Clusters": { "users-cluster": {"Destinations": {"users": {"Address": "http://localhost:5001/"}}}, ... }
```

Cada API nueva necesita: un `{nombre}-route`/`{nombre}-cluster` en `appsettings.json` del Gateway, y su servicio correspondiente en `docker-compose.yml` con las variables de entorno `ReverseProxy__Clusters__{nombre}-cluster__Destinations__{nombre}__Address`.

## Front — MVC server-rendered, NO SPA

`AstraSystemsRental.Front` es ASP.NET Core MVC/Razor clásico en **.NET 10**, sin Blazor ni React/Angular. Interactividad vía **htmx** (atributos `hx-get`/`hx-post`/`hx-target`/`hx-swap`/`hx-trigger`). UI con **Tailwind 3.4 + Flowbite 2.5**, dark theme por defecto.

### Organización: Feature folders

```
Features/{Nombre}/{Nombre}Controller.cs
Features/{Nombre}/Views/*.cshtml
```

Habilitado por `Shared/FeatureViewLocationExpander.cs` — Razor busca vistas dentro de la carpeta de cada feature en vez del `Views/{Controller}/` clásico de MVC.

Features existentes: `Admin` (gestión usuarios/planes/nodos), `Auth` (login/forgot password), `Home` (dashboard + página de suscripción expirada), `Fleet` (cotización de vehículos, ver doc 03).

### Patrón de controller + búsqueda con htmx

Referencia viva: `Features/Admin/AdminController.cs` + `Views/Users.cshtml` + `Views/_UsersTable.cshtml`.

- Input de búsqueda: `hx-get="/admin/users" hx-target="#users-table" hx-swap="innerHTML" hx-trigger="submit, keyup changed delay:400ms from:input[name=search]"` — debounce de 400ms sin JS custom.
- El controller devuelve `View(...)` completa en GET normal, o `PartialView("_Nombre", modelo)` cuando la request viene de htmx (mismo action sirve ambos casos).
- Toggles/switches (ej. asignar nodo a plan): un `<form>` por fila con inputs hidden + `hx-post`, re-renderiza el grid completo (`_PlansGrid.cshtml`).

### Cliente HTTP hacia el Gateway

`Services/GatewayClient.cs` (`IGatewayClient`). Métodos:
- `GetAsync(path, token, ct)` / `GetAsync(path, token, nodeKey, ct)` — el segundo overload agrega header `X-Astra-Node`.
- `SendAsync(method, path, token, body, ct)` — legado, no propaga errores del backend al caller.
- `SendForDataAsync(method, path, token, body, nodeKey, ct)` — devuelve `GatewayResponse { StatusCode, Data, Errors }`, permite inspeccionar el resultado real (404, 400 con mensajes, etc.) en vez de solo bool.

Desenvuelve automáticamente el envelope `{success, data, errors, traceId}` de las APIs.

### Sesión y autenticación en el Front

`ISessionService` guarda el JWT en una cookie protegida (`IDataProtector`), no en sesión de servidor. `ICurrentUser`/`AstraPrincipal` (`Shared/Security/`) expone `HasNode(key)`, `IsSuperUser`, `SubscriptionExpired`, `DaysRemaining` — se arma leyendo los claims del JWT decodificado (`JwtReader`), vía `AstraSessionMiddleware`.

### Clases CSS propias (Tailwind `@layer components`, en `wwwroot/css/app.css`)

- `.astra-panel` — contenedor grande (tablas, paneles).
- `.astra-card` / `.astra-kpi` — card individual (resultado único, métrica).
- `.astra-btn` / `.astra-btn-primary` / `.astra-btn-ghost` — botones.
- `.astra-input` / `.astra-label` — formularios.
- `.astra-badge` — chips de estado, con colores semánticos: `bg-accent/15 text-accent` (éxito), `bg-warn/15 text-warn` (advertencia), `bg-danger/15 text-danger` (error/fallo).
- `.astra-sidebar-item` (+ `.active`) — ítems del sidebar.
- `.astra-alert-error` — mensaje de error inline.

**Convención**: tablas para listados paginados (Admin/Users), cards para resultado único o datos puntuales (KPIs, resultado de una búsqueda).

## Users.Api — dueña de identidad y catálogo

Puerto 5001. Esquemas SQL: `users`, `subscriptions`, `access`, `companies` (todos en la misma base `AstraSystemsRental`). Scripts idempotentes en `AstraSystemsRental.Users.Api/SolutionItems/db/`, ejecutados en el orden que define `run.ps1 reset-db`.

Endpoints relevantes: `AuthEndpoints` (login/registro/confirmación), `CatalogEndpoints` (CRUD de nodos/planes/roles, ver doc 02), `CompanyEndpoints`, `UserEndpoints`.

**Patrón CLI dentro de una API**: `Program.cs` intercepta `args[0]` antes de construir el `WebApplicationBuilder` normal:
```csharp
if (args.Length > 0 && args[0] == MiComando.CommandName)
    return await MiComando.RunAsync(args);
```
Ejemplo real: `Cli/SeedSuperUserCommand.cs` (`dotnet run -- seed-superuser --email ... --password ...`). Replicado en Vehicles.Api para el import del catálogo de placas (`Cli/ImportVehicleCatalogCommand.cs`).

## Cómo correr y depurar localmente

- **Todo en Docker**: `./run.ps1 up` — construye y levanta los 6 servicios.
- **Reset de BD**: `./run.ps1 reset-db` — aplica los scripts SQL de Users.Api en orden contra SQL Server local (usa `SQLCMD.EXE`, ruta hardcodeada en el script: `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE`).
- **Rebuild de un solo servicio**: `docker compose up -d --build {nombre-servicio}` (ej. `api-vehicles`).
- **Logs**: `docker logs {nombre-contenedor} --tail N` (contenedores se llaman `astralrental-{servicio}`, ej. `astralrental-api-vehicles`).
- **Compilar sin Docker** (rápido, para detectar errores de compilación antes de reconstruir imagen): `dotnet build {ruta}/{proyecto}.csproj -v quiet` desde la raíz del repo.
