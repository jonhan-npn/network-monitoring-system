# Frontend

Angular 20 standalone application for Netwatch. It is built inside Docker and served by Nginx as part of `docker compose up`.

- **Framework:** Angular standalone components, signals, `provideHttpClient` + functional interceptors.
- **Styling:** Tailwind CSS v4 (`@import 'tailwindcss'`) plus one global design system in `src/styles.css`.
- **Live data:** SignalR client (`@microsoft/signalr`) connected through Nginx at `/hubs/monitoring`.
- **Auth:** JWT bearer token stored in `localStorage`, attached by `auth-interceptor.ts`, enforced by `authGuard`.

## How it runs

```
Browser
  │  http://localhost:${FRONTEND_PORT} (default 8080)
  ▼
Nginx (frontend container)
  ├── /            → Angular SPA (try_files → index.html)
  ├── /api/*       → api:8080/api/*
  └── /hubs/*      → api:8080/hubs/*  (WebSocket upgrade)
```

- `infrastructure/docker/frontend.Dockerfile` builds the app (`npm run build -- --configuration production`) and copies `dist/frontend/browser` into Nginx.
- `infrastructure/nginx/conf.d/frontend.conf` owns SPA fallback, static asset caching, the API proxy, and the SignalR proxy.
- The container has no Node runtime; the compiled bundle is static. Rebuild after frontend changes:

```powershell
$env:PATH += ";$env:LOCALAPPDATA\Programs\DockerDesktop\resources\bin"
docker compose build frontend
docker compose up -d frontend
```

Build-time configuration lives in `src/environments/environment.ts` (`apiUrl: '/api'`), so the browser always calls the same origin it was served from.

## Directory layout

```
frontend/src/
├── index.html                     # title, meta, Inter/Outfit font links
├── main.ts
├── styles.css                     # global design system (tokens + components)
└── app/
    ├── app.ts / app.html / app.config.ts
    ├── app.routes.ts              # all routes; authenticated routes under AdminLayout
    │
    ├── core/
    │   ├── authentication/auth.ts
    │   ├── guards/auth-guard.ts
    │   ├── interceptors/
    │   │   ├── auth-interceptor.ts     # adds Authorization: Bearer <token>
    │   │   └── error-interceptor.ts
    │   ├── models/                     # contracts mirrored from backend DTOs
    │   │   ├── alert.model.ts
    │   │   ├── auth.model.ts
    │   │   ├── device.model.ts
    │   │   ├── event-log.model.ts
    │   │   └── ping-result.model.ts
    │   ├── services/
    │   │   ├── alert.ts
    │   │   ├── auth.service.ts
    │   │   ├── device.ts
    │   │   ├── monitoring.ts
    │   │   └── signalr.ts
    │   └── utils/ranges.ts             # time ranges + chart bucketing
    │
    ├── layouts/
    │   ├── admin-layout/               # fixed sidebar + topbar shell
    │   │   ├── sidebar/
    │   │   └── topbar/
    │   └── auth-layout/                # split-screen brand layout for login/register
    │
    ├── shared/components/
    │   ├── line-chart/                 # real SVG series, axes, grid, empty state
    │   ├── confirm-dialog/
    │   ├── loading-spinner/
    │   └── status-badge/
    │
    └── features/
        ├── dashboard/
        ├── devices/
        │   ├── device-list/
        │   ├── device-form/            # create + edit
        │   └── device-detail/
        ├── monitoring/
        │   ├── ping-history/
        │   └── resource-history/
        ├── alerts/
        │   ├── alert-rules/
        │   └── alert-history/
        ├── logs/event-log-viewer/
        ├── settings/
        └── authentication/
            ├── login/
            └── register/
```

There is deliberately no `features/users`: the API exposes no user-management endpoints, so the UI does not pretend to manage users.

## Routes

Authenticated routes are children of `AdminLayout` and protected by `canActivateChild: [authGuard]`.

| Path | Component | Purpose |
| --- | --- | --- |
| `/auth/login` | `Login` | Sign in (public) |
| `/auth/register` | `Register` | Create account (public) |
| `/` | `Dashboard` | Summary, live status, aggregated ping chart |
| `/dashboard` | — | Redirects to `/` |
| `/devices` | `DeviceList` | Search, type/state filters, paging, monitor toggle, delete |
| `/devices/new` | `DeviceForm` | Register a device |
| `/devices/:id` | `DeviceDetail` | Stats, connection details, live CPU/memory, 24 h latency, recent pings |
| `/devices/:id/edit` | `DeviceForm` | Update device |
| `/devices/:id/ping-history` | `PingHistory` | Latency rows and 1H/6H/24H/7D availability chart |
| `/devices/:id/resource-history` | `ResourceHistory` | CPU/memory samples and charts |
| `/alerts/rules` | `AlertRules` | Rule CRUD, enable/disable, test delivery |
| `/alerts/history` | `AlertHistory` | Delivered alerts, device filter, paging |
| `/logs` | `EventLogViewer` | Status/alert/config events, filters, paging |
| `/settings` | `Settings` | Account, API/hub health, read-only server defaults |
| `**` | — | Redirects to `/` |

## Design system

`src/styles.css` is the single source of truth. Pages combine these classes with Tailwind utilities; feature `.css` files only hold page-specific layout details.

**Tokens** (`:root`):

- Brand: `--brand #6d4aff`, `--brand-600`, `--brand-700`, `--brand-soft`, `--brand-ring`
- Ink/text: `--ink`, `--ink-2`, `--muted`, `--muted-2`
- Lines/surfaces: `--line`, `--line-strong`, `--bg #f6f8fc`, `--surface #fff`
- Status: `--ok`, `--warn`, `--danger`, `--info` plus matching `*-soft` fills
- Shape: `--radius-sm`, `--radius`, `--radius-lg`, `--shadow-sm`, `--shadow`, `--shadow-lg`

**Reusable classes:**

| Group | Classes |
| --- | --- |
| Page scaffolding | `.page`, `.page-head`, `.page-title`, `.page-subtitle`, `.eyebrow`, `.stack`, `.row` |
| Surfaces | `.panel`, `.panel-head`, `.panel-title`, `.panel-sub`, `.panel-body` |
| Buttons | `.btn`, `.btn-primary`, `.btn-secondary`, `.btn-ghost`, `.btn-danger`, `.btn-sm` |
| Forms | `.field`, `.label`, `.input`, `.select`, `.textarea`, `.form-error`, `.field-hint` |
| Tables | `.table-wrap`, `.table`, `.table-footer`, `.pager` |
| Badges | `.badge`, `.badge-ok`, `.badge-warn`, `.badge-danger`, `.badge-info`, `.badge-muted` |
| Stats | `.stat-grid`, `.stat`, `.stat-label`, `.stat-value`, `.stat-foot`, `.stat-icon` |
| Feedback | `.empty`, `.empty-icon`, `.skeleton`, `.alert-error` |
| Overlays | `.modal`, `.modal-backdrop`, `.switch`, `.segmented` |
| Charts | classes used by `app-line-chart` |

Keep new screens on these classes so spacing, colour, and states stay consistent across the app.

## Data flow

1. A page injects one of the typed services: `Device`, `Monitoring`, `Alert`, `Signalr`.
2. Services call relative URLs (`/api/...`), so Nginx proxies them to the API container.
3. `auth-interceptor.ts` adds the bearer token; `error-interceptor.ts` handles failure responses.
4. Models in `core/models` mirror backend DTOs; when a DTO changes, update the model and the mapping together.
5. Pages that need live values subscribe to `Signalr`:
   - `deviceStatus$` — online/offline transitions
   - `resourceSample$` — CPU/memory samples
   - `alert$` — triggered alerts
   - Dashboard joins the `dashboard` group; device detail subscribes to one device and unsubscribes on destroy.

`core/utils/ranges.ts` provides `HISTORY_RANGES`, `rangeWindow(hours)`, and `bucketAverages(...)` so the dashboard and both history pages share the same range semantics and chart bucketing.

## Page notes

- **Dashboard** reads `/api/monitoring/dashboard/summary`, `/status`, and aggregated ping history. No hardcoded demo numbers. Live updates arrive over SignalR.
- **Device list** uses server-side filters (`search`, `type`, `isMonitored`) and server paging. Monitoring toggle and delete use the device endpoints.
- **Device detail** shows real connection details (`SshCredentialRef` is a reference, never a password) and only renders resource meters when samples exist.
- **History pages** request an explicit `from`/`to` window and render an honest empty state when no samples exist.
- **Alert rules** use the backend rule DTOs; the test-alert dialog validates the destination before calling the API.
- **Settings** shows the current account, can probe `GET /health`, can test the SignalR connection, and lists read-only server defaults. It intentionally has no fake persisted toggles.
- **Auth pages** use `AuthLayout` (split-screen brand panel + form) and support password visibility toggles.

## Conventions

- Standalone components, `inject()` for dependencies, signals for component state, `computed()` for derived values.
- Templates use Angular control flow (`@if`, `@for`) and the shared `.css` classes above.
- Keep contracts aligned with backend DTO casing; the API uses camelCase JSON.
- Every async page needs three states: loading (skeleton), error (retry), and empty — never a silent blank screen.
- Validate frontend changes with the Docker build (no local Node/npm requirement):

```powershell
docker compose build frontend
```

A bundle-size budget warning is expected and harmless; a compile error is not.
