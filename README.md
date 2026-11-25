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
