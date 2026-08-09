# Domain Validation Patterns

For AI assistants. Compressed. `Guard` extendido y el patrón de validador de dominio compartido entre múltiples flujos de entrada.

Reference: `AstraSystemsRental.Base/src/AstraSystemsRental.Base/Validation/Guard.cs`.

---

## 1. `Guard` — validador fluido acumulativo

Acumula todos los errores en vez de fallar al primero, para devolver la lista completa en `OperationResult.Fail(guard.Errors)`:

```csharp
var guard = new Guard()
    .NotEmpty(value, "Field")
    .MaxLength(value, 10, "Field")
    .Range(year, 1980, DateTime.UtcNow.Year + 1, "ModelYear")
    .NotInFuture(purchaseDate, "PurchaseDate")
    .Positive(quantity, "Quantity")
    .NotNegative(kilometers, "Kilometers");

if (guard.HasErrors)
    return OperationResult.Fail(guard.Errors);
```

Métodos disponibles: `NotEmpty`, `MaxLength`, `Email`, `Must(condition, error)`, `Range<T>(value, min, max, field)`, `Positive(int, field)`, `NotNegative(int, field)`, `NotInFuture(DateOnly/DateTime, field)`, `Before(DateOnly, reference, field)`, `After(DateOnly, reference, field)`.

**Antes de agregar un campo de fecha/número nuevo a cualquier request DTO, preguntarse si necesita `Guard`.** El gap real detectado (Fase 0): campos como `PurchaseDate`/`ModelYear` no tenían ninguna validación de rango en ninguna capa (ni HTML `min`/`max`, ni `Guard`, ni `CHECK` SQL) — solo `Kilometers >= 0` estaba protegido de punta a punta. No repetir ese patrón en módulos nuevos: si el dominio tiene un rango razonable conocido (año de modelo, fecha no futura, cantidad positiva), validarlo con `Guard` desde el primer commit.

## 2. Validador de dominio puro, invocado desde múltiples puntos de entrada

Cuando una regla de negocio se necesita desde 2+ flujos distintos (ej. la validación de monotonía de kilometraje se necesita tanto desde el alta manual de una lectura como desde una reserva de taller que registra kilometraje automáticamente), extraerla a una clase de dominio pura, sin I/O, invocada desde ambos:

```csharp
public static class MileageMonotonicityValidator
{
    public static bool IsValid(int value, IReadOnlyList<MileageReading> neighbors, DateOnly date, int dailyProjection)
    {
        // cálculo puro contra lecturas vecinas, sin acceso a BD/HTTP dentro del validador
    }
}
```

El servicio de cada flujo de entrada (`MileageReadingService.AddAsync`, `WorkshopReservationService.RecordFromWorkshopReservationAsync`) obtiene los datos necesarios (I/O) y luego llama al validador puro — la regla vive en un solo lugar, nunca reimplementada por flujo. Esto es el mismo criterio que ya se aplicó para evitar la duplicación de 14 líneas entre `CreateAsync`/`UpdateAsync` en Fase 0: una regla, un lugar, invocada N veces.

Señal de que hace falta este patrón: si al escribir la segunda implementación de una regla ya escrita en otro servicio, la respuesta es "cópiala y ajusta" — pausar y extraer en cambio.
