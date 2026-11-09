import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Monitoring } from '../../../core/services/monitoring';
import { Device } from '../../../core/services/device';
import { ResourceSample } from '../../../core/models/device.model';
import { ChartSeries, LineChart } from '../../../shared/components/line-chart/line-chart';
import { HISTORY_RANGES, bucketAverages, rangeWindow } from '../../../core/utils/ranges';

@Component({
  selector: 'app-resource-history',
  imports: [CommonModule, DatePipe, RouterLink, LineChart],
  templateUrl: './resource-history.html',
  styleUrl: './resource-history.css',
})
export class ResourceHistory implements OnInit {
  private readonly monitoring = inject(Monitoring);
  private readonly devices = inject(Device);
  private readonly route = inject(ActivatedRoute);

  readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  readonly deviceName = signal('Device');
  readonly points = signal<ResourceSample[]>([]);
  readonly range = signal('24H');
  readonly ranges = HISTORY_RANGES;
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly chart = computed<{ series: ChartSeries[]; labels: string[] }>(() => {
    const labelOf = (point: ResourceSample) =>
      new Date(point.sampledAt).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
    const cpu = bucketAverages(this.points(), 80, (point) => point.cpuPercent, labelOf);
    const memory = bucketAverages(this.points(), 80, (point) => point.memoryPercent, labelOf);

    return {
      labels: cpu.labels.length ? cpu.labels : memory.labels,
      series: [
        { name: 'CPU', color: '#6d4aff', values: cpu.values },
        { name: 'Memory', color: '#22b573', values: memory.values },
      ],
    };
  });

  readonly stats = computed(() => {
    const points = this.points();
    if (!points.length) return null;

    const summarize = (values: Array<number | undefined>) => {
      const numbers = values.filter((value): value is number => value != null);
      if (!numbers.length) return null;
      return {
        average: numbers.reduce((sum, value) => sum + value, 0) / numbers.length,
        min: Math.min(...numbers),
        max: Math.max(...numbers),
      };
    };

    return {
      samples: points.length,
      cpu: summarize(points.map((point) => point.cpuPercent)),
      memory: summarize(points.map((point) => point.memoryPercent)),
      latest: points[points.length - 1],
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

    this.monitoring.getResourceHistory(this.id, from.toISOString(), to.toISOString()).subscribe({
      next: (points) => {
        this.points.set(points);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load resource history.');
        this.loading.set(false);
      },
    });
  }

  percent(value: number | null | undefined): string {
    return value == null ? '—' : `${Math.round(value * 10) / 10}%`;
  }
}
