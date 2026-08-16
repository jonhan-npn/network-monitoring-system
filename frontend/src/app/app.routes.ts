import { Routes } from '@angular/router';
import { Dashboard } from './features/dashboard/dashboard';
import { DeviceList } from './features/devices/device-list/device-list';
import { DeviceDetail } from './features/devices/device-detail/device-detail';
import { DeviceForm } from './features/devices/device-form/device-form';
import { EventLogViewer } from './features/logs/event-log-viewer/event-log-viewer';
import { PingHistory } from './features/monitoring/ping-history/ping-history';
import { ResourceHistory } from './features/monitoring/resource-history/resource-history';
import { AlertHistory } from './features/alerts/alert-history/alert-history';
import { AlertRules } from './features/alerts/alert-rules/alert-rules';
import { Settings } from './features/settings/settings';
import { Users } from './features/users/users';
import { Login } from './features/authentication/login/login';
import { Register } from './features/authentication/register/register';
import { authGuard } from './core/guards/auth-guard';

export const routes: Routes = [
  { path: 'auth/login', component: Login, title: 'Netwatch · Sign in' },
  { path: 'auth/register', component: Register, title: 'Netwatch · Register' },
  { path: '', component: Dashboard, canActivate: [authGuard], title: 'Netwatch · Overview' },
  { path: 'dashboard', redirectTo: '', pathMatch: 'full' },
  { path: 'devices', component: DeviceList, canActivate: [authGuard], title: 'Netwatch · Devices' },
  { path: 'devices/new', component: DeviceForm, canActivate: [authGuard], title: 'Netwatch · Add device' },
  { path: 'devices/:id/edit', component: DeviceForm, canActivate: [authGuard], title: 'Netwatch · Edit device' },
  { path: 'devices/:id/ping-history', component: PingHistory, canActivate: [authGuard], title: 'Netwatch · Ping history' },
  { path: 'devices/:id/resource-history', component: ResourceHistory, canActivate: [authGuard], title: 'Netwatch · Resource history' },
  { path: 'devices/:id', component: DeviceDetail, canActivate: [authGuard], title: 'Netwatch · Device' },
  { path: 'alerts/rules', component: AlertRules, canActivate: [authGuard], title: 'Netwatch · Alert rules' },
  { path: 'alerts/history', component: AlertHistory, canActivate: [authGuard], title: 'Netwatch · Alert history' },
  { path: 'logs', component: EventLogViewer, canActivate: [authGuard], title: 'Netwatch · Event log' },
  { path: 'settings', component: Settings, canActivate: [authGuard], title: 'Netwatch · Settings' },
  { path: 'users', component: Users, canActivate: [authGuard], title: 'Netwatch · Users' },
  { path: '**', redirectTo: '' },
];
