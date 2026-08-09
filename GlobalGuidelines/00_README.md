# Guidelines — AstraSystemsRental

For AI assistants and developers. Índice de estándares transversales a Front + todas las APIs (Users, Vehicles, Mail, y microservicios nuevos como Maintenance). Vive en la raíz del repo porque ninguno de estos documentos es específico de un solo proyecto.

---

## Índice

| Documento | Contenido |
|-----------|-----------|
| [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md) | Mapa de microservicios, cuándo un dominio nuevo amerita un microservicio propio vs. extender uno existente. |
| [BACKEND_SERVICE_BLUEPRINT.md](BACKEND_SERVICE_BLUEPRINT.md) | Patrón replicable de un servicio de API nueva: pasos explícitos, mapeo, `OperationResult`, `CrossApiResult`, repositorios, cuotas, owner-scoped. |
| [FRONT_UI_PATTERNS.md](FRONT_UI_PATTERNS.md) | Cards vs. tablas, paginación, Post-Redirect-Get, helper JSON, htmx, spinners. |
| [ODISEO_TESTING_GUIDE.md](ODISEO_TESTING_GUIDE.md) | Convención de test plans E2E (Probe/Odiseo/SAGA), gotchas reales del motor y de escritura de planes. |
| [DOMAIN_VALIDATION_PATTERNS.md](DOMAIN_VALIDATION_PATTERNS.md) | `Guard` extendido, validador de dominio puro invocado desde múltiples flujos. |

---

## Reglas de oro (resumen)

1. **`OperationResult` es el único idioma de error** en toda API — nunca relanzar excepciones de negocio crudas, nunca fallar en silencio en un cliente cross-API, nunca inventar un tipo de resultado propio por servicio.
2. **`BaseRepository<TContext,T>` siempre que exista un `DbContext`** — un repositorio nuevo nunca reimplementa paginación, CRUD genérico o clamp de página a mano.
3. **Owner-scoped en todo acceso a datos multi-tenant** — nunca un `GetByIdAsync` plano en un endpoint expuesto; siempre `GetOwnedAsync(id, owner, ct)` o equivalente filtrando por `OwnerType`/`OwnerId`.
4. **Mapeo con propiedades nombradas, no constructor posicional**, para cualquier DTO con más de ~6-8 campos — ver `BACKEND_SERVICE_BLUEPRINT.md`.
5. **Cero comentarios en código.**
6. **Cards en grid para listados, nunca tablas HTML** — ver `FRONT_UI_PATTERNS.md`.
7. **Test plan Odiseo obligatorio al cerrar una vista o feature nueva**, corrido en modo real (no solo `--dry-run`) contra la UI — ver `ODISEO_TESTING_GUIDE.md`.

---

## Checklist de verificación antes de dar una feature por terminada

```bash
dotnet build <proyecto tocado>.csproj      # 0 errores
npm run build:css                          # si se tocó CSS del Front
dotnet test <proyecto>.Tests.csproj        # si el proyecto tiene tests
# correr el plan Odiseo correspondiente en modo real (no --dry-run) y confirmar 0 fallos
```

- `MOBILE_APP_GUIDE.md` — app .NET MAUI Android: toolchain (ojo con el JDK), offline selectivo, cámara, empaque del APK.
- `IOS_SIN_MAC.md` — cómo llevar la app a un iPhone sin comprar una Mac: opciones, costos y el límite que no se puede evadir.
