# Plan aprobado: registro operativo de vehículos + compañías + cuotas

Este documento reemplaza/complementa `05-registro-vehiculos.md` (que era una propuesta previa) con el **plan definitivo aprobado** tras una ronda de revisión de seguridad. El texto completo del plan vive en `C:\Users\Jonathan\.claude\plans\ten-en-cuenta-que-joyful-quasar.md` — si ese archivo ya no existe (se limpian con el tiempo), este resumen es la referencia.

## Qué se aprobó construir

1. **Autoservicio de compañías** (Users.Api): crear compañía propia, invitar por email con token hasheado, listar/remover miembros, transferir propiedad del último owner.
2. **Cuotas genéricas por plan+nodo** (Users.Api): tabla `PlanNodeQuotas` + contador atómico `PlanNodeUsageCounters`, con endpoints `reserve`/`release` — reutilizable para cualquier nodo futuro, no solo vehículos.
3. **Registro operativo de vehículos** (Vehicles.Api): `FleetVehicles` + `FleetVehicleStatusHistory` + `FleetVehicleOdometerReadings` + `FleetVehicleDocuments` (solo metadatos, sin storage de archivos).
4. **Fix de un IDOR real ya existente**: `QuoteRequests` nunca filtraba por propietario — se corrige en el mismo esfuerzo.

## Decisiones de seguridad clave (no relajar sin repasar el porqué)

- `X-Astra-Company` inválido → **403 explícito**, nunca fallback silencioso a flota personal.
- Cuota consumida con `UPDATE ... WHERE CurrentCount < MaxCount` atómico — nunca "contar y luego insertar" en dos pasos (condición de carrera).
- Endpoint de cuotas nunca acepta `ownerType`/`ownerId` por querystring — siempre resuelto del token.
- Tokens de invitación: se genera un token real de 256 bits, se persiste solo su **hash SHA-256** (`TokenHash`), igual principio que el hashing de contraseñas.
- Remover a alguien de una compañía corta su capacidad de **escribir** en minutos (revalidación en caliente contra `CompanyMembers`), no espera el TTL completo del JWT (60 min).
- Todo query de `FleetVehicles`/satélites filtra por `Id` + `OwnerType` + `OwnerId` — nunca `GetByIdAsync` plano expuesto a estos endpoints.

## Prerrequisito de infraestructura

`IAstraRequestContext` se agrega a `AstraSystemsRental.Base` — esto implica **bump de versión del paquete NuGet local** (`1.2.0` → `1.3.0`) y actualizar la referencia en `Directory.Packages.props` de Users.Api y Vehicles.Api. Es el primer paso antes de poder tocar cualquier endpoint nuevo.

## Alcance planes

- `Demo` = 3 vehículos, info básica (formulario oculta VIN/chasis/compra; si llegan igual, el backend los ignora).
- `Basic` (mostrado como "Standard" en UI, `Code` sin cambiar) = 80 vehículos.
- `Full` (plan nuevo) = 1000 vehículos, 30 días.
- Downgrade con vehículos por encima de la nueva cuota: no retroactivo, se conservan los existentes, solo se bloquean altas nuevas.

## Preguntas que quedaron abiertas al aprobar

1. Nombre exacto del nodo (`vehicle-registry` es el propuesto, no confirmado con 100% de certeza).
2. TTL del caché de membresía para la revalidación en caliente (propuesto 2 minutos).

## Estado (actualizado durante implementación)

**Pasos 0-4 completados y compilando limpio.** Pasos 5-7 pendientes.

- **Paso 0** ✅: `AstraSystemsRental.Base` bumpeado a **1.3.0** (nupkg regenerado en `nuget-local/`, referencia actualizada en `Directory.Packages.props`). Agregado: `AstraClaims.Company`, `OwnerType`/`OwnerContext` (`Security/OwnerContext.cs`), `IAstraRequestContext`/`AstraRequestContext` (resuelve `Owner` desde JWT + header `X-Astra-Company`, lanza `CompanyContextForbiddenException` si el companyId pedido no está en el claim — capturada en `ExceptionHandlingMiddleware` y devuelta como 403 `CompanyContextForbidden`). Registrado `AddScoped<IAstraRequestContext, AstraRequestContext>` dentro de `AddAstraApi()`. `AuthService.LoginAsync` ahora emite el claim `company` multivaluado (uno por cada `CompanyMembers` del usuario) vía `IUserRepository.GetMemberCompanyIdsAsync` (nuevo).
- **Paso 2** ✅: Users.Api, schema `subscriptions`. Tablas `PlanNodeQuotas` (PlanId+NodeKey→MaxCount) y `PlanNodeUsageCounters` (OwnerType+OwnerId+NodeKey→CurrentCount) en `07_schema_plan_node_quotas.sql`. `QuotaRepository` implementa reserva **atómica** vía `UPDATE...WHERE CurrentCount < MaxCount` con `ExecuteSqlInterpolatedAsync` (no read-then-write). Endpoints `GET/POST /apiUsers/quotas/{nodeKey}`, `/reserve`, `/release` — **nunca** aceptan ownerType/ownerId por querystring, siempre resueltos de `IAstraRequestContext.Owner`. Seed: Demo=3, Basic=80, Full=1000 para nodo `vehicle-registry`.
- **Paso 3** ✅: Users.Api, schema `companies`. Tabla `CompanyInvitations` (`08_schema_company_invitations.sql`) con `TokenHash` (SHA-256 del token real de 256 bits, el token en claro nunca se persiste). Servicio `CompanySelfService` completo: crear compañía propia, listar "mis compañías", invitar por email (rate-limit: 1 pendiente por email, reutiliza la fila si reintenta), revocar invitación, aceptar invitación (valida email del token = email del usuario logueado), remover miembro (409 si es el único owner), transferir propiedad, y `CheckActiveMembershipAsync` (el endpoint interno `GET /apiUsers/companies/{id}/members/{userId}/is-active` para la revalidación en caliente del Paso 1 del plan — **este endpoint se creó pero Vehicles.Api todavía no lo consume**, eso es parte del Paso 5). Endpoints bajo `/apiUsers/companies/self/*`, autorización genérica (no SuperUser) — separado del `/companies` viejo que sigue intacto y SuperUser-only.
- **Paso 4** ✅: fix del IDOR real en `QuoteRequests`. `QuoteEndpoints.GetQuoteStatus` ahora usa `IAstraRequestContext.UserId` (ya no lee el claim inline) y llama a `IVehicleQuoteService.GetQuoteStatusAsync(requestId, userId, ct)` → `IQuoteRepository.GetOwnedQuoteRequestAsync(requestId, userId, ct)` (nuevo método, filtra por `RequestedByUserId`). El método viejo `GetQuoteRequestAsync(requestId)` sin filtro se dejó intacto porque lo sigue usando `QuoteOrchestrationService` internamente (proceso de sistema, no request de usuario — ahí sí es correcto sin filtro).
- **Paso 5** ✅: Vehicles.Api, schema `vehicles`. 4 tablas nuevas en `05_schema_fleet_vehicles.sql` (`FleetVehicles` con `RowVersion`/concurrencia optimista + único índice `OwnerType+OwnerId+PlateNumber`, `FleetVehicleStatusHistory`, `FleetVehicleOdometerReadings`, `FleetVehicleDocuments`). `IUsersApiClient`/`UsersApiClient` (HttpClient tipado, `BaseUrl` configurable vía `UsersApi:BaseUrl`, reenvía `Authorization` y `X-Astra-Company` del request entrante hacia Users.Api) con 4 operaciones: `GetQuotaAsync`, `TryReserveQuotaAsync`, `ReleaseQuotaAsync`, `IsActiveCompanyMemberAsync`. `FleetVehicleRepository` sigue el patrón `GetOwnedAsync`/`OwnedVehicleIds()` estricto — ninguna tabla satélite se consulta sin revalidar propiedad vía subquery contra `FleetVehicles`. `FleetVehicleService`: saga de 2 pasos en `CreateAsync` (reserva cuota en Users.Api → inserta → si el insert lanza, libera la cuota reservada), gating de campos "básicos" en plan Demo (si `IsCurrentPlanDemoAsync` da true, los campos avanzados —VIN, chasis, motor, compra, notas— se ignoran silenciosamente al crear/editar, sin rechazar el request), `EnsureCompanyMembershipAsync` revalida en caliente contra Users.Api en **toda** escritura cuando `Owner.OwnerType==Company` (create/update/delete/status/odometer/documents) devolviendo 403 `CompanyContextForbidden` si ya no es miembro. Endpoints en `/apiVehicles/fleet-vehicles/*` (11 rutas: CRUD + status-transitions + status-history + odometer-readings + documents). Gateway no necesitó cambios — ya rutea por catch-all de prefijo (`/apiUsers/{**catch-all}`, `/apiVehicles/{**catch-all}`), así que los endpoints nuevos ya son alcanzables. `docker-compose.yml` actualizado: `api-vehicles` ahora tiene `UsersApi__BaseUrl=http://api-users:8080` y `depends_on: api-users`.
- **Paso 6** ✅: Front completo.
  - `GatewayClient` extendido con overloads `(..., companyId)` que setean `X-Astra-Company` — sobrecarga adicional sobre la ya existente de `nodeKey`, sin romper firmas previas.
  - `ISessionService` gana `GetActiveCompanyId()`/`SetActiveCompanyId(long?)` en una cookie **separada** de la del JWT (`astra.company`, HttpOnly, no protegida con DataProtector porque solo guarda un id numérico, no un secreto).
  - `Features/CompanyContext/CompanyContextController.cs` — `POST /context/company` setea la cookie y redirige al `Referer`.
  - `Shared/ViewComponents/CompanyContextSwitcherViewComponent` + vista en `Shared/Views/Shared/Components/CompanyContextSwitcher/Default.cshtml` — dropdown Alpine.js en el header de `_Layout.cshtml`, junto al menú de usuario (que tuvo que ganar su propio `x-data` local porque antes vivía en el div padre compartido — gotcha real, ver doc 04 si se generaliza).
  - `Features/Companies/` completo: crear compañía propia, listar "mis compañías", ver detalle con miembros, invitar (muestra el token una única vez en un cuadro de aviso — el backend nunca lo vuelve a exponer, coherente con que solo se persiste el hash), quitar miembro.
  - `Features/VehicleRegistry/` completo: nodo `vehicle-registry` (agregado al seed de Users.Api en el Paso 2, ya en `PlanNodes` de Demo/Basic/Full). Listado con filtro de estado+búsqueda (htmx debounce, mismo patrón que Admin/Fleet). Alta en **asistente de 3 pasos** con Alpine (`x-data="{step: 1}"`, sin recargar página entre pasos, un solo POST final). Detalle con **4 tabs** Alpine (identificación, kilometraje, documentos, historial) — cada tab con su propio formulario htmx que re-renderiza `_VehicleDetail` completo tras cada acción (cambio de estado, nueva lectura de odómetro, nuevo documento).
  - Build 0 errores en Front, **8/8 tests existentes siguen pasando** (no se rompió nada de Auth/Admin/Home/Fleet).
- **Paso 7** ✅: `scripts/run.ps1 reset-db` reescrito para aplicar scripts de **ambos** directorios (Users.Api y Vehicles.Api) en el orden correcto — antes solo tocaba Users.Api, los 4 scripts de Vehicles.Api (incluido el nuevo `05_schema_fleet_vehicles.sql`) se aplicaban a mano. `99_deployment.sql` de Users.Api también actualizado con sus 2 scripts nuevos.

## Estado final: LOS 7 PASOS DEL PLAN ESTÁN IMPLEMENTADOS

Verificación de compilación completa corrida al cierre: los 6 proyectos del repo (`Base`, `Users.Api`, `Vehicles.Api`, `Mail.Api`, `Gateway`, `Front`) compilan con **0 errores**. Tests del Front: 8/8 OK.

**Lo que falta para considerar esto "probado en vivo" (no implementado, solo no verificado end-to-end todavía)**:
1. Correr `./scripts/run.ps1 reset-db` contra una base real y confirmar que las tablas nuevas se crean sin error (los scripts nunca se ejecutaron contra SQL Server, solo se revisaron por inspección).
2. Levantar todo con `./scripts/run.ps1 up` y probar el flujo real: crear compañía → invitar → aceptar → cambiar contexto → registrar vehículo → alcanzar cuota → verificar 403 en accesos cruzados. Ver la sección "Verificación end-to-end" del plan completo (13 puntos) para el checklist exacto.
3. No hay tests automatizados nuevos para nada de esto (compañías, cuotas, fleet vehicles) — solo se verificó que compila y que los tests *preexistentes* del Front no se rompieron.

**Importante para quien retome**: todo lo del Paso 0-4 ya compila limpio (`dotnet build` verificado en Users.Api y Vehicles.Api tras cada paso). No hay tablas nuevas todavía aplicadas a ninguna base de datos real — los scripts SQL existen en el repo pero no se corrieron (`scripts/run.ps1 reset-db` aún no los incluye, ver Paso 7).
