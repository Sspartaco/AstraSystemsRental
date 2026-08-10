# Compilar la app para iPhone (macOS)

Guía autocontenida. Si sos un agente trabajando en la Mac y no conocés este repo, esto es todo lo que necesitás.

---

## Qué se hace acá y qué no

**El único propósito de la Mac es compilar y firmar el binario de iOS.** Apple exige que eso ocurra en macOS; no es una limitación de .NET MAUI.

| En la Mac | En la máquina Windows |
|---|---|
| Compilar la app iOS | Todo el backend (6 APIs, Gateway, SQL Server, Front) |

**No levantes Docker, ni SQL Server, ni las APIs en la Mac.** No hacen falta y el script no los toca. El repo se clona entero solo porque la app referencia el proyecto `AstraSystemsRental.Base` por ruta relativa.

---

## Requisitos previos

### 1. Xcode 26 o superior

Desde la App Store. Son ~15 GB.

**Abrilo una vez después de instalar** y aceptá la licencia — si no, `xcodebuild` no responde y el script se detiene.

> Xcode 16 **no sirve**: el SDK de .NET para iOS exige Xcode 26 y falla con *"requires Xcode 26.x"*.

### 2. .NET 10

```bash
brew install --cask dotnet-sdk
```

O desde <https://dotnet.microsoft.com/download> (elegir .NET 10, Arm64).

El workload `maui-ios` **no hace falta instalarlo a mano** — el script lo instala si falta.

### 3. Un Apple ID agregado en Xcode

Esto genera el certificado de firma. Sin él iOS no permite instalar la app.

1. Abrir **Xcode**
2. **Xcode → Settings → Accounts**
3. **`+`** → **Apple ID** → iniciar sesión
4. Seleccionar la cuenta → **Manage Certificates** → **`+`** → **Apple Development**

> **Un Apple ID gratuito alcanza** para instalar en el propio iPhone. No hace falta la cuenta de Apple Developer de 99 USD/año. El límite del plan gratuito: **la app caduca a los 7 días** y hay que volver a ejecutar el script para renovarla.

### 4. El iPhone conectado

- Por **cable**
- Con la pantalla **desbloqueada**
- Tocar **"Confiar en este equipo"** cuando aparezca el aviso

Sin confiar, el script no detecta el dispositivo.

---

## Ejecución

```bash
git clone https://github.com/Sspartaco/AstraSystemsRental.git
cd AstraSystemsRental
./astralrental-ios.sh --no-backend-check
```

Conviene correr primero el diagnóstico, que no compila nada:

```bash
./astralrental-ios.sh --check
```

Repetirlo hasta que diga *"Entorno listo"*.

### Opciones

| Opción | Para qué |
|---|---|
| *(sin argumentos)* | Compila e instala en el iPhone; comprueba que el backend responda |
| `--no-backend-check` | Igual, pero sin comprobar el backend. **Es la opción normal si el backend está en otra máquina** |
| `--check` | Solo diagnostica el entorno |
| `--simulator` | Corre en el simulador de Xcode. No requiere iPhone ni certificado |
| `--server http://IP:8080` | Apunta la app a otro Gateway |

### Qué hace el script

1. Verifica macOS, Xcode 26+, .NET
2. Instala el workload `maui-ios` si falta
3. Busca el iPhone conectado y el certificado de firma
4. Regenera el paquete local `AstraSystemsRental.Base` (`nuget-local/` no está versionado, hay que reconstruirlo desde el fuente)
5. Compila `AstraSystemsRental.Mobile` para `ios-arm64` firmando con el certificado
6. Instala y lanza la app en el iPhone

Cuando algo falta **no compila a medias**: se detiene y dice qué hacer.

---

## Después de instalar

La primera vez que abras la app, iOS dirá **"Desarrollador no confiable"**:

**Ajustes → General → VPN y gestión de dispositivos → tu Apple ID → Confiar**

---

## Errores frecuentes

| Mensaje | Causa y solución |
|---|---|
| `Failed to register bundle identifier` | El id `app.codalea.astrasystems` ya está tomado en Apple. Cambiar `<ApplicationId>` en `AstraSystemsRental.Mobile/src/AstraSystemsRental.Mobile/AstraSystemsRental.Mobile.csproj` por uno único (ej. `app.tunombre.astrasystems`) |
| `No profiles for ... were found` | Abrir el proyecto una vez en Xcode para que genere el provisioning profile, o verificar que el Apple ID siga en Settings → Accounts |
| `requires Xcode 26.x` | Xcode desactualizado. Actualizar desde la App Store |
| `NETSDK1147` / falta workload | El script lo instala solo; si falla: `dotnet workload install maui-ios --skip-sign-check` |
| `NU1301: local source doesn't exist` | Falta `nuget-local/`. El script lo regenera; si falla, ver la nota de abajo |
| `permission denied` al ejecutar | `bash astralrental-ios.sh` |
| `bad interpreter: No such file` | El script llegó con finales de línea CRLF. `.gitattributes` lo previene; si pasa: `sed -i '' 's/\r$//' astralrental-ios.sh` |
| La app abre pero no inicia sesión | Es esperado si el backend no está accesible. Ver la sección siguiente |

### Nota sobre el paquete Base

`nuget-local/` contiene binarios y está en `.gitignore`, así que no viaja con el clone. Si hay que regenerarlo a mano:

```bash
mkdir -p nuget-local
dotnet build AstraSystemsRental.Base/src/AstraSystemsRental.Base/AstraSystemsRental.Base.csproj -c Release
dotnet pack  AstraSystemsRental.Base/src/AstraSystemsRental.Base/AstraSystemsRental.Base.csproj -c Release -o nuget-local --no-build
```

⚠️ **`pack` debe ir después de `build`**: sin un build previo empaqueta un binario obsoleto o falla.

---

## Para *usar* la app hace falta el backend

La app no tiene datos propios: consume las APIs a través del Gateway. Instalarla funciona sin backend, pero **no se puede ni iniciar sesión** hasta que:

1. El stack de Docker esté levantado en la máquina Windows
2. Esa máquina haya ejecutado `allow-lan-access.ps1` (abre el firewall en los puertos 8080/8443, solo en perfil Privado)
3. El iPhone y esa máquina estén en **la misma red**

Dos cosas que fallan aunque el firewall esté bien:
- **Aislamiento de clientes** en redes de invitados: los dispositivos no se ven entre sí
- **La IP cambia** con DHCP

Por eso la app permite cambiar la dirección en caliente: **Mi cuenta → Servidor**, sin recompilar. Se guarda en `Preferences` y tiene prioridad sobre el valor compilado.

---

## Referencias

- [`IOS_SIN_MAC.md`](IOS_SIN_MAC.md) — alternativas cuando no hay Mac (GitHub Actions, Mac en la nube), y por qué el artefacto del CI sin firma no se instala en un iPhone
- [`MOBILE_APP_GUIDE.md`](MOBILE_APP_GUIDE.md) — arquitectura de la app, configuración de red, empaque de Android
