# AI Context — AstraSystemsRental

Esta carpeta es la memoria persistente del proyecto para cualquier asistente de IA (Claude, GPT, Copilot, etc.) que retome trabajo aquí sin contexto previo. No es documentación de usuario ni de negocio — es el terreno técnico ya mapeado, las decisiones ya tomadas y los gotchas ya pisados, para no repetir investigación ni errores.

## Cómo usar esta carpeta

Antes de tocar código en este repo, leé en este orden:

1. **`01-arquitectura.md`** — mapa completo del sistema: soluciones, puertos, stack, patrones de código, cómo se ejecuta todo.
2. **`02-sistema-nodos-accesos.md`** — cómo funciona el control de acceso por nodo/plan/rol/demo. Es el corazón de seguridad del sistema, cualquier feature nueva debe integrarse acá.
3. **`03-feature-vehiculos-placas.md`** — blueprint completo de la feature de cotización de vehículos por placa: decisiones, arquitectura, fuentes de datos reales, gotchas de implementación.
4. **`04-gotchas-tecnicos.md`** — bugs reales ya encontrados y corregidos, con síntoma + causa + fix, para no volver a caer en ellos.

## Regla de oro

Si vas a proponer una decisión que contradice algo escrito acá, primero verificá contra el código real (estos documentos pueden quedar desactualizados) y luego preguntale al usuario — no asumas que la memoria está mal solo porque no coincide con tu intuición, pero tampoco la trates como verdad absoluta e inmutable.

## Mantenimiento

Cuando esta carpeta quede desactualizada (una decisión cambia, un bug nuevo aparece, una fuente de datos deja de funcionar), actualizala en el mismo commit que el cambio de código — no la dejes desincronizada del repo real.
