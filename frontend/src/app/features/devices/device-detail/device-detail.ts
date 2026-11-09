import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { Device } from '../../../core/services/device';
import { Monitoring } from '../../../core/services/monitoring';
import { Signalr } from '../../../core/services/signalr';
import { DEVICE_TYPE_LABELS, Devices as DeviceModel } from '../../../core/models/device.model';
import { PingResult } from '../../../core/models/ping-result.model';
import { ChartSeries, LineChart } from '../../../shared/components/line-chart/line-chart';

@Component({
  selector: 'app-device-detail',
  imports: [CommonModule, DatePipe, RouterLink, LineChart],
  templateUrl: './device-detail.html',
  styleUrl: './device-detail.css',
})
export class DeviceDetail implements OnInit, OnDestroy {
  private readonly service = inject(Device);
  private readonly monitoring = inject(Monitoring);
  private readonly signalr = inject(Signalr);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  readonly device = signal<DeviceModel | null>(null);
  readonly pings = signal<PingResult[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly toggling = signal(false);
  readonly showDelete = signal(false);
  readonly deleting = signal(false);
  readonly liveCpu = signal<number | null>(null);
  readonly liveMemory = signal<number | null>(null);

  private readonly subscriptions = new Subscription();

  readonly typeLabel = computed(() => {
    const device = this.device();
    return device ? DEVICE_TYPE_LABELS[device.deviceType] ?? device.deviceType : '';
  });

  readonly status = computed(() => {
    const device = this.device();
    if (!device) return 'Unknown';
    if (!device.isMonitored) return 'Paused';
    if (device.isOnline === true) return 'Online';
    if (device.isOnline === false) return 'Offline';
    return 'Unknown';
  });

  readonly badgeClass = computed(() => {
    switch (this.status()) {
      case 'Online':
        return 'badge-ok';
      case 'Offline':
        return 'badge-danger';
      case 'Paused':
        return 'badge-muted';
      default:
        return 'badge-warn';
    }
  });

  readonly cpu = computed(() => this.liveCpu() ?? this.device()?.lastCpuPercent ?? null);
  readonly memory = computed(() => this.liveMemory() ?? this.device()?.lastMemoryPercent ?? null);

  readonly chart = computed(() => {
    const points = this.pings().slice(-180);
    const series: ChartSeries[] = [
      {
        name: 'Latency',
        color: '#6d4aff',
        values: points.map((point) => (point.isOnline ? point.latencyMs ?? null : null)),
      },
    ];
    return {
      series,
      labels: points.map((point) =>
        new Date(point.checkedAt).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' }),
      ),
    };
  });

  readonly pingStats = computed(() => {
    const points = this.pings();
    if (!points.length) return null;
    const online = points.filter((point) => point.isOnline);
    const latencies = online
      .map((point) => point.latencyMs)
      .filter((value): value is number => value != null);
    return {
      samples: points.length,
      availability: Math.round((online.length / points.length) * 1000) / 10,
      average: latencies.length ? latencies.reduce((sum, value) => sum + value, 0) / latencies.length : null,
      latest: points[points.length - 1],
    };
  });

  readonly recentPings = computed(() => this.pings().slice(-12).reverse());

  ngOnInit(): void {
    this.load();

    if (this.id) {
      this.signalr
        .subscribeToDevice(this.id)
        .then(() => {
          this.subscriptions.add(
            this.signalr.deviceStatus$.subscribe((update) => {
              if (update.deviceId !== this.id) return;
              this.device.update((device) =>
                device
                  ? {
                      ...device,
                      isOnline: update.isOnline,
                      lastCheckedAt: update.timestampUtc,
                    }
                  : device,
              );
              this.pings.update((points) => [
                ...points,
                {
                  isOnline: update.isOnline,
                  latencyMs: update.latencyMs,
                  checkedAt: update.timestampUtc,
                },
              ]);
            }),
          );

          this.subscriptions.add(
            this.signalr.resourceSample$.subscribe((update) => {
              if (update.deviceId !== this.id) return;
              if (update.cpuPercent != null) this.liveCpu.set(update.cpuPercent);
              if (update.ramPercent != null) this.liveMemory.set(update.ramPercent);
            }),
          );
        })
        .catch((err) => console.warn('Live device updates unavailable:', err));
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.id) void this.signalr.unsubscribeFromDevice(this.id);
  }

  load(): void {
    if (!this.id) {
      this.error.set('Device not found.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.service.getById(this.id).subscribe({
      next: (device) => {
        this.device.set(device);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Device not found, or it was deleted.');
        this.loading.set(false);
      },
    });

    const to = new Date();
    const from = new Date(to.getTime() - 24 * 60 * 60 * 1000);
    this.monitoring.getPingHistory(this.id, from.toISOString(), to.toISOString()).subscribe({
      next: (points) => this.pings.set(points),
      error: () => console.warn('Could not load ping history for this device.'),
    });
  }

  toggleMonitoring(): void {
    const device = this.device();
    if (!device) return;

    this.actionError.set(null);
    this.toggling.set(true);

    this.service.setMonitoring(device.id, !device.isMonitored).subscribe({
      next: () => {
        this.device.update((current) => (current ? { ...current, isMonitored: !current.isMonitored } : current));
        this.toggling.set(false);
      },
      error: () => {
        this.actionError.set('Unable to change the monitoring state.');
        this.toggling.set(false);
      },
    });
  }

  confirmDelete(): void {
    const device = this.device();
    if (!device) return;

    this.deleting.set(true);
    this.service.delete(device.id).subscribe({
      next: () => this.router.navigate(['/devices']),
      error: () => {
        this.deleting.set(false);
        this.showDelete.set(false);
        this.actionError.set('Unable to delete this device.');
      },
    });
  }

  percent(value: number | null | undefined): number {
    if (value == null) return 0;
    return Math.min(100, Math.max(0, value));
  }
}
