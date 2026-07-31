frontend/
│
├── angular.json
├── package.json
├── tsconfig.json
├── tsconfig.app.json
├── tsconfig.spec.json
│
└── src/
    │
    ├── main.ts
    ├── index.html
    ├── styles.scss
    │
    ├── app/
    │   │
    │   ├── app.component.ts
    │   ├── app.routes.ts
    │   └── app.config.ts
    │
    │
    ├── core/
    │   │
    │   ├── guards/
    │   │   └── auth.guard.ts
    │   │
    │   ├── interceptors/
    │   │   ├── auth.interceptor.ts
    │   │   └── error.interceptor.ts
    │   │
    │   ├── services/
    │   │   ├── device.service.ts
    │   │   ├── monitoring.service.ts
    │   │   ├── alert.service.ts
    │   │   └── signalr.service.ts
    │   │
    │   ├── models/
    │   │   ├── device.model.ts
    │   │   ├── ping-result.model.ts
    │   │   └── event-log.model.ts
    │   │
    │   └── authentication/
    │       └── auth.service.ts
    │
    │
    ├── shared/
    │   │
    │   ├── components/
    │   │   ├── status-badge/
    │   │   │   ├── status-badge.component.ts
    │   │   │   ├── status-badge.component.html
    │   │   │   └── status-badge.component.scss
    │   │   │
    │   │   ├── loading-spinner/
    │   │   │   ├── loading-spinner.component.ts
    │   │   │   ├── loading-spinner.component.html
    │   │   │   └── loading-spinner.component.scss
    │   │   │
    │   │   └── confirm-dialog/
    │   │       ├── confirm-dialog.component.ts
    │   │       ├── confirm-dialog.component.html
    │   │       └── confirm-dialog.component.scss
    │   │
    │   ├── directives/
    │   │
    │   ├── pipes/
    │   │
    │   └── utils/
    │
    │
    ├── layouts/
    │   │
    │   ├── admin-layout/
    │   │   ├── admin-layout.component.ts
    │   │   ├── admin-layout.component.html
    │   │   ├── admin-layout.component.scss
    │   │   │
    │   │   ├── sidebar/
    │   │   │   ├── sidebar.component.ts
    │   │   │   ├── sidebar.component.html
    │   │   │   └── sidebar.component.scss
    │   │   │
    │   │   └── topbar/
    │   │       ├── topbar.component.ts
    │   │       ├── topbar.component.html
    │   │       └── topbar.component.scss
    │   │
    │   └── auth-layout/
    │       ├── auth-layout.component.ts
    │       ├── auth-layout.component.html
    │       └── auth-layout.component.scss
    │
    │
    ├── features/
    │   │
    │   ├── dashboard/
    │   │   ├── dashboard.component.ts
    │   │   ├── dashboard.component.html
    │   │   ├── dashboard.component.scss
    │   │   │
    │   │   └── charts/
    │   │       ├── device-status-chart/
    │   │       ├── response-time-chart/
    │   │       └── resource-usage-chart/
    │   │
    │   │
    │   ├── devices/
    │   │   │
    │   │   ├── device-list/
    │   │   │   ├── device-list.component.ts
    │   │   │   ├── device-list.component.html
    │   │   │   └── device-list.component.scss
    │   │   │
    │   │   ├── device-form/
    │   │   │   ├── device-form.component.ts
    │   │   │   ├── device-form.component.html
    │   │   │   └── device-form.component.scss
    │   │   │
    │   │   └── device-detail/
    │   │       ├── device-detail.component.ts
    │   │       ├── device-detail.component.html
    │   │       └── device-detail.component.scss
    │   │
    │   │
    │   ├── monitoring/
    │   │   │
    │   │   ├── ping-history/
    │   │   │   ├── ping-history.component.ts
    │   │   │   ├── ping-history.component.html
    │   │   │   └── ping-history.component.scss
    │   │   │
    │   │   └── resource-history/
    │   │       ├── resource-history.component.ts
    │   │       ├── resource-history.component.html
    │   │       └── resource-history.component.scss
    │   │
    │   │
    │   ├── alerts/
    │   │   │
    │   │   ├── alert-rules/
    │   │   │   ├── alert-rules.component.ts
    │   │   │   ├── alert-rules.component.html
    │   │   │   └── alert-rules.component.scss
    │   │   │
    │   │   └── alert-history/
    │   │       ├── alert-history.component.ts
    │   │       ├── alert-history.component.html
    │   │       └── alert-history.component.scss
    │   │
    │   │
    │   ├── users/
    │   │   ├── users.component.ts
    │   │   ├── users.component.html
    │   │   └── users.component.scss
    │   │
    │   │
    │   ├── logs/
    │   │   │
    │   │   └── event-log-viewer/
    │   │       ├── event-log-viewer.component.ts
    │   │       ├── event-log-viewer.component.html
    │   │       └── event-log-viewer.component.scss
    │   │
    │   │
    │   ├── settings/
    │   │   ├── settings.component.ts
    │   │   ├── settings.component.html
    │   │   └── settings.component.scss
    │   │
    │   │
    │   └── authentication/
    │       │
    │       ├── login/
    │       │   ├── login.component.ts
    │       │   ├── login.component.html
    │       │   └── login.component.scss
    │       │
    │       └── register/
    │           ├── register.component.ts
    │           ├── register.component.html
    │           └── register.component.scss
    │
    │
    ├── assets/
    │   ├── images/
    │   ├── icons/
    │   └── fonts/
    │
    │
    └── environments/
        ├── environment.ts
        └── environment.development.ts