import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Device } from '../../../core/services/device';
import { DEVICE_TYPE_LABELS, DeviceType, Devices as DeviceModel } from '../../../core/models/device.model';

@Component({
  selector: 'app-device-list',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './device-list.html',
  styleUrl: './device-list.css',
})
export class DeviceList implements OnInit {
  private readonly deviceService = inject(Device);

  readonly devices = signal<DeviceModel[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly togglingId = signal<string | null>(null);
  readonly pendingDelete = signal<DeviceModel | null>(null);
  readonly deleting = signal(false);

  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly search = signal('');
  readonly type = signal<DeviceType | ''>('');
  readonly monitored = signal<'' | 'true' | 'false'>('');

  readonly types = Object.values(DeviceType);
  readonly typeLabels = DEVICE_TYPE_LABELS;

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
    () => !!this.search().trim() || !!this.type() || this.monitored() !== '',
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.deviceService
      .getAll({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search(),
        type: this.type(),
        isMonitored: this.monitored() === '' ? undefined : this.monitored() === 'true',
      })
      .subscribe({
        next: (result) => {
          this.devices.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);

          // If a filter change emptied the current page, step back one page.
          if (!result.items.length && this.page() > 1) {
            this.page.update((page) => page - 1);
            this.load();
          }
        },
        error: () => {
          this.error.set('Unable to load devices. Check that the API is running.');
          this.loading.set(false);
        },
      });
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  clearFilters(): void {
    this.search.set('');
    this.type.set('');
    this.monitored.set('');
    this.applyFilters();
  }

  goToPage(page: number): void {
    const target = Math.min(Math.max(1, page), this.pages());
    if (target === this.page()) return;
    this.page.set(target);
    this.load();
  }

  statusOf(device: DeviceModel): 'Online' | 'Offline' | 'Paused' | 'Unknown' {
    if (!device.isMonitored) return 'Paused';
    if (device.isOnline === true) return 'Online';
    if (device.isOnline === false) return 'Offline';
    return 'Unknown';
  }

  badgeClass(device: DeviceModel): string {
    switch (this.statusOf(device)) {
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

  toggleMonitoring(device: DeviceModel): void {
    this.actionError.set(null);
    this.togglingId.set(device.id);

    this.deviceService.setMonitoring(device.id, !device.isMonitored).subscribe({
      next: () => {
        this.devices.update((items) =>
          items.map((item) => (item.id === device.id ? { ...item, isMonitored: !item.isMonitored } : item)),
        );
        this.togglingId.set(null);
      },
      error: () => {
        this.actionError.set('Unable to change the monitoring state for this device.');
        this.togglingId.set(null);
      },
    });
  }

  confirmDelete(): void {
    const device = this.pendingDelete();
    if (!device) return;

    this.deleting.set(true);
    this.deviceService.delete(device.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.pendingDelete.set(null);
        if (this.devices().length === 1 && this.page() > 1) this.page.update((page) => page - 1);
        this.load();
      },
      error: () => {
        this.deleting.set(false);
        this.actionError.set('Unable to delete this device.');
        this.pendingDelete.set(null);
      },
    });
  }
}
