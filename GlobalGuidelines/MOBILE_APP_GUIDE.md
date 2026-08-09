# Mobile App Guide — `AstraSystemsRental.Mobile` (.NET MAUI, Android)

For AI assistants. Compressed. La app **no tiene backend propio**: consume los mismos 75 endpoints del Gateway que el Front.

---

## Toolchain (lo que hay que tener instalado)

| Componente | Versión | Cómo se instaló |
|---|---|---|
| Workload MAUI | `maui-android` 10.0.20 | `dotnet workload install maui-android --skip-sign-check` |
| **JDK 17** | Microsoft OpenJDK 17.0.20 | `winget install Microsoft.OpenJDK.17` |
| Android SDK | API 36 | `dotnet build -t:InstallAndroidDependencies -p:AcceptAndroidSDKLicenses=true` |

**Gotcha crítico:** el instalador del Android SDK **falla con un JDK moderno**. Con solo JDK 26 presente reventaba con `ArgumentNullException (Parameter 'path1')` en `GetJdkRevision` — un mensaje que no menciona Java. Hay que instalar JDK 17 y pasarlo explícitamente con `-p:JavaSdkDirectory`. Ambas rutas quedaron fijadas en el `.csproj` (`AndroidSdkDirectory`, `JavaSdkDirectory`) para no repetirlas en cada build.

---

## Arquitectura

```
AstraSystemsRental.Contracts   ← DTOs compartidos (APIs + Front + app)
        ↑                ↑
   Front (web)      Mobile (MAUI)
```

`Contracts` también aloja `StatusText` y `ErrorText`: las traducciones son **las mismas** en web y app, no dos copias que se desincronizan.

### Servicios

- **`AstraApiClient`** — Bearer + `X-Astra-Node` + `X-Astra-Company`. Ante 401 renueva con el refresh **una vez** y reintenta; si falla, dispara `SessionExpired`. El refresh está protegido con `SemaphoreSlim`: si 3 peticiones reciben 401 a la vez, solo una renueva (las demás detectan que el token ya cambió y reusan el nuevo).
- **`SessionStore`** — `SecureStorage` de MAUI → Keychain (iOS) / EncryptedSharedPreferences (Android). Nunca en texto plano.
- **`AuthService`** — decodifica los claims `node` del JWT en cliente para replicar el gating de la web sin pedir permisos al servidor en cada pantalla.

### Offline selectivo

`OfflineQueue` (SQLite) + `SyncService`. Solo se encolan **3 acciones de campo**: kilometraje, reserva y foto.

La regla que evita el bug clásico: **un 4xx no se encola**. Si el servidor evaluó y rechazó (monotonía, solapamiento), reintentar no cambia nada — se marca como conflicto y el usuario lo ve en Mi cuenta. Solo se reintenta lo que puede tener éxito después: sin red (`0`), 5xx, 401, 408, 429.

```csharp
var isBusinessError = statusCode is >= 400 and < 500 and not 401 and not 408 and not 429;
```

---

## Prevención de errores en cliente

`TrackingViewModel` replica `MileageMonotonicityValidator` para mostrar **"Entre 45.200 y 47.800"** antes de que el usuario escriba, en vez de dejar que el servidor rechace. Usa `next-maintenance` (que ya devuelve `CurrentValue` y `LastReadingDate`), no todo el historial.

`ReservationsViewModel` **siempre envía `ExpectedEndAtUtc`**. Una reserva activa sin fecha de fin se trata como intervalo abierto y bloquea toda reserva posterior de ese vehículo; la app evita ese estado por construcción.

---

## Cámara (lo que la web no puede dar)

`MediaPicker.CapturePhotoAsync()` → se copia a `CacheDirectory` → se encola. El backend **estampa el estado de la reserva en cada foto** (`Status = entity.Status`), así que se obtiene un antes/después automático sin cambios de contrato: foto en `Pending` = al entregar, en `Collected` = al retirar.

Permisos en `Platforms/Android/AndroidManifest.xml`: `CAMERA`, `READ_MEDIA_IMAGES`, `INTERNET`, `ACCESS_NETWORK_STATE`.

---

## Configuración de red

`AppConfig.GatewayBaseUrl = "http://10.0.2.2:8080"` — `10.0.2.2` es el host de desarrollo **visto desde el emulador de Android**. En un teléfono real hay que cambiarlo por la IP de la red local o la URL pública.

`usesCleartextTraffic="true"` está activo porque el Gateway local es HTTP. **Quitarlo al pasar a HTTPS en producción.**

---

## Empaque

```bash
dotnet publish AstraSystemsRental.Mobile/src/AstraSystemsRental.Mobile/AstraSystemsRental.Mobile.csproj \
  -f net10.0-android -c Release
```

Genera APK (instalación directa) y AAB (Play Store) en `bin/Release/net10.0-android/publish/`. El APK sale firmado con la **clave de debug**; para publicar hace falta un keystore propio (`-p:AndroidKeyStore=true -p:AndroidSigningKeyStore=...`).

Verificar firma: `%LOCALAPPDATA%\Android\Sdk\build-tools\36.0.0\apksigner.bat verify --print-certs <apk>`

---

## Límite de verificación

**Odiseo es Playwright: no cubre MAUI.** Las pruebas de la app son manuales en emulador. Es el único componente del proyecto sin verificación automatizada — tenerlo presente al cambiar algo aquí.

---

## Identidad visual: portar la web de verdad, no solo sus colores

Primer intento fallido: copié los tokens de color de `tailwind.config.js` y la app igual se veía plana y ajena. **Los valores hex eran correctos; lo que faltaba era todo lo demás.** Lo que le da vida a la web y hay que replicar:

| Elemento de la web | Equivalente en MAUI |
|---|---|
| `bg-gradient-mesh` en el `body` | `RadialGradientBrush` en un `Grid` raíz que envuelve cada página |
| `bg-gradient-brand` en botones y logo | `LinearGradientBrush` 135° (`#6e93ff` → `#4f7cff` → `#2d4ed8`) |
| `shadow-brand` / `shadow-glow` | `Shadow` con `Brush="#4f7cff"` y opacidad ~0.4 |
| `shadow-card` / `shadow-panel` | `Shadow` negra, radios 18 y 30 |
| Iconos SVG del sidebar | `Geometry` en `Icons.cs`, portados de `access.Nodes.Icon` |

### Las tarjetas van en COLOR PLANO, no en gradiente

Error que costó dos iteraciones: inventé `LinearGradientBrush` para las tarjetas (`#131826` → `#0e1119`) buscando "profundidad". El resultado fue un **tinte verde-azulado (teal)** que no existe en el Front y que salta a la vista al comparar pantallas lado a lado.

La web usa color plano:

```css
.astra-card  { @apply bg-bg-card/80 border border-white/[0.07] shadow-card; }  /* #0e1119 */
.astra-panel { @apply bg-bg-panel border border-white/[0.07] shadow-panel; }   /* #101521 */
```

En MAUI: `BackgroundColor` (no `Background` con brush) y **borde `#12FFFFFF`** — blanco al 7%, no un azul sólido. Un borde de color propio también desvía el tono.

Los gradientes van **solo** donde la web los usa: fondo mesh del body, botones primarios y el orbe del logo. En ningún otro lado.

**Cómo verificar el tono**: capturar la misma zona en web y app y comparar. Si las tarjetas de la app tiran a verde o a morado, hay un gradiente o un borde de color que no corresponde.

### Iconos: `Geometry`, no `string`

`Path.Data` con `x:Static` **debe** ser `Geometry`. Con `string` compila en Debug pero **XamlC falla en Release** con `XC0009: No property, BindableProperty, or event found for "Data"` — un mensaje que no menciona el tipo. Se convierten una vez al cargar:

```csharp
private static readonly PathGeometryConverter Converter = new();
private static Geometry Parse(string data) => (Geometry)Converter.ConvertFromInvariantString(data)!;
public static readonly Geometry Truck = Parse("M2.75 6.75A1.75...");
```

**Siempre compilar en Release antes de dar por buena una vista**: Debug no ejecuta XamlC con las mismas reglas.

### Navegación: un `FlyoutItem` que contiene las pestañas

Bug real: con `TabBar` y `FlyoutItem` como hermanos, entrar a una vista del menú **reemplaza** la barra de pestañas y el usuario queda atrapado sin forma de volver. La solución es meter los `Tab` dentro de un `FlyoutItem` llamado "Inicio":

```xml
<FlyoutItem x:Name="MainItem" Title="Inicio" Route="main">
    <Tab Title="Panel" Route="dashboard">...</Tab>
    <Tab Title="Flota" Route="fleet">...</Tab>
</FlyoutItem>
```

Y navegar con `//main/dashboard`, no `//dashboard`.

### Qué NO va en la app

**Logs del sistema es exclusivo de la web.** Es una pantalla de diagnóstico densa que se consulta desde un escritorio; en un teléfono ocupa espacio en el menú sin aportar. El gating por rol sigue existiendo en el backend y en la web.

### Zona horaria

`DateTime.Now` en el emulador devuelve UTC y el saludo mostraba "Buenas noches" a las 10 AM. Para textos dependientes de la hora, fijar la zona de operación (`America/Bogota`) con fallback a la local.
