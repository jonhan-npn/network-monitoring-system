import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Monitoring } from '../../../core/services/monitoring';
import { Device } from '../../../core/services/device';
import { PingResult } from '../../../core/models/ping-result.model';
import { ChartSeries, LineChart } from '../../../shared/components/line-chart/line-chart';
import { HISTORY_RANGES, bucketAverages, rangeWindow } from '../../../core/utils/ranges';

@Component({
  selector: 'app-ping-history',
  imports: [CommonModule, DatePipe, RouterLink, LineChart],
  templateUrl: './ping-history.html',
  styleUrl: './ping-history.css',
})
export class PingHistory implements OnInit {
  private readonly monitoring = inject(Monitoring);
  private readonly devices = inject(Device);
  private readonly route = inject(ActivatedRoute);

  readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  readonly deviceName = signal('Device');
  readonly points = signal<PingResult[]>([]);
  readonly range = signal('24H');
  readonly ranges = HISTORY_RANGES;
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly chart = computed<{ series: ChartSeries[]; labels: string[] }>(() => {
    const bucketed = bucketAverages(
      this.points(),
      80,
      (point) => (point.isOnline ? point.latencyMs : null),
      (point) =>
        new Date(point.checkedAt).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' }),
    );
    return {
      labels: bucketed.labels,
      series: [{ name: 'Latency', color: '#6d4aff', values: bucketed.values }],
    };
  });

  readonly stats = computed(() => {
    const points = this.points();
    if (!points.length) return null;

    const online = points.filter((point) => point.isOnline);
    const latencies = online
      .map((point) => point.latencyMs)
      .filter((value): value is number => value != null);
    const average = latencies.length ? latencies.reduce((sum, value) => sum + value, 0) / latencies.length : null;

    return {
      samples: points.length,
      availability: Math.round((online.length / points.length) * 1000) / 10,
      loss: Math.round(((points.length - online.length) / points.length) * 1000) / 10,
      average,
      min: latencies.length ? Math.min(...latencies) : null,
      max: latencies.length ? Math.max(...latencies) : null,
    };
  });

  readonly recent = computed(() => this.points().slice(-100).reverse());

  ngOnInit(): void {
    if (this.id) {
      this.devices.getById(this.id).subscribe({
        next: (device) => this.deviceName.set(device.name),
        error: () => undefined,
      });
    }
    this.load();
  }

  setRange(label: string): void {
    if (this.range() === label) return;
    this.range.set(label);
    this.load();
  }

  load(): void {
    if (!this.id) {
      this.error.set('Device not found.');
      this.loading.set(false);
      return;
    }

    const option = this.ranges.find((item) => item.label === this.range()) ?? this.ranges[2];
    const { from, to } = rangeWindow(option.hours);

    this.loading.set(true);
    this.error.set(null);

    this.monitoring.getPingHistory(this.id, from.toISOString(), to.toISOString()).subscribe({
      next: (points) => {
        this.points.set(points);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load ping history.');
        this.loading.set(false);
      },
    });
  }

  latency(value: number | null | undefined): string {
    return value == null ? '—' : `${Math.round(value * 10) / 10} ms`;
  }
}
