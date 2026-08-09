# Gotchas técnicos — bugs reales ya encontrados y corregidos

Cada entrada es un bug que efectivamente ocurrió en este repo, con síntoma, causa raíz y fix. El objetivo es no perder tiempo re-diagnosticando algo que ya se resolvió una vez.

## 1. `AddHttpClient<T>` + `AddScoped<IInterfaz, T>` duplicado rompe el HttpClient silenciosamente

**Síntoma**: una fuente que depende de `HttpClient` (ej. `WikimediaVehicleImageSource`) actualiza su timestamp de "última consulta" (`ImageFetchedAtUtc`) pero el valor real queda siempre vacío/null. **Cero excepciones visibles, cero logs de "Sending HTTP request"** — parece que el código nunca corrió, pero sí corrió (el timestamp se actualiza).

**Causa**: registrar
```csharp
builder.Services.AddHttpClient<MiClase>(...).AddStandardResilienceHandler();
builder.Services.AddScoped<IMiInterfaz, MiClase>(); // ← BUG
```
crea **dos instancias distintas** de `MiClase`. La primera (typed client, resuelta vía `IHttpClientFactory`) tiene el `HttpClient` bien configurado. La segunda (resuelta por el contenedor de DI scoped normal, activada por el segundo `AddScoped`) construye su propio `HttpClient` fuera del factory, sin la configuración ni los handlers — la llamada real queda rota de una forma que no lanza excepción visible.

**Fix**: nunca duplicar el registro. Reutilizar la instancia ya creada por el typed-client factory:
```csharp
builder.Services.AddHttpClient<MiClase>(...).AddStandardResilienceHandler();
builder.Services.AddScoped<IMiInterfaz>(sp => sp.GetRequiredService<MiClase>()); // ← correcto
```
Este es el patrón que ya usaban bien las fuentes de valoración (Tucarro, AsoUsados) desde el principio — el bug apareció solo en la fuente de imagen, agregada después, que no siguió el mismo patrón al principio.

## 2. EF Core + columna `TINYINT` + enum de C# → `InvalidCastException`

**Síntoma**: `System.InvalidCastException: Unable to cast object of type 'System.Byte' to type 'System.Int32'` al leer o actualizar cualquier fila de una tabla con una columna enum. Rompe tanto queries como el propio worker en background (`QuoteOrchestrationService`).

**Causa**: un enum de C# (ej. `ValuationStatus`, `QuoteRequestStatus`) mapea a `int` (4 bytes) por defecto en EF Core, pero la columna SQL se creó como `TINYINT` (1 byte) — al leer, el driver de SQL Server intenta castear un `byte` a `int` sin la conversión explícita y falla.

**Fix**: declarar la conversión explícita en `OnModelCreating` del `DbContext`:
```csharp
entity.Property(e => e.Status).HasConversion<byte>();
```
Aplicar esto a **toda** columna `TINYINT` que mapee a un enum de C#. Ya corregido en `AstraVehiclesDbContext` para `ValuationCacheEntry.Status` y `QuoteRequest.Status`.

## 3. `.xls` binario legado (BIFF) no lo soporta ClosedXML

**Síntoma**: `ClosedXML.Excel.XLWorkbook` lanza excepción o no puede abrir un archivo `.xls` (extensión antigua de Excel 97-2003).

**Causa**: ClosedXML solo lee el formato moderno OOXML (`.xlsx`, XML+ZIP). El formato `.xls` legado es binario (BIFF) y requiere una librería distinta.

**Fix**: usar `ExcelDataReader` + `ExcelDataReader.DataSet` (paquete de extensión que agrega el método `.AsDataSet()`) para archivos `.xls`. Requiere registrar el proveedor de codepages legacy antes de usarlo (una sola vez, ej. en un constructor estático):
```csharp
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
```
El catálogo base de placas de este proyecto usa `.xlsx` → ClosedXML está bien ahí. Los archivos de Revista Motor son `.xls` → necesitan ExcelDataReader. No asumir que un solo paquete sirve para "todo lo que sea Excel".

## 4. Razor no permite `@if` dentro de la etiqueta de apertura de un elemento

**Síntoma**: error de compilación de la vista `.cshtml` al intentar poner un atributo HTML condicional directamente con `@if` dentro del tag.

**Causa**: Razor no soporta bloques de código de control (`@if`, `@foreach`) como hijos sintácticos dentro de la lista de atributos de una etiqueta — solo permite interpolación de expresiones (`@algo`), no bloques.

**Fix**: armar el string de atributos condicionales fuera del tag (en un bloque `@{ }`) y volcarlo con `@Html.Raw(...)`:
```csharp
@{
    var pollAttributes = condicion
        ? "hx-get=\"...\" hx-trigger=\"every 1.5s\" ..."
        : string.Empty;
}
<div id="algo" @Html.Raw(pollAttributes)>
```
Usado en `_QuoteResult.cshtml` para el polling htmx auto-terminante (ver doc 03).

## 5. `sqlcmd` sin BOM garbla tildes/ñ en scripts SQL

**Síntoma**: después de aplicar un script `.sql` con `SQLCMD.EXE`, los textos con tildes o eñes quedan corruptos en la base de datos (ej. "Vehículos" se guarda mal).

**Causa**: `sqlcmd` asume la codificación del archivo según su BOM (byte order mark). Un `.sql` guardado como UTF-8 **sin BOM** se interpreta con la codepage por defecto del sistema, no como UTF-8, y los caracteres no-ASCII se corrompen.

**Fix**: guardar los scripts `.sql` que contengan texto en español con tildes/ñ como **UTF-8 con BOM** explícito. Ya aplicado a `05_seed.sql` (Users.Api) tras detectar el problema con el label "Vehículos" del nodo `fleet`.

## 6. Timeout de fuente externa vs timeout de la primera descarga "en frío"

**Síntoma**: una fuente que depende de descargar un archivo grande la primera vez (ej. Revista Motor descargando 3 `.xls` de varios MB) puede fallar por timeout aunque el sitio esté funcionando bien, si el timeout configurado es muy corto.

**Causa**: el timeout por fuente (`ValuationOptions.SourceTimeoutSeconds`) se pensó originalmente para requests HTML livianos (~8s), pero no contempló fuentes cuya primera consulta implica descargar varios MB antes de tener cualquier resultado cacheado.

**Fix aplicado**: subido de 8 a 18 segundos en `appsettings.json`. Si se agrega una fuente nueva con un patrón similar (descarga pesada + caché posterior), considerar si el timeout global sigue siendo suficiente, o si esa fuente necesita su propio timeout más generoso.
