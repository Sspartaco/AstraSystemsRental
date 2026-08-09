# Front UI Patterns — AstraSystemsRental.Front

For AI assistants. Compressed. Convenciones ya aplicadas y verificadas end-to-end (Odiseo) en Fleet/Companies/VehicleRegistry — mirror exactamente en features nuevas.

Reference files: `Features/VehicleRegistry/Views/Index.cshtml` + `_FleetTable.cshtml` (cards + paginación real), `Features/VehicleRegistry/VehicleRegistryController.cs` (Post-Redirect-Get), `Shared/Views/Shared/_Layout.cshtml` (barra de progreso htmx global).

---

## 1. Cards en grid, nunca tablas HTML para listados

```html
<div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
    @foreach (var item in Model.Items)
    {
        <a href="/feature/@item.Id" class="astra-card p-5 flex flex-col gap-4 group hover:border-brand/30 hover:-translate-y-0.5 transition">
            ...
        </a>
    }
</div>
```

Cada card: ícono SVG inline (no imágenes externas), título destacado, 1-2 líneas de metadata, badge de estado si aplica (`.astra-badge` + color semántico `accent`/`warn`/`danger`). Nunca `<table>` para presentar una lista de entidades al usuario.

## 2. Paginación real, no solo backend

`BaseRepository.GetPagedAsync` ya normaliza página/tamaño en el backend — eso NO es suficiente por sí solo. Toda vista de listado paginado necesita controles de paginación reales en el HTML (botones Anterior/Siguiente + números de página), conectados vía `hx-get` al mismo endpoint con `page` en la querystring. Ver `_FleetTable.cshtml` como referencia — un listado sin controles de paginación visibles es un bug, no un detalle menor.

## 3. Post-Redirect-Get en todo formulario que puede fallar

Un `POST` que devuelve la vista directamente sobre la respuesta (`return View("Create")`) dispara el diálogo del navegador "¿Volver a enviar el formulario?" al refrescar. Patrón correcto:

```csharp
[HttpPost("/feature")]
public async Task<IActionResult> Store(...)
{
    var response = await gateway.SendForDataAsync(...);
    if (!response.IsSuccess)
    {
        TempData["CreateError"] = TranslateError(response.Errors.FirstOrDefault());
        return Redirect("/feature/create");
    }
    return Redirect($"/feature/{id}");
}

[HttpGet("/feature/create")]
public IActionResult Create()
{
    ViewData["Error"] = TempData["CreateError"];
    return View();
}
```

Todo `POST` que no usa `hx-post` (submit HTML normal, forms de wizard multi-paso) sigue este patrón. Los `POST` vía htmx con `hx-target`/`hx-swap` no lo necesitan — el swap parcial no dispara el diálogo de reenvío.

## 4. Helper de parseo JSON compartido

`Shared/Json/JsonElementExtensions.cs` expone `GetString`/`GetStringOrNull`/`GetLongOrNull`/`GetDateOnlyOrNull` como extension methods sobre `JsonElement`. Un controller nuevo nunca reimplementa su propio helper privado de parseo — 3 implementaciones divergentes del mismo concepto ya fue el problema real detectado (Fase 0).

**Para features nuevas, preferir DTOs tipados con `ReadFromJsonAsync<T>` sobre `JsonElement` crudo** cuando sea razonable — el propio `GatewayClient.LoginAsync` ya demuestra el patrón correcto (`record Envelope` + deserialización tipada). `JsonElement` manual solo cuando el contrato de respuesta es genuinamente dinámico.

## 5. Búsqueda con htmx, debounce estándar

```html
<form hx-get="/feature" hx-target="#feature-list" hx-swap="innerHTML"
      hx-trigger="submit, keyup changed delay:400ms from:input[name=search], change from:select[name=status]">
    <input type="search" name="search" class="astra-input" autocomplete="off" />
    <select name="status" class="astra-input">...</select>
</form>
```

400ms de debounce en `keyup`, sin JS custom. El controller sirve `View(...)` completa en GET normal, o `PartialView("_Nombre", modelo)` cuando la request viene de htmx (mismo action, ambos casos).

## 6. Feedback de carga

- Barra de progreso global htmx (`#astra-progress`, ya cableada en `_Layout.cshtml`/`_LayoutAuth.cshtml` vía `htmx-progress.js`) cubre toda request `hx-*` automáticamente — no requiere nada adicional por vista.
- Formularios con submit HTML normal (sin htmx, ej. wizards multi-paso) necesitan `data-astra-submit-spinner` + `wwwroot/js/form-submit-spinner.js` explícitamente, ya que la barra global no los cubre.
- Botones individuales que disparan una request htmx puntual: `hx-indicator` + spinner SVG inline (ver `_LoginForm.cshtml`).

## 7. Design system existente (no reinventar clases)

`.astra-panel` (contenedor grande), `.astra-card`/`.astra-kpi` (card individual), `.astra-btn-primary`/`.astra-btn-ghost`, `.astra-input`/`.astra-label`, `.astra-badge` (+ `bg-accent/15 text-accent` éxito, `bg-warn/15 text-warn` advertencia, `bg-danger/15 text-danger` error), `.astra-alert-error`. Definidas en `wwwroot/css/app.css` `@layer components`. Tras tocar CSS: `npm run build:css`.

---

## 8. La acción GET de un listado DEBE devolver partial en htmx

Bug real y recurrente: `Index` devolvía siempre `View(vm)`. Al filtrar, htmx recibía la página **completa** (layout + sidebar) y la inyectaba dentro del contenedor de resultados — la app se renderizaba anidada dentro de sí misma. Ocurrió en `VehicleRegistry` y en `Admin/Users`.

Toda acción GET que sea destino de un `hx-get` debe ramificar:

```csharp
var vm = await LoadPage(...);

return Request.Headers.ContainsKey("HX-Request")
    ? PartialView("_FleetTable", vm)
    : View(vm);
```

Regla: si una vista tiene un `hx-get` apuntando a su propia ruta, la acción tiene que ramificar. Sin excepción.

---

## 9. Nada de estados de dominio en inglés en pantalla

Los enum viajan en inglés por la API (`Active`, `InWorkshop`, `Valid`) porque son el contrato. Lo que **se pinta** se traduce con `Shared/Display/StatusText.cs`: `StatusText.Vehicle(...)`, `.Reservation(...)`, `.Document(...)`, `.Role(...)`, `.MeasurementUnit(...)`, `.ReadingSource(...)`.

Aplica también al `<option>` de los filtros: el `value` queda en inglés (es lo que consume la API), la etiqueta visible se traduce.

```cshtml
<option value="@s" selected="@(Model.Status == s)">@StatusText.Vehicle(s)</option>
```

Al agregar un enum nuevo al dominio, agregar su método a `StatusText` en el mismo commit.

**Gotcha de codificación:** un `.cs` con acentos guardado **sin BOM UTF-8** lo lee el compilador como ANSI y los literales salen con mojibake ("años" → "aÃ±os"). Los archivos con texto en español necesitan BOM. Ojo al diagnosticar: la consola de Windows también muestra mojibake aunque los bytes estén bien — comparar contra el literal esperado en Python antes de "arreglar" nada.

---

## 10. Sidebar: nodo activo por prefijo, no por igualdad

`currentPath == href` deja el menú sin marcar en cualquier ruta anidada (`/maintenance/vehicle/5` no marcaba nada). Se resuelve con match por prefijo + desempate por el href más largo, para que `/maintenance/routines` marque Rutinas y no también Control de Recorrido:

```csharp
var bestMatch = resolved
    .Where(x => IsActive(x.Href))       // igualdad o StartsWith(href + "/")
    .OrderByDescending(x => x.Href.Length)
    .Select(x => x.Href)
    .FirstOrDefault();
```

El colapso vive en `astraShell()` (Alpine) con persistencia en `localStorage`, y el estado se comparte a los hijos por scope — por eso `Sidebar/Default.cshtml` puede usar `x-show="!collapsed"` sin declarar su propio `x-data`.

---

## 11. Panel de inicio: datos, no un segundo menú

El home original repetía el sidebar como cuadrícula de "accesos rápidos" con KPIs fijos (128/14/3/87%). El usuario lo señaló: *"es un menú 2.0, para qué tengo un sidebar si el index ya tiene todo"*.

Un panel de inicio debe responder "¿qué pasa hoy?": KPIs reales consultados, agenda próxima, lo que requiere atención, distribución. Los KPIs son enlaces a su módulo — eso cubre el acceso rápido sin duplicar el menú. Nunca cablear números de ejemplo en una vista que parece real.
