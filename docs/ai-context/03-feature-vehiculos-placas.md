# Feature: Cotización de vehículos por placa

## Qué hace

El usuario digita una placa colombiana en `/fleet`, y el sistema muestra una cotización consolidada del vehículo (marca/línea/modelo + imagen de referencia) junto con el detalle de valor por cada fuente externa consultada (TuCarro, AutoExpo, Revista Motor, Fasecolda), con estado de carga progresivo por fuente (no bloquea la pantalla completa esperando a la fuente más lenta).

Plan de diseño original completo (contexto de por qué se tomó cada decisión, con las preguntas que se le hicieron al usuario): `C:\Users\Jonathan\.claude\plans\ten-en-cuenta-que-joyful-quasar.md` en la máquina donde se diseñó — si no está disponible, este documento resume lo esencial.

## Decisiones de diseño (ya tomadas, no volver a discutir sin motivo)

- **Nodo**: reutiliza el nodo `fleet` existente (no se creó nodo nuevo) — label actualizado a "Vehículos". Esto obliga a que el controller Front se llame `FleetController` (el `Route='Fleet/Index'` del nodo determina el nombre).
- **Nodo incluido en el plan Demo**: la cotización es el gancho comercial del trial de 3 días, así que `fleet` está en `PlanNodes` tanto de `Demo` como de `Basic`.
- **API separada**: `AstraSystemsRental.Vehicles.Api` (puerto 5011), no se metió dentro de Users.Api — dominio de datos completamente distinto (catálogo de vehículos + caché de scraping vs identidad/accesos).
- **Mismo servidor de base de datos**: schema `[vehicles]` nuevo, pero sobre la misma base `AstraSystemsRental` (no una BD separada) — mismo patrón que se usó para agregar el schema `companies`.
- **Persistencia EF Core** (no Dapper), usando `BaseRepository<TContext,TEntity>` de `AstraSystemsRental.Base`, igual que Users.Api.
- **Scraping asíncrono in-process**: `BackgroundService` + `Channel<T>` de .NET puro, sin cola externa (RabbitMQ/Azure Service Bus se descartó por sobre-ingeniería para el volumen esperado — una búsqueda manual por usuario, no un batch masivo).
- **Catálogo base**: se cargó una vez desde un Excel (`placasValoracion.xlsx`, 8414 placas con marca/línea/modelo/motor) vía comando CLI, no vía endpoint de importación (carga única de arranque, no flujo recurrente).
- **RUNT y DIAN** quedaron documentados como fuentes futuras, no implementadas: RUNT sería la fuente correcta para ampliar/mantener el catálogo de identificación de placas (el organismo real, no la DIAN), DIAN sería una fuente de avalúo fiscal adicional. Ninguna tiene hoy una API pública viable para integración automatizada.

## Modelo de datos (`schema [vehicles]`)

- **`VehicleCatalog`**: `PlateNumber` (PK), `VehicleClass`, `Brand`, `Line`, `ModelYear`, `FullLine`, `Engine`, `IsUserAdded`, `ImageUrl`, `ImageAttribution`, `ImageFetchedAtUtc`, timestamps. Una fila por placa. El catálogo real incluye maquinaria pesada con placas **numéricas** (ej. `"1122"`, `"121600"`), no solo el formato civil `ABC123` — la validación de formato de placa es deliberadamente laxa: `^[A-Z0-9]{3,10}$`. **No restringir a formato de placa de carro civil**, se rompe con maquinaria real del catálogo.
- **`ValuationSources`**: catálogo estático de fuentes (`Code` PK, `DisplayName`, `IsActive`, `SortOrder`). Fuentes activas hoy: `Fasecolda` (stub), `RevistaMotor`, `Tucarro`, `AsoUsados` (= AutoExpo). Inactivas: `MercadoLibre`, `FacebookMarketplace` (código dejado en la tabla por compatibilidad histórica pero `IsActive=0`, no se llaman).
- **`ValuationCache`**: una fila **vigente** por `(PlateNumber, SourceCode)` — se actualiza in-place (UPSERT), no es histórico append-only. Campos: `Status` (Pending/Success/NotFound/Failed), `ValueMin/Max/Avg`, `Currency`, `RawPayload`, `FetchedAtUtc`, `ExpiresAtUtc`, `ErrorMessage`.
- **`QuoteRequests`**: job efímero de orquestación de una búsqueda — `RequestId` (Guid expuesto al Front), `PlateNumber`, `RequestedByUserId`, `Status` (Running/Completed/CompletedWithErrors), timestamps. Se purga automáticamente pasadas 24h de completado (tick horario dentro del mismo `BackgroundService`).

## Flujo de una búsqueda

1. `POST /apiVehicles/quotes {plateNumber}` (Vehicles.Api): resuelve el vehículo en `VehicleCatalog`. Si no existe → `404 VehicleNotFound` (el Front ofrece completar los datos a mano). Si existe: crea `QuoteRequests`, revisa qué fuentes ya tienen caché vigente (no vencido) y cuáles hay que consultar, encola el resto en el canal del orquestador, responde `202/201` de inmediato con `{requestId}` **sin esperar** a que las fuentes respondan.
2. `GET /apiVehicles/quotes/{requestId}`: devuelve el estado agregado + por-fuente. Es el endpoint que el Front consulta por polling.
3. El `QuoteOrchestrationService` (worker en background) procesa el job: por cada fuente activa sin caché vigente, llama a `IValuationSource.FetchAsync` con timeout individual (`SourceTimeoutSeconds`, hoy 18s — subido desde 8s original porque Revista Motor puede tardar en su primera descarga), guarda el resultado en `ValuationCache` con un TTL según si tuvo éxito (`CacheTtlDays`, 7 días) o falló (`FailedCacheTtlMinutes`, 15 min — evita reintentar en loop si una fuente está caída). Al terminar todas, marca `QuoteRequests.Status`.

## Front (`Features/Fleet/`)

- `FleetController.cs`: gating `HasNode("fleet")`. Rutas: `GET /fleet` (pantalla), `POST /fleet/quotes` (dispara búsqueda), `GET /fleet/quotes/{guid}` (polling), `POST /fleet/vehicles` (registro manual cuando la placa no está en catálogo, encadena la cotización automáticamente).
- **Polling htmx auto-terminante**: `_QuoteResult.cshtml` solo emite `hx-trigger="every 1.5s"` mientras `Status=="Running"` — al completarse, el partial se re-renderiza sin ese atributo y htmx deja de consultar solo, sin JS custom.
- Vistas: `Index.cshtml` (buscador), `_QuoteResult.cshtml` (card consolidada + grid de cards por fuente), `_SourceCard.cshtml` (skeleton mientras pending, valor + badge de antigüedad si success, mensaje de error si failed/not-found), `_ManualVehicleForm.cshtml`.
- `wwwroot/js/fleet.js`: máscara de placa (mayúsculas, solo alfanumérico) + feedback visual de formato válido/inválido mientras se tipea.

## Las fuentes de valoración (estado real, verificado contra los sitios)

Todas implementan `IValuationSource.FetchAsync(VehicleQuery, ct)`, registradas en `Program.cs` de Vehicles.Api. `VehicleQuery` lleva marca/línea/modelo/motor (no solo la placa) porque las fuentes valoran por esos atributos, no por placa cruda.

### TuCarro (`TucarroValuationSource`) — scraping HTML real
URL: `https://carros.tucarro.com.co/{marca}/{linea}/` (slugs en minúscula, espacios → guiones). Parsea cards de listado con HtmlAgilityPack: título en `h3`/`.poly-component__title` (se usa para filtrar por año, porque la URL no lo soporta), precio en `span.andes-money-amount__fraction`. Selectores confirmados contra una captura real del sitio.

### AutoExpo (`AsoUsadosValuationSource`) — scraping HTML real
**Importante**: el código interno/`Code` en la base de datos sigue siendo `AsoUsados` (nombre original antes de descubrir cuál era el sitio real), pero el sitio que efectivamente se scrapea es **AutoExpo** (`autoexpo.com.co`), no "asousados.com" (ese dominio no existe/no resuelve). URL: `https://autoexpo.com.co/concesionario-carros-usados-bogota/{linea}/{marca}/?condition=usado&type=automovil`. `DisplayName` en el seed está como "AutoExpo" para reflejar la fuente real en la UI.

### Revista Motor (`RevistaMotorValuationSource`) — NO es scraping de HTML
El sitio `motor.com.co/seccion/precios` resuelve su formulario (categoría→marca→modelo→año) completamente client-side en JavaScript — no hay ningún request AJAX que interceptar. Todo el dataset de precios se descarga como **3 archivos `.xls` públicos** que el propio JS del sitio consume:

```
https://www.eltiempo.com/infografias/2024/05/revista_motor_entrega_final/xls/usados_importados.xls
https://www.eltiempo.com/infografias/2024/05/revista_motor_entrega_final/xls/usados_nacionales.xls
https://www.eltiempo.com/infografias/2024/05/revista_motor_entrega_final/xls/nuevos.xls
```

Estructura de fila (deducida leyendo el `funcion.js` real del sitio):
- **Usados** (importados/nacionales): `{marca, buscador, tipo, USADOS_IMPORTADOS|__EMPTY, "15", "16", ..., "25"}` — cada clave de 2 dígitos es un año (`20` + dígitos), el valor es un **decimal en millones de pesos** (ej. `13.17` = $131.700.000, se calcula como `valor * 1_000_000`).
- **Nuevos**: `{marca, buscador, categoria, nuevos, precio, nomenclatura?}` — `precio` ya viene en pesos completos, un solo valor (no por año).

Implementación: descarga los 3 `.xls` con `HttpClient`, los parsea con **`ExcelDataReader` + `ExcelDataReader.DataSet`** (necesario porque `.xls` es formato binario legado BIFF — **ClosedXML no lo soporta**, solo lee `.xlsx`/OOXML moderno), cachea el resultado parseado en `IMemoryCache` con TTL de 6 horas (no tiene sentido re-descargar varios MB en cada consulta de placa), y hace match de marca+línea por tokenización simple (contiene todas las palabras, case-insensitive) porque los nombres "buscador"/"marca" de Revista Motor no coinciden exactamente con los nombres del catálogo de placas.

### Fasecolda (`FasecoldaValuationSource`) — stub intencional
Retorna siempre `NotFound` con mensaje explicativo. La guía comercial Fasecolda es un servicio de pago sin scraping público confiable — queda como interfaz lista para activar si se consigue acceso oficial (API o convenio).

### Eliminadas: Mercado Libre y Facebook Marketplace
Se implementaron en una primera pasada y luego se **eliminaron por completo** (archivos borrados, quitadas de `Program.cs`, de `ValuationSourceCode`, desactivadas en el seed con `IsActive=0` pero sin borrar la fila para no romper el historial de `ValuationCache`) por decisión explícita del usuario: "no es viable". Si se reconsidera en el futuro, no reactivar sin confirmar de nuevo — la razón de descarte no quedó más detallada que esa.

### `OtrasFuentes` — extension point
Fuente stub sin contenido, pensada como plantilla para sumar otros clasificados colombianos (Carro Ya, El Carro Colombia, etc.) sin tocar el orquestador — basta con implementar `IValuationSource` y registrarlo.

## Imagen de referencia del vehículo

`VehicleCatalog.ImageUrl/ImageAttribution/ImageFetchedAtUtc`. Fuente: **Wikimedia Commons** (API pública sin key, `WikimediaVehicleImageSource`), busca por "Marca Línea Modelo" en el namespace de archivos (`ns=6`). Resolución perezosa con TTL de 30 días — se dispara al consultar o crear un vehículo (`VehicleQuoteService.EnsureImageAsync`), no bloquea el flujo de cotización. Si no encuentra imagen, el Front muestra un placeholder SVG genérico de auto.

## Import del catálogo base

CLI: `dotnet run --project AstraSystemsRental.Vehicles.Api -- import-catalog --file <ruta-al-excel.xlsx>`. Lee con **ClosedXML** (el catálogo base sí es `.xlsx` moderno, distinto del `.xls` legado de Revista Motor), upsert por `PlateNumber` en lotes de 500. Idempotente, se puede re-correr sin duplicar. Columnas esperadas del Excel: `placa, clase, marca, linea, modelo, lineaCompleta, Motor` (más 4 columnas de valoración que el Excel trae vacías y no se usan — son el hueco que esta feature llena en vivo, no un insumo).
