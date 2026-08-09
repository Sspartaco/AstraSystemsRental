param(
    [Parameter(Position = 0)]
    [ValidateSet("up", "down", "rebuild", "logs", "reset-db", "seed-superuser")]
    [string]$Command = "up",

    [Parameter(Position = 1)]
    [string]$Service,

    [string]$Email,
    [string]$Password,
    [string]$FirstNames = "System",
    [string]$LastNames = "Administrator",
    [string]$DocumentNumber = "SUPERUSER"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "Created .env from .env.example. Review secrets before running." -ForegroundColor Yellow
}

function Get-EnvValue([string]$key) {
    $line = Get-Content ".env" | Where-Object { $_ -match "^$key=" } | Select-Object -First 1
    if ($line) { return $line.Substring($line.IndexOf('=') + 1) }
    return $null
}

$sqlcmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE"

switch ($Command) {
    "up"      { docker compose up -d --build }
    "down"    { docker compose down }
    "rebuild" { docker compose build --no-cache; docker compose up -d }
    "logs"    {
        if ($Service) { docker compose logs -f $Service }
        else { docker compose logs -f }
    }
    "reset-db" {
        $usersDbDir = Join-Path $root "AstraSystemsRental.Users.Api\SolutionItems\db"
        $vehiclesDbDir = Join-Path $root "AstraSystemsRental.Vehicles.Api\SolutionItems\db"
        $maintenanceDbDir = Join-Path $root "AstraSystemsRental.Maintenance.Api\SolutionItems\db"
        $order = @(
            @{ Dir = $usersDbDir;    File = "00_create_database.sql" },
            @{ Dir = $usersDbDir;    File = "03_schema_access.sql" },
            @{ Dir = $usersDbDir;    File = "02_schema_subscriptions.sql" },
            @{ Dir = $usersDbDir;    File = "01_schema_users.sql" },
            @{ Dir = $usersDbDir;    File = "06_schema_companies.sql" },
            @{ Dir = $usersDbDir;    File = "07_schema_plan_node_quotas.sql" },
            @{ Dir = $usersDbDir;    File = "08_schema_company_invitations.sql" },
            @{ Dir = $usersDbDir;    File = "04_indexes.sql" },
            @{ Dir = $usersDbDir;    File = "05_seed.sql" },
            @{ Dir = $usersDbDir;    File = "09_seed_maintenance_nodes.sql" },
            @{ Dir = $usersDbDir;    File = "10_schema_logs.sql" },
            @{ Dir = $usersDbDir;    File = "11_schema_refresh_tokens.sql" },
            @{ Dir = $vehiclesDbDir; File = "01_schema_vehicles.sql" },
            @{ Dir = $vehiclesDbDir; File = "02_seed_sources.sql" },
            @{ Dir = $vehiclesDbDir; File = "03_indexes.sql" },
            @{ Dir = $vehiclesDbDir; File = "04_add_vehicle_image.sql" },
            @{ Dir = $vehiclesDbDir; File = "05_schema_fleet_vehicles.sql" },
            @{ Dir = $vehiclesDbDir; File = "06_schema_pending_quota_compensations.sql" },
            @{ Dir = $maintenanceDbDir; File = "01_schema_maintenance.sql" },
            @{ Dir = $maintenanceDbDir; File = "02_migrate_odometer_readings.sql" }
        )
        foreach ($item in $order) {
            $path = Join-Path $item.Dir $item.File
            Write-Host "Applying $($item.File) ..." -ForegroundColor Cyan
            & $sqlcmd -S localhost -E -C -i $path
        }
        Write-Host "Database reset complete (local SQL Server)." -ForegroundColor Green
    }
    "seed-superuser" {
        if (-not $Email -or -not $Password) {
            Write-Host "Usage: ./run.ps1 seed-superuser -Email <email> -Password <password> [-FirstNames ..] [-LastNames ..] [-DocumentNumber ..]" -ForegroundColor Yellow
            Write-Host "Creates the first SuperUser. Only works once (disabled after a SuperUser exists)." -ForegroundColor Yellow
            return
        }
        $secret = Get-EnvValue "BOOTSTRAP_SECRET"
        if (-not $secret) {
            Write-Host "BOOTSTRAP_SECRET is not set in .env." -ForegroundColor Red
            return
        }
        Write-Host "Seeding SuperUser via the api-users container ..." -ForegroundColor Cyan
        docker compose exec -T api-users dotnet AstraSystemsRental.Users.Api.dll seed-superuser `
            --email $Email `
            --password $Password `
            --firstNames $FirstNames `
            --lastNames $LastNames `
            --documentNumber $DocumentNumber `
            --secret $secret
    }
}
