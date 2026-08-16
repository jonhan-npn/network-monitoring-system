import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Device } from '../../../core/services/device';
import { Devices as DeviceModel } from '../../../core/models/device.model';

@Component({ selector: 'app-device-list', imports: [CommonModule, FormsModule, RouterLink], templateUrl: './device-list.html', styleUrl: './device-list.css' })
export class DeviceList implements OnInit {
  private readonly deviceService = inject(Device);
  readonly query = signal('');
  readonly devices = signal<DeviceModel[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  ngOnInit(): void { this.load(); }
  load(): void {
    this.loading.set(true); this.error.set(null);
    this.deviceService.getAll(1, 100, this.query()).subscribe({
      next: result => { this.devices.set(result.items); this.totalCount.set(result.totalCount); this.loading.set(false); },
      error: () => { this.error.set('Unable to load devices.'); this.loading.set(false); },
    });
  }
  toggleMonitoring(device: DeviceModel): void {
    this.deviceService.setMonitoring(device.id, !device.isMonitored).subscribe({ next: () => this.load(), error: () => this.error.set('Unable to update monitoring state.') });
  }
  status(device: DeviceModel): string { return !device.isMonitored ? 'Paused' : device.isOnline === true ? 'Online' : device.isOnline === false ? 'Offline' : 'Unknown'; }
}
