# Network Monitoring System

An enterprise-style network monitor: ICMP ping sweeps, SNMP resource polling,
SSH inventory, alert rules (email / Telegram), a JWT-secured REST API, and a
live Angular dashboard fed by SignalR.

Everything runs in Docker: PostgreSQL, the ASP.NET Core API, the background
Worker, and the Angular frontend behind Nginx.

## Stack

| Piece | Technology | Container |
|---|---|---|
| Database | PostgreSQL 16 | `netmonitor-postgres` |
| API | ASP.NET Core 10, EF Core 10, MediatR, SignalR | `netmonitor-api` |
| Worker | .NET 10 hosted services (ping / SNMP / SSH / alerts) | `netmonitor-worker` |
| Frontend | Angular (standalone), Nginx | `netmonitor-frontend` |

Backend follows Clean Architecture + CQRS (`Domain → Application →
Infrastructure → API/Worker`). See [docs/architecture.md](docs/architecture.md)
and [docs/backend.md](docs/backend.md).

## Quick start

### Prerequisites

- Docker Desktop (with the Linux engine running)
- Nothing else — .NET and Node are only needed if you want to build outside
  Docker

### Run

```bash
cp .env.example .env          # once, then edit secrets
docker compose up -d --build
docker compose ps
```

On Windows PowerShell:

```powershell
Copy-Item .env.example .env
docker compose up -d --build
```

`docker compose up` is safe to re-run; only changed images are rebuilt.

### Open

| What | URL |
|---|---|
| Dashboard / login | http://localhost:8080 |
| API (direct) | http://localhost:5080 |
| API health | http://localhost:5080/health |
| Swagger (Development only) | http://localhost:5080/swagger |

Ports are configurable in `.env` (`API_PORT`, `FRONTEND_PORT`). Port `5000`
is avoided because Windows frequently reserves it.

### Sign in

A seeded admin account is created by
[`infrastructure/postgres/init.sql`](infrastructure/postgres/init.sql):

```
username: admin
password: ChangeMe123!
```

Change this before using the stack anywhere other than your machine. New
self-registered users get the read-only `Viewer` role.

## What you should see

1. Log in at `/auth/login` → redirected to the dashboard.
2. Stat cards read live data from `/api/monitoring/dashboard/summary`.
3. Add a device; the Worker picks it up on the next sweep and the table
   updates live over SignalR (no refresh).
4. Ping history accumulates in `ping_results`; an online/offline transition
   writes an `event_log_entries` row and fires the alert engine.

Example — add a reachable and an unreachable device (PowerShell):

```powershell
$login = Invoke-RestMethod -Method Post `
  -Uri http://localhost:8080/api/auth/login `
  -ContentType 'application/json' `
  -Body (@{ username='admin'; password='ChangeMe123!' } | ConvertTo-Json)

$h = @{ Authorization = "Bearer $($login.accessToken)" }

Invoke-RestMethod -Method Post -Uri http://localhost:8080/api/devices -Headers $h `
  -ContentType 'application/json' `
  -Body (@{ name='Google DNS'; ipAddress='8.8.8.8'; deviceType='Other'; location='Internet' } | ConvertTo-Json)

Invoke-RestMethod -Method Post -Uri http://localhost:8080/api/devices -Headers $h `
  -ContentType 'application/json' `
  -Body (@{ name='Unreachable'; ipAddress='192.0.2.123'; deviceType='Server'; location='Lab' } | ConvertTo-Json)
```

Wait one sweep (`PING_INTERVAL_SECONDS`, default 30 s), then:

```powershell
Invoke-RestMethod -Uri http://localhost:8080/api/monitoring/status -Headers $h
```

## Common commands

```bash
docker compose ps                       # status
docker compose logs -f api worker       # follow logs
docker compose up -d --build api worker # rebuild after backend changes
docker compose down                     # stop (keeps data volume)
docker compose down -v                  # stop and delete Postgres data
```

Database shell:

```bash
docker compose exec postgres psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"
```

## Configuration

All configuration is environment-driven; `.env` is read by
`docker-compose.yml`. See [docs/deployment.md](docs/deployment.md) for the
full list. The important ones:

| Variable | Purpose |
|---|---|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | database credentials |
| `API_PORT` / `FRONTEND_PORT` | published host ports |
| `JWT_SIGNING_KEY` | HS256 signing key, **must be ≥ 32 bytes** |
| `REALTIME_INTERNAL_SECRET` | shared secret for Worker → API live updates |
| `PING_INTERVAL_SECONDS` / `PING_TIMEOUT_MS` | ping sweep cadence |
| `OFFLINE_THRESHOLD` | consecutive failures before "offline" |
| `HISTORY_RETENTION_DAYS` | retention sweep window |
| `SMTP_*`, `TELEGRAM_BOT_TOKEN` | alert delivery |

`infrastructure/postgres/init.sql` is the day-zero schema. There are no EF
migrations yet, so `Database:MigrateOnStartup` is deliberately `false`. If you
change the schema, update `init.sql` and the matching EF configuration in
`backend/src/NetworkMonitor.Infrastructure/Data/Configurations/`.

## Repository layout

```
backend/src/
  NetworkMonitor.Domain/          entities, enums
  NetworkMonitor.Application/     commands, queries, DTOs, interfaces
  NetworkMonitor.Infrastructure/  EF Core, ping/SNMP/SSH, auth, notifiers
  NetworkMonitor.API/             REST controllers, SignalR hub
  NetworkMonitor.Worker/          ping / SNMP / SSH / retention background jobs
frontend/                         Angular app
infrastructure/
  docker/                         Dockerfiles
  nginx/                          reverse proxy + SPA fallback
  postgres/init.sql               schema + seed admin
docs/                             architecture, API, database, deployment, backend, frontend
```

## Troubleshooting

- **Port already in use / `bind: ... forbidden`** — change `API_PORT` or
  `FRONTEND_PORT` in `.env`. Windows reserves ranges such as 5000–5059.
- **`column d.Id does not exist`** — an EF entity property is missing
  `HasColumnName("snake_case")`; the schema is lowercase.
- **`libgssapi_krb5.so.2` on startup** — harmless Npgsql message in the slim
  runtime image; not an error.
- **Dashboard empty / `401`** — make sure you logged in; the guard redirects
  to `/auth/login` when there is no token.
- **Frontend not updating** — check `docker compose logs api` for
  `POST /internal/notifications/... 202`; if those are missing, verify
  `REALTIME_INTERNAL_SECRET` is identical for `api` and `worker`.
- **`docker` not found on Windows** — add
  `%LOCALAPPDATA%\Programs\DockerDesktop\resources\bin` to `PATH`, or use the
  Docker Desktop terminal.

## Documentation

- [Architecture](docs/architecture.md)
- [Backend guide](docs/backend.md)
- [API reference](docs/api.md)
- [Database](docs/database.md)
- [Deployment](docs/deployment.md)
- [Frontend](docs/frontend.md)
