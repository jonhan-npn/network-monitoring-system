import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Monitoring, DashboardSummary } from '../../core/services/monitoring';

type NavItem = { label: string; icon: string; badge?: string };
type Device = { name: string; type: string; location: string; ip: string; status: 'Online' | 'Warning' | 'Offline'; latency: string; initials: string; color: string };

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private readonly monitoring = inject(Monitoring);
  constructor(private readonly router: Router) {}
  readonly sidebarOpen = signal(false);
  readonly activeNav = signal('Overview');
  readonly period = signal('24 hours');
  readonly selectedRange = signal('24H');
  readonly summary = signal<DashboardSummary | null>(null);

  readonly primaryNav: NavItem[] = [
    { label: 'Overview', icon: 'grid' },
    { label: 'Devices', icon: 'server', badge: '48' },
    { label: 'Topology', icon: 'nodes' },
    { label: 'Alerts', icon: 'bell', badge: '3' },
  ];
  readonly secondaryNav: NavItem[] = [
    { label: 'Reports', icon: 'chart' },
    { label: 'Event log', icon: 'activity' },
    { label: 'Settings', icon: 'settings' },
  ];
  readonly devices: Device[] = [
    { name: 'Core Router', type: 'Cisco ISR 4451', location: 'HQ · Floor 1', ip: '10.10.0.1', status: 'Online', latency: '2 ms', initials: 'CR', color: 'violet' },
    { name: 'Warehouse SW-01', type: 'Aruba 2930F', location: 'Warehouse A', ip: '10.10.24.11', status: 'Warning', latency: '48 ms', initials: 'SW', color: 'amber' },
    { name: 'VPN Gateway', type: 'FortiGate 100F', location: 'HQ · DMZ', ip: '10.10.1.254', status: 'Online', latency: '5 ms', initials: 'VG', color: 'blue' },
    { name: 'Branch AP-12', type: 'Ubiquiti U6 Pro', location: 'Branch · Phnom Penh', ip: '10.40.2.12', status: 'Offline', latency: '—', initials: 'AP', color: 'rose' },
  ];

  ngOnInit(): void {
    this.monitoring.getSummary().subscribe({ next: summary => this.summary.set(summary) });
    this.monitoring.getStatus().subscribe({ next: statuses => {
      this.devices.splice(0, this.devices.length, ...statuses.slice(0, 4).map(status => ({
        name: status.deviceName, type: 'Monitored device', location: '—', ip: status.deviceId,
        status: (status.isOnline ? 'Online' : 'Offline') as const, latency: status.latencyMs == null ? '—' : `${status.latencyMs} ms`,
        initials: status.deviceName.slice(0, 2).toUpperCase(), color: status.isOnline ? 'violet' : 'rose',
      })));
    }});
  }

  setRange(range: string) { this.selectedRange.set(range); }
  selectNav(label: string) {
    const paths: Record<string, string> = { Overview: '/', Devices: '/devices', 'Event log': '/logs', Settings: '/settings', Alerts: '/alerts/rules' };
    this.activeNav.set(label);
    this.sidebarOpen.set(false);
    if (paths[label]) this.router.navigateByUrl(paths[label]);
  }
}
