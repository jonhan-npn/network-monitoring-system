import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
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

export const routes: Routes = [
  {
    path: '',
    canActivateChild: [authGuard],
    children: [
      { path: '', component: Dashboard, title: 'Netwatch · Overview' },
      { path: 'dashboard', redirectTo: '', pathMatch: 'full' },
      { path: 'devices', component: DeviceList, title: 'Netwatch · Devices' },
      { path: 'devices/new', component: DeviceForm, title: 'Netwatch · Add device' },
      { path: 'devices/:id/edit', component: DeviceForm, title: 'Netwatch · Edit device' },
      {
        path: 'devices/:id/ping-history',
        component: PingHistory,
        title: 'Netwatch · Ping history',
      },
      {
        path: 'devices/:id/resource-history',
        component: ResourceHistory,
        title: 'Netwatch · Resource history',
      },
      { path: 'devices/:id', component: DeviceDetail, title: 'Netwatch · Device' },
      { path: 'alerts/rules', component: AlertRules, title: 'Netwatch · Alert rules' },
      { path: 'alerts/history', component: AlertHistory, title: 'Netwatch · Alert history' },
      { path: 'logs', component: EventLogViewer, title: 'Netwatch · Event log' },
      { path: 'settings', component: Settings, title: 'Netwatch · Settings' },
      { path: 'users', component: Users, title: 'Netwatch · Users' },
      { path: '**', redirectTo: '' },
    ],
  },
];
