# Odiseo Testing Guide — planes E2E (Probe/SAGA)

For AI assistants. Compressed. Convención de test plans para el comando `argus probe` (identidad de agente "Odiseo", herramienta SAGA en `C:\Users\Jonathan\source\repos\SAGA`). Gotchas documentados aquí fueron encontrados y corregidos reales durante el desarrollo de Fleet/Companies/VehicleRegistry — ver también memoria `saga_probe_odiseo.md`, no debe divergir de este documento.

Reference plans: `AstraSystemsRental.Front/SolutionItems/probe/{Auth,Fleet,Companies,VehicleRegistry}/TestPlan*.json`. Plataforma: `SAGA/EquiSoft.Codegen/Probe/Platforms/probe.astralsystems.json`.

---

## Estructura estándar de un plan

```json
{
  "name": "nombre-del-plan",
  "baseUrl": "https://localhost:8444",
  "defaultTimeoutMs": 15000,
  "stopOnFailure": false,
  "viewport": { "width": 1440, "height": 900 },
  "steps": [ ... ]
}
```

Bloque 1 (autenticación, idéntico en todo plan):
```json
{ "action": "goto", "url": "/auth/login", "description": "=== BLOQUE 1: AUTENTICACIÓN ===" },
{ "action": "wait", "selector": "#email" },
{ "action": "fill", "selector": "#email", "value": "${PROBE_USERNAME}" },
{ "action": "fill", "selector": "#password", "value": "${PROBE_PASSWORD}", "secret": true },
{ "action": "click", "selector": "#login-submit" },
{ "action": "wait", "selector": "nav", "state": "visible", "timeoutMs": 15000 }
```

Comando: `dotnet run --project "EquiSoft.Codegen/Saga.Codegen.csproj" --no-build -- probe --platform astralsystems --plan TestPlanXxx`. Siempre validar con `--dry-run` primero, luego correr real (sin ese flag) — `--dry-run` solo valida sintaxis, no comportamiento real de la UI.

## Al cerrar una vista/feature nueva

Obligatorio: crear/actualizar el plan correspondiente en `probe/<Feature>/TestPlan<Feature>.json`, correrlo en modo real, y corregir selectores/timing hasta 0 fallos — no basta con que compile o pase `--dry-run`.

---

## Gotchas reales del motor (ya corregidos en el código de SAGA, documentados para reconocerlos si reaparecen en otra copia)

1. **`ScreenshotAsync` sin timeout colgaba el proceso entero.** Al fallar un step, el motor toma un screenshot de diagnóstico. Sin `Timeout` explícito, en una página con polling htmx activo o animaciones CSS en curso, Playwright nunca la considera "estable" — el proceso quedaba colgado indefinidamente, sin nunca respetar el timeout del step que ya había fallado. Si un plan se cuelga sin nunca fallar por timeout tras un step fallido, sospechar de esto primero.
2. **El campo `text` de la acción `expect` no resolvía `${VAR}`** (a diferencia de `value`) — un `expect` con `"text": "${PROBE_USERNAME}"` comparaba contra el string literal, dando falso negativo con el valor real visible en el mensaje de error.

## Gotchas de escritura de planes (recurrentes, no bugs del motor)

1. **`window.__x` se pierde en cada `goto`** (navegación completa reinicia el contexto JS). Para recordar un valor generado en runtime (ej. una placa aleatoria) a través de varios `goto`, usar `sessionStorage.setItem`/`getItem` (sobrevive same-origin) y rehidratar `window.__x = window.__x || sessionStorage.getItem('__x')` al inicio de cada `eval` que lo usa tras un `goto`.
2. **No usar `eval` con `window.location.href = ...` para navegar** — no espera a que la navegación complete antes del siguiente step. Usar la acción `goto` real, o si el destino es dinámico, navegar a un listado conocido y hacer click programático sobre el elemento correcto (`eval` con `querySelector(...).click()` sobre un `<a href>` real, que sí dispara navegación nativa trackeable).
3. **Selectores de texto (`button:has-text('X')`) sin scope pueden matchear elementos ocultos.** `x-show`/`x-cloak` de Alpine no remueve el elemento del DOM. Si el mismo texto aparece dos veces (ej. "Siguiente" en cada paso de un wizard, o un botón oculto en el header con el mismo texto), el motor espera indefinidamente a resolver la ambigüedad. Fix: `button:visible:has-text('X')` o dar scope con un contenedor/form padre.
4. **`x-data` de Alpine se reinicializa en cada swap htmx** sobre el contenedor que lo envuelve (`hx-swap="innerHTML"`). El estado de tabs/wizards vuelve a su valor inicial tras cualquier submit que refresque ese contenedor, aunque visualmente no sea obvio. Si un plan hace click en un tab tras una interacción previa que disparó un swap, verificar con `eval` que el tab realmente quedó activo antes de interactuar con su contenido — reintentar el click si no.
5. **Precedencia de operadores en `console.error('label: ' + a && b)`** — se evalúa como `(string + a) && b`, la concatenación produce un string truthy y el booleano real se descarta. El normalizador de hallazgos puede marcar esto como falso `ConsoleError`. Siempre envolver: `'label: ' + (a && b)`.
6. **No asumir que una placa/entidad "de referencia" fija existe en el entorno.** Un catálogo importado de datos externos (ej. Excel de valoración) no es estable entre entornos/resets de BD. Preferir un dato creado y verificado dentro del propio flujo de setup del plan (ej. un vehículo dado de alta manual) sobre uno asumido preexistente.
7. **Leer un valor del DOM solo desde un elemento visible en el tab activo.** Un `querySelector` sobre un contenedor que vive en otro tab (oculto por `x-show`) devuelve vacío sin error, y el plan sigue con un valor por defecto silenciosamente incorrecto. Preferir elementos siempre visibles (ej. un KPI de cabecera) como fuente de datos de setup.
8. **Un plan que no ensucia datos también debe poder correr dos veces seguidas.** Si un plan registra una entidad con clave única (placa, nombre de rutina), generarla aleatoria en runtime; si depende de un estado previo (ej. "no existe reserva activa"), limpiar o generar fechas que no colisionen. Un plan que solo pasa la primera vez es un plan roto.
9. **Un elemento bajo `x-transition` nunca es "actionable" para Playwright si queda con `opacity-0`.** La acción `click` espera a que el elemento sea visible *y* estable; un form con `x-transition:enter-start="opacity-0 -translate-y-1"` puede quedar con esa clase aplicada, y entonces el click agota cualquier timeout (30s tampoco alcanzan — no es un problema de latencia, subir el timeout no lo arregla). Síntoma diagnóstico: el step de `click` falla por timeout pero los steps siguientes pasan, porque el submit sí ocurrió. Fix: `eval` con `querySelector(...).click()`, que ignora la actionability.
10. **Un fallo de validación "que dejó de rechazar" suele ser residuo de datos, no una regresión.** Los bloques de rechazo (monotonía de odómetro, solapamiento de reservas, duplicados) dependen del estado que dejó un bloque anterior. Si una corrida previa dejó filas, el valor de referencia cambia y la validación deja de dispararse donde el plan lo espera. Antes de tocar código, verificar en BD si la entidad que debía rechazarse realmente **no** se insertó: si no está, el validador funcionó y el problema es el estado de partida.
11. **Cubrir la navegación por el sidebar, no solo el flujo interno.** Un bug real (404 en `/maintenance/reservations`) sobrevivió a un plan de 68 pasos porque el plan entraba a la funcionalidad desde la ficha del vehículo y nunca por el enlace del menú. Todo nodo sembrado en `access.Nodes` debe tener un paso que navegue a su `Route` y verifique que responde.

---

## Gotchas del motor descubiertos en la Fase 1 de mejoras

12. **`eval` NO soporta `await`.** Un script con `await fetch(...)` falla con error de sintaxis, no con un mensaje claro. Para verificar una respuesta del servidor hay que navegar de verdad (`goto` con query string) y leer el DOM resultante — lo cual además prueba el flujo real del usuario en vez de un `fetch` sintético.

13. **No existe la acción `screenshot`.** Las acciones válidas son `goto, fill, click, select, upload, eval, expect, wait, note, dbexpect`. El motor solo captura pantalla automáticamente cuando un paso falla. Para obtener una captura deliberada, provocar un fallo controlado (`wait` sobre un selector inexistente con timeout corto).

14. **El orden de los bloques importa: cada bloque hereda la página donde lo dejó el anterior.** Insertar un bloque nuevo al final de un plan lo deja en la última pantalla visitada, no en la que asume. Un bloque que opera sobre la ficha del vehículo debe ir *antes* de los bloques que navegan a pantallas globales, o volver a navegar explícitamente. Síntoma: cascada de timeouts a partir del primer paso del bloque nuevo.

---

## Cubrir una mejora con el plan, no solo la funcionalidad

Cuando una mejora corrige un desalineamiento entre cliente y servidor, el paso de prueba debe afirmar **el invariante**, no el valor. Ejemplo real (rango de `ModelYear`):

```js
const expectedMax = new Date().getFullYear() + 1;
if (max !== expectedMax) throw new Error('max esperado ' + expectedMax + ', encontrado ' + max);
```

Así el test sigue siendo válido el año que viene. Un `expect` contra `"2027"` literal se rompe solo en enero.

Para mensajes traducidos, afirmar **las dos caras**: que el texto esperado está y que el original en inglés **no** está. Si solo se comprueba lo primero, una traducción parcial pasa desapercibida:

```js
if (t.includes('already has an active reservation')) throw new Error('El error sigue en ingles: ' + t);
```
