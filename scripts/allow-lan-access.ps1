#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Permite que otros dispositivos de tu red (iPhone, Android real) alcancen el Gateway.

.DESCRIPTION
    Docker ya publica los puertos en todas las interfaces, asi que el Gateway responde
    en la IP de red del equipo. Lo unico que falta es que el firewall de Windows deje
    entrar esas conexiones.

    La regla se limita al perfil PRIVADO: solo aplica en redes marcadas como privadas
    (tu casa/oficina), no en redes publicas.

.EXAMPLE
    Click derecho > Ejecutar con PowerShell (como administrador)
    o:  powershell -ExecutionPolicy Bypass -File .\allow-lan-access.ps1
#>
param(
    [switch]$Remove
)

$ErrorActionPreference = "Stop"
$RuleName = "AstraSystems Gateway (LAN)"

if ($Remove) {
    Get-NetFirewallRule -DisplayName $RuleName -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    Write-Host "Regla eliminada." -ForegroundColor Yellow
    exit 0
}

$existing = Get-NetFirewallRule -DisplayName $RuleName -ErrorAction SilentlyContinue

if ($existing) {
    Write-Host "La regla ya existe." -ForegroundColor Green
}
else {
    New-NetFirewallRule -DisplayName $RuleName `
        -Direction Inbound -LocalPort 8080, 8443 -Protocol TCP `
        -Action Allow -Profile Private | Out-Null
    Write-Host "Regla creada para los puertos 8080 y 8443 (perfil privado)." -ForegroundColor Green
}

# La regla solo sirve si la red actual esta marcada como privada.
$publicNets = Get-NetConnectionProfile | Where-Object { $_.NetworkCategory -eq 'Public' }

if ($publicNets) {
    Write-Host ""
    Write-Host "ATENCION: estas redes estan marcadas como PUBLICAS y la regla no aplica:" -ForegroundColor Yellow
    $publicNets | ForEach-Object { Write-Host "  - $($_.Name) ($($_.InterfaceAlias))" -ForegroundColor Yellow }
    Write-Host "Para cambiarlas:  Set-NetConnectionProfile -InterfaceAlias '<alias>' -NetworkCategory Private" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "IPs de este equipo en la red local:" -ForegroundColor Cyan
Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' -and $_.InterfaceAlias -notlike '*WSL*' -and $_.InterfaceAlias -notlike '*Default Switch*' } |
    ForEach-Object { Write-Host "  http://$($_.IPAddress):8080   ($($_.InterfaceAlias))" -ForegroundColor White }

Write-Host ""
Write-Host "Probalo desde el navegador del telefono:  http://<esa-ip>:8080/health" -ForegroundColor Cyan
Write-Host "Debe responder: {`"status`":`"healthy`"...}" -ForegroundColor DarkGray
