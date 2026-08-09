#Requires -Version 5.1
<#
.SYNOPSIS
    Levanta el emulador de Android, instala el APK y abre la app.

.DESCRIPTION
    La app apunta a http://10.0.2.2:8080, que es como el emulador ve al host.
    Por eso funciona sin tocar nada, siempre que el Gateway este corriendo en Docker.

.EXAMPLE
    ./run-emulator.ps1              # crea el AVD si no existe, levanta e instala
    ./run-emulator.ps1 -Rebuild     # recompila el APK antes de instalar
#>
param(
    [switch]$Rebuild,
    [switch]$SkipDocker,
    [string]$AvdName = "astra-pixel"
)

$ErrorActionPreference = "Stop"

$Sdk = "$env:LOCALAPPDATA\Android\Sdk"
$Emulator = "$Sdk\emulator\emulator.exe"
$Adb = "$Sdk\platform-tools\adb.exe"
$AvdManager = "$Sdk\cmdline-tools\latest\bin\avdmanager.bat"
$SystemImage = "system-images;android-35;google_apis;x86_64"
$Apk = "$PSScriptRoot\AstraSystemsRental.Mobile\src\AstraSystemsRental.Mobile\bin\Release\net10.0-android\publish\app.codalea.astrasystems-Signed.apk"

if (-not $env:JAVA_HOME) {
    $env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-17.0.20.8-hotspot"
}

function Test-Gateway {
    try {
        $health = Invoke-RestMethod -Uri "http://localhost:8080/health" -TimeoutSec 5
        return $health.status -eq "healthy"
    }
    catch {
        return $false
    }
}

Write-Host "1. Verificando el backend..." -ForegroundColor Cyan

if (Test-Gateway) {
    Write-Host "   Gateway respondiendo en localhost:8080." -ForegroundColor Green
}
elseif ($SkipDocker) {
    Write-Host "   El Gateway no responde y se pidio -SkipDocker." -ForegroundColor Yellow
    Write-Host "   La app abrira, pero el login va a fallar." -ForegroundColor Yellow
}
else {
    Write-Host "   El Gateway no responde. Levantando el stack..." -ForegroundColor Yellow

    Push-Location $PSScriptRoot
    try {
        docker compose up -d --build
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose fallo. Revisa que Docker Desktop este corriendo."
        }
    }
    finally {
        Pop-Location
    }

    Write-Host "   Esperando a que el Gateway responda..." -ForegroundColor Yellow

    $ready = $false
    foreach ($attempt in 1..30) {
        Start-Sleep -Seconds 3
        if (Test-Gateway) { $ready = $true; break }
    }

    if ($ready) {
        Write-Host "   Stack levantado." -ForegroundColor Green
    }
    else {
        Write-Host "   El Gateway sigue sin responder tras 90s." -ForegroundColor Red
        Write-Host "   Revisa: docker compose ps  /  docker compose logs gateway" -ForegroundColor Red
    }
}

Write-Host "2. Verificando el AVD..." -ForegroundColor Cyan
$avds = & $Emulator -list-avds 2>$null

if ($avds -notcontains $AvdName) {
    Write-Host "   Creando '$AvdName'..." -ForegroundColor Yellow
    "no" | & $AvdManager create avd -n $AvdName -k $SystemImage --device "pixel_6" --force
}
Write-Host "   AVD listo: $AvdName" -ForegroundColor Green

Write-Host "3. Levantando el emulador..." -ForegroundColor Cyan
$running = & $Adb devices | Select-String "emulator-"

if (-not $running) {
    Start-Process -FilePath $Emulator -ArgumentList "-avd", $AvdName, "-no-snapshot-load"
    Write-Host "   Esperando a que arranque (puede tardar 1-2 min)..." -ForegroundColor Yellow
    & $Adb wait-for-device

    do {
        Start-Sleep -Seconds 3
        $booted = (& $Adb shell getprop sys.boot_completed 2>$null) -replace '\s', ''
    } while ($booted -ne "1")
}
Write-Host "   Emulador listo." -ForegroundColor Green

if ($Rebuild) {
    Write-Host "4. Recompilando el APK..." -ForegroundColor Cyan
    dotnet publish "$PSScriptRoot\AstraSystemsRental.Mobile\src\AstraSystemsRental.Mobile\AstraSystemsRental.Mobile.csproj" `
        -f net10.0-android -c Release --nologo
}

Write-Host "5. Instalando la app..." -ForegroundColor Cyan
if (-not (Test-Path $Apk)) {
    throw "No se encontro el APK en $Apk. Corre con -Rebuild."
}
& $Adb install -r $Apk

Write-Host "6. Abriendo la app..." -ForegroundColor Cyan
& $Adb shell monkey -p app.codalea.astrasystems -c android.intent.category.LAUNCHER 1 | Out-Null

Write-Host ""
Write-Host "Listo. Credenciales: admin@codalea.app" -ForegroundColor Green
Write-Host "Para ver logs de la app:  $Adb logcat -s DOTNET:* AndroidRuntime:E" -ForegroundColor DarkGray
