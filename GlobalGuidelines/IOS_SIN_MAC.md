# Llevar la app a un iPhone sin tener una Mac

For AI assistants + referencia del equipo. Estado verificado a agosto 2026.

---

## El límite que no se puede evadir

**El binario de iOS tiene que compilarse y firmarse en macOS.** Es una condición de la licencia de Apple, no una limitación técnica de .NET MAUI. No existe forma legal ni práctica de producir un `.ipa` instalable desde Windows.

Lo que **sí** se puede evitar es *comprar* una Mac: hay servicios que la alquilan por horas o la proveen dentro de un pipeline de CI.

Además, **para instalar en un iPhone físico siempre hace falta una cuenta de Apple Developer** (99 USD/año) o, como mínimo, un Apple ID gratuito con las limitaciones que se explican abajo.

---

## Opciones, de menor a mayor fricción

### 1. GitHub Actions con runner macOS ✅ FUNCIONANDO

Configurado en `.github/workflows/ios-build.yml` y **verificado en verde**: compila la app para iOS y publica un artefacto de ~24 MB. Se dispara a mano desde la pestaña *Actions* o con `gh workflow run ios-build.yml`.

**Cinco obstáculos reales que costó resolver** (documentados para no repetirlos):

1. **`secrets` no se evalúa en un `if:` de step** → `Unrecognized named-value: 'secrets'`. Hay que promoverlo a `env` del job y que la condición mire `env`.
2. **Faltaba el workload de Android.** Aunque se compile con `-f net10.0-ios`, MSBuild evalúa *todos* los `TargetFrameworks` del csproj → `NETSDK1147`. Solución: `dotnet workload install maui-ios maui-android`.
3. **`nuget-local/` no se versiona** (son binarios en `.gitignore`) → `NU1301: The local source doesn't exist`. El CI regenera el paquete `Base` desde el fuente.
4. **`dotnet pack` sin `build` previo** no encuentra la dll. Mismo gotcha ya documentado para la publicación local.
5. **`macos-15` trae Xcode 16.4** y el SDK de .NET para iOS exige Xcode 26 → hay que usar `runs-on: macos-26`. Y **no forzar** un Xcode específico: `Xcode_26.6.0.app` está instalado pero **incompleto** (sin SDK de macOS), y `actool` falla con `exit 72`. El Xcode por defecto de la imagen es el bueno.

- **Costo**: gratis en repos públicos. En repos privados los minutos de macOS consumen **10x** del cupo — el plan gratuito da 2.000 min/mes, o sea ~200 min reales de macOS.
- **Sin secretos de firma**: produce un `.app` para simulador. Sirve para revisar que la UI compile y se vea bien, **no** se instala en un iPhone.
- **Con secretos de firma**: produce el `.ipa` instalable. Hay que cargar en *Settings → Secrets*: `APPLE_CERTIFICATE_P12`, `APPLE_CERTIFICATE_PASSWORD`, `APPLE_PROVISIONING_PROFILE`, `APPLE_PROVISIONING_PROFILE_NAME`, `APPLE_SIGNING_IDENTITY`.

El obstáculo: **generar el certificado `.p12` y el provisioning profile requiere entrar al portal de Apple Developer**, lo cual se puede hacer desde el navegador en Windows, pero el CSR (Certificate Signing Request) tradicionalmente se crea en macOS. Alternativa sin Mac: generar el CSR con OpenSSL en Windows y subirlo al portal.

### 2. Mac en la nube por horas

MacInCloud, MacStadium, Scaleway Apple Silicon. **~30-60 USD/mes**, o planes por hora más baratos para uso puntual.

Ventaja sobre CI: es una Mac real con escritorio, así que sirve para generar los certificados, usar Xcode y **depurar en el simulador**. Es la opción si vas a iterar sobre iOS, no solo compilar una vez.

### 3. Codemagic / Bitrise / Expo EAS

CI especializado en móvil, con tier gratuito acotado. Manejan la firma de forma más guiada que GitHub Actions (suben el `.p12` por interfaz web). Codemagic tiene soporte explícito para .NET MAUI.

### 4. Pair to Mac de Visual Studio

Si conseguís acceso a **cualquier** Mac en tu red (prestada, de un colega, un Mac mini viejo), Visual Studio en Windows se conecta a ella y compila/depura remotamente. Programás en Windows, la Mac solo compila.

---

## Cómo instalar el IPA en el iPhone una vez generado

| Método | Requiere | Vigencia | Notas |
|---|---|---|---|
| **TestFlight** | Apple Developer (99 USD/año) | 90 días por build | El más cómodo: el usuario instala desde la app TestFlight. Hasta 100 testers internos |
| **Ad Hoc** | Apple Developer + UDID del dispositivo | 1 año | Se instala por cable o por link. Hay que registrar cada iPhone |
| **Apple ID gratuito** | Solo un Apple ID | **7 días** | La app deja de abrir a la semana y hay que reinstalar. Solo para probar |
| **Enterprise** | Apple Developer Enterprise (299 USD/año) | 1 año | Distribución interna sin App Store. Requiere ser una organización |

Para tu caso (probar la app en tu propio iPhone), **TestFlight con la cuenta de 99 USD/año es lo más práctico**: instalás y actualizás sin cables ni UDIDs.

---

## Estado del proyecto

El código **ya está preparado para iOS**:

- `Platforms/iOS/` con `AppDelegate.cs`, `Program.cs` e `Info.plist` (incluye los permisos de cámara y galería que la app usa, obligatorios en iOS o la App Store rechaza el binario).
- El `.csproj` agrega `net10.0-ios` **solo cuando compila en macOS**, así Android sigue compilando en Windows sin tocar nada.
- **No hay código específico de Android** en `Services/` ni `ViewModels/`: todo usa APIs de MAUI que funcionan igual en ambas plataformas.

Falta únicamente el paso que exige macOS: compilar y firmar.

---

## Antes de instalar en un iPhone real

### ¿Hay que hacer públicas las APIs? No.

La pregunta natural es si el backend tiene que salir a internet. **No hace falta**: alcanza con que el iPhone y el equipo estén en la misma red local.

Docker ya publica los puertos en **todas** las interfaces, así que el Gateway responde en la IP de red del equipo sin tocar una línea de backend. Lo único que lo bloquea es el firewall de Windows:

```
.\allow-lan-access.ps1        # como administrador (o la opción [L] de astralrental-local.cmd)
```

Crea la regla `AstraSystems Gateway (LAN)` para 8080/8443, **solo en el perfil Privado**. Si la red actual figura como Pública la regla no aplica — el script lo avisa y muestra el comando para cambiarla.

Verificá desde Safari en el iPhone: `http://<ip-del-equipo>:8080/health` debe devolver `{"status":"healthy"...}`.

Dos cosas que fallan aunque el firewall esté bien:
- **Aislamiento de clientes** en redes de invitados: los dispositivos no se ven entre sí. Hay que usar la red normal.
- **La IP del equipo cambia** con DHCP. Por eso la app permite editarla en caliente.

### Configurar la dirección en la app

`AppConfig.GatewayBaseUrl` ya no es una constante: en el emulador de Android resuelve `10.0.2.2` (alias interno hacia el host, **inexistente en un dispositivo físico**) y en cualquier dispositivo real usa `AppConfig.LanHost`.

Además, ***Mi cuenta → Servidor*** permite escribir otra URL sin recompilar; se guarda en `Preferences` y tiene prioridad sobre todo lo demás. Es la vía práctica para el iPhone: instalás una vez y ajustás la dirección desde el teléfono cuando cambie la red.

`NSAllowsArbitraryLoads` está en `true` en el `Info.plist` porque el Gateway local es HTTP. **Apple rechaza apps con esa bandera en la App Store sin justificación**, así que hay que quitarla al pasar a HTTPS.
