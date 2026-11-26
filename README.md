# VitalTrek Platform API

API RESTful de VitalTrek, plataforma de seguridad y operaciones para turismo de aventura en zonas de baja conectividad. Construida con ASP.NET Core (.NET 10), EF Core con MySQL, autenticación JWT y documentación OpenAPI con Scalar.

## Bounded contexts

- **Shared**: infraestructura común (persistencia EF Core, unidad de trabajo, convención de rutas kebab-case, manejo de errores).
- **IAM**: agencias, usuarios, roles y autenticación con JWT (`sign-up`, `sign-in`).
- **ExpeditionSetup**: rutas, checkpoints, ventanas de tiempo, notas de ruta, protocolos de seguridad, grupos de expedición, guías de campo, manifiesto de turistas, wearables y preparación de participantes.

## Requisitos

- .NET 10 SDK
- MySQL 8

## Ejecución local

1. Configura tu contraseña local de MySQL con User Secrets (desde la raíz del repositorio). Así no queda en los `appsettings`:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;port=3306;user=root;password=<tu-password>;database=vitaltrek-db" --project VitalTrek.API
   ```

2. Ejecuta la API:

   ```bash
   dotnet run --project VitalTrek.API --launch-profile http
   ```

3. API en http://localhost:5213 y documentación en http://localhost:5213/scalar

Al iniciar, la API aplica las migraciones pendientes y carga datos de demostración si la base de datos está vacía.

## Pruebas

```bash
dotnet test
```

Las pruebas de persistencia de Field Tracking requieren MySQL 8 local y permiso para crear bases temporales.
Actívalas en PowerShell con una conexión al servidor de pruebas:

```powershell
$env:VITALTREK_TEST_MYSQL = 'server=127.0.0.1;port=3306;user=<usuario>;password=<password>'
dotnet test
Remove-Item Env:VITALTREK_TEST_MYSQL
```

Cada prueba crea y elimina únicamente su propia base `vitaltrek_m02_test_<guid>`; ignora la base indicada
en la conexión y rechaza servidores remotos. Sin esa variable, las pruebas MySQL se omiten.

## Inicio y finalización de expediciones

`POST /api/v1/tracking-sessions` requiere el guía asignado y la agencia autenticada. Valida ruta habilitada,
ventanas de todos los tramos, protocolo activo, manifiesto dentro de capacidad y preparación de cada participante
(wearable asignado sin devolución, contacto de emergencia y consentimiento activo). El grupo y la sesión
se guardan juntos. Devuelve `201` con `Location` al crear, `200` con la sesión original al repetir y `409`
si falta preparación o el grupo ya finalizó. La base impide dos sesiones activas para un mismo grupo.

La migración `EnforceSingleActiveTrackingSession` conserva el historial. Si ya existen sesiones activas
duplicadas, MySQL rechaza el cambio completo; deben revisarse antes de reintentar la migración.

El endpoint público `/tracking-sessions/{id}/end` fue retirado. Tour Closure debe usar
`IFieldTrackingContextFacade.StageSessionEndAsync`: esta operación valida acceso y prepara el fin de sesión
sin guardar. El cierre completo debe validar las devoluciones, finalizar el grupo y guardar todos los cambios
en una sola unidad de trabajo.

## Contrato de telemetría para M03 y Sync Batch

`POST /api/v1/tracking-sessions/{sessionId}/telemetry-records` conserva su respuesta `200` con un arreglo
de lecturas. Requiere el guía asignado, sesión activa, wearable asignado al grupo, consentimiento y activación.
La agencia y el participante se resuelven en servidor; los identificadores enviados en el cuerpo no conceden acceso.

```json
{
  "wearableDeviceId": 70,
  "records": [{
    "clientRecordId": "83b522e1-46a7-46e7-b144-c21c7bf15a33",
    "recordedAt": "2026-11-25T15:10:00-05:00",
    "latitude": -9.5,
    "longitude": -77.5,
    "altitude": 3000,
    "heartRate": 88,
    "battery": 85,
    "deviationMeters": 12.5
  }]
}
```

El cliente crea el UUID al capturar y lo conserva en cada reenvío. La identidad es única por sesión.
El mismo contenido devuelve la lectura original sin cambiar `synchronizedAt`; reutilizar la identidad con otro
contenido devuelve `409` y código `Telemetry.RecordIdentityConflict`. Un error en una lectura del POST directo
rechaza toda esa solicitud. Se admiten como máximo 500 lecturas por solicitud.

`recordedAt` conserva la captura, normalizada a UTC y microsegundos; `synchronizedAt` representa la recepción
en servidor y reemplaza al nombre anterior `syncedAt` en las respuestas. La captura admite un minuto de tolerancia
de reloj y debe corresponder a la sesión. `heartRate` es un entero positivo, `battery` un porcentaje entero
entre 0 y 100 y `deviationMeters` un número finito no negativo. La desviación es una entrada simulada,
identificada por `isDeviationSimulated` en la respuesta; no es una distancia calculada por GPS en servidor.

Para Sync Batch, usar `IFieldTrackingContextFacade.StageTelemetryReadingAsync(actor, sessionId, reading)`.
`TelemetryReadingInput` incluye también `wearableDeviceId` por lectura. El resultado contiene `clientRecordId`
y un estado tipado: `Accepted`, `Duplicate` o una causa de rechazo. **Accepted significa preparado, aún sin guardar.**
El puerto no ejecuta `CompleteAsync` ni comparte entidades de dominio. Repetir una identidad ya preparada dentro
del mismo DbContext también devuelve `Duplicate`.

Miler debe reunir los resultados por lectura y guardar el lote y las lecturas aceptadas una sola vez mediante
la unidad de trabajo compartida. Ante `RecordIdentityConflict`, un fallo de persistencia o una violación concurrente
del índice único, debe descartar la transacción y reconsultar las identidades antes de reintentar el lote.
El puerto no registra pasos, evalúa riesgo ni crea lotes: esas operaciones corresponden a sus contextos.

La migración conserva las fechas existentes y asigna UUIDs deterministas a las lecturas históricas.
Sus métricas y participante quedan en `null` cuando no se conocían: no se inventan valores ni se infiere una
asignación pasada a partir de la asignación actual del wearable. La integración del cliente se realiza en M04.

## Variables de entorno de producción

| Variable | Descripción |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución (`Production`). |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Habilita los encabezados reenviados por el proxy. |
| `DATABASE_URL` | Host del servidor MySQL. |
| `DATABASE_PORT` | Puerto del servidor MySQL. |
| `DATABASE_USER` | Usuario de MySQL. |
| `DATABASE_PASSWORD` | Contraseña de MySQL. |
| `DATABASE_SCHEMA` | Nombre de la base de datos. |
| `TOKEN_SECRET` | Clave de firma JWT (mínimo 64 caracteres). |

`appsettings.Production.json` contiene placeholders `%VARIABLE%` que se reemplazan con estas variables al iniciar.

## Despliegue

Render con Docker desde la rama `main` (redeploy automático al cambiar `main`) y MySQL en Aiven.

URL pública: https://vitaltrek-api.onrender.com/scalar

## Usuarios demo

Contraseña de todos: `VitalTrek123!`

| Usuario | Rol |
| --- | --- |
| `admin@vitaltrek.demo` | Operations Administrator |
| `guide@vitaltrek.demo` | Field Guide |
| `guide2@vitaltrek.demo` | Field Guide |
| `tourist@vitaltrek.demo` | Adventure Tourist |
| `other@vitaltrek.demo` | Operations Administrator (otra agencia: Andes Other Agency) |

## Flujo de trabajo

- GitFlow: `feature/*` se crean desde `develop` y vuelven a `develop` por PR; `release/*` sale de `develop` y se mergea en `main` y `develop`.
- Cada versión publicada en `main` se marca con un tag `vX.Y.Z`.
- Commits con Conventional Commits: `feat`, `fix`, `docs`, `test`, `chore`, con scope opcional (por ejemplo `feat(iam): ...`).
