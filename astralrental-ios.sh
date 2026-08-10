#!/usr/bin/env bash
#
# AstraSystemsRental - instalar la app en un iPhone (solo macOS)
#
#   git clone https://github.com/Sspartaco/AstraSystemsRental.git
#   cd AstraSystemsRental
#   ./astralrental-ios.sh
#
# Comprueba el entorno, instala lo que falte, compila y despliega al iPhone
# conectado por cable. Sin argumentos hace todo de punta a punta.
#
#   --server http://IP:8080   direccion del Gateway (por defecto la del csproj)
#   --simulator               correr en el simulador en vez del iPhone
#   --check                   solo diagnosticar, sin compilar
#   --no-backend-check        no comprobar el Gateway (util si esta apagado)
#
set -euo pipefail

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$REPO_DIR"

MOBILE_CSPROJ="AstraSystemsRental.Mobile/src/AstraSystemsRental.Mobile/AstraSystemsRental.Mobile.csproj"
BASE_CSPROJ="AstraSystemsRental.Base/src/AstraSystemsRental.Base/AstraSystemsRental.Base.csproj"
MAUI_PROGRAM="AstraSystemsRental.Mobile/src/AstraSystemsRental.Mobile/MauiProgram.cs"

# Xcode 16 falla con "requires Xcode 26.x": el SDK de .NET para iOS lo exige.
MIN_XCODE_MAJOR=26

SERVER_URL=""
TARGET="device"
CHECK_ONLY=0
SKIP_BACKEND=0

while [[ $# -gt 0 ]]; do
    case "$1" in
        --server)            SERVER_URL="${2:-}"; shift 2 ;;
        --simulator)         TARGET="simulator"; shift ;;
        --check)             CHECK_ONLY=1; shift ;;
        --no-backend-check)  SKIP_BACKEND=1; shift ;;
        -h|--help)   sed -n '2,16p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        *) echo "Opcion desconocida: $1 (usa --help)"; exit 1 ;;
    esac
done

if [[ -t 1 ]]; then
    BOLD=$'\033[1m'; DIM=$'\033[2m'; RED=$'\033[31m'; GREEN=$'\033[32m'
    YELLOW=$'\033[33m'; CYAN=$'\033[36m'; RESET=$'\033[0m'
else
    BOLD=""; DIM=""; RED=""; GREEN=""; YELLOW=""; CYAN=""; RESET=""
fi

step()  { echo; echo "${BOLD}${CYAN}==> $*${RESET}"; }
ok()    { echo "  ${GREEN}OK${RESET}  $*"; }
warn()  { echo "  ${YELLOW}!${RESET}   $*"; }
info()  { echo "  ${DIM}$*${RESET}"; }

fail() {
    echo
    echo "${RED}${BOLD}No se pudo continuar${RESET}"
    echo "  $1"
    [[ $# -gt 1 ]] && { echo; echo "  ${BOLD}Como resolverlo:${RESET}"; echo "  $2"; }
    echo
    exit 1
}

# ---------------------------------------------------------------- entorno

step "Comprobando el entorno"

[[ "$(uname -s)" == "Darwin" ]] || fail \
    "Este script solo corre en macOS (detectado: $(uname -s))." \
    "Apple exige que el binario de iOS se compile y firme en macOS. En Windows usa astralrental-local.cmd para Android."

[[ -f "$MOBILE_CSPROJ" ]] || fail \
    "No encuentro $MOBILE_CSPROJ" \
    "Ejecuta el script desde la raiz del repo clonado."

ok "macOS $(sw_vers -productVersion)"

if ! xcode-select -p >/dev/null 2>&1; then
    fail "Xcode no esta instalado o no esta seleccionado." \
         "Instala Xcode desde la App Store, abrelo una vez para que termine de configurarse, y vuelve a ejecutar."
fi

XCODE_VER="$(xcodebuild -version 2>/dev/null | head -1 | awk '{print $2}')" || XCODE_VER=""
XCODE_MAJOR="${XCODE_VER%%.*}"

if [[ -z "$XCODE_VER" ]]; then
    fail "Xcode esta instalado pero 'xcodebuild' no responde." \
         "Abre Xcode una vez y acepta la licencia, o ejecuta:  sudo xcodebuild -license accept"
elif [[ "${XCODE_MAJOR:-0}" -lt "$MIN_XCODE_MAJOR" ]]; then
    fail "Xcode $XCODE_VER es demasiado antiguo (el SDK de .NET para iOS exige Xcode $MIN_XCODE_MAJOR o superior)." \
         "Actualiza Xcode desde la App Store."
fi
ok "Xcode $XCODE_VER"

if ! command -v dotnet >/dev/null 2>&1; then
    fail "No encuentro el SDK de .NET." \
         "Instalalo con:  brew install --cask dotnet-sdk    (o desde https://dotnet.microsoft.com/download)"
fi
ok ".NET $(dotnet --version)"

# El workload de iOS no viene con el SDK: sin el, el build falla con NETSDK1147.
if ! dotnet workload list 2>/dev/null | grep -qE '^\s*maui-ios|^\s*ios'; then
    step "Instalando el workload de MAUI para iOS"
    info "Solo pasa la primera vez. Puede pedir la contrasena de administrador."
    dotnet workload install maui-ios --skip-sign-check
fi
ok "workload maui-ios"

# ------------------------------------------------------------- dispositivo

DEVICE_ID=""

if [[ "$TARGET" == "device" ]]; then
    step "Buscando el iPhone"

    DEVICE_LINE="$(xcrun xctrace list devices 2>/dev/null \
        | sed -n '/^== Devices ==$/,/^== /p' \
        | grep -vi 'simulator' \
        | grep -iE 'iphone|ipad' \
        | head -1 || true)"

    if [[ -z "$DEVICE_LINE" ]]; then
        warn "No hay ningun iPhone conectado."
        info "Conectalo por cable, desbloquea la pantalla y toca 'Confiar en este equipo'."
        info "Si prefieres probar sin telefono:  ./astralrental-ios.sh --simulator"
        fail "Sin dispositivo no hay nada que instalar." \
             "Conecta el iPhone y vuelve a ejecutar, o usa --simulator."
    fi

    DEVICE_ID="$(sed -n 's/.*(\([0-9A-Fa-f-]\{8,\}\)).*/\1/p' <<<"$DEVICE_LINE" | tail -1)"
    ok "$(sed 's/ *(.*//' <<<"$DEVICE_LINE")"
    [[ -n "$DEVICE_ID" ]] && info "udid $DEVICE_ID"
fi

# ------------------------------------------------------------------ firma

if [[ "$TARGET" == "device" ]]; then
    step "Comprobando el certificado de firma"

    if ! security find-identity -v -p codesigning 2>/dev/null | grep -q "Apple Develop"; then
        warn "No hay ningun certificado 'Apple Development' en el llavero."
        echo
        echo "  ${BOLD}Que hacer (una sola vez):${RESET}"
        echo "    1. Abre Xcode"
        echo "    2. Xcode > Settings > Accounts > + > Apple ID"
        echo "    3. Inicia sesion con tu Apple ID (la cuenta gratuita sirve)"
        echo "    4. Selecciona la cuenta > 'Manage Certificates' > + > 'Apple Development'"
        echo
        info "Con Apple ID gratuito la app caduca a los 7 dias y hay que reinstalarla."
        info "Con Apple Developer (99 USD/ano) dura un ano y puedes usar TestFlight."
        fail "Sin certificado, iOS no deja instalar la app." \
             "Sigue los pasos de arriba y vuelve a ejecutar este script."
    fi

    IDENTITY="$(security find-identity -v -p codesigning 2>/dev/null \
        | grep "Apple Develop" | head -1 | sed 's/.*"\(.*\)".*/\1/')"
    ok "$IDENTITY"
fi

if [[ $CHECK_ONLY -eq 1 ]]; then
    echo
    echo "${GREEN}${BOLD}Entorno listo.${RESET} Vuelve a ejecutar sin --check para compilar e instalar."
    echo
    exit 0
fi

# ----------------------------------------------------------------- backend

CURRENT_HOST="$(sed -n 's/.*LanHost = "\([^"]*\)".*/\1/p' "$MAUI_PROGRAM" | head -1)"

if [[ -n "$SERVER_URL" ]]; then
    GATEWAY="$SERVER_URL"
else
    GATEWAY="http://${CURRENT_HOST}:8080"
fi

step "Comprobando el backend"
info "Gateway: $GATEWAY"

if [[ $SKIP_BACKEND -eq 1 ]]; then
    info "omitido (--no-backend-check)"
    info "La app quedara apuntando a $GATEWAY; se puede cambiar desde 'Mi cuenta > Servidor'."
elif curl -fsS -m 5 "${GATEWAY}/health" >/dev/null 2>&1; then
    ok "el Gateway responde"
else
    warn "el Gateway NO responde en $GATEWAY"
    echo
    echo "  La app se instalara igual, pero no podra iniciar sesion hasta que"
    echo "  alcance el backend. Revisa que:"
    echo "    - el stack de Docker este levantado en la maquina Windows"
    echo "    - esa maquina haya ejecutado allow-lan-access.ps1 (firewall)"
    echo "    - el iPhone y el servidor esten en la MISMA red"
    echo "      (las redes de invitados aislan los dispositivos entre si)"
    echo
    info "Puedes corregir la direccion sin recompilar desde 'Mi cuenta > Servidor',"
    info "o volver a ejecutar con:  ./astralrental-ios.sh --server http://TU-IP:8080"
    echo
    read -r -p "  Continuar de todos modos? [s/N] " REPLY
    [[ "${REPLY:-}" =~ ^[SsYy]$ ]] || exit 1
fi

# -------------------------------------------------------------- paquete Base

# nuget-local/ son binarios y no se versionan: hay que regenerarlos desde el fuente.
if ! ls nuget-local/AstraSystemsRental.Base.*.nupkg >/dev/null 2>&1; then
    step "Generando el paquete local AstraSystemsRental.Base"
    mkdir -p nuget-local
    dotnet build "$BASE_CSPROJ" -c Release --nologo -v quiet
    # pack sin un build previo empaqueta un binario obsoleto (o falla si no existe).
    dotnet pack  "$BASE_CSPROJ" -c Release -o nuget-local --no-build --nologo -v quiet
    ok "$(ls nuget-local/AstraSystemsRental.Base.*.nupkg | head -1)"
fi

# ---------------------------------------------------------------- compilar

if [[ -n "$SERVER_URL" ]]; then
    NEW_HOST="$(sed -E 's#^https?://##; s#[:/].*$##' <<<"$SERVER_URL")"
    if [[ -n "$NEW_HOST" && "$NEW_HOST" != "$CURRENT_HOST" ]]; then
        step "Apuntando la app a $NEW_HOST"
        sed -i '' "s|LanHost = \"${CURRENT_HOST}\"|LanHost = \"${NEW_HOST}\"|" "$MAUI_PROGRAM"
        ok "LanHost = $NEW_HOST"
        info "Es un cambio local; no lo subas salvo que quieras fijarlo para todos."
    fi
fi

if [[ "$TARGET" == "simulator" ]]; then
    step "Compilando y abriendo en el simulador"
    dotnet build "$MOBILE_CSPROJ" \
        -f net10.0-ios -c Release \
        -p:RuntimeIdentifier=iossimulator-arm64 \
        -t:Run --nologo
else
    step "Compilando e instalando en el iPhone"
    info "La primera vez tarda varios minutos."

    BUILD_ARGS=(
        "$MOBILE_CSPROJ"
        -f net10.0-ios -c Release
        -p:RuntimeIdentifier=ios-arm64
        -p:CodesignKey="$IDENTITY"
    )
    [[ -n "$DEVICE_ID" ]] && BUILD_ARGS+=(-p:_DeviceName="$DEVICE_ID")

    if ! dotnet build "${BUILD_ARGS[@]}" -t:Run --nologo; then
        echo
        echo "${YELLOW}${BOLD}La compilacion fallo.${RESET} Causas mas frecuentes:"
        echo
        echo "  ${BOLD}'Failed to register bundle identifier'${RESET}"
        echo "    El id app.codalea.astrasystems ya esta tomado en Apple."
        echo "    Cambia <ApplicationId> en:"
        echo "      $MOBILE_CSPROJ"
        echo "    por algo unico, p.ej. app.tunombre.astrasystems"
        echo
        echo "  ${BOLD}'No profiles for ... were found'${RESET}"
        echo "    Abre el proyecto una vez en Xcode para que genere el perfil,"
        echo "    o revisa que tu Apple ID siga agregado en Settings > Accounts."
        echo
        echo "  ${BOLD}'Untrusted Developer' en el iPhone${RESET}"
        echo "    Ajustes > General > VPN y gestion de dispositivos > confia en tu certificado."
        echo
        exit 1
    fi
fi

# ------------------------------------------------------------------ cierre

echo
echo "${GREEN}${BOLD}Listo.${RESET} La app quedo instalada."
echo

if [[ "$TARGET" == "device" ]]; then
    echo "  Si el iPhone dice ${BOLD}'Desarrollador no confiable'${RESET} al abrirla:"
    echo "    Ajustes > General > VPN y gestion de dispositivos > tu Apple ID > Confiar"
    echo
    echo "  ${YELLOW}Con Apple ID gratuito la app caduca a los 7 dias.${RESET}"
    echo "  Para renovarla, vuelve a ejecutar este script."
    echo
fi

echo "  Backend: $GATEWAY"
echo "  ${DIM}Si cambia la IP, ajustala en la app desde 'Mi cuenta > Servidor'.${RESET}"
echo
