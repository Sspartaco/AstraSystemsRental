# AstraSystemsRental

Fleet management platform (codalea), built as independent .NET 10 minimal-API microservices behind a YARP gateway. Each API is its own solution and shares a single reusable base package (`AstraSystemsRental.Base`) served from a local NuGet feed.

---

## ¿Estás en una Mac para compilar la app de iOS?

**Es lo único que se hace en macOS. No levantes nada más de este repo ahí.**

```bash
./scripts/astralrental-ios.sh --no-backend-check
```

Un solo comando: comprueba el entorno, instala lo que falte, compila e instala la app en el iPhone conectado por cable. Si algo falta, se detiene y explica qué hacer.

**Lee [`GlobalGuidelines/COMPILAR_IOS_EN_MAC.md`](GlobalGuidelines/COMPILAR_IOS_EN_MAC.md) antes de ejecutarlo** — tiene los requisitos previos (Xcode 26+, Apple ID en Xcode) y los errores frecuentes con su solución.

Lo que la Mac **no** necesita: Docker, SQL Server, las APIs, el Front. El backend corre en otra máquina.

---

## Antes de tocar código

Lee **[`GlobalGuidelines/00_README.md`](GlobalGuidelines/00_README.md)** — índice de estándares y las 10 reglas de oro. Tres que se rompen seguido:

- **Toda vista nueva del Front se evalúa para la app en el mismo PR.** Operación → va a la app; administración (usuarios, compañías, planes, logs) → solo web. La decisión se escribe, no se omite.
- **Leer con tracking lo que se vaya a escribir.** `AsNoTracking` devuelve `200 OK` sin emitir el `UPDATE`; falla en silencio.
- **Ningún error llega al usuario en inglés** — todo pasa por `ErrorText.Translate`, en `Contracts`, compartido por web y app.

## Estructura del repo

```
AstraSystemsRental.*/     Las 10 soluciones (6 APIs, Gateway, Front, Contracts, Mobile)
GlobalGuidelines/         Guías vigentes: arquitectura, blueprint, UI, app móvil, testing
docs/ai-context/          Contexto histórico por feature
docs/planes/              Planes ya ejecutados (referencia)
scripts/                  Utilidades del entorno local — ver scripts/README.md
certs/ keys/              Certificados y llaves de desarrollo (NO versionados)
nuget-local/              Paquete Base compilado (NO versionado, se regenera)

astralrental-local.cmd    ← punto de entrada: menú del entorno local
docker-compose.yml        Los 6 servicios + Front
```

**Empezá por `astralrental-local.cmd`**: levanta el stack, resetea la BD, arranca
el emulador Android y muestra cómo compilar para iPhone.

## Solutions

| Solution | Port | Responsibility |
|----------|------|----------------|
| `AstraSystemsRental.Base` | — | Reusable library (response envelope, API bootstrap, JWT RS256, Dapper persistence, validation). Packed to `nuget-local/`. |
| `AstraSystemsRental.Gateway` | 8080 | Single entry point. YARP reverse proxy + access control (JWT, subscription window, node access). |
| `AstraSystemsRental.Users.Api` | 5001 | Persons, users, email confirmation, login and RS256 token issuing. Owns the SQL schema. |
| `AstraSystemsRental.Mail.Api` | 5006 | Welcome email delivery (Gmail SMTP + Razor HTML5 templates). |
| `AstraSystemsRental.Vehicles.Api` | 5011 | Vehicle catalog, fleet registry and plate-based quotation. |
| `AstraSystemsRental.Maintenance.Api` | 5016 | Maintenance routines, mileage tracking and workshop reservations. |
| `AstraSystemsRental.Reports.Api` | 5021 | Composes fleet + workshop metrics into the dashboard. |
| `AstraSystemsRental.Front` | 8444 | MVC + htmx + Tailwind web app. |
| `AstraSystemsRental.Contracts` | — | DTOs and display text shared by the APIs, the Front and the mobile app. |
| `AstraSystemsRental.Mobile` | — | .NET MAUI app (Android + iOS) consuming the same gateway endpoints. |

## Standardized response

Every endpoint returns the same envelope:

```json
{ "success": true, "data": { }, "errors": [], "traceId": "..." }
```

There are no per-endpoint response models; `data` carries any payload.

## Security

- **RS256 asymmetric JWT**: Users.Api signs with the private key; the Gateway and other APIs validate with the public key. Keys live in `keys/` for local development and are mounted read-only into the containers. Production keys are supplied through environment/volume, never committed.
- The Gateway is the first filter: it validates the token, checks the subscription window (SuperUser bypasses), and enforces node access (`X-Astra-Node` header) against the plan/role nodes carried in the token.
- Passwords are hashed with PBKDF2 (SHA-256, 210k iterations).
- Security headers, rate limiting and a global exception filter are applied by the base pipeline.

## Data model

One database `AstraSystemsRental`, three schemas (`users`, `subscriptions`, `access`), eight tables. Idempotent scripts live in `AstraSystemsRental.Users.Api/SolutionItems/db/` and run in the order defined by `99_deployment.sql`.

## Run it locally (Docker)

```powershell
# From the repo root
Copy-Item .env.example .env   # then fill in SA_PASSWORD, Gmail credentials, INTERNAL_API_KEY
./scripts/run.ps1 up                  # builds and starts sql + gateway + users + mail
./scripts/run.ps1 reset-db            # applies the SQL schema and seed to the sql container
./scripts/run.ps1 logs gateway        # tail a service
./scripts/run.ps1 down                # stop everything
```

The gateway is reachable at `http://localhost:8080`. Each API also has its own `run.ps1`/`docker-compose.yml` for running in isolation.

## End-to-end flow

1. `POST /apiUsers/users` → creates the person + user (Demo role/plan), sends the welcome email with a confirmation link.
2. `POST /apiUsers/users/confirm` → validates the token, activates the account and sets the password.
3. `POST /apiUsers/auth/login` → returns an RS256 access token.
4. Any protected call goes through the gateway, which validates the token and node access before proxying.

## Tests

Each solution has an xUnit test project:

```powershell
dotnet test AstraSystemsRental.Base       # 17
dotnet test AstraSystemsRental.Mail.Api   # 4
dotnet test AstraSystemsRental.Users.Api  # 11
dotnet test AstraSystemsRental.Gateway    # 7
```

Postman collections are under each solution's `postman/` folder.

## Updating the base package

The base is consumed as a versioned NuGet package from `nuget-local/`. After changing it:

```powershell
dotnet pack AstraSystemsRental.Base/src/AstraSystemsRental.Base -c Release
# bump <Version> in the csproj, then update the version in each API's Directory.Packages.props
```
