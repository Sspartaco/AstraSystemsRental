param(
    [Parameter(Position = 0)]
    [ValidateSet("up", "down", "rebuild", "logs")]
    [string]$Command = "up"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "Created .env from .env.example. Fill in your Gmail credentials before sending mail." -ForegroundColor Yellow
}

switch ($Command) {
    "up"      { docker compose up -d --build }
    "down"    { docker compose down }
    "rebuild" { docker compose build --no-cache; docker compose up -d }
    "logs"    { docker compose logs -f api-mail }
}
