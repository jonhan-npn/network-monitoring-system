# Deployment

## Prerequisites

- Docker Engine + Docker Compose v2, on Ubuntu (or any Linux host)
- A `.env` file at the repo root (see **Environment variables** below)
- Ports free on the host: `80`/`443` (Nginx), and whichever port you expose
  Postgres on for local debugging (not required in production — see below)

## Local development

```bash
git clone <repo-url>
cd enterprise-network-monitor
cp .env.example .env      # fill in real values before starting
docker compose up --build
```

- `docker-compose.yml` — base service definitions (postgres, api, worker,
  frontend, nginx)
- `docker-compose.override.yml` — local-dev overrides, loaded
  automatically by `docker compose up` (bind-mounts source for hot reload,
  exposes Postgres on `localhost:5432`, sets `ASPNETCORE_ENVIRONMENT=Development`)

In production you'd run:
```bash
docker compose -f docker-compose.yml up -d --build
```
explicitly excluding the override file (or maintain a separate
`docker-compose.prod.yml` if you want the same explicitness in the other
direction).

## Services

| Service | Image built from | Exposes | Depends on |
|---|---|---|---|
| `postgres` | `postgres:16-alpine` (official) | internal only | — |
| `api` | `infrastructure/docker/api.Dockerfile` | internal `8080` | `postgres` (healthy) |
| `worker` | `infrastructure/docker/worker.Dockerfile` | none (no HTTP listener) | `postgres` (healthy) |
| `frontend` | `infrastructure/docker/frontend.Dockerfile` | `80` (Nginx) | `api` |

Only `frontend`'s port 80 needs to be published to the host — `api` and
`worker` are reached over the internal Docker network (`netmon_api:8080`
per the Nginx upstream config), which is also why the Angular build's
`environment.prod.ts` should point at a **relative** `/api` path rather
than a hardcoded host:port.

## Environment variables

Set these in `.env` at the repo root; `docker-compose.yml` references them
via `${VAR_NAME}`.

```bash
# Postgres
POSTGRES_DB=netmonitor
POSTGRES_USER=netmonitor
POSTGRES_PASSWORD=change-me

# API / Worker (connection string built from the same values)
ConnectionStrings__Default=Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}

# JWT
Jwt__Issuer=netmonitor
Jwt__Audience=netmonitor-clients
Jwt__SigningKey=replace-with-a-real-32-byte-secret
Jwt__ExpiryMinutes=30

# Alerting
Alerts__Smtp__Host=smtp.example.com
Alerts__Smtp__Port=587
Alerts__Smtp__Username=alerts@example.com
Alerts__Smtp__Password=change-me
Alerts__Telegram__BotToken=change-me
```

Never commit a real `.env` — keep `.env.example` with placeholder values
in version control and add `.env` to `.gitignore` (already listed in the
repo's `.gitignore`).

## Why `PeriodicTimer`, not `Thread.Sleep`, in the Worker

Worth calling out explicitly since it affects how cleanly the container
shuts down: `PingBackgroundService` (and the SNMP/SSH equivalents) should
use `PeriodicTimer` inside an `ExecuteAsync` loop that observes the hosted
service's `CancellationToken`, e.g.:

```csharp
var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
while (await timer.WaitForNextTickAsync(stoppingToken))
{
    await RunPingSweepAsync(stoppingToken);
}
```

`Thread.Sleep` blocks a thread pool thread and doesn't observe cancellation,
which means `docker stop` has to wait out the full grace period (or SIGKILL
the container) instead of shutting down promptly. This matters more than it
sounds like it should once you're doing rolling deploys.

## Health checks

Each of `api`, `worker`, and `frontend` has a `HEALTHCHECK` baked into its
Dockerfile (see [infra Dockerfiles](../infrastructure/docker/)). Wire these
into `docker-compose.yml` dependency conditions so `api` and `worker` don't
start hammering Postgres before it's actually accepting connections:

```yaml
depends_on:
  postgres:
    condition: service_healthy
```

## HTTPS / reverse proxy (production)

The `nginx` service in this project serves the Angular build and proxies
`/api` + `/hubs` — it does **not** terminate TLS in the config shipped here
(`infrastructure/nginx/conf.d/frontend.conf` listens on plain `80`). For a
real deployment, either:

1. Put this whole stack behind an existing reverse proxy / load balancer
   that terminates TLS (common in enterprise environments — the network
   team likely already has one), or
2. Add a `listen 443 ssl;` server block plus certificates (self-managed, or
   via a `certbot` sidecar container for Let's Encrypt) directly in
   `frontend.conf`.

Either way, once TLS terminates somewhere in front of this stack, make sure
`ASPNETCORE_FORWARDEDHEADERS` / `UseForwardedHeaders()` is configured in
`Program.cs` so the API sees the real client scheme/IP rather than
`http`/the proxy's IP.

## Backups

`scripts/backup.sh` / `scripts/restore.sh` should wrap:
```bash
docker compose exec -T postgres pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB" > backup.sql
```
and the inverse `psql` restore. Given the retention discussion in
[database.md](./database.md), consider excluding `ping_results` /
`resource_samples` from routine backups once they're large — back up
`devices`, `users`, `alert_rules`, and recent `event_log_entries` instead,
and treat raw ping/resource history as regenerable telemetry rather than
data you can't afford to lose.

## Cross-references

- [architecture.md](./architecture.md) — what each container actually runs
- [database.md](./database.md) — schema and retention behind `postgres`
- [api.md](./api.md) — what `frontend`/Nginx is proxying to