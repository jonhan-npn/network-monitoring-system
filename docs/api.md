# API Reference

Base URL (behind Nginx): `/api`
All endpoints except `/api/auth/login` and `/api/health` require a JWT
bearer token: `Authorization: Bearer <token>`.

> This document describes the intended contract as the project is built out
> step by step (see the build order in the main README). Endpoints not yet
> implemented for your current milestone are marked **(planned)**.

## Conventions

- Request/response bodies are JSON.
- Timestamps are ISO 8601 UTC (`2026-07-29T09:15:00Z`).
- List endpoints support `?page=1&pageSize=25` and return:
  ```json
  { "items": [...], "page": 1, "pageSize": 25, "totalCount": 132 }
  ```
- Errors follow a consistent shape (via `ExceptionHandlingMiddleware`):
  ```json
  { "statusCode": 400, "message": "IP address is not valid.", "traceId": "..." }
  ```

---

## Auth — `AuthController`

### `POST /api/auth/login`
Authenticates a user and returns a JWT.

Request:
```json
{ "username": "admin", "password": "ChangeMe123!" }
```

Response `200`:
```json
{
  "accessToken": "eyJhbGciOi...",
  "expiresAt": "2026-07-29T09:45:00Z",
  "user": { "id": "...", "username": "admin", "role": "Admin" }
}
```

`401` on bad credentials.

### `POST /api/auth/refresh` *(planned)*
Exchanges a refresh token for a new access token, once refresh tokens are
added — not required for the v1 learning build (a short-lived access token
with re-login is fine to start).

### `GET /api/auth/me`
Returns the currently authenticated user's profile, derived from the JWT
claims. Useful for the Angular `auth.guard.ts` to hydrate app state after a
refresh.

---

## Devices — `DevicesController`

| Method | Route | Description | Role |
|---|---|---|---|
| GET | `/api/devices` | Paginated device list, filterable by `type`, `isMonitored`, `search` | Viewer+ |
| GET | `/api/devices/{id}` | Single device detail | Viewer+ |
| POST | `/api/devices` | Create a device | Operator+ |
| PUT | `/api/devices/{id}` | Update a device | Operator+ |
| DELETE | `/api/devices/{id}` | Remove a device (cascades ping/resource history) | Admin |
| PATCH | `/api/devices/{id}/monitoring` | Toggle `isMonitored` without a full update | Operator+ |

Create/update body:
```json
{
  "name": "core-switch-01",
  "ipAddress": "10.0.0.1",
  "deviceType": "Switch",
  "location": "Rack 3, Server Room A",
  "snmpCommunity": "public",
  "snmpPort": 161,
  "snmpVersion": "2c",
  "sshUsername": "monitor",
  "sshCredentialRef": "vault://ssh/core-switch-01"
}
```

Note `sshCredentialRef` is a pointer into wherever secrets actually live
(env var, Docker secret, or a vault) — raw passwords are never stored in
the `devices` table or returned by this API.

---

## Monitoring — `MonitoringController`

| Method | Route | Description |
|---|---|---|
| GET | `/api/monitoring/status` | Current online/offline status for every monitored device (single snapshot) |
| GET | `/api/monitoring/devices/{id}/ping-history?from=&to=` | Ping history for charts |
| GET | `/api/monitoring/devices/{id}/resource-history?from=&to=` | CPU/RAM history (from SNMP polling) |
| GET | `/api/monitoring/dashboard/summary` | Aggregate counts for the dashboard cards (online/offline/total, avg CPU, etc.) |

This is the data the Angular dashboard's `ngx-charts`/Chart.js components
consume for historical views. Live status changes arrive separately over
the SignalR hub described below — the REST endpoints here are for initial
load and historical range queries, not for the live tick.

---

## Alerts — `AlertsController`

| Method | Route | Description |
|---|---|---|
| GET | `/api/alerts/rules` | List alert rules |
| POST | `/api/alerts/rules` | Create a rule (metric, threshold, channel, destination) |
| PUT | `/api/alerts/rules/{id}` | Update a rule |
| DELETE | `/api/alerts/rules/{id}` | Remove a rule |
| GET | `/api/alerts/history` | Sent-alert history (joins `event_log_entries` where `event_type = 'AlertSent'`) |

Rule body:
```json
{
  "deviceId": null,
  "name": "High CPU — all devices",
  "metric": "Cpu",
  "thresholdPercent": 90,
  "consecutiveBreaches": 3,
  "channel": "Telegram",
  "destination": "-1001234567890"
}
```
`deviceId: null` means the rule applies to every monitored device.

---

## Event logs — `LogsController` *(planned, or folded into MonitoringController)*

| Method | Route | Description |
|---|---|---|
| GET | `/api/logs?deviceId=&eventType=&from=&to=` | Paginated, filterable event log viewer backing `features/logs/event-log-viewer` |

---

## Health — `HealthController`

### `GET /api/health`
Used by the API's own Docker `HEALTHCHECK` and by Nginx/uptime checks.
Returns `200 OK` with a small JSON body once the app can reach Postgres:
```json
{ "status": "Healthy", "database": "Healthy" }
```

---

## SignalR Hub — `MonitoringHub` (`/hubs/monitoring`)

Angular connects via `@microsoft/signalr` (through `signalr.service.ts`),
authenticating the same JWT used for REST calls (passed as an
`access_token` query param on the hub URL, per SignalR's convention for
browser clients that can't set custom headers on the WebSocket handshake).

### Server → client events

| Event | Payload | Fired when |
|---|---|---|
| `deviceStatusChanged` | `{ deviceId, isOnline, checkedAt }` | Ping sweep detects online↔offline transition |
| `resourceSampleReceived` | `{ deviceId, cpuPercent, memoryPercent, sampledAt }` | New SNMP poll result |
| `alertTriggered` | `{ ruleId, deviceId, metric, message, triggeredAt }` | Alert threshold engine fires |

The Worker publishes these by resolving `IHubContext<MonitoringHub>`
through the shared DI container (both API and Worker reference the same
`NetworkMonitor.API` hub type, or a hub-notification interface lives in
`Application`/`Infrastructure` so the Worker doesn't need a hard reference
to the API project — pick whichever keeps the dependency graph clean for
you; documented here as a decision point, not a fixed answer).

## Cross-references

- [architecture.md](./architecture.md) — how these endpoints map to the
  layered backend and the Worker's push events
- [database.md](./database.md) — underlying tables each endpoint reads/writes