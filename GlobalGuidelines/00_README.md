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
| [MOBILE_APP_GUIDE.md](MOBILE_APP_GUIDE.md) | App .NET MAUI: **reparto app/web**, toolchain, offline selectivo, cámara, empaque del APK. |
| [COMPILAR_IOS_EN_MAC.md](COMPILAR_IOS_EN_MAC.md) | Compilar e instalar la app en un iPhone desde una Mac. Autocontenida. |
| [IOS_SIN_MAC.md](IOS_SIN_MAC.md) | Alternativas cuando no hay Mac: CI, Mac en la nube, costos y límites. |

---

## Reglas de oro (resumen)

1. **`OperationResult` es el único idioma de error** en toda API — nunca relanzar excepciones de negocio crudas, nunca fallar en silencio en un cliente cross-API, nunca inventar un tipo de resultado propio por servicio.
2. **`BaseRepository<TContext,T>` siempre que exista un `DbContext`** — un repositorio nuevo nunca reimplementa paginación, CRUD genérico o clamp de página a mano.
3. **Owner-scoped en todo acceso a datos multi-tenant** — nunca un `GetByIdAsync` plano en un endpoint expuesto; siempre `GetOwnedAsync(id, owner, ct)` o equivalente filtrando por `OwnerType`/`OwnerId`.
4. **Mapeo con propiedades nombradas, no constructor posicional**, para cualquier DTO con más de ~6-8 campos — ver `BACKEND_SERVICE_BLUEPRINT.md`.
5. **Cero comentarios en código.**
6. **Cards en grid para listados, nunca tablas HTML** — ver `FRONT_UI_PATTERNS.md`.
7. **Test plan Odiseo obligatorio al cerrar una vista o feature nueva**, corrido en modo real (no solo `--dry-run`) contra la UI — ver `ODISEO_TESTING_GUIDE.md`.
8. **Toda vista nueva del Front se evalúa para la app en el MISMO PR.** Si es de operación (algo que se hace parado al lado del vehículo) va a la app; si es administrativa (usuarios, compañías, planes, logs) se queda solo en la web — pero la decisión se escribe. La app ya se desincronizó una vez y el usuario lo detectó antes que nosotros. Ver `MOBILE_APP_GUIDE.md`.
9. **Leer con tracking todo lo que se vaya a escribir.** `GetFirstOrDefaultAsync` de `BaseRepository` usa `AsNoTracking()`: modificar esa entidad y llamar a `SaveChangesAsync` devuelve **200 OK sin emitir un solo `UPDATE`**. Ver `BACKEND_SERVICE_BLUEPRINT.md`.
10. **Ningún mensaje de error llega al usuario en inglés.** Todo texto que se muestre pasa por `ErrorText.Translate` (en `Contracts`, compartido por web y app). Un `_ => error` al final de un `switch` de traducción es un bug esperando salir.

---

## Checklist de verificación antes de dar una feature por terminada

```bash
dotnet build <proyecto tocado>.csproj      # 0 errores
npm run build:css                          # si se tocó CSS del Front
dotnet test <proyecto>.Tests.csproj        # si el proyecto tiene tests
# correr el plan Odiseo correspondiente en modo real (no --dry-run) y confirmar 0 fallos
```

### Si se tocó una vista o un endpoint

- [ ] **¿La vista va también a la app?** Decidir con la tabla de `MOBILE_APP_GUIDE.md`. Si va: `AppShell.xaml` + ViewModel + gating en `ApplyVisibility`. Si no va: dejarlo escrito en el PR.
- [ ] **¿Se escribe en la base?** Confirmar que la lectura previa usa tracking (`GetOwnedForUpdateAsync`, no `GetOwnedAsync`), y **verificar en la BD que la fila cambió** — no alcanza con un 200.
- [ ] **¿Hay mensajes de error nuevos?** Agregarlos a `ErrorText` en `Contracts`, o van a salir en inglés.
- [ ] **¿DTO nuevo?** Va en `AstraSystemsRental.Contracts`, nunca duplicado entre Front y app.
- [ ] **¿Nodo nuevo?** Agregar la constante a `AppConfig` de la app aunque la vista sea solo web: la app los usa para el gating del menú.

---

## Recompilar la app tras tocar el backend

Si el cambio es solo de servidor, **el APK no se recompila** — la app consume los
mismos endpoints. Solo hace falta regenerarlo al tocar `AstraSystemsRental.Mobile`
o `Contracts`:

```bash
dotnet publish AstraSystemsRental.Mobile/src/AstraSystemsRental.Mobile/AstraSystemsRental.Mobile.csproj -f net10.0-android -c Release
```
