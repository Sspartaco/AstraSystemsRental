@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

set SCRIPT_DIR=%~dp0
set RUN=%SCRIPT_DIR%run.ps1
set PS=powershell -NoProfile -ExecutionPolicy Bypass -File "%RUN%"
set DOCKER_BUILDKIT=1
set COMPOSE_DOCKER_CLI_BUILD=1

:menu
cls
echo ====================================================
echo   AstralRental - Entorno Local (orquestador)
echo ====================================================
echo.
echo   Repo:    %SCRIPT_DIR%
echo   Compose: %SCRIPT_DIR%docker-compose.yml
echo.
echo   [1] Levantar / reconstruir TODO el stack (up)
echo   [2] Reconstruir sin cache (rebuild)
echo   [3] Levantar / reconstruir UNA API
echo   [4] Aplicar esquema + seed a la BD local (reset-db)
echo   [5] Crear PRIMER superusuario (seed-superuser)
echo   [6] Ver estado de los contenedores
echo   [7] Ver logs (todos o de un servicio)
echo   [F] Levantar / reconstruir solo el FRONT
echo   [A] App ANDROID: stack + emulador + instalar APK
echo   [B] App ANDROID: igual que [A] pero recompilando el APK
echo   [L] Permitir acceso desde la RED LOCAL (iPhone / Android real)
echo   [C] Regenerar certificado HTTPS de desarrollo
echo   [U] Ver URLs (Scalar / health / Front)
echo   [P] Parar todos los contenedores (down)
echo   [0] Salir
echo.
set /p OPT=Seleccione una opcion:

if /I "%OPT%"=="1" goto up
if /I "%OPT%"=="2" goto rebuild
if /I "%OPT%"=="3" goto up_one
if /I "%OPT%"=="4" goto resetdb
if /I "%OPT%"=="5" goto seed
if /I "%OPT%"=="6" goto ps
if /I "%OPT%"=="7" goto logs
if /I "%OPT%"=="F" goto front
if /I "%OPT%"=="A" goto android
if /I "%OPT%"=="B" goto android_rebuild
if /I "%OPT%"=="L" goto lan
if /I "%OPT%"=="C" goto cert
if /I "%OPT%"=="U" goto urls
if /I "%OPT%"=="P" goto stop
if /I "%OPT%"=="0" exit /b 0
goto menu

:lan
echo.
echo === Permitir acceso desde la red local ===
echo Abre el puerto 8080 del firewall (solo en redes privadas) para que tu
echo iPhone o un Android real puedan alcanzar el Gateway.
echo Requiere permisos de administrador: Windows va a pedir confirmacion.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process powershell -Verb RunAs -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-NoExit','-File','%SCRIPT_DIR%allow-lan-access.ps1'"
pause
goto menu

:android
echo.
echo === App Android: stack + emulador + instalar APK ===
echo Hace todo de punta a punta:
echo   1. Levanta el stack de Docker si el Gateway no responde
echo   2. Crea el emulador (AVD) si no existe y lo arranca
echo   3. Instala el APK y abre la app
echo.
echo La app apunta a http://10.0.2.2:8080, que es como el emulador ve al host.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%run-emulator.ps1"
pause
goto menu

:android_rebuild
echo.
echo === App Android: recompilar APK e instalar ===
echo Igual que [A], pero recompila la app en Release antes de instalarla.
echo Usalo despues de tocar codigo de AstraSystemsRental.Mobile.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%run-emulator.ps1" -Rebuild
pause
goto menu

:up
%PS% up
pause
goto menu

:rebuild
%PS% rebuild
pause
goto menu

:up_one
echo.
echo Servicios: gateway  api-users  api-mail  api-vehicles  front
set /p SVC=Nombre del servicio:
if "%SVC%"=="" goto menu
docker compose up -d --build %SVC%
docker compose ps %SVC%
pause
goto menu

:resetdb
echo.
echo === Aplicar esquema + seed a la BD local (SQL Server local) ===
echo Esto crea/actualiza la base AstraSystemsRental en tu SQL local,
echo incluyendo TODOS los scripts de Users.Api y Vehicles.Api
echo (usuarios, planes, cuotas, companias, catalogo de vehiculos y flota).
set "CONF=N"
set /p "CONF=Continuar? (S/N) [N]: "
if /I not "%CONF%"=="S" goto menu
%PS% reset-db
pause
goto menu

:seed
echo.
echo === Crear PRIMER superusuario ===
echo Solo funciona UNA vez (se bloquea si ya existe un SuperUser).
echo Requiere el stack levantado (la api-users corriendo).
echo.
set "SU_EMAIL="
set "SU_PASS="
set /p "SU_EMAIL=Email del superusuario: "
if "%SU_EMAIL%"=="" goto menu
set /p "SU_PASS=Password (min 12 caracteres): "
if "%SU_PASS%"=="" goto menu
%PS% seed-superuser -Email "%SU_EMAIL%" -Password "%SU_PASS%"
echo.
echo [i] Guarda estas credenciales. Ingresa por /apiUsers/auth/login
pause
goto menu

:ps
docker compose ps
pause
goto menu

:logs
echo.
echo Deja vacio para ver TODOS, o escribe: gateway ^| api-users ^| api-mail ^| api-vehicles ^| front
set /p SVC=Servicio:
if "%SVC%"=="" ( %PS% logs ) else ( %PS% logs %SVC% )
goto menu

:front
echo.
echo [*] Reconstruyendo y levantando el FRONT ...
docker compose up -d --build front
docker compose ps front
echo.
echo [i] Front disponible en: https://localhost:8444/auth/login
pause
goto menu

:cert
echo.
echo [*] Regenerando certificado HTTPS de desarrollo en certs\astra-dev.pfx ...
if not exist "%SCRIPT_DIR%certs" mkdir "%SCRIPT_DIR%certs"
dotnet dev-certs https -ep "%SCRIPT_DIR%certs\astra-dev.pfx" -p astra-dev-password
dotnet dev-certs https --trust
echo [OK] Certificado listo. Recrea los contenedores (opcion 1) para que lo tomen.
pause
goto menu

:urls
echo.
echo ====================================================
echo   URLs del entorno local
echo ====================================================
echo.
echo   Front (app/login):   https://localhost:8444/auth/login
echo.
echo   Gateway  (entrada):  https://localhost:8443/scalar/v1
echo   Users:               https://localhost:5443/apiUsers/scalar/v1
echo   Mail:                https://localhost:5446/apiMail/scalar/v1
echo   Vehicles:            https://localhost:5451/apiVehicles/scalar/v1
echo.
echo   HTTP (sin TLS):      http://localhost:8080/scalar/v1
echo.
echo   Health gateway:      https://localhost:8443/health
echo.
pause
goto menu

:stop
%PS% down
pause
goto menu
