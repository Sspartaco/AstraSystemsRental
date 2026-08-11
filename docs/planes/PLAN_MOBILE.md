# Plan — `AstraSystemsRental.Mobile` (.NET MAUI)

**Entregable de esta iteración: APK Android.** iOS queda multi-target y compilable, pero su empaque se aplaza (decisión tuya).

## Contexto verificado

Levantado del repo real, no asumido:

- **SDK .NET 10.0.400-preview**; workload MAUI **no instalado** pero disponible (`maui`, `maui-android`, `maui-ios`).
- **75 endpoints ya expuestos por el Gateway** (Users 34, Vehicles 15, Maintenance 24, Reports 1, Mail 1) sobre 5 rutas `catch-all`. **La app no necesita backend nuevo**: consume exactamente lo mismo que el Front.
- Auth JWT RS256 con claims multivaluados `node`/`company`; contexto de compañía por header `X-Astra-Company` y nodo por `X-Astra-Node`.
- Upload de fotos ya existe como `multipart` (`IFormFile`) en `POST /workshop-reservations/{id}/photos` — la cámara del móvil encaja sin tocar la API.
- `Reports.Api` ya entrega el dashboard compuesto y degradado por sección: es el mismo modelo que consumirá el home móvil.

### Tres bloqueantes reales detectados

1. **No hay refresh token y el access dura 60 min** (`JwtOptions.AccessTokenMinutes = 60`). En web se tolera; en móvil expulsa al usuario cada hora. **Decidido: implementar refresh tokens.**
2. **No hay CORS configurado** en Gateway ni en Base. MAUI nativo no lo necesita (no es navegador), pero sí lo necesitaría cualquier cliente web futuro — se deja anotado, no se construye.
3. **El rate limit de 120/min particionado por IP** golpearía a *toda* la flota de móviles como si fueran un solo cliente, porque salen por la misma IP pública. Esto ya está documentado en `GlobalGuidelines/ARCHITECTURE_OVERVIEW.md` y **sigue pendiente de tu decisión**; con app móvil deja de ser teórico.

---

## Decisiones tomadas

| Punto | Decisión |
|---|---|
| Stack | **.NET MAUI nativo** (C# + XAML) → APK Android ahora, IPA cuando lo retomemos |
| Sesión | **Refresh tokens** + almacén seguro del dispositivo |
| Alcance v1 | **Paridad total** con las 10 vistas de la web |
| Backend nuevo | **Ninguno**, salvo el endpoint de refresh |
| Reutilización | DTOs y contratos compartidos por proyecto de clase; **no** se reutiliza XAML↔cshtml (son tecnologías de vista distintas) |

---

## FASE 0 — Refresh tokens (bloqueante, backend)

Es lo primero porque sin esto la app es inusable, y además mejora la web.

- `11_schema_refresh_tokens.sql`: tabla `users.RefreshTokens` (Id, UserId, TokenHash, ExpiresAtUtc, RevokedAtUtc, ReplacedByTokenHash, CreatedAtUtc, DeviceInfo). Se guarda **hash**, nunca el token en claro.
- `POST /auth/refresh` → valida el refresh vigente, **rota** (revoca el anterior y emite uno nuevo) y devuelve access nuevo. La rotación permite detectar reuso de un token robado.
- `POST /auth/logout` → revoca el refresh del dispositivo.
- `LoginResponse` gana `refreshToken` y `expiresIn`. **El Front actual sigue funcionando sin cambios** (campo aditivo).
- Vida: access 60 min (igual), refresh 30 días con rotación.
- Tests unitarios de `RefreshTokenService`: token vencido, revocado, reusado tras rotación, y feliz.

## FASE 1 — Andamiaje y contrato compartido

- `dotnet workload install maui` (requiere permiso: instala componentes de Android/iOS).
- Proyecto `AstraSystemsRental.Mobile/src/AstraSystemsRental.Mobile/` con `net10.0-android;net10.0-ios`.
- **`AstraSystemsRental.Contracts`** (netstandard/net10.0, sin dependencias de ASP.NET): los DTOs de request/response que hoy están duplicados entre cada API y el Front. Lo referencian las APIs, el Front y la app. Esto elimina la triple duplicación actual de DTOs, no solo sirve al móvil.
- `AstraApiClient`: cliente HTTP tipado con `DelegatingHandler` que (a) adjunta el Bearer, (b) inyecta `X-Astra-Company` de la compañía activa, (c) al recibir 401 renueva con el refresh **una vez** y reintenta, (d) si el refresh falla, expulsa al login.
- Almacén seguro: `SecureStorage` de MAUI → Keychain en iOS, EncryptedSharedPreferences en Android.
- Configuración de `BaseUrl` por entorno (dev apunta al Gateway de tu red local; prod a la URL pública).

## FASE 2 — Shell, sesión y navegación por nodos

- Login, logout, y **selector de compañía** (el equivalente móvil del `CompanyContextSwitcher`).
- `AppShell` con menú construido **dinámicamente desde `/apiUsers/nodes`**, aplicando el mismo gating que la web: `SuperUserOnly`, `DiagnosticsOnly` y `HasNode`. Así el menú móvil respeta plan y rol sin lógica duplicada.
- Tema oscuro portando los tokens de `tailwind.config.js` a `ResourceDictionary` (brand `#4f7cff`, accent `#3ddc97`, warn `#ffb454`, danger `#ff6b6b`, fondos `#08090d`/`#0e1119`), y las fuentes Space Grotesk / Inter.
- Manejo de sesión expirada y de "sin conexión" como estados de primera clase de la UI.

## FASE 3 — Vistas de operación (donde el móvil aporta valor)

1. **Panel** — `/apiReports/dashboard`: KPIs, agenda y alertas. Respeta la degradación por sección.
2. **Mi Flota** — listado paginado con búsqueda y filtro por estado; detalle del vehículo con sus tabs.
3. **Control de Recorrido** — registrar lecturas de kilometraje. La validación de monotonía la sigue haciendo la API; la app muestra el rango permitido para evitar el rechazo.
4. **Reservas de Taller** — agenda, crear reserva, transiciones de estado, **y fotos tomadas con la cámara** contra el endpoint multipart existente. Esta es la función que justifica la app.
5. **Rutinas de Mantenimiento** — consulta y asignación.
6. **Mi cuenta** — perfil, plan y módulos habilitados.

## FASE 4 — Vistas administrativas (paridad, como pediste)

7. **Vehículos** (cotización por placa).
8. **Usuarios** — crear, rol, plan, activar/inhabilitar (respetando el anti-autobloqueo del backend).
9. **Planes y nodos**.
10. **Logs del sistema** — gated por `CanSeeDiagnostics`, con filtros y detalle de stack trace.

Nota honesta: estas cuatro son pantallas densas, pensadas para escritorio. En móvil se resuelven con listas y hojas modales en vez de tablas, y las asumo de uso ocasional.

## FASE 5 — Empaque Android

- Keystore de firma, `APK` para distribución directa y `AAB` para Play Store.
- Íconos, splash y checklist de tienda.

**iOS queda aplazado por decisión tuya.** El proyecto se crea multi-target (`net10.0-android;net10.0-ios`) desde la Fase 1, así que el código iOS se mantiene compilable y no acumula deuda; simplemente no se empaqueta ni se publica ahora. Tienes Mac, así que cuando lo retomemos solo falta la cuenta Apple Developer y una fase corta de firma — sin reescribir nada.

---

## Verificación

- Tests unitarios del servicio de refresh tokens (Fase 0).
- Los 8 planes Odiseo existentes deben seguir en verde tras tocar el login (Fase 0 modifica `LoginResponse`).
- Pruebas manuales en emulador Android por cada vista, contra el backend real levantado en Docker.
- Odiseo **no** cubre MAUI (es Playwright/web). Las pruebas de la app son manuales o requerirían un runner de UI móvil, que queda fuera de este plan.

## Orden

Fase 0 → 1 → 2 → 3 → 4 → 5. Las fases 3 y 4 son incrementales: cada vista se puede dar por terminada y probar por separado.

## Preguntas abiertas

1. ~~**iOS**~~ — resuelto: aplazado. El proyecto queda multi-target para retomarlo sin reescribir.
2. **Rate limit**: con móviles en campo, los 120/min por IP pasan a ser un problema real. ¿Lo subimos y particionamos por identidad ahora, dentro de la Fase 0?
3. **URL pública**: hoy el Gateway solo corre local. Para probar en un teléfono real hace falta exponerlo (red local o túnel). ¿Cómo prefieres?
4. **Notificaciones push**: no están en el alcance. Tienen sentido para "tu vehículo está listo en el taller" (hoy eso es un email vía Mail.Api). ¿Las quieres en una fase posterior?
