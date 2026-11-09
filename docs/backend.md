# Backend guide

This document is the map for the ASP.NET Core side of the project: what each
project is for, how a request travels through it, and where to add code.

See [architecture.md](./architecture.md) for the high-level picture and
[api.md](./api.md) for the HTTP contract.

## Solution layout

```
backend/
  NetworkMonitor.slnx
  src/
    NetworkMonitor.Domain/          entities + enums, no dependencies
    NetworkMonitor.Shared/          dependency-free helpers used by any layer
    NetworkMonitor.Application/     use cases, DTOs, interfaces, validators
    NetworkMonitor.Infrastructure/  EF Core, Npgsql, ping/SNMP/SSH, JWT, notifiers
    NetworkMonitor.API/             controllers, SignalR hub, middleware, Program.cs
    NetworkMonitor.Worker/          background services (ping, SNMP, SSH, alerts)
  tests/
    UnitTests/                      fast, no I/O
    IntegrationTests/               repository / infrastructure tests
    API.Tests/                      controller tests
```

Dependencies point inward only:

```
API ─┐
     ├──> Infrastructure ──> Application ──> Domain
Worker ┘                         └──────> Shared
```

The API and Worker never reference each other. They share `Application`,
`Infrastructure`, and `Domain`, and coordinate through Postgres. The only
HTTP call between them is the Worker → API live-update bridge described
below.

## The projects

### Domain

Pure C#: `Device`, `User`, `PingResult`, `ResourceSample`, `EventLogEntry`,
`AlertRule`, plus enums (`DeviceType`, `EventType`, `AlertChannel`). No EF,
no ASP.NET, no package dependencies. Business invariants that don't need
I/O belong here.

### Application

The use-case layer. Organised by feature (`Features/Devices`,
`Features/Monitoring`, `Features/Alerts`, `Features/Authentication`, ...).
Each feature holds MediatR commands/queries and their handlers.

- `Interfaces/` — the ports Infrastructure implements: `IDeviceRepository`,
  `IMonitoringRepository`, `IUserRepository`, `IPingService`, `ISnmpService`,
  `IJwtTokenGenerator`, `IRealtimeNotifier`, ...
- `DTOs/` — the shapes returned by the API.
- `Validators/` — FluentValidation validators for commands.
- `Common/Behaviors/` — MediatR pipeline behaviours (validation).
- `Services/` — dependency-free domain logic such as
  `DeviceStatusEvaluator` (decides if a ping is a real transition).

### Infrastructure

Adapters for everything Application declared:

- `Data/AppDbContext.cs` + `Data/Configurations/*` — EF Core mappings to the
  lowercase `snake_case` schema defined in `infrastructure/postgres/init.sql`.
  **Every column is mapped explicitly**; if you add an entity property without
  a `HasColumnName`, EF will emit PascalCase and Postgres will return
  `42703: column ... does not exist`.
- `Repositories/` — repository implementations.
- `Ping/`, `SNMP/`, `SSH/` — protocol clients and per-sweep services.
- `Authentication/` — BCrypt hashing and JWT generation.
- `Notifications/` — SMTP and Telegram dispatchers.
- `Monitoring/HttpRealtimeNotifier.cs` — the Worker's HTTP client for the
  internal live-update endpoints.

### API

ASP.NET Core host. `Program.cs` stays thin; wiring lives in
`Extensions/ServiceCollectionExtensions.cs` and `Extensions/AuthExtensions.cs`.

- `Controllers/` — translate HTTP into MediatR commands/queries. No business
  logic here.
- `Hubs/MonitoringHub.cs` — typed SignalR hub (`Hub<IMonitoringClient>`).
  Clients call `SubscribeToDashboard` or `SubscribeToDevice(id)`.
- `Controllers/InternalNotificationsController.cs` — internal-only endpoints
  the Worker posts to; validates `X-Internal-Secret` and pushes to the hub.
- `Middleware/ExceptionHandlingMiddleware.cs` — turns application exceptions
  into ProblemDetails-shaped JSON.

### Worker

A `BackgroundService` host. Each sweeper runs on `PeriodicTimer`, creates its
own DI scope, and resolves its own `AppDbContext` — never share a scope across
parallel device checks.

- `Services/PingBackgroundService.cs` → `PingSweepService`
- `Services/SnmpBackgroundService.cs` → `SnmpPollingService`
- `Services/SshInventoryService.cs`
- `Alerts/AlertThresholdEngine.cs`
- `Jobs/DataRetentionJob.cs`

## How a request flows (example: `POST /api/devices`)

1. Nginx routes `/api/*` to the API container.
2. `DevicesController.Create` parses `DeviceType` and builds a
   `CreateDeviceCommand`.
3. The MediatR pipeline runs `ValidationBehavior` → FluentValidation validators.
4. `CreateDeviceCommandHandler` checks for duplicate IPs through
   `IDeviceRepository` and adds the entity.
5. `DeviceRepository` saves via `AppDbContext`.
6. The handler returns a `DeviceDto`; the controller responds `201 Created`
   with a `Location` header.

## How live updates flow

1. `PingSweepService` pings each monitored device and sends
   `RecordPingResultCommand` through MediatR.
2. The handler persists the result, writes an `EventLogEntry` on a status
   transition, then calls `IRealtimeNotifier.NotifyDeviceStatusChangedAsync`.
3. In the Worker that interface is `HttpRealtimeNotifier`, which POSTs the
   payload to `http://api:8080/internal/notifications/device-status` with the
   `X-Internal-Secret` header.
4. `InternalNotificationsController` checks the secret and calls
   `IHubContext<MonitoringHub, IMonitoringClient>` to broadcast
   `DeviceStatusChanged` to the `dashboard` group and the `device:{id}` group.
5. Angular's `Signalr` service receives the event and the dashboard updates
   the affected row without a refresh.

The shared secret is `Realtime:InternalSecret` (Worker and API) and comes from
`REALTIME_INTERNAL_SECRET` in `.env`. `/internal/*` is not exposed by Nginx.

## Authentication

- `POST /api/auth/login` → `LoginCommand` → BCrypt verify → JWT.
- All controllers/hubs are `[Authorize]` unless explicitly
  `[AllowAnonymous]` (only login/register/refresh).
- Policies: `OperatorOrAdmin`, `AdminOnly` (see `AuthExtensions`).
- The Angular `authInterceptor` attaches `Authorization: Bearer <token>`;
  `authGuard` protects app routes and redirects to `/auth/login`.
- Refresh tokens are not implemented (the endpoint returns `501`); clients
  log in again when the access token expires.

## Working on the backend

Build:

```powershell
dotnet build backend\NetworkMonitor.slnx
```

Run tests:

```powershell
dotnet test backend\NetworkMonitor.slnx
```

Add a feature, in order:

1. Command/query + handler in `Application/Features/<Area>`.
2. Validator in `Application/Validators` if it accepts user input.
3. Any new repository method on the `Application/Interfaces` port, then
   implement it in `Infrastructure/Repositories`.
4. A thin controller action in `API/Controllers` that calls `ISender.Send`.
5. If it touches the schema, update `infrastructure/postgres/init.sql` **and**
   the matching EF configuration. There are currently no EF migrations; the
   `init.sql` file is the source of truth for a fresh database.

## Common pitfalls in this codebase

- **snake_case columns**: always add `HasColumnName("...")` when mapping a
  property. Missing `HasColumnName("id")` on a key produces a runtime 42703.
- **DbContext threading**: never `Task.WhenAll` several repository calls that
  share one scoped `AppDbContext`; query sequentially or add a batched
  repository method (see `GetLatestResourceSamplesAsync`).
- **Optional filters**: a `null` filter means "no filter", not "invalid" —
  validate only when a value was supplied.
- **The migration flag**: `Database:MigrateOnStartup` is `false` by default
  because `init.sql` owns the schema and no migrations exist.
- **Npgsql GSSAPI warning**: `Cannot load library libgssapi_krb5.so.2` is a
  harmless startup message in the slim runtime images.
