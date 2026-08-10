<#
.SYNOPSIS
    Publica el Gateway en internet con HTTPS, usando un tunel de Cloudflare.

.DESCRIPTION
    Abrir el puerto 8080 en el router NO es una alternativa equivalente: el
    Gateway habla HTTP plano, asi que las credenciales viajarian sin cifrar y
    ademas quedaria expuesta la maquina donde corre SQL Server.

    El tunel resuelve las dos cosas: Cloudflare termina TLS y la conexion sale
    DESDE este equipo, por lo que no hay puertos abiertos ni IP publica expuesta.

    La URL cambia cada vez que se levanta (son tuneles anonimos). Para una URL
    fija hace falta una cuenta de Cloudflare y un tunel con nombre.

.EXAMPLE
    .\expose-public.ps1
    .\expose-public.ps1 -Stop
#>
param(
    [switch]$Stop
)

$ErrorActionPreference = "Stop"

$exe = "$env:ProgramFiles(x86)\cloudflared\cloudflared.exe"
if (-not (Test-Path $exe)) { $exe = "$env:ProgramFiles\cloudflared\cloudflared.exe" }

if ($Stop) {
    Get-Process cloudflared -ErrorAction SilentlyContinue | Stop-Process -Force
    Write-Host "Tunel detenido. Las APIs vuelven a ser accesibles solo en la red local." -ForegroundColor Yellow
    exit 0
}

if (-not (Test-Path $exe)) {
    Write-Host "cloudflared no esta instalado. Instalalo con:" -ForegroundColor Red
    Write-Host "  winget install --id Cloudflare.cloudflared" -ForegroundColor White
    exit 1
}

try {
    $null = Invoke-WebRequest -Uri "http://localhost:8080/health" -TimeoutSec 5 -UseBasicParsing
}
catch {
    Write-Host "El Gateway no responde en localhost:8080." -ForegroundColor Red
    Write-Host "Levanta el stack primero (opcion [1] de astralrental-local.cmd)." -ForegroundColor White
    exit 1
}

Get-Process cloudflared -ErrorAction SilentlyContinue | Stop-Process -Force

$log = Join-Path $env:TEMP "astra-tunnel.log"
Remove-Item $log -ErrorAction SilentlyContinue

Write-Host "Levantando el tunel..." -ForegroundColor Cyan

# --edge-ip-version 4: el resolver interno de cloudflared falla de forma
# intermitente contra los AAAA de api.trycloudflare.com ("no such host").
Start-Process -FilePath $exe `
    -ArgumentList "tunnel", "--url", "http://localhost:8080", "--no-autoupdate", "--edge-ip-version", "4" `
    -RedirectStandardError $log -WindowStyle Hidden

$url = $null
foreach ($i in 1..30) {
    Start-Sleep -Seconds 2
    if (Test-Path $log) {
        $match = Select-String -Path $log -Pattern "https://[a-z0-9-]+\.trycloudflare\.com" -AllMatches |
                 ForEach-Object { $_.Matches.Value } |
                 Where-Object { $_ -notlike "*api.trycloudflare*" } |
                 Select-Object -First 1
        if ($match) { $url = $match; break }
    }
}

if (-not $url) {
    Write-Host "No se pudo levantar el tunel. Revisa: $log" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "  URL publica:  $url" -ForegroundColor Green
Write-Host ""

try {
    $null = Invoke-WebRequest -Uri "$url/health" -TimeoutSec 20 -UseBasicParsing
    Write-Host "  Verificado: el Gateway responde por HTTPS." -ForegroundColor Green
}
catch {
    Write-Host "  El tunel esta arriba pero /health no respondio todavia. Reintenta en unos segundos." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  En la app:  Mi cuenta > Servidor > pega esa URL > Guardar" -ForegroundColor Cyan
Write-Host "  Cerra y volve a abrir la app para aplicarla." -ForegroundColor DarkGray
Write-Host ""
Write-Host "  La URL cambia cada vez que levantas el tunel." -ForegroundColor Yellow
Write-Host "  Para detenerlo:  .\expose-public.ps1 -Stop" -ForegroundColor DarkGray
Write-Host ""
