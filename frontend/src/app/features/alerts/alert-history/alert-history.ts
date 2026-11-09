import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Alert } from '../../../core/services/alert';
import { Device } from '../../../core/services/device';
import { AlertChannel, AlertHistoryEntry } from '../../../core/models/alert.model';
import { Devices } from '../../../core/models/device.model';

@Component({
  selector: 'app-alert-history',
  imports: [CommonModule, DatePipe, FormsModule, RouterLink],
  templateUrl: './alert-history.html',
  styleUrl: './alert-history.css',
})
export class AlertHistory implements OnInit {
  private readonly alerts = inject(Alert);
  private readonly devicesApi = inject(Device);

  readonly history = signal<AlertHistoryEntry[]>([]);
  readonly devices = signal<Devices[]>([]);
  readonly deviceId = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
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

  ngOnInit(): void {
    this.load();

    this.devicesApi.getAll({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => this.devices.set(result.items),
      error: () => console.warn('Could not load devices for alert filtering.'),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.alerts.getHistory(this.page(), this.pageSize(), this.deviceId() || undefined).subscribe({
      next: (result) => {
        this.history.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load alert history.');
        this.loading.set(false);
      },
    });
  }

  applyFilter(): void {
    this.page.set(1);
    this.load();
  }

  goToPage(page: number): void {
    const target = Math.min(Math.max(1, page), this.pages());
    if (target === this.page()) return;
    this.page.set(target);
    this.load();
  }

  badgeForChannel(channel: string): string {
    return channel === AlertChannel.Telegram ? 'badge-info' : 'badge-brand';
  }
}
