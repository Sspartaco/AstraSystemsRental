# Sistema de nodos, planes, roles y demos

Este es el corazón de seguridad y comercialización del producto: qué ve cada usuario en el sidebar, y qué puede llamar en el backend. Cualquier feature nueva (módulo/pantalla) tiene que integrarse acá — no es opcional ni cosmético.

## Conceptos

- **Nodo** (`[access].[Nodes]`): la unidad atómica de un módulo/feature del producto. PK es `[Key] VARCHAR(80)` (no un id numérico), más `Label`, `Icon` (SVG path crudo), `Route` (ej. `'Fleet/Index'` → controller `Fleet`, action `Index`), `SortOrder`, `IsActive`.
- **Rol** (`[access].[Roles]`): `Standard`, `Demo`, `SuperUser`. Un usuario tiene un rol.
- **Plan** (`[subscriptions].[Plans]`): `Demo` (3 días) y `Basic` (30 días), cada uno con `DurationDays`. Un usuario tiene una suscripción activa a un plan.
- **`RoleNodes`** / **`PlanNodes`**: tablas puente que dicen qué nodos desbloquea cada rol / cada plan. El acceso efectivo de un usuario a los nodos es la **unión** de los nodos de su rol y los nodos de su plan activo (`GetAllowedNodesAsync` en Users.Api).
- **"Demo" no es una entidad separada.** Es simplemente el rol `Demo` + el plan `Demo` (3 días) actuando juntos. No hay tabla `Demos` ni lógica de ciclo de vida especial — el vencimiento de una demo es exactamente el mismo mecanismo que el vencimiento de un plan Basic, solo que más corto.

## Seed actual (`Users.Api/SolutionItems/db/05_seed.sql`)

```
Roles: Standard, Demo, SuperUser
Nodes: dashboard (Home/Index), fleet (Fleet/Index, "Vehículos"), reports (Reports/Index)
Plans: Demo (3 días), Basic (30 días)
PlanNodes: Demo→dashboard, Demo→fleet, Basic→dashboard, Basic→fleet, Basic→reports
RoleNodes: SuperUser→'*' (wildcard, bypass total)
```

El nodo `fleet` fue originalmente un placeholder sin implementar (label "Flota", ícono de auto) — se reutilizó para la feature de cotización de vehículos (ver doc 03) en vez de crear un nodo nuevo, porque el ícono y la ruta ya calzaban exactamente.

## Ciclo de vida de una suscripción

1. **Alta de usuario** (`POST /apiUsers/users`): crea `Person` + `User` (inactivo, no confirmado) con rol/plan Demo por defecto. Se crea inmediatamente una fila en `[subscriptions].[Subscriptions]`: `StartsAtUtc = now`, `EndsAtUtc = now + plan.DurationDays`.
2. **Confirmación** (`POST /apiUsers/users/confirm`): activa la cuenta, no toca la suscripción.
3. **Login** (`AuthService.LoginAsync`): si `!IsSuperUser && SubscriptionEndsAtUtc <= UtcNow` → rechaza con "Subscription has expired.", no emite token. Si es válido, arma los claims JWT: `role`, `plan`, `sub_end` (unix timestamp), y un claim `node` **repetido una vez por cada nodo permitido** (unión rol+plan).
4. **Cada request al Gateway**: se vuelve a chequear `SubscriptionEnd` contra `nowUtc` — si venció entre el login y una request posterior, se corta ahí (independiente de si el JWT sigue siendo válido criptográficamente).
5. **Compañías**: una suscripción también puede pertenecer a una `Company` (`OwnerType='Company'`) en vez de a un `User` — la suscripción efectiva de un usuario es la unión de sus suscripciones propias y las de cualquier compañía de la que sea miembro, tomando la de `EndsAtUtc` más lejana.

## Gateway — dónde se aplica el control

`AstraSystemsRental.Gateway/Access/AccessControlMiddleware.cs` + `AccessEvaluator.cs`.

- Header que identifica qué nodo se está pidiendo acceso: **`X-Astra-Node`**.
- Bypass total sin evaluar nada: paths que empiezan con `/health`, `/scalar`, `/openapi`, `/apiUsers/auth`, `/apiUsers/users`.
- Reglas de `AccessEvaluator.Evaluate(context, now)`, en orden:
  1. No autenticado → rechazado (401).
  2. `RoleCode == "SuperUser"` → permitido siempre, bypass de suscripción y de nodo.
  3. Suscripción vencida → 403 `SubscriptionExpired`.
  4. No se pidió nodo específico (`RequestedNode` vacío, header ausente) → permitido (llamadas genéricas tipo `/apiUsers/users/me`).
  5. `AllowedNodes` contiene `"*"` o contiene exactamente el nodo pedido → permitido.
  6. Si no → 403 `NodeForbidden`.

**Gap histórico ya cerrado**: hasta antes de la feature de vehículos, `GatewayClient.cs` del Front nunca enviaba el header `X-Astra-Node` — el control de acceso a nodos era efectivamente solo cosmético (el sidebar ocultaba el link, pero un request directo a la API pasaba igual). Se extendió `IGatewayClient` con overloads que aceptan un `nodeKey` opcional y setean el header. **Toda feature nueva que proteja un nodo debe pasar su `nodeKey` en las llamadas al Gateway**, no solo confiar en el gating del lado Front.

## Front — dónde se aplica el filtro de UI

- `Shared/Security/AstraPrincipal.cs`: `HasNode(nodeKey) => IsSuperUser || Nodes.Contains("*") || Nodes.Contains(nodeKey)`.
- `Shared/ViewComponents/SidebarViewComponent.cs` y `Features/Home/HomeController.cs`: filtran la lista de nodos visibles con `HasNode`, para armar el sidebar y el dashboard.
- **Patrón de gating en un controller de feature** (ej. `AdminController`, `FleetController`): al inicio de cada action,
  ```csharp
  if (!currentUser.Principal.HasNode("mi-nodo")) return Redirect("/");
  ```

## Panel de administración de nodos/planes

`Features/Admin/` — solo visible/usable si `currentUser.Principal.IsSuperUser`.

- `GET /admin/catalog` → `Catalog.cshtml`, carga `GET /apiUsers/plans` + `GET /apiUsers/nodes` del Gateway.
- `_PlansGrid.cshtml`: por cada plan, un toggle por nodo (`role="switch"`, form individual con `hx-post="/admin/catalog/plan-node"`).
- **Sin cambios de código necesarios** cuando se agrega un nodo nuevo al seed: la grilla lee el catálogo dinámicamente, el nodo aparece solo.

## Checklist para integrar una feature nueva al sistema de nodos

1. Decidir: ¿reutilizar un nodo placeholder existente, o crear uno nuevo en el seed (`[access].[Nodes]` + `[subscriptions].[PlanNodes]`)?
2. El `Route` del nodo (`{Controller}/{Action}`) determina el nombre del controller Front — no es arbitrario.
3. Controller: gating `HasNode(nodeKey)` en cada action.
4. Llamadas al Gateway desde ese controller: pasar `nodeKey` a `IGatewayClient` para que el header `X-Astra-Node` viaje y el Gateway también proteja (no solo la UI).
5. Si el nodo debe estar disponible en el plan Demo (para que sea parte del "gancho comercial" del trial), agregarlo a `PlanNodes` para el plan `Demo`, no solo `Basic`.
6. No hace falta tocar `AdminController`/`_PlansGrid.cshtml` — aparece solo en la grilla de gestión.
