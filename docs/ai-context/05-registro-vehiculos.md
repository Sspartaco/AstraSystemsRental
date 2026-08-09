# Guía de migración: registro operativo de vehículos

## Objetivo

Migrar el registro de vehículos de Herramienta de Gestión hacia AstraSystemsRental como un módulo independiente, seguro y apto para un producto público por suscripción. El módulo debe registrar y operar la ficha del vehículo sin depender de tablas, servicios ni archivos del sistema legado.

La cotización actual por placa se mantiene como una capacidad independiente. Puede precargar datos de identificación del vehículo, pero no debe contener ni exponer información privada de la flota.

## Alcance inicial

La primera entrega debe cubrir:

- Alta, consulta y edición controlada de la ficha operativa del vehículo.
- Identificación legal y técnica: placa, licencia de tránsito, motor, VIN, serie, chasis y sus regrabados.
- Clasificación operacional: marca, línea, modelo, clase, carrocería, servicio, combustible, transmisión, color y estados.
- Datos de compra básicos: factura, fecha, valor y declaración de importación.
- Estado operativo, historial de cambios y lecturas de kilometraje.
- Metadatos y vencimientos de documentos, sin almacenar archivos dentro de SQL Server.
- Aislamiento de datos por suscriptor y control de acceso por nodo.

Quedan para fases posteriores: contratos, actas de entrega/recibo, inventario, alistamiento, mantenimiento, comparendos, proveedores, recaudo, traslado y control de recorrido completo.

## Decisiones arquitectónicas

### Separar catálogo de registro operativo

`vehicles.VehicleCatalog` ya existe y es la fuente compartida de la cotización: placa, marca, línea, modelo, motor, imagen y datos de valoración. Debe permanecer sin datos sensibles ni propios de un suscriptor.

El nuevo agregado recomendado es `FleetVehicle`. Contiene la ficha operativa privada y referencia el catálogo por placa. Esta separación evita que un usuario que cotiza una placa pueda ver VIN, compra, factura, documentos o historial de otra empresa.

La ficha privada no debe modificar automáticamente un registro compartido del catálogo. Si los datos de la ficha difieren del catálogo, se registra la discrepancia para revisión o se mantiene una corrección privada; nunca se permite que un usuario cambie datos globales de otros suscriptores.

### Propiedad y multitenencia

Antes de implementar, debe definirse el propietario funcional de una flota:

1. Usuario individual: cada registro pertenece al `UserId` del token.
2. Compañía: los miembros de `companies.CompanyMembers` comparten la flota de una compañía seleccionada.
3. Ambos: una flota personal y una o más flotas corporativas con un contexto de trabajo explícito.

Para un producto público, la opción recomendada es la tercera. El token o un header validado por el Gateway debe identificar el contexto activo, y toda consulta debe filtrar por ese propietario. No se debe aceptar un `OwnerId`, `CompanyId` ni `UserId` enviado en el cuerpo de una petición.

El modelo debe usar una clave de propietario estable, por ejemplo `OwnerType` y `OwnerId`, o un `FleetWorkspaceId` dedicado. La elección debe hacerse antes de crear la primera tabla para evitar una migración de seguridad posterior.

### Suscripción y nodos

La cotización conserva el nodo `fleet`, disponible para Demo y Basic. El registro operativo debe tener un nodo distinto, por ejemplo `vehicle-registry`, con ruta `VehicleRegistry/Index`.

La propuesta inicial es habilitarlo solo en Basic. Si se decide usar el registro como gancho de la demo, se debe definir un límite explícito de vehículos; el modelo actual de planes no tiene cuotas, por lo que no debe prometerse ese límite sin implementarlo.

Cada acción del Front debe validar `HasNode("vehicle-registry")` y cada llamada al Gateway debe enviar `X-Astra-Node: vehicle-registry`. La autorización de la vista no sustituye la autorización del Gateway ni el filtro de propietario en la API.

### Servicio y persistencia

El dominio pertenece a `AstraSystemsRental.Vehicles.Api`; no se debe crear una dependencia hacia el proyecto legado. Debe seguir el patrón actual de Minimal API, `OperationResult`, EF Core, `BaseRepository`, JWT RS256 y scripts SQL idempotentes.

Los scripts del módulo deben ejecutarse desde `run.ps1 reset-db` después de los esquemas de `users`, `subscriptions`, `access`, `companies` y del esquema actual `vehicles`.

## Modelo de datos propuesto

| Entidad | Datos principales | Regla clave |
| --- | --- | --- |
| `VehicleCatalog` existente | Placa, marca, línea, modelo, motor, imagen y datos de cotización | Compartido; no contiene datos privados. |
| `FleetVehicles` | Propietario, placa, licencia de tránsito, identificación técnica, configuración, estado, compra y notas | Índice único por propietario y placa. |
| `FleetVehicleStatusHistory` | Estado anterior, estado nuevo, motivo, usuario y fecha | Inmutable; toda transición genera una fila. |
| `FleetVehicleOdometerReadings` | Kilometraje, fecha, fuente, usuario y observación | No se sobrescribe; el último valor se deriva de la lectura más reciente. |
| `FleetVehicleDocuments` | Tipo, número, expedición, vencimiento, estado y referencia privada de archivo | Solo metadatos; los archivos se almacenan fuera de la base. |
| `FleetVehicleParties` | Propietario, proveedor, administrador o tercero relacionado | Permite personas naturales y jurídicas sin duplicar columnas. |
| `FleetVehicleAssignments` | Contrato, ubicación, responsable y fechas | Se introduce cuando se migre la asignación contractual. |

Los estados iniciales recomendados son `Draft`, `Active`, `Maintenance`, `Blocked`, `Inactive` y `Sold`. `Draft` permite guardar información incompleta antes de publicar el vehículo para operación. Las transiciones permitidas deben definirse en una máquina de estados del servicio, no únicamente con un `CHECK` de SQL.

Los identificadores técnicos deben tener límites, normalización y unicidad donde corresponda. La placa se normaliza a mayúsculas, sin espacios ni guiones, manteniendo la validación laxa actual de tres a diez caracteres alfanuméricos para incluir maquinaria con placa numérica.

## Mapeo desde Herramienta de Gestión

| Origen legado | Destino propuesto | Tratamiento |
| --- | --- | --- |
| `dbo.veh_vehiculos` | `VehicleCatalog` y `FleetVehicles` | Separar identificación compartida de información privada y operativa. |
| `dbo.vea_vehiculoadicionales` | `FleetVehicles` | Licencia de tránsito, VIN, regrabados, transmisión y atributos adicionales. |
| `dbo.vdo_vehiculodocumentos` | `FleetVehicleDocuments` | Migrar metadatos, vencimientos y referencias; no rutas FTP ni archivos sin reprocesarlos. |
| `dbo.cre_controlrecorrido` | `FleetVehicleOdometerReadings` | Conservar fecha, lectura y fuente de la captura. |
| `dbo.vnv_vehiculonovedad` | `FleetVehicleStatusHistory` | Migrar solo novedades que representen cambio de estado. |
| `dbo.vct_vehiculocontrato` | `FleetVehicleAssignments` | Postergar hasta definir el dominio contractual de AstraSystemsRental. |
| Actas, inventario y alistamiento | Módulos posteriores | No bloquear el registro inicial por estas dependencias. |

La importación debe ser un comando CLI idempotente, análogo a `import-catalog`. Debe leer un archivo de extracción versionado, validar y normalizar por lotes, conservar el identificador legado en una tabla de trazabilidad y emitir un reporte de filas migradas, omitidas y rechazadas. No se debe conectar AstraSystemsRental en producción directamente a la base de datos del legado.

## Contrato de API propuesto

El prefijo continúa siendo `/apiVehicles`. Las rutas iniciales recomendadas son:

| Método | Ruta | Uso |
| --- | --- | --- |
| `GET` | `/fleet-vehicles` | Lista paginada, filtrada por propietario y estado. |
| `POST` | `/fleet-vehicles` | Crea una ficha en `Draft` o `Active`, según completitud. |
| `GET` | `/fleet-vehicles/{id}` | Devuelve una ficha solo si pertenece al contexto activo. |
| `PUT` | `/fleet-vehicles/{id}` | Edita datos permitidos con control de concurrencia. |
| `POST` | `/fleet-vehicles/{id}/status-transitions` | Cambia estado y deja historial. |
| `POST` | `/fleet-vehicles/{id}/odometer-readings` | Registra una lectura inmutable. |
| `GET` | `/fleet-vehicles/{id}/documents` | Lista metadatos y alertas de vencimiento. |

Las respuestas mantienen el envelope estándar. Las rutas de detalle nunca deben devolver números de motor, VIN, chasis, facturas o documentos en una lista general. La edición debe usar un token de concurrencia para impedir que dos usuarios sobrescriban cambios entre sí.

## Experiencia de usuario propuesta

El alta debe ser un asistente de tres pasos, con indicador de avance y validación en el campo:

1. Identificación: placa, búsqueda en catálogo, marca, línea, modelo, clase y licencia de tránsito.
2. Configuración técnica: color, servicio, carrocería, combustible, transmisión, motor, VIN, serie, chasis y regrabados.
3. Operación: estado inicial, propietario o responsable, compra y documentos pendientes.

Al ingresar la placa, la pantalla debe consultar primero el catálogo y precargar los datos disponibles. El usuario solo completa lo que no esté confirmado. La ficha debe permitir guardar como borrador, revisar un resumen final y registrar posteriormente documentos o kilometraje sin volver a editar campos técnicos.

El listado debe priorizar placa, marca/línea/modelo, estado, último kilometraje, alertas documentales y fecha de actualización. Los datos técnicos sensibles solo se revelan dentro de la ficha y a usuarios autorizados del propietario.

Para el cotizador de taller, la ficha debe mostrar acciones contextuales desde el detalle: iniciar cotización de taller, consultar historial de kilometraje y ver documentos vigentes. Estas acciones se integran después de definir su propio nodo, permisos y modelo de costos.

## Mejoras propuestas al módulo

| Mejora | Propuesta | Por qué |
| --- | --- | --- |
| Alta guiada | Asistente de tres pasos con guardado de borrador y revisión antes de activar el vehículo. | Reduce errores frente al formulario único y permite completar información incompleta sin bloquear la operación. |
| Precarga por placa | Buscar primero en el catálogo y sugerir marca, línea, modelo, clase e imagen. | Evita redigitación, mejora consistencia y conecta naturalmente con la cotización existente. |
| Datos por nivel de sensibilidad | Mostrar solo datos operativos en el listado; revelar VIN, motor, compra y documentos en la ficha autorizada. | Minimiza exposición de información sensible en un producto público y mejora la lectura del listado. |
| Estados explícitos | Usar borrador, activo, mantenimiento, bloqueado, inactivo y vendido con transiciones y motivo. | Impide que un vehículo se use cuando no está disponible y deja trazabilidad para operaciones, actas y taller. |
| Kilometraje inmutable | Registrar lecturas con fecha, fuente y usuario; calcular el último valor en vez de sobrescribirlo. | Permite detectar inconsistencias, soporta control de recorrido y habilita mantenimiento y cotización de taller confiables. |
| Alertas de documentos | Centralizar vencimientos con indicadores de 30, 15 y 7 días y tareas de renovación. | Reduce riesgos operativos y legales por SOAT, tecnomecánica, licencia de tránsito u otros documentos vencidos. |
| Adjuntos privados | Almacenar metadatos en la base y archivos en almacenamiento privado con enlaces firmados temporales. | Evita saturar SQL Server, protege archivos sensibles y facilita escalamiento. |
| Búsqueda y filtros operativos | Filtros por placa, estado, responsable, ubicación, vencimientos y último kilometraje, con paginación. | Hace útil el módulo cuando la flota crece y evita consultas costosas que carguen toda la información. |
| Auditoría de cambios | Registrar creación, edición, estado, documento, kilometraje e importación, sin valores sensibles en logs. | Facilita soporte, control interno y resolución de discrepancias sin crear nuevas brechas de privacidad. |
| Detección de duplicados | Validar placa normalizada por propietario y advertir diferencias con el catálogo compartido. | Previene vehículos duplicados y evita que una ficha privada altere información pública de cotización. |
| Catálogos administrables | Mantener catálogos de estados, servicio, carrocería, combustible, transmisión y tipos documentales con códigos estables. | Reemplaza dependencias del maestro legado, permite evolución sin despliegues y mejora calidad de datos. |
| Integración gradual con taller | Desde la ficha, iniciar una cotización de taller con datos prellenados, sin mezclar sus costos ni flujos en el registro. | Reutiliza la información confiable del vehículo y mantiene los dominios separados para evolucionar cada módulo de forma segura. |

Las mejoras de mayor prioridad son el aislamiento por propietario, alta guiada, estados auditables, kilometraje inmutable y documentos privados. Sin esas bases, la migración reproduciría los problemas de seguridad, trazabilidad y usabilidad del módulo legado.

## Seguridad

- Aplicar propiedad del contexto activo en cada consulta y actualización.
- Validar el claim del usuario en el servicio, no solo en el controller o el Gateway.
- Usar identificadores internos no adivinables en enlaces públicos; los IDs numéricos solo son seguros junto con el filtro de propietario.
- Registrar eventos de creación, edición, transición de estado y acceso a documentos sin registrar VIN, motor, números de documento ni valores de compra en logs.
- Guardar archivos en almacenamiento privado con `StorageKey`, análisis antimalware, límite de tamaño y tipo permitido, y URLs firmadas de corta duración.
- Exigir autorización para descargar documentos y verificar nuevamente el propietario antes de emitir una URL.
- Aplicar límites de tasa a altas, cambios de estado y cargas de documentos.
- Mantener datos de compra e identificadores técnicos fuera del catálogo público, de cachés compartidos y de respuestas de búsqueda.

## Rendimiento y operación

- Índices por propietario más placa, propietario más estado, vehículo más fecha de kilometraje y vehículo más vencimiento documental.
- Listas paginadas y proyectadas a DTOs; nunca cargar documentos ni historiales completos con el listado.
- Búsqueda por placa normalizada y filtros por estado, vencimiento y fecha.
- Importaciones por lotes con tamaño configurable y transacciones acotadas.
- Procesos de alertas de documentos y mantenimiento mediante trabajo en background, no al cargar la ficha.
- Telemetría por altas, errores de validación, latencia y operaciones de importación, sin datos sensibles.

## Fases de ejecución

1. Definir el propietario funcional, la disponibilidad por plan, los roles dentro de una compañía y el catálogo de estados.
2. Diseñar y revisar scripts SQL idempotentes, índices, claves foráneas y estrategia de almacenamiento documental.
3. Implementar el agregado, repositorio, validaciones, endpoints y pruebas de aislamiento entre propietarios.
4. Construir el flujo Front con htmx, accesibilidad, guardado de borrador y resumen de validaciones.
5. Añadir documentos, historial de estado y kilometraje.
6. Construir el CLI de importación con trazabilidad, previsualización y reporte de errores.
7. Integrar contratos, actas, inventario, alistamiento y cotizador de taller como módulos separados.

## Decisiones pendientes

- Definir si la flota pertenece a un usuario, a una compañía o a ambos mediante espacios de trabajo.
- Definir qué plan incluye registro de vehículos y si Demo tendrá cuota.
- Definir los tipos documentales colombianos requeridos y sus reglas de vencimiento.
- Definir la política de conservación, anonimización y eliminación de datos y documentos.
- Definir el proveedor de almacenamiento privado para archivos.
- Definir qué información del legado se migra históricamente y cuál inicia desde cero.
- Definir el alcance del cotizador de taller, sus fuentes de precios y su modelo de autorización antes de acoplarlo al registro.
