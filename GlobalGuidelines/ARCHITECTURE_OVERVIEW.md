# Architecture Overview — AstraSystemsRental

For AI assistants. Compressed. Mapa de servicios + criterio de decisión para dominios nuevos.

---

## Mapa de soluciones

Cada carpeta de primer nivel es un proyecto .NET standalone con su propio `.slnx`, orquestados vía `docker-compose.yml` en la raíz. Detalle completo en `docs/ai-context/01-arquitectura.md`.

| Solución | Puerto host | Rol |
|---|---|---|
| `AstraSystemsRental.Base` | — | Librería NuGet compartida (feed local `nuget-local/`) |
| `AstraSystemsRental.Gateway` | 8080 / 8443 | YARP reverse proxy — único punto de entrada |
| `AstraSystemsRental.Users.Api` | 5001 / 5443 | Identidad, auth, catálogo de nodos/planes/roles, compañías |
| `AstraSystemsRental.Mail.Api` | 5006 / 5446 | Emails transaccionales |
| `AstraSystemsRental.Vehicles.Api` | 5011 | Cotización por placa + registro de flota (`FleetVehicles`) |
| `AstraSystemsRental.Maintenance.Api` | 5016 / 5446 | Rutinas de mantenimiento, control de recorrido, reservas de taller |
| `AstraSystemsRental.Reports.Api` | 5021 / 5461 | **Compositor de métricas** — no tiene BD propia; agrega `/fleet-metrics` + `/maintenance-metrics` |
| `AstraSystemsRental.Front` | 5000 / 8444 | MVC server-rendered, la UI |

### Reports.Api: el patrón "compositor sin esquema propio"

Reports **no consulta esquemas ajenos**. Cada API dueña expone las métricas de su propio esquema (`/fleet-metrics` en Vehicles, `/maintenance-metrics` en Maintenance) y Reports las compone en `/dashboard`. Esto conserva la propiedad del esquema: si Vehicles cambia una columna, ajusta su propio endpoint y Reports no se entera.

Dos consecuencias de diseño a respetar al extenderlo:

- **Las llamadas van directas entre contenedores** (`http://api-vehicles:8080`), nunca a través del Gateway. Esto evita el rate limit compartido descrito abajo. El cliente reenvía `Authorization` y `X-Astra-Company`, y fija el `X-Astra-Node` que corresponde a la API destino.
- **Degradación por sección, no total**: `DashboardResponse` lleva `FleetAvailable`/`WorkshopAvailable`. Si una fuente cae, esa sección viene null y el resto del panel sigue funcionando. Nunca hacer que una fuente caída rompa la pantalla entera.

Al agregar una API nueva: siguiente puerto libre en la numeración (5001, 5006, 5011, 5016, ...), nuevo `{nombre}-route`/`{nombre}-cluster` en `Gateway/appsettings.json`, nuevo servicio en `docker-compose.yml`, scripts SQL agregados a `run.ps1 reset-db` en orden.

---

## Cuándo un dominio nuevo amerita microservicio propio

No es "tiene que ver con lo mismo" — es bounded context + transacción de negocio compartida + volumen. Preguntas en orden:

1. **¿Este dominio tiene su propio ciclo de vida de datos, distinto del de un servicio existente?** Ej.: "información descriptiva de un vehículo" (Vehicles.Api) es un ciclo de vida distinto de "mantenimiento operativo a lo largo del tiempo" (Maintenance.Api), aunque ambos hablen de "el mismo vehículo".
2. **¿Las entidades nuevas comparten una transacción de negocio real con algo que ya existe en otro servicio?** Si sí (ej. Reservas de Taller y Control de Recorrido: una reserva puede registrar una lectura, en la misma unit-of-work) — van juntas en el mismo servicio. Si la relación es solo "consultan datos del mismo vehículo" — eso no basta para fusionar, una llamada cross-API resuelve la referencia lógica (ver `BACKEND_SERVICE_BLUEPRINT.md`).
3. **¿El volumen justifica el overhead de un servicio adicional?** Este proyecto ya rechazó colas externas (RabbitMQ/Azure Queue) en el scraping de vehículos por sobre-ingeniería al volumen actual — mismo criterio aplica a decidir cuándo separar servicios: si la única razón para separar es "aislamiento conceptual" sin beneficio operativo real (escalado independiente, equipos distintos, ciclos de despliegue distintos), no separar.

Si un dominio nuevo pasa las 3 preguntas con "sí, amerita separación" → microservicio propio, mismo layout que los existentes (`Program.cs`, `Domain/`, `Dtos/`, `Persistence/`, `Services/`, `Endpoints/`, `SolutionItems/db/`). Si no → extender el servicio existente más afín.

---

## `AstraSystemsRental.Base` — qué provee

Ver `BACKEND_SERVICE_BLUEPRINT.md` para el detalle de uso. Resumen de lo que ya existe (no reinventar):

- `Contracts/OperationResult.cs` — envelope de resultado estándar.
- `Http/CrossApiResult.cs` — resultado de llamadas entre APIs, distingue "rechazado por regla de negocio" de "la API no respondió".
- `Persistence/BaseRepository<TContext,T>` — CRUD genérico + paginación normalizada + proyección.
- `Mapping/MappingExtensions.cs` — `ProjectPaged<T,TResult>`.
- `Security/IAstraRequestContext` — `UserId`, `RoleCode`, `Owner` del caller autenticado.
- `Validation/Guard` — validador fluido acumulativo, incluye rangos de fecha/número.
- `Api/AstraApiExtensions.cs` — bootstrap (`AddAstraApi`/`UseAstraPipeline`).

---

## Rate limiting: la partición por IP colapsa detrás del Gateway

`UseAstraPipeline` activa un `FixedWindowRateLimiter` de `RateLimitPermitPerMinute` (**default 120**) por minuto, `QueueLimit = 0`, particionado por `context.Connection.RemoteIpAddress`.

En Docker (y detrás de cualquier reverse proxy) el Front habla con el Gateway desde **una sola IP de contenedor**, así que la partición por IP no separa usuarios: *todo el tráfico comparte un único cubo de 120 req/min*. Consecuencias observadas en vivo:

- El Gateway responde `429`, el `AddStandardResilienceHandler` de Polly lo cuenta como fallo y **abre el circuito** (`BrokenCircuitException`), que es lo que el usuario ve como "servicio no disponible" o un 500 en pantalla.
- Corridas de suites Odiseo consecutivas agotan la ventana y producen fallos en cascada que *parecen* bugs de la aplicación (pantallas con "No se pudo cargar…", steps que fallan por timeout). Antes de diagnosticar un fallo así, revisar `docker compose logs front | grep -i circuit`: si aparece `Result: '429'`, es rate limit, no un defecto de la vista.
- Diagnóstico rápido: `docker compose logs front --since 12m | grep -c "Result: '429'"`. **Medición real:** una sola corrida del plan de 97 pasos de `WorkshopReservations` genera **139 respuestas 429** — es decir, un único usuario navegando activamente ya supera el límite. El síntoma es errático (falla un step distinto en cada corrida, según dónde caiga la ventana de 1 minuto), lo que fácilmente se confunde con flakiness del test.
- El límite de 120/min no es solo un problema de suites de prueba: con la partición por IP actual, es el techo agregado de toda la instalación.

Al desplegar detrás de un proxy, la partición debe hacerse por identidad del usuario (o por `X-Forwarded-For` con `ForwardedHeaders` configurado), no por `RemoteIpAddress`. Con la partición actual, el límite efectivo es global para toda la instalación.

**Pendiente de decisión del usuario:** subir `RateLimitPermitPerMinute`, cambiar la clave de partición, o ambas.
