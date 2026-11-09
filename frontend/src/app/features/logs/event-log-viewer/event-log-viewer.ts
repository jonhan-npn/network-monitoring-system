import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Monitoring } from '../../../core/services/monitoring';
import { Device } from '../../../core/services/device';
import { EVENT_TYPE_LABELS, EventLogEntry, EventType } from '../../../core/models/event-log.model';
import { Devices } from '../../../core/models/device.model';

@Component({
  selector: 'app-event-log-viewer',
  imports: [CommonModule, DatePipe, FormsModule, RouterLink],
  templateUrl: './event-log-viewer.html',
  styleUrl: './event-log-viewer.css',
})
export class EventLogViewer implements OnInit {
  private readonly monitoring = inject(Monitoring);
  private readonly devicesApi = inject(Device);

  readonly events = signal<EventLogEntry[]>([]);
  readonly devices = signal<Devices[]>([]);
  readonly eventTypes = Object.values(EventType);
  readonly typeLabels = EVENT_TYPE_LABELS;

  readonly deviceId = signal('');
  readonly eventType = signal('');
  readonly from = signal('');
  readonly to = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly pages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
  readonly pageNumbers = computed(() => {
    const total = this.pages();
    const current = this.page();
    const start = Math.max(1, Math.min(current - 2, total - 4));
    return Array.from({ length: Math.min(5, total) }, (_, index) => start + index);
  });
  readonly rangeStart = computed(() =>
    this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );
  readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.totalCount()));
  readonly hasFilters = computed(
    () => !!this.deviceId() || !!this.eventType() || !!this.from() || !!this.to(),
  );

  ngOnInit(): void {
    this.load();

    this.devicesApi.getAll({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => this.devices.set(result.items),
      error: () => console.warn('Could not load devices for event filtering.'),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.monitoring
      .getEvents({
        deviceId: this.deviceId() || undefined,
        eventType: this.eventType() || undefined,
        from: this.from() ? new Date(`${this.from()}T00:00:00`).toISOString() : undefined,
        to: this.to() ? new Date(`${this.to()}T23:59:59`).toISOString() : undefined,
        page: this.page(),
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.events.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Unable to load the event log.');
          this.loading.set(false);
        },
      });
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  clearFilters(): void {
    this.deviceId.set('');
    this.eventType.set('');
    this.from.set('');
    this.to.set('');
    this.applyFilters();
  }

  goToPage(page: number): void {
    const target = Math.min(Math.max(1, page), this.pages());
    if (target === this.page()) return;
    this.page.set(target);
    this.load();
  }

  badgeFor(type: string): string {
    switch (type) {
      case 'DeviceOnline':
        return 'badge-ok';
      case 'DeviceOffline':
        return 'badge-danger';
      case 'AlertTriggered':
        return 'badge-warn';
      case 'DeviceCreated':
      case 'DeviceUpdated':
        return 'badge-info';
      default:
        return 'badge-muted';
    }
  }

  labelFor(type: string): string {
    return this.typeLabels[type] ?? type;
  }

  eventIcon(type: string): string {
    switch (type) {
      case 'DeviceOnline':
        return '↑';
      case 'DeviceOffline':
        return '↓';
      case 'AlertTriggered':
        return '!';
      case 'UserLoggedIn':
        return '·';
      default:
        return '•';
    }
  }
}
