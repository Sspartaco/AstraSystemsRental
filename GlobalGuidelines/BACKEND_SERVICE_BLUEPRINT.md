# Backend Service Blueprint — patrón replicable de servicio de API

For AI assistants. Compressed. Extraído del refactor de Fase 0 sobre `FleetVehicleService`/`UsersApiClient`/`FleetVehicleRepository` (Vehicles.Api) — mirror este patrón exactamente al escribir un servicio nuevo.

Reference files: `AstraSystemsRental.Vehicles.Api/src/AstraSystemsRental.Vehicles.Api/Services/FleetVehicleService.cs`, `Services/UsersApiClient.cs`, `Persistence/FleetVehicleRepository.cs`, `Dtos/FleetVehicleDtos.cs`.

---

## 1. Estructura de un método de servicio (pasos explícitos, no mezclados)

Cada método público de un servicio sigue este orden — no mezclar responsabilidades en un solo bloque:

```
1. Validar input (Guard) → return OperationResult.Fail(guard.Errors) si falla
2. Verificar autorización/membresía (si aplica, vía llamada cross-API)
3. Verificar reglas de negocio contra el propio repositorio (duplicados, existencia)
4. Orquestar llamadas cross-API (reservar cuota, etc.) — SIEMPRE vía CrossApiResult
5. Construir la entidad (función pura, sin I/O, testeable aislada de lo anterior)
6. Persistir, con compensación explícita si algo posterior puede fallar
7. Mapear a DTO de respuesta y devolver OperationResult
```

Si un método mezcla 3+ de estos pasos en un bloque sin separación clara, extraer a métodos privados nombrados por lo que hacen (`EnsureCompanyMembershipAsync`, `ReserveQuotaAsync`, `BuildEntity`, `PersistWithCompensationAsync`) — no un solo método de 60+ líneas.

## 2. Mapeo — propiedades nombradas, no constructor posicional

Un DTO de respuesta o request con más de ~6-8 campos usa `record` con propiedades `init`, nunca constructor posicional:

```csharp
// MAL — 20 parámetros posicionales del mismo tipo string?/decimal?, swap silencioso compila
public sealed record FleetVehicleResponse(long Id, string PlateNumber, string? VehicleClass, ...);

// BIEN
public sealed record FleetVehicleResponse
{
    public required long Id { get; init; }
    public required string PlateNumber { get; init; }
    public string? VehicleClass { get; init; }
    ...
}
```

Mapeo entidad→DTO: inicializador con nombres explícitos, nunca un `new(a, b, c, ...)` con más de 6-8 argumentos posicionales:

```csharp
private static FleetVehicleResponse ToResponse(FleetVehicle v) => new()
{
    Id = v.Id,
    PlateNumber = v.PlateNumber,
    ...
};
```

Para queries paginadas con proyección, usar `MappingExtensions.ProjectPaged<TSource,TResult>` (`AstraSystemsRental.Base/Mapping/`) en vez de escribir la paginación a mano.

## 3. `OperationResult` — único tipo de retorno de servicio

Todo método público de un servicio de negocio devuelve `Task<OperationResult>`. Nunca:
- Lanzar una excepción de negocio prevista (placa duplicada, cuota agotada, etc.) esperando que la agarre el middleware genérico — eso es solo para errores verdaderamente inesperados.
- Inventar un tipo de resultado propio por servicio (`BootstrapResult`, etc.) — usar `OperationResult.Ok/Created/Fail/NotFound/Conflict/Unauthorized/Forbidden`.
- Devolver `bool`/`void` desde un método que puede fallar por más de una razón — eso colapsa casos semánticamente distintos.

## 4. `CrossApiResult` — llamadas entre APIs

Todo cliente HTTP interno (`IUsersApiClient`, y el equivalente que construya cualquier API nueva hacia otra) devuelve `Task<CrossApiResult>`, nunca `bool` ni `void`:

```csharp
public sealed record CrossApiResult(bool Success, bool Unreachable, string? Error);
```

El caller distingue explícitamente los 3 casos:
```csharp
var result = await usersApiClient.IsActiveCompanyMemberAsync(...);
if (result.Success) return null;
return result.Unreachable
    ? OperationResult.Fail("UpstreamUnavailable", HttpStatusCode.ServiceUnavailable)
    : OperationResult.Forbidden("CompanyContextForbidden");
```

Nunca dejar que un fallo de llamada cross-API se pierda solo en un `logger.LogWarning` sin que el caller decida qué hacer.

## 5. Patrón de repositorio

Todo repositorio con un `DbContext` hereda `BaseRepository<TContext,TEntity>`:

```csharp
public sealed class FleetVehicleRepository(AstraVehiclesDbContext context)
    : BaseRepository<AstraVehiclesDbContext, FleetVehicle>(context), IFleetVehicleRepository
```

Los métodos genéricos (`AddAsync`, `GetFirstOrDefaultAsync`, `AnyAsync`, `CountAsync`, `GetPagedAsync`, `SaveChangesAsync`) se heredan y se delega en ellos — nunca reimplementar paginación, clamp de página, o CRUD básico a mano. Los métodos de dominio específicos (`GetOwnedAsync`, `PlateExistsForOwnerAsync`) se agregan encima, usando `GetFirstOrDefaultAsync`/`AnyAsync` de la base cuando aplica.

Para un repositorio que gestiona un agregado con varias entidades relacionadas (ej. `FleetVehicleRepository` también gestiona `FleetVehicleStatusHistory`, `FleetVehicleDocument`), la entidad principal del agregado es el `T` genérico de `BaseRepository`; las subentidades se acceden vía `DbContext.OtroSet` directo dentro del mismo repositorio — no requieren su propio `BaseRepository`.

## 6. Patrón de cuotas por plan+nodo

Reutilizar `PlanNodeQuotas`/`PlanNodeUsageCounters` (Users.Api) para cualquier nodo nuevo que necesite límite por plan. Secuencia en el servicio consumidor:

```
1. TryReserveQuotaAsync(nodeKey) → CrossApiResult
   - Success=false, Unreachable=false → OperationResult.Fail("QuotaExceeded", Conflict)
   - Success=false, Unreachable=true  → OperationResult.Fail("UpstreamUnavailable", ServiceUnavailable)
2. Construir y persistir la entidad
3. Si la persistencia falla → ReleaseQuotaAsync(nodeKey) (compensación)
   - Si la compensación también falla → registrar en tabla de compensaciones pendientes
     (ver vehicles.PendingQuotaCompensations), nunca perder el evento en silencio
```

## 7. Patrón owner-scoped (multi-tenant)

Todo acceso a datos de una entidad ligada a `OwnerType`/`OwnerId` filtra por owner en la query misma, nunca en una segunda comprobación tras traer el dato completo:

```csharp
// BIEN — el filtro de owner es parte de la condición de búsqueda
Task<FleetVehicle?> GetOwnedAsync(long id, OwnerContext owner, CancellationToken ct)
    => GetFirstOrDefaultAsync(v => v.Id == id && v.OwnerType == owner.OwnerType && v.OwnerId == owner.OwnerId, ct);
```

Nunca exponer un `GetByIdAsync(long id)` plano desde un endpoint multi-tenant — es la vía directa a un IDOR. Si una entidad hija no tiene `OwnerType`/`OwnerId` propio (ej. `FleetVehicleOdometerReading`), el filtro de propiedad se revalida vía join contra el padre en cada acceso, nunca asumiendo que un id de la URL ya fue validado antes.

Para referencias cross-servicio (ej. `FleetVehicleId` en `Maintenance.Api` apuntando a una entidad de `Vehicles.Api`), no hay FK física — se valida con una llamada síncrona equivalente a `EnsureCompanyMembershipAsync`.

---

## Logs de aplicación persistidos (`logs.ApplicationLogs`)

`AstraSystemsRental.Base` incluye el sink; **una API no tiene que hacer nada para participar**. `AddAstraApi` registra `ApplicationLogWriter` y `ExceptionHandlingMiddleware` escribe ahí las tres ramas que ya capturaba (`CompanyContextForbidden` → Warning, `BadHttpRequest` → Warning, no manejada → Error), con `TraceId`, ruta, método, status, usuario y stack trace completo.

Condición de activación: `ApplicationLogs:ConnectionString`, o en su defecto `ConnectionStrings:Default`. Una API sin BD propia (como Reports) necesita que se le pase una cadena explícita en `docker-compose.yml` para poder loguear; sin ella el sink queda inerte y no falla.

Propiedades del writer que hay que preservar al tocarlo:

- **Nunca bloquea la request.** Cola en memoria + `BackgroundService`. Si la escritura falla, hace `LogWarning` y sigue: un problema de logging jamás debe tumbar una petición.
- **Cola acotada** (`MaxQueued = 500`). Bajo una tormenta de errores descarta en vez de crecer sin límite.
- **Trunca a la longitud de la columna** antes de insertar, para que un mensaje largo no reviente el `INSERT`.

Lo que **no** se registra: los `OperationResult.Fail`/`NotFound` de negocio. Un 404 o un 400 de validación son respuestas correctas, no incidentes. Solo se persisten excepciones. Diagnóstico habitual: si un error esperado no aparece en la tabla, verificar que sea realmente una excepción y no un `Fail` de negocio.

---

## Rol `SysAdmin` y la política `Diagnostics`

Rol para desarrollo/soporte: ve diagnóstico sin ser dueño del negocio. Sembrado en `10_schema_logs.sql` con `RoleNodes.NodeKey = '*'`.

```csharp
.AddPolicy(AstraPolicies.Diagnostics, policy =>
    policy.RequireClaim(AstraClaims.Role, RoleCode.SuperUser, RoleCode.SysAdmin));
```

En el Front el gate es `AstraPrincipal.CanSeeDiagnostics`. **Ojo con `HasNode`**: devuelve `true` para SuperUser en cualquier nodo, así que no sirve para distinguir un rol de otro — para gating por rol hay que comprobar el rol explícitamente. El catálogo de nodos usa `DiagnosticsOnly` (además de `SuperUserOnly`) y los tres puntos que filtran nodos deben respetar ambos flags: `SidebarViewComponent`, `ProfileController` y el propio controlador.

Verificado en vivo: `Standard` → 403, `SysAdmin` → 200.

---

## Gotcha: publicar `AstraSystemsRental.Base` al feed local

`dotnet pack` puede empaquetar un binario obsoleto de `obj/Release` y producir un `.nupkg` **sin los tipos nuevos** — el síntoma es desconcertante: las APIs dejan de compilar por tipos que sí existen en el fuente (`IAstraRequestContext no se encontró`).

Secuencia correcta:

```bash
dotnet build <Base.csproj> -c Release          # build explícito primero
dotnet pack  <Base.csproj> -c Release -o nuget-local --no-build
rm -rf ~/.nuget/packages/astrasystemsrental.base/<version>   # purgar la extraída
```

Verificación rápida antes de dar por buena la publicación: abrir el `.nupkg` como zip y comprobar que `lib/net10.0/*.dll` contiene el nombre del tipo nuevo. Subir siempre la versión en `Base.csproj` **y** en `Directory.Packages.props` en el mismo commit.

---

## Refresh tokens con rotación (`users.RefreshTokens`)

El access token dura 60 min. Sin refresh, el usuario queda expulsado cada hora — tolerable en web, inaceptable en móvil. Reglas del diseño implementado:

- **Se persiste el hash SHA-256, nunca el token en claro.** Una filtración de la tabla no da sesiones utilizables.
- **Rotación en cada uso**: `/auth/refresh` revoca el token presentado y emite uno nuevo, enlazándolos por `ReplacedByTokenHash`.
- **Detección de reuso**: si llega un token *ya revocado*, se revoca **toda la familia del usuario**. Es la señal de que alguien clonó un token; se corta la sesión completa en vez de dejar convivir al atacante con el usuario legítimo. Verificado en vivo: reusar un token rotado dejó los 3 tokens de la familia en `revocado`.
- `LoginResponse` gana `refreshToken`/`refreshTokenExpiresAtUtc` como **campos aditivos** — los clientes que no los usan siguen funcionando sin cambios.

`AuthService.BuildClaimsAsync` es el único punto que arma los claims; login y refresh lo comparten. Si se agrega un claim nuevo, va ahí y ambos flujos lo heredan.

---

## Rate limiting: partición por identidad, y el orden del middleware

Corregido respecto a la versión anterior de este documento. Dos cambios, ambos necesarios:

1. **La clave de partición ya no es la IP.** `ResolvePartitionKey` usa `user:{id}` del claim, cae a `X-Forwarded-For` y solo como último recurso a `RemoteIpAddress`. Con la partición por IP, todo el tráfico detrás de Docker (o de una red móvil con NAT) compartía un único cubo.
2. **`UseRateLimiter()` va DESPUÉS de `UseAuthentication()`.** Estaba antes, con lo cual `context.User` siempre venía vacío y cualquier partición por identidad habría degradado silenciosamente a la rama de IP. Este es el tipo de bug que no falla, solo funciona mal.

Límite por defecto: **600/min por identidad** (antes 120 global). Verificado: 200 peticiones consecutivas → 200 OK, 0 respuestas 429.

---

## Filtrar en el servidor, nunca en la página ya traída

Bug de correctitud real: `MaintenanceController` filtraba reservas por taller y rango de fechas **en memoria sobre la página de 50** que ya había pedido. Una reserva que cumplía el filtro pero caía en la página 2 simplemente no aparecía — sin error, sin aviso.

Regla: si un filtro puede excluir registros, tiene que viajar como parámetro de consulta hasta el repositorio. Filtrar el `PagedResult` que ya volvió solo es válido cuando el conjunto completo cabe en la página, y eso rara vez se puede garantizar.

Al agregar un filtro nuevo, la cadena completa es: endpoint (`[FromQuery]`) → interfaz de servicio → servicio → interfaz de repositorio → `queryBuilder`. Saltarse un eslabón produce exactamente este bug.

---

## Escribir exige tracking: `AsNoTracking` devuelve 200 sin guardar nada

⚠️ **El bug más caro de esta sesión.** Costó cinco rondas de diagnóstico porque *no falla*: responde `200 OK`, devuelve la entidad con los valores nuevos, y no escribe una sola fila.

`BaseRepository.GetFirstOrDefaultAsync` usa **`AsNoTracking()`**:

```csharp
public virtual async Task<T?> GetFirstOrDefaultAsync(...)
    => await EntitySet.AsNoTracking().FirstOrDefaultAsync(predicate, ct);
```

La entidad que devuelve **no la sigue el `DbContext`**. Modificar sus propiedades no marca nada como `Modified`, así que `SaveChangesAsync()` no encuentra cambios y **no emite ningún `UPDATE`**. Sin excepción, sin log, sin pista.

`FleetVehicleService.UpdateAsync`, `ChangeStatusAsync` y `AddDocumentAsync` leían así. Los tres respondían 200 sin persistir — en la web *y* en la app.

**Regla: todo método que modifique y llame a `SaveChangesAsync` debe leer con tracking.**

```csharp
// LEER (listados, detalle): sin tracking, es más rápido
public Task<FleetVehicle?> GetOwnedAsync(...)
    => GetFirstOrDefaultAsync(...);

// ESCRIBIR: con tracking, o el UPDATE nunca sale
public Task<FleetVehicle?> GetOwnedForUpdateAsync(long id, OwnerContext owner, CancellationToken ct)
    => DbContext.FleetVehicles.FirstOrDefaultAsync(
        v => v.Id == id && v.OwnerType == owner.OwnerType && v.OwnerId == owner.OwnerId, ct);
```

### Cómo detectarlo rápido

El síntoma es *"guardé y no pasó nada"* con la petición devolviendo éxito. La forma más rápida de confirmarlo:

```bash
# ¿el servidor recibió el PUT?
docker logs astralrental-api-vehicles --since 5m 2>&1 | grep -E "Request (starting|finished).*PUT"

# ¿emitió el UPDATE?
docker logs astralrental-api-vehicles --since 5m 2>&1 | grep -c "UPDATE \[vehicles\]"
```

**`PUT 200` + `0 UPDATEs` = falta tracking.** Si el `UPDATE` sale pero los valores son `null`, entonces sí es el payload.

⚠️ `Microsoft.AspNetCore` está en `Warning`: los logs **no muestran el método HTTP ni el tamaño del body**. Para diagnosticar hay que subirlo a `Information` temporalmente — sin eso no se distingue un `GET` de un `PUT`, ni un `PUT` con datos de uno vacío.

### No culpar al cliente antes de mirar el servidor

En este caso perdí dos rondas revisando la app (bindings, comandos, serialización) cuando el fallo estaba en el repositorio y afectaba **igual a la web**. Antes de tocar el cliente: **¿la fila cambió en la base?** Si no cambió y la respuesta fue 200, el problema es del backend.
