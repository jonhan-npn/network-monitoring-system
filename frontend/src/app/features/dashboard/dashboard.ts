import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { DashboardSummary, DeviceStatus, Monitoring } from '../../core/services/monitoring';
import { Device as DeviceApi } from '../../core/services/device';
import { Signalr } from '../../core/services/signalr';
import { Devices } from '../../core/models/device.model';
import { DeviceStatusUpdate } from '../../core/models/ping-result.model';
import { AuthService } from '../../core/services/auth.service';
import { ChartSeries, LineChart } from '../../shared/components/line-chart/line-chart';

type DeviceRow = {
  id: string;
  name: string;
  type: string;
  location: string;
  ip: string;
  isMonitored: boolean;
  isOnline?: boolean;
  latencyMs?: number;
  lastCheckedAt?: string;
};

const RANGE_HOURS: Record<string, number> = { '1H': 1, '6H': 6, '24H': 24, '7D': 24 * 7 };
const BUCKETS = 14;

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, DatePipe, RouterLink, LineChart],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private readonly monitoring = inject(Monitoring);
  private readonly deviceApi = inject(DeviceApi);
  private readonly signalr = inject(Signalr);
  private readonly auth = inject(AuthService);

  readonly summary = signal<DashboardSummary | null>(null);
  readonly rows = signal<DeviceRow[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly range = signal('24H');
  readonly ranges = Object.keys(RANGE_HOURS);
  readonly chartLabels = signal<string[]>([]);
  readonly chartSeries = signal<ChartSeries[]>([]);
  readonly chartLoading = signal(false);

  readonly today = new Date();

  readonly greeting = computed(() => {
    const hour = new Date().getHours();
    const part = hour < 12 ? 'morning' : hour < 18 ? 'afternoon' : 'evening';
    const username = this.auth.currentUser()?.username ?? 'there';
    const name = username.split(/[\s._-]+/).filter(Boolean)[0] ?? username;
    return `Good ${part}, ${name.charAt(0).toUpperCase() + name.slice(1)}`;
  });

  readonly totalDevices = computed(() => this.rows().length || this.summary()?.totalDevices || 0);
  readonly onlineDevices = computed(() => this.rows().filter((row) => row.isOnline === true).length);
  readonly offlineDevices = computed(() => this.rows().filter((row) => row.isOnline === false).length);
  readonly unknownDevices = computed(
    () => this.totalDevices() - this.onlineDevices() - this.offlineDevices(),
  );
  readonly monitoredDevices = computed(() => this.rows().filter((row) => row.isMonitored).length);
  readonly onlinePercent = computed(() => {
    const total = this.totalDevices();
    return total ? Math.round((this.onlineDevices() / total) * 100) : 0;
  });
  readonly activeAlerts = computed(() => this.summary()?.alertsTriggeredLast24h ?? 0);
  readonly activeRules = computed(() => this.summary()?.activeAlertRules ?? 0);

  readonly donut = computed(() => {
    const circumference = 2 * Math.PI * 40;
    const total = this.totalDevices() || 1;
    const dash = (value: number) => `${((circumference * value) / total).toFixed(2)} ${circumference.toFixed(2)}`;
    const online = this.onlineDevices();
    const offline = this.offlineDevices();
    return {
      onlineDash: dash(online),
      offlineDash: dash(offline),
      offlineOffset: `-${((circumference * online) / total).toFixed(2)}`,
      unknownDash: dash(this.unknownDevices()),
      unknownOffset: `-${((circumference * (online + offline)) / total).toFixed(2)}`,
    };
  });

  readonly chartStats = computed(() => {
    const values = this.chartSeries()[0]?.values.filter((value): value is number => value != null) ?? [];
    if (!values.length) return null;
    return {
      average: values.reduce((sum, value) => sum + value, 0) / values.length,
      peak: Math.max(...values),
    };
  });

  ngOnInit(): void {
    this.loadOverview();

    this.signalr
      .connect()
      .then(() => this.signalr.deviceStatus$.subscribe((update) => this.applyLiveStatus(update)))
      .catch((err) => console.warn('Live updates unavailable:', err));
  }

  setRange(range: string): void {
    if (this.range() === range) return;
    this.range.set(range);
    this.loadPerformance();
  }

  latency(row: DeviceRow): string {
    return row.latencyMs == null ? '—' : `${Math.round(row.latencyMs)} ms`;
  }

  statusOf(row: DeviceRow): 'Online' | 'Offline' | 'Paused' | 'Unknown' {
    if (!row.isMonitored) return 'Paused';
    if (row.isOnline === true) return 'Online';
    if (row.isOnline === false) return 'Offline';
    return 'Unknown';
  }

  badgeClass(row: DeviceRow): string {
    switch (this.statusOf(row)) {
      case 'Online':
        return 'badge-ok';
      case 'Offline':
        return 'badge-danger';
      case 'Paused':
        return 'badge-muted';
      default:
        return 'badge-warn';
    }
  }

  private loadOverview(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      summary: this.monitoring.getSummary(),
      devices: this.deviceApi.getAll({ page: 1, pageSize: 100 }),
      statuses: this.monitoring.getStatus(),
    }).subscribe({
      next: ({ summary, devices, statuses }) => {
        this.summary.set(summary);
        this.rows.set(this.toRows(devices.items, statuses));
        this.loading.set(false);
        this.loadPerformance();
      },
      error: () => {
        this.error.set('Unable to load the dashboard right now. Check that the API is running.');
        this.loading.set(false);
      },
    });
  }

  private loadPerformance(): void {
    const devices = this.rows();
    const hours = RANGE_HOURS[this.range()] ?? 24;
    const to = new Date();
    const from = new Date(to.getTime() - hours * 60 * 60 * 1000);

    if (!devices.length) {
      this.chartSeries.set([]);
      this.chartLabels.set([]);
      return;
    }

    this.chartLoading.set(true);

    forkJoin(
      devices.map((device) =>
        this.monitoring
          .getPingHistory(device.id, from.toISOString(), to.toISOString())
          .pipe(catchError(() => of([]))),
      ),
    ).subscribe({
      next: (histories) => {
        const sums = new Array<number>(BUCKETS).fill(0);
        const counts = new Array<number>(BUCKETS).fill(0);
        const bucketMs = (to.getTime() - from.getTime()) / BUCKETS;

        histories.forEach((points) => {
          points.forEach((point) => {
            if (point.latencyMs == null) return;
            const time = new Date(point.checkedAt).getTime();
            const index = Math.floor((time - from.getTime()) / bucketMs);
            if (index < 0 || index >= BUCKETS) return;
            sums[index] += point.latencyMs;
            counts[index] += 1;
          });
        });

        this.chartLabels.set(
          Array.from({ length: BUCKETS }, (_, index) =>
            this.bucketLabel(new Date(from.getTime() + index * bucketMs), hours > 24),
          ),
        );
        this.chartSeries.set([
          {
            name: 'Average latency',
            color: '#6d4aff',
            values: sums.map((sum, index) => (counts[index] ? sum / counts[index] : null)),
          },
        ]);
        this.chartLoading.set(false);
      },
      error: () => this.chartLoading.set(false),
    });
  }

  private toRows(devices: Devices[], statuses: DeviceStatus[]): DeviceRow[] {
    const byId = new Map(statuses.map((status) => [status.deviceId, status]));

    return devices.map((device) => {
      const status = byId.get(device.id);
      return {
        id: device.id,
        name: device.name,
        type: device.deviceType,
        location: device.location ?? '—',
        ip: device.ipAddress,
        isMonitored: device.isMonitored,
        isOnline: status?.isOnline,
        latencyMs: status?.latencyMs,
        lastCheckedAt: status?.checkedAt,
      };
    });
  }

  private applyLiveStatus(update: DeviceStatusUpdate): void {
    this.rows.update((rows) =>
      rows.map((row) =>
        row.id === update.deviceId
          ? {
              ...row,
              isOnline: update.isOnline,
              latencyMs: update.latencyMs,
              lastCheckedAt: update.timestampUtc,
            }
          : row,
      ),
    );
  }

  private bucketLabel(date: Date, includeDay: boolean): string {
    return includeDay
      ? date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
      : date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
  }
}
